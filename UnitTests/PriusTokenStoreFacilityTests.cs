﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using OwinFramework.Builder;
using OwinFramework.Facilities.TokenStore.Prius;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Records;
using OwinFramework.Facilities.TokenStore.Prius.Tokens;
using OwinFramework.Interfaces.Builder;
using OwinFramework.InterfacesV1.Facilities;
using OwinFramework.Mocks.Builder;
using Prius.Contracts.Interfaces.External;

namespace UnitTests
{
    [TestFixture]
    public class PriusTokenStoreFacilityTests : Moq.Modules.TestBase
    {
        private ITokenStore _tokenStore;
        private MockConfiguration _mockConfiguration;

        [SetUp]
        public void Setup()
        {
            var factory = SetupMock<IFactory>();
            var configuration = SetupMock<IConfiguration>();

            _mockConfiguration = GetMock<MockConfiguration, IConfiguration>();

            _tokenStore = new TokenStoreFacility(
                new TokenFactory(factory, configuration),
                new TokenDatabase(),
                configuration);

            var tokenStoreConfiguration = new Configuration
            {
                TokenTypes = new List<TokenTypeConfiguration> 
                { 
                    new TokenTypeConfiguration
                    {
                        Name = "session",
                        Rules = new List<RuleConfiguration>
                        {
                            new RuleConfiguration{Type = "Expiry", Json = "{\"expiryTime\":\"00:00:10\"}"}
                        }
                    },
                    new TokenTypeConfiguration
                    {
                        Name = "password-reset",
                        Rules = new List<RuleConfiguration>
                        {
                            new RuleConfiguration{Type = "UseCount", Json = "{\"maxUseCount\":2}"}
                        }
                    },
                    new TokenTypeConfiguration
                    {
                        Name = "api-call",
                        Rules = new List<RuleConfiguration>
                        {
                            new RuleConfiguration{Type = "Rate", Json = "{\"window\":\"00:00:02\", \"maxUseCount\":10}"}
                        }
                    }
                }
            };

            _mockConfiguration.SetConfiguration("/owinFramework/facility/tokenStore.Prius", tokenStoreConfiguration);
            _mockConfiguration.SetConfiguration("/owinFramework/facility/tokenStore.Prius/tokenTypes", tokenStoreConfiguration.TokenTypes);
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
            Assert.AreEqual(TokenStatus.Invalid, token.Status);
        }

        [Test]
        public void Should_not_allow_random_tokens()
        {
            var token = _tokenStore.GetToken("session", Guid.NewGuid().ToShortString());

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Invalid, token.Status);
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
        public void Should_support_multi_purpose_tokens()
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

        [Test]
        public void Should_support_limited_use_count_tokens()
        {
            const string tokenType = "password-reset";
            const string identity = "urn:user:1234";

            var tokenId = _tokenStore.CreateToken(tokenType, "", identity);

            var token = _tokenStore.GetToken(tokenType, tokenId, null, identity);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, null, identity);

            Assert.IsNotNull(token);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);

            token = _tokenStore.GetToken(tokenType, tokenId, null, identity);

            Assert.IsNotNull(token);
            Assert.AreNotEqual(TokenStatus.Allowed, token.Status);
        }

        [Test]
        public void Should_support_rate_limiting_tokens()
        {
            const string tokenType = "api-call";
            const string purpose = "getUser";

            var tokenId = _tokenStore.CreateToken(tokenType, purpose);

            IToken token;

            for (var i = 0; i < 10; i++)
            {
                token = _tokenStore.GetToken(tokenType, tokenId, purpose);
                Assert.AreEqual(TokenStatus.Allowed, token.Status);
            }
            token = _tokenStore.GetToken(tokenType, tokenId, purpose);
            Assert.AreEqual(TokenStatus.NotAllowed, token.Status);

            Thread.Sleep(TimeSpan.FromSeconds(3));

            token = _tokenStore.GetToken(tokenType, tokenId, purpose);
            Assert.AreEqual(TokenStatus.Allowed, token.Status);
        }

        #region Mock implementations

        private class TokenDatabase : ITokenDatabase
        {
            private List<TokenRecord> _tokens;
            private int _nextId = 1;

            public TokenDatabase()
            {
                Clear();
            }

            public void Clear()
            {
                _tokens = new List<TokenRecord>();
            }

            public TokenRecord AddToken(string token, string tokenType, string state)
            {
                var result = new TokenRecord
                {
                    Id = _nextId++,
                    Token = token,
                    TokenType = tokenType,
                    TokenState = state
                };
                _tokens.Add(result);
                return result;
            }

            public TokenRecord GetTokenById(long tokenId)
            {
                return _tokens.FirstOrDefault(t => t.Id == tokenId);
            }

            public TokenRecord GetToken(string token)
            {
                return _tokens.FirstOrDefault(t => t.Token == token);
            }

            public bool UpdateToken(long tokenId, string state)
            {
                var tokenRecord = GetTokenById(tokenId);
                if (tokenRecord == null) return false;
                tokenRecord.TokenState = state;
                return true;
            }

            public bool DeleteToken(string token)
            {
                _tokens = _tokens.Where(t => t.Token != token).ToList();
                return true;
            }

            public bool DeleteToken(long tokenId)
            {
                _tokens = _tokens.Where(t => t.Id != tokenId).ToList();
                return true;
            }

            public long Clean()
            {
                return 0;
            }
        }

        #endregion
    }
}
