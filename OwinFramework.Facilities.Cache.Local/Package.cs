using System.Collections.Generic;
using Ioc.Modules;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.Cache.Local
{
    [Package]
    public class Package : IPackage
    {
        string IPackage.Name { get { return "Owin framework local cache facility"; } }
        IList<IocRegistration> IPackage.IocRegistrations { get { return _iocRegistrations; } }

        private readonly IList<IocRegistration> _iocRegistrations;

        /// <summary>
        /// Consutucts this IoC.Modules package definition
        /// </summary>
        public Package()
        {
            _iocRegistrations = new List<IocRegistration>
            {
                new IocRegistration().Init<ICache, CacheFacility>(),
            };
        }
    }
}
