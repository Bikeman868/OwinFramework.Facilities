using System;

namespace OwinFramework.Facilities.IdentityStore.Prius.Exceptions
{
    public class InvalidPasswordException: IdentityStoreException
    {
        public InvalidPasswordException(){ }
        public InvalidPasswordException(string message):base(message) { }
        public InvalidPasswordException(string message, Exception innerException) : base(message, innerException) { }
    }
}
