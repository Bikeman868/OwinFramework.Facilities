using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.TokenStore.Cache
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
                new IocRegistration().Init<ITokenStore, TokenStoreFacility>(),
                new IocRegistration().Init<ICache>(),
                new IocRegistration().Init<IConfiguration>(),
            };
        }
    }
}
