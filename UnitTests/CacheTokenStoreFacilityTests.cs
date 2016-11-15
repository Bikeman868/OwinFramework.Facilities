using NUnit.Framework;
using OwinFramework.Facilities.TokenStore.Cache;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;

namespace UnitTests
{
    [TestFixture]
    public class CacheTokenStoreFacilityTests : Moq.Modules.TestBase
    {
        private ITokenStore _tokenStore;

        [SetUp]
        public void Setup()
        {
            _tokenStore = new TokenStoreFacility(
                SetupMock<ICache>(),
                SetupMock<IConfiguration>());
        }

        [Test]
        public void Should_allow_default_tokens_for_any_identity_and_purpose()
        {
            const string tokenType = "session";

            var sessionToken = _tokenStore.CreateToken(tokenType);
            var token = _tokenStore.GetToken(tokenType, sessionToken);

            Assert.IsNotNull(token);
            Assert.AreEqual(sessionToken, token.Value);
            Assert.IsTrue(string.IsNullOrEmpty(token.Identity));
            Assert.IsTrue(string.IsNullOrEmpty(token.Purpose));
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            const string identity = "urn:user:431";
            const string purpose = "login";
            token = _tokenStore.GetToken(tokenType, sessionToken, purpose, identity);

            Assert.IsNotNull(token);
            Assert.AreEqual(sessionToken, token.Value);
            Assert.AreEqual(purpose, token.Purpose);
            Assert.AreEqual(identity, token.Identity);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);
        }

        [Test]
        public void Should_check_token_type()
        {
            var sessionToken = _tokenStore.CreateToken("session");
            var token = _tokenStore.GetToken("authentication", sessionToken);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);
        }

        [Test]
        public void Should_check_identity()
        {
            const string tokenType = "session";
            const string identity = "urn:user:1234";

            var tokenId = _tokenStore.CreateToken(tokenType, "", identity);
            var token = _tokenStore.GetToken(tokenType, tokenId, "login", identity);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, "", identity);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, "login", identity + "00");

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, "login");

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);
        }

        [Test]
        public void Should_check_purpose()
        {
            const string tokenType = "session";
            const string purpose = "login";

            var tokenId = _tokenStore.CreateToken(tokenType, purpose);
            var token = _tokenStore.GetToken(tokenType, tokenId, purpose);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, "wrong purpose");

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);
        }

        [Test]
        public void Should_support_milti_purpose_tokens()
        {
            const string tokenType = "session";
            var purpose = new[] { "login", "logout" };

            var tokenId = _tokenStore.CreateToken(tokenType, purpose);

            var token = _tokenStore.GetToken(tokenType, tokenId, purpose[0]);
            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, purpose[1]);
            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, "wrong purpose");
            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId);
            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);
        }
    }
}
