using System;
using System.Collections.Generic;
using OwinFramework.Facilities.TokenStore.Prius.DataContracts;
using OwinFramework.Facilities.TokenStore.Prius.Records;

namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    internal interface ITokenFactory
    {
        IList<string> TokenTypes { get; }

        Token CreateToken(string type, string identity, IEnumerable<string> purposes);
        Token CreateToken(TokenRecord tokenRecord);
    }
}
