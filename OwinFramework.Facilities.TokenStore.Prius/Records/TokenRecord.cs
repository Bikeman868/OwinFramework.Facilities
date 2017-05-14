using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.TokenStore.Prius.Records
{
    internal class TokenRecord
    {
        [Mapping("token_id")]
        public long Id { get; set; }

        [Mapping("token")]
        public string Token { get; set; }

        [Mapping("type")]
        public string TokenType { get; set; }

        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("purposes")]
        public string Purposes { get; set; }

        [Mapping("state")]
        public string TokenState { get; set; }
    }
}
