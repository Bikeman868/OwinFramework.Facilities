
namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    public enum Validity { Valid, TemporaryInvalid, PermenantInvalid }

    public class CheckResult
    {
        public Validity Validity;
        public bool IsStatusModified;
    }

    public interface ITokenValidator
    {
        CheckResult CheckIsValid(string identity, string purpose);
        bool CheckIsExpired();
        string Serialize();
        void Hydrate(string serializedData);
    }
}