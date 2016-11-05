using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class IdentityRecord
    {
        [Mapping("identity")]
        public string Identity { get; set; }
    }
}
