using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using System.Web;

namespace SampleWebSite.Middleware
{
    /// <summary>
    /// This is deliberately kept very minimal since we are not trying
    /// to demonstrate how to write an API in this sample application.
    /// This API just takes two numbers and returns the sum of these two numbers.
    /// </summary>
    public class ApiMiddleware: IMiddleware<object>
    {
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private readonly PathString _path = new PathString("/api/add");

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var request = context.Request;

            if (string.Equals("GET", request.Method, StringComparison.OrdinalIgnoreCase) &&
                request.Path.StartsWithSegments(_path))
            {
                int a, b;

                if (!int.TryParse(request.Query["a"], out a))
                    throw new HttpException((int)HttpStatusCode.BadRequest, "Parameter a must be an integer");

                if (!int.TryParse(request.Query["b"], out b))
                    throw new HttpException((int)HttpStatusCode.BadRequest, "Parameter b must be an integer");

                var result = "{\"answer\":" + (a + b) + "}";

                context.Response.ContentType = "application/json";
                return context.Response.WriteAsync(result);
            }

            return next();
        }

    }
}
