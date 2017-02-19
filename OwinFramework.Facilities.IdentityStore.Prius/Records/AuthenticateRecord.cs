using System;
using Prius.Contracts.Attributes;

namespace OwinFramework.Facilities.IdentityStore.Prius.Records
{
    internal class AuthenticateRecord
    {
        [Mapping("authenticate_id")]
        public long AuthenticateId { get; set; }

        [Mapping("when")]
        public DateTime When { get; set; }

        [Mapping("identity_id")]
        public long IdentityId { get; set; }

        [Mapping("identity")]
        public string Identity { get; set; }

        [Mapping("purposes")]
        public string Purposes { get; set; }
        
        [Mapping("remember_me_token")]
        public string RememberMeToken { get; set; }

        [Mapping("authenticate_method")]
        public string AuthenticateMethod { get; set; }

        [Mapping("method_id")]
        public long? MethodId { get; set; }

        [Mapping("expires")]
        public DateTime? Expires { get; set; }
    }
}
