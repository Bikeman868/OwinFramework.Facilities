using System.Collections.Generic;

namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    public interface ITokenType
    {
        IList<ITokenValidator> GetValidators(string identity, IEnumerable<string> purposes);
        void AddValidation(ITokenValidationRule rule);
    }
}