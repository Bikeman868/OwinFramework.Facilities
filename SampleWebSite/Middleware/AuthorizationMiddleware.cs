using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.InterfacesV1.Upstream;

namespace SampleWebSite.Middleware
{
    public class AuthorizationMiddleware : IMiddleware<IAuthorization>, IRoutingProcessor
    {
        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var authorization = (Authorization)context.GetFeature<IUpstreamAuthorization>();
            authorization.Identification = context.GetFeature<IIdentification>();
            context.SetFeature<IAuthorization>(authorization);

            if (!authorization.IsAllowed)
            {
                throw new HttpException((int)HttpStatusCode.Forbidden, "You do not have permission to access this resource");
            }

            return next();
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            var authorization = new Authorization();
            context.SetFeature<IUpstreamAuthorization>(authorization);

            return next();
        }

        /// <summary>
        /// This class contains the request speciifc information relating to user authorization.
        /// One of these objects is created for each request and provides authorization information
        /// to other middleware that needs it.
        /// This very simple example has no ability to configure permissions for users, it just
        /// implements the very simple rule that all identified callers have the 'user' permission
        /// and the 'user' role, and all anonymous users have no permissions and no roles.
        /// </summary>
        private class Authorization : IUpstreamAuthorization, IAuthorization
        {
            private readonly List<string> _requiredPermissions;
            private readonly List<string> _requiredRoles;

            public IIdentification Identification { get; set; }

            public Authorization()
            {
                _requiredPermissions = new List<string>();
                _requiredRoles = new List<string>();
            }

            public void AddRequiredPermission(string permissionName)
            {
                _requiredPermissions.Add(permissionName);
            }

            public void AddRequiredRole(string roleName)
            {
                _requiredRoles.Add(roleName);
            }

            public bool IsAllowed
            {
                get
                {
                    if (_requiredRoles.Any(r => !IsInRole(r))) return false;
                    if (_requiredPermissions.Any(r => !HasPermission(r))) return false;
                    return true; 
                }
            }

            public bool HasPermission(string permissionName)
            {
                if (Identification == null || Identification.IsAnonymous) return false;
                return string.Equals(permissionName, "user", StringComparison.InvariantCultureIgnoreCase);
            }

            public bool IsInRole(string roleName)
            {
                if (Identification == null || Identification.IsAnonymous) return false;
                return string.Equals(roleName, "user", StringComparison.InvariantCultureIgnoreCase);
            }
        }
    }
}
