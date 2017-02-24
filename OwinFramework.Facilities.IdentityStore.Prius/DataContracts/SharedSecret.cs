using System.Collections.Generic;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.IdentityStore.Prius.DataContracts
{
    internal class SharedSecret: ISharedSecret
    {
        public string Name { get; set; }
        public IList<string> Purposes { get; set; }
        public string Secret { get; set; }
    }
}
