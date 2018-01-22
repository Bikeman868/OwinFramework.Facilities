using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;
using Prius.Contracts.Interfaces;
using Prius.Contracts.Interfaces.Factory;

namespace OwinFramework.Facilities.IdentityStore.Prius
{
    [Package]
    public class Package : IPackage
    {
        string IPackage.Name { get { return "Owin framework Prius identity store facility"; } }
        IList<IocRegistration> IPackage.IocRegistrations { get { return _iocRegistrations; } }

        private readonly IList<IocRegistration> _iocRegistrations;

        /// <summary>
        /// Consutucts this IoC.Modules package definition
        /// </summary>
        public Package()
        {
            _iocRegistrations = new List<IocRegistration>
            {
                // Interfaces implemented in this package
                new IocRegistration().Init<IIdentityStore, IdentityStoreFacility>(),
                new IocRegistration().Init<IIdentityDirectory, IdentityStoreFacility>(),

                // These parts of the OWIN Framework are required by this package
                new IocRegistration().Init<IConfiguration>(),

                // Prius is required by this package
                new IocRegistration().Init<ICommandFactory>(),
                new IocRegistration().Init<IConnectionFactory>(),
            };
        }
    }
}
