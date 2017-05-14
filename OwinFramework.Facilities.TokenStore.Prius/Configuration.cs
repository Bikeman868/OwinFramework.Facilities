using System;
using System.Collections.Generic;
using OwinFramework.Facilities.TokenStore.Prius.Tokens;

namespace OwinFramework.Facilities.TokenStore.Prius
{
    [Serializable]
    public class Configuration
    {
        public List<TokenTypeConfiguration> TokenTypes { get; set; }

        public Configuration()
        {
            TokenTypes = new List<TokenTypeConfiguration>();
        }
    }
}
