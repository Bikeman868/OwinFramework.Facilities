using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class CredentialRecord
    {
        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("userName")]
        public string UserName { get; set; }

        [Mapping("purposes")]
        public string Purposes { get; set; }

        [Mapping("version")]
        public int Version { get; set; }

        [Mapping("salt")]
        public byte[] Salt { get; set; }

        [Mapping("hash")]
        public byte[] Hash { get; set; }
    }
}
