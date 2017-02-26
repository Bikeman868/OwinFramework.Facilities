using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Owin;
using OwinFramework.Builder;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Middleware;
using OwinFramework.InterfacesV1.Upstream;

namespace SampleWebSite.Middleware
{
    public class InProcessSession : IMiddleware<ISession>, IUpstreamCommunicator<IUpstreamSession>
    {
        public string Name { get; set; }
        public IList<IDependency> Dependencies { get; private set; }

        private readonly IDictionary<string, Session> _sessions;

        private const string SessionCookie = "sid";

        public InProcessSession()
        {
            _sessions = new Dictionary<string, Session>();
            Dependencies = new List<IDependency>();
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            var sessionId = context.Request.Cookies[SessionCookie];
            if (string.IsNullOrEmpty(sessionId))
            {
                sessionId = Guid.NewGuid().ToShortString();
                context.Response.Cookies.Append(
                    SessionCookie, 
                    sessionId,
                    new CookieOptions { Expires = DateTime.UtcNow.AddMinutes(15) });
            }

            Session session;
            lock (_sessions)
            {
                if (!_sessions.TryGetValue(sessionId, out session))
                {
                    session = new Session(sessionId);
                    _sessions.Add(sessionId, session);
                }
            }
            
            context.SetFeature<IUpstreamSession>(session);
            context.SetFeature<ISession>(session);
            return next();
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            return next();
        }

        private class Session : ISession, IUpstreamSession
        {
            private readonly IDictionary<string, object> _sessionVariables;

            public bool HasSession { get { return _sessionVariables != null; } }
            public string SessionId { get; private set; }

            public Session(string sessionId)
            {
                SessionId = sessionId;
                _sessionVariables = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            }

            public bool EstablishSession(string sessionId)
            {
                return true;
            }

            public T Get<T>(string name)
            {
                if (!HasSession)return default(T);

                object value;
                return _sessionVariables.TryGetValue(name, out value) ? (T)value : default(T);
            }

            public void Set<T>(string name, T value)
            {
                if (HasSession)
                {
                    _sessionVariables[name] = value;
                }
            }

            public object this[string name]
            {
                get { return Get<object>(name); }
                set { Set(name, value); }
            }
        }

    }
}
