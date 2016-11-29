using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;

namespace SampleWebSite.Middleware
{
    // This middleware finds {{apiToken}} markers in HTML and replaces them with
    // a token obtained from the token store facility. It also handles
    // requests from the front-end to delete tokens when the user exits from the
    // page, and inserts this Javascript into the page.
    public class ApiTokenMiddleware: IMiddleware<object>
    {
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private readonly ITokenStore _tokenStore;
        private readonly PathString _deleteTokenPath;

        public ApiTokenMiddleware(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;

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
            var accessToken = request.Headers["Api-token"];
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

                    if (html.Contains("{{apiToken}}"))
                    {

                        var token = _tokenStore.CreateToken("api");

                        var unloadStript = "<script>\n"+
                                           "window.onunload = function(){\n" +
                                           "  var xhttp = new XMLHttpRequest();\n" +
                                           "  xhttp.open('DELETE', '" + _deleteTokenPath.Value + "', true);\n" +
                                           "  xhttp.setRequestHeader('Api-token', '" + token + "');\n" +
                                           "  xhttp.send();\n" +
                                           "}\n" +
                                           "</script>\n";
                        html = html.Replace("</body>", unloadStript + "</body>");

                        html = html.Replace("{{apiToken}}", token);
                        var newBytes = encoding.GetBytes(html);

                        originalStream.Write(newBytes, 0, newBytes.Length);
                    }
                    else
                        newStream.WriteTo(originalStream);
                }
                else
                    newStream.WriteTo(originalStream);
            });
        }
    }
}
