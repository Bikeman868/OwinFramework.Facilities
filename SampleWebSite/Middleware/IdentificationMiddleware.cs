using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.InterfacesV1.Upstream;

namespace SampleWebSite.Middleware
{
    /// <summary>
    /// This middleware identifies the user from a cookie stored on their browser.
    /// If the user does not have an identifying cookie then UI is injected into
    /// the page to allow them to login or create an account. If they are logged
    /// in then the user is given a logout option.
    /// 
    /// Note that this is an example of using the IIdentityStore and is not an example
    /// of how to build a secure identification middleware, in fact this imolementation
    /// has very weak security to keep the exaple very simple. If you want to see an
    /// implemetation that implements a robust identification mechanism, read the
    /// source code for the OWIN Framework Identification NuGet package.
    /// </summary>
    public class IdentificationMiddleware: IMiddleware<IIdentification>, IRoutingProcessor
    {
        private readonly IIdentityStore _identityStore;

        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private const string CookieName = "identification";
        private const string HomePage = "/assets/home.html";
        private const string LoginPostback = "/login";
        private const string LogoutPostback = "/logout";
        private const string RegisterPostback = "/register";

        public IdentificationMiddleware(
            IIdentityStore identityStore
            )
        {
            _identityStore = identityStore;
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var identification = new Identification();
            context.SetFeature<IIdentification>(identification);

            var cookie = context.Request.Cookies[CookieName];
            if (cookie == null)
            {
                identification.IsAnonymous = true;
            }
            else
            {
                identification.Identity = cookie;
            }

            if (string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value;
                if (string.Equals(LoginPostback, path, StringComparison.OrdinalIgnoreCase))
                    return Login(context, identification);
                if (string.Equals(LogoutPostback, path, StringComparison.OrdinalIgnoreCase))
                    return Logout(context, identification);
                if (string.Equals(RegisterPostback, path, StringComparison.OrdinalIgnoreCase))
                    return Register(context, identification);
            }

            if (identification.IsAnonymous)
            {
                var upstream = context.GetFeature<IUpstreamIdentification>();
                if (!upstream.AllowAnonymous)
                {
                    return Task.Factory.StartNew(() => context.Response.Redirect(HomePage));
                }
            }
            return next();
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            var upstream = new Upstream();
            if (string.Equals(HomePage, context.Request.Path.Value, StringComparison.OrdinalIgnoreCase))
                upstream.AllowAnonymous = true;

            context.SetFeature<IUpstreamIdentification>(upstream);

            return next();
        }

        private Task Login(IOwinContext context, Identification identification)
        {
            return Task.Factory.StartNew(() => 
            { 
            });
        }

        private Task Logout(IOwinContext context, Identification identification)
        {
            return Task.Factory.StartNew(() =>
            {
            });
        }

        private Task Register(IOwinContext context, Identification identification)
        {
            return Task.Factory.StartNew(() =>
            {
            });
        }

        private class Upstream : IUpstreamIdentification
        {
            public bool AllowAnonymous { get; set; }
        }

        private class Identification : IIdentification
        {
            public string Identity { get; set; }
            public bool IsAnonymous { get; set; }
        }

    }
}
