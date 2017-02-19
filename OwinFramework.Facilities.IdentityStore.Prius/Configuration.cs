using System;

namespace OwinFramework.Facilities.IdentityStore.Prius
{
    [Serializable]
    internal class Configuration
    {
        public string PriusRepositoryName { get; set; }
        public string IdentityUrnNamespace { get; set; }

        public int MinimumUserNameLength { get; set; }
        public int MaximumUserNameLength { get; set; }
        public string UserNameRegex { get; set; }

        public int MinimumPasswordLength { get; set; }
        public int MaximumPasswordLength { get; set; }
        public string PasswordRegex { get; set; }

        public TimeSpan LockDuration { get; set; }
        public int FailedLoginsToLock { get; set; }
        public TimeSpan RememberMeFor { get; set; }
        
        public Configuration()
        {
            PriusRepositoryName = "IdentityStore";
            IdentityUrnNamespace = "identity";

            MinimumUserNameLength = 3;
            MaximumUserNameLength = 80;
            UserNameRegex = @"^[a-zA-Z0-9@_.-]*$";

            MinimumPasswordLength = 8;
            MaximumPasswordLength = 160;
            PasswordRegex = @".*";

            LockDuration = TimeSpan.FromHours(4);
            FailedLoginsToLock = 5;
            RememberMeFor = TimeSpan.FromDays(90);
        }
    }
}
