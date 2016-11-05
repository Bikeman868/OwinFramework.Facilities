using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class SharedSecretRecord
    {
        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("name")]
        public string Name { get; set; }

        [Mapping("secret")]
        public string Secret { get; set; }

        [Mapping("purposes")]
        public string Purposes { get; set; }
    }
}
