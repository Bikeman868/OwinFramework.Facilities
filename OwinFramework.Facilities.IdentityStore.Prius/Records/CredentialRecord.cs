using Prius.Contracts.Interfaces;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class CredentialRecord: IDataContract<CredentialRecord>
    {
        public string Identity { get; set; }
        public string UserName { get; set; }
        public string Purposes { get; set; }
        public int Version { get; set; }
        public byte[] Salt { get; set; }
        public byte[] Hash { get; set; }

        public void AddMappings(ITypeDefinition<CredentialRecord> typeDefinition, string dataSetName)
        {
            typeDefinition.AddField("identity", c => c.Identity, string.Empty);
            typeDefinition.AddField("userName", c => c.UserName, string.Empty);
            typeDefinition.AddField("purposes", c => c.Purposes, string.Empty);
            typeDefinition.AddField("version", c => c.Version, 1);
            typeDefinition.AddField("hash", c => c.Hash, new byte[0]);
            typeDefinition.AddField("salt", c => c.Salt, new byte[0]);
        }

        public void SetCalculated(IDataReader dataReader, string dataSetName)
        {
        }
    }
}
