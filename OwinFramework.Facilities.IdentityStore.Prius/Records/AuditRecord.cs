using System;
using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class AuditRecord
    {
        [Mapping("audit_id")]
        public long AuditId { get; set; }

        [Mapping("when")]
        public DateTime When { get; set; }

        [Mapping("who_id")]
        public long WhoIdentityId { get; set; }

        [Mapping("who_identity")]
        public string WhoIdentity { get; set; }

        [Mapping("reason")]
        public string Reason { get; set; }

        [Mapping("identity_id")]
        public long IdentityId { get; set; }

        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("action")]
        public string Action { get; set; }
        
        [Mapping("original_value")]
        public string OriginalValue { get; set; }

        [Mapping("new_value")]
        public string NewValue { get; set; }
    }
}
