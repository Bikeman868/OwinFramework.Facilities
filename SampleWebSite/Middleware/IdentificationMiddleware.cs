using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.InterfacesV1.Upstream;
using SampleWebSite.Extensions;

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

        private const string IdentityCookie = "identification";
        private const string RememberMeCookie = "remember-me";
        private const string HomePage = "/home.html";
        private const string LoginPostback = "/login";
        private const string LogoutPostback = "/logout";
        private const string RegisterPostback = "/register";
        private const string EndSessionPostback = "/endSession";
        private const string ChangePasswordPostback = "/changePassword";
        private const string RequestPasswordResetPostback = "/requestPasswordReset";
        private const string ResetPasswordPostback = "/resetPassword";

        public IdentificationMiddleware(
            IIdentityStore identityStore)
        {
            _identityStore = identityStore;
            this.RunAfter<ISession>();
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            var identification = new Identification();
            context.SetFeature<IIdentification>(identification);

            var upstream = new Upstream();
            context.SetFeature<IUpstreamIdentification>(upstream);

            var cookie = context.Request.Cookies[IdentityCookie];
            if (cookie == null)
            {
                identification.IsAnonymous = true;
                var rememberMe = context.Request.Cookies[RememberMeCookie];
                if (rememberMe != null)
                {
                    var authenticationResult = _identityStore.RememberMe(rememberMe);
                    if (authenticationResult.Status == AuthenticationStatus.Authenticated)
                    {
                        identification.IsAnonymous = false;
                        identification.Identity = authenticationResult.Identity;
                        context.Response.Cookies.Append(IdentityCookie, authenticationResult.Identity);
                    }
                    SetAuthentication(context, authenticationResult);
                }
            }
            else
            {
                identification.Identity = cookie;
            }

            if (string.Equals(HomePage, context.Request.Path.Value, StringComparison.OrdinalIgnoreCase))
                upstream.AllowAnonymous = true;

            if (string.Equals("POST", context.Request.Method, StringComparison.OrdinalIgnoreCase))
            {
                var path = context.Request.Path.Value;

                if (string.Equals(LoginPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    Login(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(LogoutPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    Logout(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(RegisterPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    Register(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(EndSessionPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    EndSession(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(ChangePasswordPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    ChangePassword(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(RequestPasswordResetPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    SendPasswordReset(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }

                if (string.Equals(ResetPasswordPostback, path, StringComparison.OrdinalIgnoreCase))
                {
                    ResetPassword(context, identification);
                    return context.Response.WriteAsync(string.Empty);
                }
            }

            return next();
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var identification = context.GetFeature<IIdentification>() as Identification;
            var upstream = context.GetFeature<IUpstreamIdentification>() as Upstream;
            if (identification == null || upstream == null)
                throw new Exception("Something went terribly wrong");
            
            if (identification.IsAnonymous && !upstream.AllowAnonymous)
                return Task.Factory.StartNew(() => context.Response.Redirect(HomePage));

            return next();
        }

        private void Register(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var identity = _identityStore.CreateIdentity();
            if (_identityStore.AddCredentials(identity, form["username"], form["password"]))
            {
                identification.Identity = identity;
                identification.IsAnonymous = false;

                var result = _identityStore.AuthenticateWithCredentials(form["username"], form["password"]);
                if (result.Status == AuthenticationStatus.Authenticated)
                {
                    context.Response.Cookies.Append(IdentityCookie, result.Identity);
                    context.Response.Cookies.Append(RememberMeCookie, result.RememberMeToken);
                }
            }
            context.Response.Redirect(HomePage);
        }

        private void Login(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var result = _identityStore.AuthenticateWithCredentials(form["username"], form["password"]);
            if (result.Status == AuthenticationStatus.Authenticated)
            {
                identification.Identity = result.Identity;
                identification.IsAnonymous = false;

                context.Response.Cookies.Append(IdentityCookie, result.Identity);
                context.Response.Cookies.Append(RememberMeCookie, result.RememberMeToken);
            }
            SetAuthentication(context, result);
            context.Response.Redirect(HomePage);
        }

        private void Logout(IOwinContext context, Identification identification)
        {
            identification.Identity = "";
            identification.IsAnonymous = true;

            context.Response.Cookies.Delete(IdentityCookie);
            context.Response.Cookies.Delete(RememberMeCookie);

            SetOutcome(context, identification, "Logged out");
            context.Response.Redirect(HomePage);
        }

        private void EndSession(IOwinContext context, Identification identification)
        {
            identification.Identity = "";
            identification.IsAnonymous = true;

            context.Response.Cookies.Delete(IdentityCookie);

            SetOutcome(context, identification, "Session cookie deleted");
            context.Response.Redirect(HomePage);
        }

        private void ChangePassword(IOwinContext context, Identification identification)
        {

        }

        private void SendPasswordReset(IOwinContext context, Identification identification)
        {

        }

        private void ResetPassword(IOwinContext context, Identification identification)
        {

        }

        private void SetOutcome(IOwinContext context, Identification identification, string outcome)
        {
            var session = context.GetFeature<ISession>();
            if (session == null) return;

            session.Set("identity", identification.IsAnonymous ? "Anonymous" : identification.Identity);
            session.Set("outcome", outcome);
        }

        private void SetAuthentication(IOwinContext context, IAuthenticationResult authenticationResult)
        {
            var session = context.GetFeature<ISession>();
            if (session == null) return;

            session.Set("identity", authenticationResult.Identity);
            session.Set("outcome", authenticationResult.Status.ToString());
            session.Set("purposes", string.Join(", ", authenticationResult.Purposes));

            if (!string.IsNullOrEmpty(authenticationResult.RememberMeToken))
            {
                var credential = _identityStore.GetRememberMeCredential(authenticationResult.RememberMeToken);
                session.Set("purposes", string.Join(", ", credential.Purposes));
                session.Set("username", credential.Username);
            }
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
