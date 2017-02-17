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

        public InProcessSession()
        {
            _sessions = new Dictionary<string, Session>();
            Dependencies = new List<IDependency>();
            this.RunAfter<IIdentification>(null, false);
        }

        public Task RouteRequest(IOwinContext context, Func<Task> next)
        {
            context.SetFeature<IUpstreamSession>(new Session());
            return next();
        }

        public Task Invoke(IOwinContext context, Func<Task> next)
        {
            var session = context.GetFeature<IUpstreamSession>() as Session;

            if (session != null && session.SessionRequired)
            {
                var identification = context.GetFeature<IIdentification>();
                if (identification != null && !identification.IsAnonymous)
                {
                    lock (_sessions)
                    {
                        Session existingSession;
                        if (_sessions.TryGetValue(identification.Identity, out existingSession))
                        {
                            session = existingSession;
                        }
                        else
                        {
                            session.EstablishSession(identification.Identity);
                            _sessions.Add(identification.Identity, session);
                        }
                    }
                }
            }

            if (session == null)
                session = new Session();

            context.SetFeature<ISession>(session);

            return next();
        }

        private class Session : ISession, IUpstreamSession
        {
            private IDictionary<string, object> _sessionVariables;

            public bool HasSession { get { return _sessionVariables != null; } }
            public bool SessionRequired;
            public string SessionId { get; private set; }

            public Session()
            {
            }

            public bool EstablishSession(string sessionId)
            {
                SessionId = sessionId;
                SessionRequired = true;
                if (!HasSession)
                    _sessionVariables = new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);
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
