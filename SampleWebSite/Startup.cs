using System;
using System.IO;
using Ioc.Modules;
using Microsoft.Owin;
using Microsoft.Owin.BuilderProperties;
using Ninject;
using Ninject.Syntax;
using Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Middleware;
using SampleWebSite;

// Make this class the entry point for OWIN hosting
using Urchin.Client.Sources;

[assembly: OwinStartup(typeof(Startup))]

namespace SampleWebSite
{
    public class Startup
    {
        public static string ApplicationName = "Sample web site for OwinFramework Facilities";

        public void Configuration(IAppBuilder app)
        {
            // These next two lines configure IocModules package to use Ninject as the IOC container
            var packageLocator = new PackageLocator().ProbeBinFolderAssemblies();
            var ninject = new StandardKernel(new Ioc.Modules.Ninject.Module(packageLocator));

            // Use this Ninject container as the Prius factory
            Package.PriusFactory.Ninject = ninject;

            // Tell urchin to get its configuration from the config.json file in this project. Note that if
            // you edit this file whilst the site is running the changes will be applied without 
            // restarting the site.
            var configFile = new FileInfo(AppDomain.CurrentDomain.SetupInformation.ApplicationBase + "config.json");
            var configurationFileSource = ninject.Get<FileSource>().Initialize(configFile, TimeSpan.FromSeconds(5));
            
            // Use the Owin Framework to build the OWIN pipeline
            BuildPipeline(app, ninject);

            // Register an OWIN app disposing handler that frees resources
            var properties = new AppProperties(app.Properties);
            var token = properties.OnAppDisposing;
            token.Register(() =>
            {
                configurationFileSource.Dispose();
                ninject.Dispose();
            });
        }

        private void BuildPipeline(IAppBuilder app, IResolutionRoot ninject)
        {
            // Ask IOC to construct an instance of the Owin Framework pipeline builder
            var builder = ninject.Get<IBuilder>();

            // Ask IOC to construct an instance of the Owin Framework configuration mechanism
            var config = ninject.Get<IConfiguration>();

            // Define separate routes for the types of request that have different OWIN pipelines
            var apiPath = new PathString("/api");
            var assetsPath = new PathString("/assets");
            var assetsConfigPath = new PathString("/config/assets");

            builder.Register(ninject.Get<IRouter>())
                .AddRoute("api", c => c.Request.Path.StartsWithSegments(apiPath))
                .AddRoute("assets", c => 
                    c.Request.Path.StartsWithSegments(assetsPath) ||
                    c.Request.Path.StartsWithSegments(assetsConfigPath))
                .AddRoute("pages", c => true)
                .As("Security policy");

            #region Site wide middleware

            // This middleware will wrap the OWIN pipeline in a try/catch and return details of the
            // exception if the request was from the local machine. Also catches HttpException and
            // returns the appropriate HTTP status.
            builder.Register(ninject.Get<OwinFramework.ExceptionReporter.ExceptionReporterMiddleware>())
                .As("Exception reporter")
                .ConfigureWith(config, "/middleware/exceptions")
                .RunFirst();

            // This middleware will rewrite requests for the web site root to a request for
            // a specific page on the site.
            builder.Register(ninject.Get<OwinFramework.DefaultDocument.DefaultDocumentMiddleware>())
                .As("Default document")
                .ConfigureWith(config, "/middleware/defaultDocument");

            // This middleware will rewrite requests for the web site root to a request for
            // a specific page on the site.
            builder.Register(ninject.Get<OwinFramework.RouteVisualizer.RouteVisualizerMiddleware>())
                .As("Route visualizer")
                .ConfigureWith(config, "/middleware/visualizer")
                .RunFirst();

            #endregion

            #region Middleware common to secure and non-secure assets

            // This middleware will return 404 (not found) response always. It is configured
            // to run after all other middleware so that 404 responses will only be
            // returned if no other middleware handled the request first.
            builder.Register(ninject.Get<OwinFramework.NotFound.NotFoundMiddleware>())
                .As("Not found")
                .RunOnRoute("pages")
                .RunOnRoute("assets")
                .RunLast();

            // This middleware is part of this sample application. It buffers HTML pages and replaces
            // {{xxxx}} markers in the HML with data.
            builder.Register(ninject.Get<Middleware.OutputFilterMiddleware>())
                .As("Output filter")
                .RunOnRoute("assets")
                .RunOnRoute("pages");

            // This middleware is ISession middleware. It stores a cookie on the browser
            // and uses it to select a dictionary specific to that browser instance. The
            // application can use session to store information between requests.
            builder.Register(ninject.Get<Middleware.InProcessSession>())
                .As("Session")
                .RunOnRoute("assets")
                .RunOnRoute("pages");

            #endregion

            #region Non-secure static file assets

            // This middleware will map the path of http requests onto the file system path and
            // return the contents of the files in response to the request. This instance runs
            // on the 'assets' route which does not identify the caller, does not establish a 
            // session, and has no security restrictions so that requests are handled as
            // efficiently as possible.
            builder.Register(ninject.Get<OwinFramework.StaticFiles.StaticFilesMiddleware>())
                .As("Public resources")
                .ConfigureWith(config, "/middleware/staticFiles/assets")
                .RunOnRoute("assets");

            #endregion

            #region REST API secured with token

            // This middleware is part of this sample application. It provides a very simple API
            // that can add 2 numbers together. The API is trivial because we are trying to
            // demonstrate the other things around it, not how to write an API
            builder.Register(ninject.Get<Middleware.ApiMiddleware>())
                .As("Api middleware")
                .RunAfter("Api security")
                .RunOnRoute("api");

            // This middleware is part of this sample application. It blocks API requests that do not
            // contain a valid API access token. API access tokens are generated and inserted into 
            // web pages by the ApiTokenMiddleware so that the web pages can call the API using javascript
            builder.Register(ninject.Get<Middleware.ApiSecurityMiddleware>())
                .As("Api security")
                .RunOnRoute("api");

            #endregion

            #region Secure static file assets

            // This is another instance of the static files middleware, but this one serves
            // pages on the 'pages' route which contains additional middleware to identify
            // the user. Tells the Authorization middleware that the caller must have the 
            // 'user' permission to retrieve these files.
            builder.Register(ninject.Get<OwinFramework.StaticFiles.StaticFilesMiddleware>())
                .As("Protected resources")
                .ConfigureWith(config, "/middleware/staticFiles/pages")
                .RunOnRoute("pages")
                .RunAfter<IAuthorization>(null, false);

            // This middleware is IIdentification middleware. It identifies the user from
            // a cookie and provides postback endoints to login, logout and create an account.
            // Note that this middleware is not designed to be secure, it is designed to 
            // test the identification store facility.
            builder.Register(ninject.Get<Middleware.IdentificationMiddleware>())
                .As("Identification")
                .RunOnRoute("pages")
                .RunAfter("Output filter");

            // This middleware is IAuthorization middleware. It gives all logged in users 
            // the 'user' permission and all anonymous visitors no permissions.
            builder.Register(ninject.Get<Middleware.AuthorizationMiddleware>())
                .As("Authorization")
                .RunOnRoute("pages");

            #endregion

            app.UseBuilder(builder);
        }
    }
}