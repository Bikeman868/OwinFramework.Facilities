
namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    public interface ITokenValidationRule
    {
        ITokenValidator GetInstance();
    }
}