using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;

using OwinInterfaces = OwinFramework.InterfacesV1.Facilities;
using PriusInterfaces = Prius.Contracts.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius
{
    [Package]
    public class Package : IPackage
    {
        string IPackage.Name { get { return "Owin framework cache token store"; } }
        IList<IocRegistration> IPackage.IocRegistrations { get { return _iocRegistrations; } }

        private readonly IList<IocRegistration> _iocRegistrations;

        /// <summary>
        /// Consutucts this IoC.Modules package definition
        /// </summary>
        public Package()
        {
            _iocRegistrations = new List<IocRegistration>
            {
                new IocRegistration().Init<OwinInterfaces.ITokenStore, TokenStoreFacility>(),

                new IocRegistration().Init<Interfaces.ITokenFactory, Tokens.TokenFactory>(),
                new IocRegistration().Init<Interfaces.ITokenDatabase, Tokens.TokenDatabase>(),

                new IocRegistration().Init<PriusInterfaces.ICommandFactory>(),
                new IocRegistration().Init<PriusInterfaces.IContextFactory>(),
                new IocRegistration().Init<IConfiguration>(),
            };
        }
    }
}
