using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;
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
                // External dependencies
                new IocRegistration().Init<IConfigurationStore>(),
                new IocRegistration().Init<IBuilder>(),
            };
        }
    }
}