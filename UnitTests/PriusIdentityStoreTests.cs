using System;
using System.Collections.Generic;
using System.Linq;
using Moq.Modules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OwinFramework.Facilities.IdentityStore.Prius;
using OwinFramework.Facilities.IdentityStore.Prius.Exceptions;
using OwinFramework.InterfacesV1.Facilities;
using Prius.Contracts.Interfaces;
using Prius.Contracts.Interfaces.Commands;
using Prius.Mocks;
using Prius.Mocks.Helper;
using OwinFramework.Interfaces.Builder;

namespace UnitTests
{
    [TestFixture]
    public class PriusIdentityStoreTests: TestBase
    {
        private IdentityStoreFacility _identityStore;

        [SetUp]
        public void Setup()
        {
            var tables = new Dictionary<string, JArray>();

            tables.Add("identity", new JArray());
            tables.Add("credentials", new JArray());
            tables.Add("secrets", new JArray());

            var mockedRepository = new MockedRepository("IdentityStore");
            mockedRepository.Add("sp_GetAllIdentities", tables["identity"]);
            mockedRepository.Add("sp_AddIdentity", new AddIdentityProcedure(tables));
            mockedRepository.Add("sp_GetIdentity", new GetIdentityProcedure(tables));

            mockedRepository.Add("sp_AddCredential", new AddCredentialProcedure(tables));
            mockedRepository.Add("sp_DeleteIdentityCredentials", new DeleteIdentityCredentialsProcedure(tables));
            mockedRepository.Add("sp_GetUserNameCredential", new GetUserNameCredentialProcedure(tables));

            mockedRepository.Add("sp_AddSharedSecret", new AddSharedSecretProcedure(tables));
            mockedRepository.Add("sp_GetSharedSecret", new GetSharedSecretProcedure(tables));
            mockedRepository.Add("sp_DeleteSharedSecret", new DeleteSharedSecretProcedure(tables));
            mockedRepository.Add("sp_GetIdentitySharedSecrets", new GetIdentitySharedSecretsProcedure(tables));

            mockedRepository.Add("sp_AuthenticateSuccess", new AuthenticateSuccessProcedure(tables));
            mockedRepository.Add("sp_AuthenticateFail", new AuthenticateFailProcedure(tables));
            
            var mockContextFactory = GetMock<MockContextFactory, IContextFactory>();
            mockContextFactory.MockedRepository = mockedRepository;

            _identityStore = new IdentityStoreFacility(
                SetupMock<IConfiguration>(),
                SetupMock<IContextFactory>(),
                SetupMock<ICommandFactory>());
        }

        [Test]
        public void Should_create_new_identities()
        {
            var identity = _identityStore.CreateIdentity();

            Assert.IsTrue(!string.IsNullOrEmpty(identity));
        }

        [Test]
        public void Should_store_and_check_credentials()
        {
            Assert.IsTrue(_identityStore.SupportsCredentials);

            var identity = _identityStore.CreateIdentity();

            const string user = "me@gmail.com";
            const string password = "password";
            var success = _identityStore.AddCredentials(identity, user, password);
            var result = _identityStore.AuthenticateWithCredentials(user, password);

            Assert.IsTrue(success);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            result = _identityStore.AuthenticateWithCredentials(user, "wrong password");
            Assert.AreEqual(AuthenticationStatus.InvalidCredentials, result.Status);

            result = _identityStore.AuthenticateWithCredentials("wrong user", password);
            Assert.AreEqual(AuthenticationStatus.NotFound, result.Status);

            result = _identityStore.AuthenticateWithCredentials(user.ToUpper(), password);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            result = _identityStore.AuthenticateWithCredentials(user, password.ToUpper());
            Assert.AreEqual(AuthenticationStatus.InvalidCredentials, result.Status);
        }

        [Test]
        public void Should_store_multiple_credentials()
        {
            var identity = _identityStore.CreateIdentity();

            const string user1 = "me1@gmail.com";
            const string password1 = "password1";
            const string user2 = "me2@gmail.com";
            const string password2 = "password2";

            var success = _identityStore.AddCredentials(identity, user1, password1);
            var result = _identityStore.AuthenticateWithCredentials(user1, password1);

            Assert.IsTrue(success);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            success = _identityStore.AddCredentials(identity, user2, password2, false, new[]{"api"});
            result = _identityStore.AuthenticateWithCredentials(user2, password2);

            Assert.IsTrue(success);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            result = _identityStore.AuthenticateWithCredentials(user1, password1);

            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);
        }

        [Test]
        public void Should_replace_credentials()
        {
            var identity = _identityStore.CreateIdentity();

            const string user1 = "me1@gmail.com";
            const string password1 = "password1";
            const string user2 = "me2@gmail.com";
            const string password2 = "password2";

            var success = _identityStore.AddCredentials(identity, user1, password1);
            var result = _identityStore.AuthenticateWithCredentials(user1, password1);

            Assert.IsTrue(success);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            success = _identityStore.AddCredentials(identity, user2, password2);
            result = _identityStore.AuthenticateWithCredentials(user2, password2);

            Assert.IsTrue(success);
            Assert.AreEqual(AuthenticationStatus.Authenticated, result.Status);
            Assert.AreEqual(identity, result.Identity);

            result = _identityStore.AuthenticateWithCredentials(user1, password1);

            Assert.AreEqual(AuthenticationStatus.NotFound, result.Status);
        }

        [Test]
        public void Should_not_allow_invalid_passwords()
        {
            var identity = _identityStore.CreateIdentity();

            const string user = "me@gmail.com";
            Assert.Throws<InvalidPasswordException>(() => _identityStore.AddCredentials(identity, user, "pass"));
            Assert.Throws<InvalidPasswordException>(() => _identityStore.AddCredentials(identity, user, ""));
            Assert.Throws<InvalidPasswordException>(() => _identityStore.AddCredentials(identity, user, new string('p', 200)));
        }

        [Test]
        public void Should_not_allow_invalid_usernames()
        {
            var identity = _identityStore.CreateIdentity();

            const string password = "val1Dpassw0rd";
            Assert.Throws<InvalidUserNameException>(() => _identityStore.AddCredentials(identity, "u", password));
            Assert.Throws<InvalidUserNameException>(() => _identityStore.AddCredentials(identity, new string('u', 90), password));
            Assert.Throws<InvalidUserNameException>(() => _identityStore.AddCredentials(identity, "cool guy", password));
        }

        [Test]
        public void Should_not_allow_duplicate_usernames()
        {
            var identity1 = _identityStore.CreateIdentity();

            const string user = "me@gmail.com";
            const string password = "password";
            var success = _identityStore.AddCredentials(identity1, user, password);

            Assert.IsTrue(success);

            var identity2 = _identityStore.CreateIdentity();
            Assert.Throws<InvalidUserNameException>(() => _identityStore.AddCredentials(identity2, user, password));
        }

        [Test]
        public void Should_support_shared_secrets()
        {
            Assert.IsTrue(_identityStore.SupportsSharedSecrets);

            var identity = _identityStore.CreateIdentity();

            const string user = "me@gmail.com";
            const string password = "password";
            var success = _identityStore.AddCredentials(identity, user, password);

            Assert.IsTrue(success);

            var apiSecret = _identityStore.AddSharedSecret(identity, "API Access", new List<string> { "api" });
            var profileSecret = _identityStore.AddSharedSecret(identity, "Profile Access", new List<string> { "profile" });

            Assert.IsNotNull(apiSecret);
            Assert.IsTrue(apiSecret.Length > 16);
            Assert.IsNotNull(profileSecret);
            Assert.IsTrue(profileSecret.Length > 16);
            Assert.AreNotEqual(apiSecret, profileSecret);

            var apiResult = _identityStore.AuthenticateWithSharedSecret(apiSecret);
            Assert.AreEqual(AuthenticationStatus.Authenticated, apiResult.Status);
            Assert.AreEqual(identity, apiResult.Identity);
            Assert.IsNotNull(apiResult.Purposes);
            Assert.AreEqual(1, apiResult.Purposes.Count);
            Assert.AreEqual("api", apiResult.Purposes[0]);

            var profileResult = _identityStore.AuthenticateWithSharedSecret(profileSecret);
            Assert.AreEqual(AuthenticationStatus.Authenticated, profileResult.Status);
            Assert.AreEqual(identity, profileResult.Identity);
            Assert.IsNotNull(profileResult.Purposes);
            Assert.AreEqual(1, profileResult.Purposes.Count);
            Assert.AreEqual("profile", profileResult.Purposes[0]);

            var secrets = _identityStore.GetAllSharedSecrets(identity).OrderBy(s => s.Name).ToList();
            Assert.AreEqual(2, secrets.Count);
            Assert.AreEqual("API Access", secrets[0].Name);
            Assert.AreEqual("api", secrets[0].Purposes[0]);
            Assert.AreEqual(apiSecret, secrets[0].Secret);
            Assert.AreEqual("Profile Access", secrets[1].Name);
            Assert.AreEqual("profile", secrets[1].Purposes[0]);
            Assert.AreEqual(profileSecret, secrets[1].Secret);

            _identityStore.DeleteSharedSecret(apiSecret);
            apiResult = _identityStore.AuthenticateWithSharedSecret(apiSecret);
            Assert.AreEqual(AuthenticationStatus.NotFound, apiResult.Status);
        }

        [Test]
        public void Should_not_support_certificates()
        {
            // Replace this test when certificates are supported
            Assert.IsFalse(_identityStore.SupportsCertificates);
        }

        [Test]
        public void Should_not_support_social_login()
        {
            // Replace this test when social login is supported
            Assert.AreEqual(0, _identityStore.SocialServices.Count);
        }

        #region Stored procedure mocks

        private class AddIdentityProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AddIdentityProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var identity = GetParameterValue(command, "identity", "");
                var newIdentity = new JObject();
                newIdentity["identity"] = identity;
                _tables["identity"].Add(newIdentity);

                SetData(_tables["identity"], null, o => o["identity"].ToString() == identity);
                return base.Query(command);
            }
        }

        private class GetIdentityProcedure: MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public GetIdentityProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var identity = GetParameterValue(command, "identity", "");
                SetData(_tables["identity"], null, o => o["identity"].ToString() == identity);
                return base.Query(command);
            }
        }

        private class AddCredentialProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AddCredentialProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var credential = new JObject();
                credential["identity"] = GetParameterValue(command, "identity", "");
                credential["userName"] = GetParameterValue(command, "userName", "");
                credential["purposes"] = GetParameterValue(command, "purposes", "");
                credential["version"] = GetParameterValue(command, "version", 1);
                credential["hash"] = GetParameterValue<byte[]>(command, "hash", null);
                credential["salt"] = GetParameterValue<byte[]>(command, "salt", null);
                _tables["credentials"].Add(credential);

                SetData(_tables["credentials"], null, o => o == credential);
                return base.Query(command);
            }
        }

        private class GetUserNameCredentialProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public GetUserNameCredentialProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var userName = GetParameterValue(command, "userName", "");
                SetData(_tables["credentials"], null, o => string.Equals(o["userName"].ToString(), userName, StringComparison.InvariantCultureIgnoreCase));
                return base.Query(command);
            }
        }

        private class DeleteIdentityCredentialsProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public DeleteIdentityCredentialsProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                var identity = GetParameterValue(command, "identity", "");

                var originalRecords = _tables["credentials"].Values<JObject>().ToList();
                var remainingRecords = originalRecords.Where(r => r["identity"].Value<string>() != identity).ToList();
                _tables["credentials"] = new JArray(remainingRecords);

                SetData(null, originalRecords.Count - remainingRecords.Count);

                return base.NonQuery(command);
            }
        }

        private class AddSharedSecretProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AddSharedSecretProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var secret = new JObject();
                secret["identity"] = GetParameterValue(command, "identity", "");
                secret["name"] = GetParameterValue(command, "name", "");
                secret["secret"] = GetParameterValue(command, "secret", "");
                secret["purposes"] = GetParameterValue(command, "purposes", "");
                _tables["secrets"].Add(secret);

                SetData(_tables["secrets"], null, o => o == secret);
                return base.Query(command);
            }
        }

        private class GetSharedSecretProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public GetSharedSecretProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var secret = GetParameterValue(command, "secret", "");
                SetData(_tables["secrets"], null, o => string.Equals(o["secret"].ToString(), secret, StringComparison.Ordinal));
                return base.Query(command);
            }
        }

        private class DeleteSharedSecretProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public DeleteSharedSecretProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                var secret = GetParameterValue(command, "secret", "");

                var originalRecords = _tables["secrets"].Values<JObject>().ToList();
                var remainingRecords = originalRecords.Where(r => !string.Equals(r["secret"].Value<string>(), secret, StringComparison.Ordinal)).ToList();
                _tables["secrets"] = new JArray(remainingRecords);

                SetData(null, originalRecords.Count - remainingRecords.Count);

                return base.NonQuery(command);
            }
        }

        private class GetIdentitySharedSecretsProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public GetIdentitySharedSecretsProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override IEnumerable<IMockedResultSet> Query(ICommand command)
            {
                var identity = GetParameterValue(command, "identity", "");
                SetData(_tables["secrets"], null, o => string.Equals(o["identity"].ToString(), identity, StringComparison.Ordinal));
                return base.Query(command);
            }
        }

        private class AuthenticateSuccessProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AuthenticateSuccessProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                return 1;
            }
        }

        private class AuthenticateFailProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AuthenticateFailProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                var parameters = command.GetParameters();
                if (parameters == null) return 0;

                var parameter = parameters.FirstOrDefault(p => string.Equals(p.Name, "fail_count", StringComparison.InvariantCultureIgnoreCase));
                if (parameter == null) return 0;

                parameter.Value = 1;
                return 1;
            }
        }

        #endregion
    }
}
