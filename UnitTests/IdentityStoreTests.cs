using System;
using System.Collections.Generic;
using System.Linq;
using Moq.Modules;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OwinFramework.Builder;
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
    public class IdentityStoreTests: TestBase
    {
        private IIdentityStore _identityStore;

        [SetUp]
        public void Setup()
        {
            var identityTable = new JArray
            {
                JObject.Parse("{identityId:1,identity:\"urn:user:1\"}")
            };

            var mockedRepository = new MockedRepository("IdentityStore");
            mockedRepository.Add("sp_GetAllIdentities", identityTable);
            mockedRepository.Add("sp_AddIdentity", new AddIdentityProcedure(identityTable));

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

        #region Stored procedure mocks

        private class AddIdentityProcedure : MockedStoredProcedure
        {
            private readonly JArray _identityTable;

            public AddIdentityProcedure(JArray identityTable)
            {
                _identityTable = identityTable;
            }

            public override long NonQuery(ICommand command)
            {
                var identity = GetParameterValue(command, "identity", "");
                Assert.IsTrue(!string.IsNullOrEmpty(identity));

                var identityRecords = _identityTable.Children<JObject>();
                var maxIdentityId = identityRecords.Any() ? identityRecords.Max(o => (int)o["identityId"]) : 0;

                var newIdentity = new JObject();
                newIdentity["identityId"] = maxIdentityId + 1;
                newIdentity["identity"] = identity;

                _identityTable.Add(newIdentity);

                SetData(null, 1);

                return base.NonQuery(command);
            }
        }

        #endregion
    }
}
