using System;
using System.Collections.Generic;

namespace OwinFramework.Facilities.TokenStore.Cache
{
    /// <summary>
    /// These objects are serialized and stored in the cache
    /// </summary>
    [Serializable]
    internal class CachedToken
    {
        public string TokenType { get; set; }
        public string Identity { get; set; }
        public List<string> Purposes { get; set; }
    }
}
