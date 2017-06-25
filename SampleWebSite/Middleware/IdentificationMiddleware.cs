using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Facilities.IdentityStore.Prius.Exceptions;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.InterfacesV1.Upstream;
using OwinFramework.MiddlewareHelpers.Identification;
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
        private readonly ITokenStore _tokenStore;

        private readonly IList<IDependency> _dependencies = new List<IDependency>();
        IList<IDependency> IMiddleware.Dependencies { get { return _dependencies; } }

        string IMiddleware.Name { get; set; }

        private const string IdentityCookie = "identification";
        private const string RememberMeCookie = "remember-me";
        private const string SecureHomePage = "/home.html";
        private const string PublicHomePage = "/assets/home.html";
        private const string LoginPostback = "/login";
        private const string LogoutPostback = "/logout";
        private const string RegisterPostback = "/register";
        private const string EndSessionPostback = "/endSession";
        private const string ChangePasswordPostback = "/changePassword";
        private const string RequestPasswordResetPostback = "/requestPasswordReset";
        private const string ResetPasswordPostback = "/resetPassword";

        public IdentificationMiddleware(
            IIdentityStore identityStore, 
            ITokenStore tokenStore)
        {
            _identityStore = identityStore;
            _tokenStore = tokenStore;

            this.RunAfter<ISession>(null, false);
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            var cookie = context.Request.Cookies[IdentityCookie];
            if (cookie == null)
            {
                cookie = string.Empty;
                var rememberMe = context.Request.Cookies[RememberMeCookie];
                if (rememberMe != null)
                {
                    var authenticationResult = _identityStore.RememberMe(rememberMe);
                    if (authenticationResult.Status == AuthenticationStatus.Authenticated)
                    {
                        context.Response.Cookies.Append(IdentityCookie, authenticationResult.Identity);
                        cookie = authenticationResult.Identity;
                    }
                    SetAuthentication(context, authenticationResult);
                }
            }
            var identification = new Identification(cookie, _identityStore.GetClaims(cookie));

            context.SetFeature<IIdentification>(identification);
            context.SetFeature<IUpstreamIdentification>(identification);

            if (string.Equals(SecureHomePage, context.Request.Path.Value, StringComparison.OrdinalIgnoreCase))
                identification.AllowAnonymous = true;

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
            if (identification == null)
                throw new Exception("Something went terribly wrong");

            if (identification.IsAnonymous && !identification.AllowAnonymous)
                return Task.Factory.StartNew(() => context.Response.Redirect(SecureHomePage));

            return next();
        }

        private void Register(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var identity = _identityStore.CreateIdentity();
            try
            {
                if (_identityStore.AddCredentials(identity, form["username"], form["password"]))
                {
                    identification.Identity = identity;
                    identification.Claims = _identityStore.GetClaims(identity);

                    var result = _identityStore.AuthenticateWithCredentials(form["username"], form["password"]);
                    if (result.Status == AuthenticationStatus.Authenticated)
                    {
                        context.Response.Cookies.Append(IdentityCookie, result.Identity);
                        context.Response.Cookies.Append(RememberMeCookie, result.RememberMeToken);
                        SetAuthentication(context, result);
                    }
                }
            }
            catch (Exception e)
            {
                SetOutcome(context, identification, e.Message);
            }
            GoHome(context, identification);
        }

        private void Login(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var result = _identityStore.AuthenticateWithCredentials(form["username"], form["password"]);
            if (result.Status == AuthenticationStatus.Authenticated)
            {
                identification.Identity = result.Identity;
                identification.Claims = _identityStore.GetClaims(result.Identity);

                context.Response.Cookies.Append(IdentityCookie, result.Identity);
                context.Response.Cookies.Append(RememberMeCookie, result.RememberMeToken);
            }
            SetAuthentication(context, result);
            GoHome(context, identification);
        }

        private void Logout(IOwinContext context, Identification identification)
        {
            identification.Identity = "";
            identification.Claims.Clear();

            context.Response.Cookies.Delete(IdentityCookie);
            context.Response.Cookies.Delete(RememberMeCookie);

            SetOutcome(context, identification, "Logged out");
            GoHome(context, identification);
        }

        private void EndSession(IOwinContext context, Identification identification)
        {
            identification.Identity = "";
            identification.Claims.Clear();

            context.Response.Cookies.Delete(IdentityCookie);

            SetOutcome(context, identification, "Session cookie deleted");
            GoHome(context, identification);
        }

        private void ChangePassword(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var result = _identityStore.AuthenticateWithCredentials(form["username"], form["password"]);
            if (result.Status == AuthenticationStatus.Authenticated)
            {
                var credential = _identityStore.GetRememberMeCredential(result.RememberMeToken);
                if (credential == null)
                {
                    SetOutcome(context, identification, "Internal error, remember me token was not valid");
                }
                else
                {
                    try
                    {
                        if (_identityStore.ChangePassword(credential, form["new-password"]))
                            SetOutcome(context, identification, "Password changed");
                        else
                            SetOutcome(context, identification, "Password was not changed");
                    }
                    catch (InvalidPasswordException e)
                    {
                        SetOutcome(context, identification, "Invalid password. " + e.Message);
                    }
                }
            }
            else
            {
                SetOutcome(context, identification, "Login failed");
            }
            GoHome(context, identification);
        }

        private void SendPasswordReset(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var userName = form["username"];
            if (userName == null)
            {
                SetOutcome(context, identification, "No user name provided");
            }
            else
            {
                var token = _tokenStore.CreateToken("passwordReset", new[] { "ResetPassword" }, userName);

                var session = context.GetFeature<ISession>();
                if (session != null)
                    session.Set("reset-token", token);

                SetOutcome(context, identification, "Password reset token is: " + token);
            }
            GoHome(context, identification);
        }

        private void ResetPassword(IOwinContext context, Identification identification)
        {
            var form = context.Request.ReadFormAsync().Result;
            var userName = form["username"];
            var resetToken = form["reset-token"];
            var newPassword = form["new-password"];

            var failed = false;
            if (resetToken == null)
            {
                SetOutcome(context, identification, "No password reset token provided");
                failed = true;
            }
            else if (userName == null)
            {
                SetOutcome(context, identification, "No username provided");
                failed = true;
            }
            else if (newPassword == null)
            {
                SetOutcome(context, identification, "No new password provided");
                failed = true;
            }

            ICredential credential = null;
            if (!failed)
            {
                credential = _identityStore.GetUsernameCredential(userName);
                if (credential == null)
                {
                    SetOutcome(context, identification, "Invalid username provided");
                    failed = true;
                }
            }

            if (!failed)
            {
                var token = _tokenStore.GetToken("passwordReset", resetToken, "ResetPassword", userName);
                if (token.Status == TokenStatus.Allowed)
                {
                    try
                    {
                        if (_identityStore.ChangePassword(credential, form["new-password"]))
                        {
                            SetOutcome(context, identification, "Password succesfully reset");
                            identification.Identity = credential.Identity;
                            identification.Claims = _identityStore.GetClaims(credential.Identity);

                            context.Response.Cookies.Append(IdentityCookie, credential.Identity);
                            context.Response.Cookies.Delete(RememberMeCookie);
                            context.Response.Redirect(SecureHomePage);
                        }
                        else
                            SetOutcome(context, identification, "Password reset failed");
                    }
                    catch (InvalidPasswordException e)
                    {
                        SetOutcome(context, identification, 
                            "Invalid password. " + e.Message
                            + ". You will need to get a new password reset token to try again.");
                    }
                }
                else
                {
                    SetOutcome(context, identification, "This password reset token has been used before");
                }
            }

            GoHome(context, identification);
        }

        private void SetOutcome(IOwinContext context, Identification identification, string outcome)
        {
            var session = context.GetFeature<ISession>();
            if (session == null) return;

            session.Set("identity", identification.IsAnonymous ? "Anonymous" : identification.Identity);
            session.Set("claims", string.Join(", ", identification.Claims.Select(c => c.Name + (c.Status == ClaimStatus.Verified ? " = " : " ~ ") + c.Value)));
            session.Set("outcome", outcome);
        }

        private void SetAuthentication(IOwinContext context, IAuthenticationResult authenticationResult)
        {
            var session = context.GetFeature<ISession>();
            if (session == null) return;

            var claims = _identityStore.GetClaims(authenticationResult.Identity);
            session.Set("claims", string.Join(", ", claims.Select(c => c.Name + (c.Status == ClaimStatus.Verified ? " = " : " ~ ") + c.Value)));

            session.Set("identity", authenticationResult.Identity);
            session.Set("outcome", authenticationResult.Status.ToString());
            session.Set("purposes", string.Join(", ", authenticationResult.Purposes ?? new List<string>()));

            if (!string.IsNullOrEmpty(authenticationResult.RememberMeToken))
            {
                var credential = _identityStore.GetRememberMeCredential(authenticationResult.RememberMeToken);
                session.Set("purposes", string.Join(", ", credential.Purposes ?? new List<string>()));
                session.Set("username", credential.Username);
            }
        }

        private void GoHome(IOwinContext context, Identification identification)
        {
            if (identification.IsAnonymous)
                context.Response.Redirect(PublicHomePage);
            else
                context.Response.Redirect(SecureHomePage);
        }
    }
}
