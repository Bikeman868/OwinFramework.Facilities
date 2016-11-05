﻿using System.Collections.Generic;
using OwinFramework.InterfacesV1.Facilities;

namespace OwinFramework.Facilities.IdentityStore.Prius
{
    internal class AuthenticationResult : IAuthenticationResult
    {
        public string Identity { get; set; }
        public IList<string> Purposes { get; set; }
        public AuthenticationStatus Status { get; set; }
    }
}
