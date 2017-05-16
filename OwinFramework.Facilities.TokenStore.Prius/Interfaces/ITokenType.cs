using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    public interface ITokenType
    {
        IList<ITokenValidator> GetValidators(string identity, IEnumerable<string> purposes);
        IList<ITokenValidator> GetValidators(JObject json);
        void AddValidation(ITokenValidationRule rule);
    }
}