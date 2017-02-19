using System;
using Prius.Contracts.Interfaces;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class CredentialRecord: IDataContract<CredentialRecord>
    {
        public long CredentialId { get; set; }
        public long IdentityId { get; set; }
        public string Identity { get; set; }
        public string UserName { get; set; }
        public string Purposes { get; set; }
        public int Version { get; set; }
        public byte[] Salt { get; set; }
        public byte[] Hash { get; set; }
        public int FailCount { get; set; }
        public DateTime? Locked { get; set; }

        public void AddMappings(ITypeDefinition<CredentialRecord> typeDefinition, string dataSetName)
        {
            typeDefinition.AddField("credential_id", c => c.CredentialId, 0);
            typeDefinition.AddField("identity_id", c => c.IdentityId, 0);
            typeDefinition.AddField("identity", c => c.Identity, string.Empty);
            typeDefinition.AddField("userName", c => c.UserName, string.Empty);
            typeDefinition.AddField("purposes", c => c.Purposes, string.Empty);
            typeDefinition.AddField("version", c => c.Version, 1);
            typeDefinition.AddField("hash", c => c.Hash, new byte[0]);
            typeDefinition.AddField("salt", c => c.Salt, new byte[0]);
            typeDefinition.AddField("fail_count", c => c.FailCount, 0);
            typeDefinition.AddField("locked", c => c.Locked, null);
        }

        public void SetCalculated(IDataReader dataReader, string dataSetName)
        {
        }
    }
}
