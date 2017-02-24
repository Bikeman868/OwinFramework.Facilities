using System.Collections.Generic;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.IdentityStore.Prius.DataContracts
{
    internal class Credential : ICredential
    {
        public string Identity { get; set; }
        public List<string> Purposes { get; set; }
        public string Username { get; set; }
    }
}
