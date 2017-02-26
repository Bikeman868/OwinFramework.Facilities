using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.InterfacesV1.Middleware;

namespace SampleWebSite.Middleware
{
    // This middleware finds {{xxxx}} markers in HTML and replaces them with
    // dynamic data values. It also handles requests from the front-end to delete 
    // API tokens when the user exits from the page, and inserts this Javascript 
    // into the page.
    public class OutputFilterMiddleware: IMiddleware<IResponseRewriter>
    {
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private readonly ITokenStore _tokenStore;
        private readonly IIdentityStore _identityStore;
        private readonly PathString _deleteTokenPath;

        public OutputFilterMiddleware(
            ITokenStore tokenStore, 
            IIdentityStore identityStore)
        {
            _tokenStore = tokenStore;
            _identityStore = identityStore;

            // Note that this path must be on a route where this middleware
            // will run. Since this middleware is designed to inject script
            // into web pages, it will run on the paths that contain pages 
            // for sure
            _deleteTokenPath = new PathString("/pages/apiToken");
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var request = context.Request;
            if (string.Equals(request.Method, "DELETE", StringComparison.OrdinalIgnoreCase) &&
                request.Path.StartsWithSegments(_deleteTokenPath))
            {
                DeleteToken(request);
                return context.Response.WriteAsync("API token deleted");
            }
            return InjectToken(context, next);
        }

        /// <summary>
        /// This method handles requests to delete API tokens. This will be called
        /// from Javascript when the browser unloads the page. Deleting the token
        /// makes it invalid (and therefore safe) and also stops the token store
        /// from having to store thousands of tokens that are no longer in use.
        /// </summary>
        private void DeleteToken(IOwinRequest request)
        {
            var accessToken = request.Headers["api-token"];
            if (!string.IsNullOrEmpty(accessToken))
                _tokenStore.DeleteToken(accessToken);
        }

        /// <summary>
        /// This method injects an access token into any html page that needs
        /// one, and also injects Javascript to delete the token when the
        /// page is unloaded.
        /// </summary>
        private Task InjectToken(IOwinContext context, Func<Task> next)
        {
            var response = context.Response;

            var newStream = new MemoryStream();
            var originalStream = response.Body;
            response.Body = newStream;

            return next().ContinueWith(downstream =>
            {
                if (downstream.Exception != null) throw downstream.Exception;

                response.Body = originalStream;
                if (string.Equals(response.ContentType, "text/html", StringComparison.OrdinalIgnoreCase))
                {
                    var encoding = Encoding.UTF8;
                    var originalBytes = newStream.ToArray();
                    var html = encoding.GetString(originalBytes);

                    var apiToken = string.Empty;
                    if (html.Contains("{{apiToken}}"))
                    {
                        apiToken = _tokenStore.CreateToken("api");

                        var unloadStript = "<script>\n" +
                                           "window.onunload = function(){\n" +
                                           "  var xhttp = new XMLHttpRequest();\n" +
                                           "  xhttp.open('DELETE', '" + _deleteTokenPath.Value + "', true);\n" +
                                           "  xhttp.setRequestHeader('api-token', '" + apiToken + "');\n" +
                                           "  xhttp.send();\n" +
                                           "}\n" +
                                           "</script>\n";

                        html = html.Replace("</body>", unloadStript + "</body>");
                    }

                    var identification = context.GetFeature<IIdentification>();
                    var identity = identification == null ? string.Empty : (identification.IsAnonymous ? "Anonymous" : identification.Identity);

                    var session = context.GetFeature<ISession>();
                    var username = session == null ? string.Empty : session.Get<string>("username");
                    var purposes = session == null ? string.Empty : session.Get<string>("purposes");
                    var outcome = session == null ? string.Empty : session.Get<string>("outcome");

                    var regex = new Regex("{{([^}]+)}}");
                    html = regex.Replace(html, m =>
                    {
                        switch (m.Groups[1].Value.ToLower())
                        {
                            case "apitoken": return apiToken;
                            case "identity": return identity;
                            case "username": return username;
                            case "purposes": return purposes;
                            case "outcome": return outcome;
                        }
                        return string.Empty;
                    });

                    var newBytes = encoding.GetBytes(html);
                    originalStream.Write(newBytes, 0, newBytes.Length);
                }
                else
                    newStream.WriteTo(originalStream);
            });
        }
    }
}
