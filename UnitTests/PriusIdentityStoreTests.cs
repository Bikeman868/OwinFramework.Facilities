using System;
using System.Collections.Generic;
using System.Linq;
using Moq.Modules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OwinFramework.Facilities.IdentityStore.Prius;
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
        private IIdentityStore _identityStore;

        [SetUp]
        public void Setup()
        {
            var tables = new Dictionary<string, JArray>();

            tables.Add("identity", new JArray());
            tables.Add("credentials", new JArray());

            var mockedRepository = new MockedRepository("IdentityStore");
            mockedRepository.Add("sp_GetAllIdentities", tables["identity"]);
            mockedRepository.Add("sp_AddIdentity", new AddIdentityProcedure(tables));
            mockedRepository.Add("sp_AddCredentials", new AddCredentialsProcedure(tables));
            mockedRepository.Add("sp_DeleteIdentityCredentials", new DeleteIdentityCredentialsProcedure(tables));
            mockedRepository.Add("sp_GetIdentity", new GetIdentityProcedure(tables));
            mockedRepository.Add("sp_GetUserNameCredential", new GetUserNameCredentialProcedure(tables));

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

        }

        [Test]
        public void Should_not_allow_invalid_usernames()
        {

        }

        [Test]
        public void Should_not_allow_duplicate_usernames()
        {

        }

        #region Stored procedure mocks

        private class AddIdentityProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AddIdentityProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                var newIdentity = new JObject();
                newIdentity["identity"] = GetParameterValue(command, "identity", "");
                _tables["identity"].Add(newIdentity);

                SetData(null, 1);
                return base.NonQuery(command);
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

        private class AddCredentialsProcedure : MockedStoredProcedure
        {
            private readonly Dictionary<string, JArray> _tables;

            public AddCredentialsProcedure(Dictionary<string, JArray> tables)
            {
                _tables = tables;
            }

            public override long NonQuery(ICommand command)
            {
                var credential = new JObject();
                credential["identity"] = GetParameterValue(command, "identity", "");
                credential["userName"] = GetParameterValue(command, "userName", "");
                credential["purposes"] = GetParameterValue(command, "purposes", "");
                credential["version"] = GetParameterValue(command, "version", 1);
                credential["hash"] = GetParameterValue<byte[]>(command, "hash", null);
                credential["salt"] = GetParameterValue<byte[]>(command, "salt", null);
                _tables["credentials"].Add(credential);

                SetData(null, 1);

                return base.NonQuery(command);
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

        #endregion
    }
}
