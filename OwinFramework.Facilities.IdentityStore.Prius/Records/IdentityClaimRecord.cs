using OwinFramework.InterfacesV1.Middleware;
using Prius.Contracts.Attributes;
using Prius.Contracts.Interfaces;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class IdentityClaimRecord : IIdentityClaim, IDataContract<IdentityClaimRecord>
    {
        [Mapping("claim_id")]
        public long ClaimId { get; set; }

        [Mapping("identity_id")]
        public long IdentityId { get; set; }
        
        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("name")]
        public string Name { get; set; }

        [Mapping("value")]
        public string Value { get; set; }

        public ClaimStatus Status { get; set; }

        public void AddMappings(ITypeDefinition<IdentityClaimRecord> typeDefinition, string dataSetName)
        {
            typeDefinition.AddField("status", (r, s) => r.Status = (ClaimStatus)s, (int)ClaimStatus.Unknown);
        }

        public void SetCalculated(IDataReader dataReader, string dataSetName)
        {
        }
    }
}
