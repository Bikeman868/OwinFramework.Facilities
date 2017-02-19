using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class IdentityRecord
    {
        [Mapping("identity_id")]
        public long IdentityId { get; set; }
        
        [Mapping("identity")]
        public string Identity { get; set; }
    }
}
