using System;
using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;
using OwinFramework.Interfaces.Routing;
using OwinFramework.InterfacesV1.Facilities;
using Urchin.Client.Interfaces;

namespace SampleWebSite
{
    [Package]
    public class Package : IPackage
    {
        public string Name { get { return Startup.ApplicationName; } }

        private readonly IList<IocRegistration> _iocRegistrations;
        public IList<IocRegistration> IocRegistrations { get { return _iocRegistrations; } }

        public Package()
        {
            _iocRegistrations = new List<IocRegistration>
            {
                // Prius requirements
                new IocRegistration().Init<Prius.Contracts.Interfaces.External.IFactory, PriusFactory>(),
                new IocRegistration().Init<Prius.Contracts.Interfaces.External.IErrorReporter, PriusErrorReporter>(),

                // External dependencies
                new IocRegistration().Init<IConfigurationStore>(),
                new IocRegistration().Init<IBuilder>(),
                new IocRegistration().Init<IRouter>(),
                new IocRegistration().Init<ITokenStore>(),
                new IocRegistration().Init<IIdentityStore>(),
            };
        }

        private class PriusFactory : Prius.Contracts.Interfaces.External.IFactory
        {
            public T Create<T>() where T : class
            {
                return (T)(typeof (T).GetConstructor(Type.EmptyTypes).Invoke(null));
            }

            public object Create(Type type)
            {
                return (type.GetConstructor(Type.EmptyTypes).Invoke(null));
            }
        }

        private class PriusErrorReporter: Prius.Contracts.Interfaces.External.IErrorReporter
        {
            public void ReportError(Exception e, System.Data.SqlClient.SqlCommand cmd, string subject, params object[] otherInfo)
            {
            }

            public void ReportError(Exception e, string subject, params object[] otherInfo)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}