using System;

namespace OwinFramework.Facilities.IdentityStore.Prius.Exceptions
{
    public class InvalidUserNameException: IdentityStoreException
    {
        public InvalidUserNameException(){ }
        public InvalidUserNameException(string message):base(message) { }
        public InvalidUserNameException(string message, Exception innerException) : base(message, innerException) { }
    }
}
