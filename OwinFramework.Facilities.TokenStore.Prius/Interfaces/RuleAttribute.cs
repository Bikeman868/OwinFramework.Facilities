using System;

namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RuleAttribute : Attribute
    {
        public string RuleName { get; set; }
        public Type ConfigType { get; set; }
    }
}