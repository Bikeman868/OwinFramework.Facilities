﻿using System;
using System.Collections.Generic;
using Ioc.Modules;
using Ninject;
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

        public class PriusFactory : Prius.Contracts.Interfaces.External.IFactory
        {
            public static StandardKernel Ninject;

            public T Create<T>() where T : class
            {
                return Ninject.Get<T>();
            }

            public object Create(Type type)
            {
                return Ninject.Get(type);
            }
        }

        public class PriusErrorReporter : Prius.Contracts.Interfaces.External.IErrorReporter
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