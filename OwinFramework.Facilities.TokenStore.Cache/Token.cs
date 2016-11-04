using System;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.TokenStore.Cache
{
    /// <summary>
    /// These objects are returned to the application when tokens are checked
    /// </summary>
    [Serializable]
    internal class Token: IToken
    {
        public string Value { get; set; }
        public string Identity { get; set; }
        public string Purpose { get; set; }
        public TokenStatus Status { get; set; }
    }
}
