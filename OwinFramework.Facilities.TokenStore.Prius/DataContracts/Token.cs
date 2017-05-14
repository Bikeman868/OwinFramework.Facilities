using System.Collections.Generic;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.DataContracts
{
    /// <summary>
    /// This is a token from the database that is full hydrated with
    /// instancecs of token validators attached and ready to handle
    /// token validation operations.
    /// </summary>
    internal class Token
    {
        public long? DatabaseId;
        public string Type;
        public IList<ITokenValidator> Validators;
    }
}
