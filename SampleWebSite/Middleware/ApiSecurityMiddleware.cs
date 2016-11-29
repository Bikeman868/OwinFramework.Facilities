using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;

namespace SampleWebSite.Middleware
{
    // This is an example of ow to use the ITokenStore facility to check the
    // status of a token.
    // This middleware looks for a token in the headers of the request and
    // validates this token with the token store.
    public class ApiSecurityMiddleware: IMiddleware<object>
    {
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private readonly ITokenStore _tokenStore;

        public ApiSecurityMiddleware(ITokenStore tokenStore)
        {
            _tokenStore = tokenStore;
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var request = context.Request;

            var accessToken = request.Headers["Api-token"];
            if (string.IsNullOrEmpty(accessToken))
                throw new HttpException((int)HttpStatusCode.Forbidden, "No API token found in the request");

            var token = _tokenStore.GetToken("api", accessToken);
            if (token.Status != TokenStatus.Allowed)
                throw new HttpException((int)HttpStatusCode.Forbidden, "This API token is not valid at this time");

            return next();
        }
    }
}
