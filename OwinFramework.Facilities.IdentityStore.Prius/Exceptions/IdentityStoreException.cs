using System;

namespace OwinFramework.Facilities.IdentityStore.Prius.Exceptions
{
    public class IdentityStoreException: ApplicationException
    {
        public IdentityStoreException(){ }
        public IdentityStoreException(string message):base(message) { }
        public IdentityStoreException(string message, Exception innerException) : base(message, innerException) { }
    }
}
