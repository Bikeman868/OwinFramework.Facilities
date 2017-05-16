
using Newtonsoft.Json.Linq;

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
        string Name { get; }

        CheckResult CheckIsValid(string identity, string purpose);
        bool CheckIsExpired();

        JObject Serialize();
        void Hydrate(JObject json);
    }
}