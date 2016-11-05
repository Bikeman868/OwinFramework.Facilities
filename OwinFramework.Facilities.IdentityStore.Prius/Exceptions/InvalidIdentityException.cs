using System;

namespace OwinFramework.Facilities.IdentityStore.Prius.Exceptions
{
    public class InvalidIdentityException: IdentityStoreException
    {
        public InvalidIdentityException(){ }
        public InvalidIdentityException(string message):base(message) { }
        public InvalidIdentityException(string message, Exception innerException) : base(message, innerException) { }
    }
}
