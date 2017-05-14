using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Rules
{
    public abstract class TokenRule : ITokenValidationRule
    {
        public abstract ITokenValidationRule Initialize(object config);
        public abstract ITokenValidator GetInstance();
    }
}