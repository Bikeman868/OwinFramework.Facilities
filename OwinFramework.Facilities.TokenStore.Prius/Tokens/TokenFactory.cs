using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using OwinFramework.Facilities.TokenStore.Prius.DataContracts;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Rules;
using Prius.Contracts.Interfaces.External;
using Urchin.Client.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Tokens
{
    internal class TokenFactory: ITokenFactory
    {
        private IDisposable _tokenTypesConfig;
        private readonly IFactory _factory;
        private Dictionary<string, ITokenType> _tokenTypes;

        public TokenFactory(
            IFactory factory,
            IConfigurationStore configurationStore)
        {
            _factory = factory;

            _tokenTypesConfig = configurationStore.Register(
                "/owinFramework/facility/tokenStore.Prius/tokenTypes", 
                TokenTypesChanged, 
                new List<TokenTypeConfiguration>());
        }

        public Token CreateToken(string type, string identity, IEnumerable<string> purposes)
        {
            type = type.ToLower();

            ITokenType tokenType;
            if (_tokenTypes.TryGetValue(type, out tokenType))
            {
                return new Token
                {
                    Type = type,
                    Validators = tokenType.GetValidators(identity, purposes)
                };
            }

            return null;
        }

        public Token CreateToken(Records.TokenRecord tokenRecord)
        {
            var purposeList = string.IsNullOrEmpty(tokenRecord.Purposes)
                ? Enumerable.Empty<string>()
                : tokenRecord.Purposes.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p));
            return CreateToken(tokenRecord.TokenType, tokenRecord.Identity, purposeList);
        }

        public IList<string> TokenTypes 
        { 
            get 
            {
                lock (_tokenTypes)
                    return _tokenTypes.Keys.ToList();
            } 
        }

        private void TokenTypesChanged(List<TokenTypeConfiguration> tokenTypes)
        {
            //#if DEBUG
            //            var endTime = DateTime.UtcNow.AddSeconds(30);
            //            while (DateTime.UtcNow < endTime && !System.Diagnostics.Debugger.IsAttached)
            //                Thread.Sleep(10);
            //            if (System.Diagnostics.Debugger.IsAttached)
            //                System.Diagnostics.Debugger.Break();
            //#endif
            
            var dictionary = new Dictionary<string, ITokenType>();
            var ruleTypes = DiscoverRuleTypes();

            foreach (var tokenTypeConfig in tokenTypes)
            {
                var tokenType = new TokenType();
                foreach (var ruleConfig in tokenTypeConfig.Rules)
                {
                    var ruleType = ruleTypes[ruleConfig.Type.ToLower()];
                    var config = JsonConvert.DeserializeObject(ruleConfig.Json, ruleType.ConfigType);

                    var rule = (TokenRule)_factory.Create(ruleType.RuleType);
                    rule.Initialize(config);

                    tokenType.AddValidation(rule);
                }
                dictionary.Add(tokenTypeConfig.Name.ToLower(), tokenType);
            }

            _tokenTypes = dictionary;
        }

        private Dictionary<string, RuleTypeDefinition> DiscoverRuleTypes()
        {
            var ruleTypes = new Dictionary<string, RuleTypeDefinition>();
            foreach (var type in GetTypes(t => typeof(ITokenValidationRule).IsAssignableFrom(t)))
            {
                var ruleAttribute = GetAttributes<RuleAttribute>(type).FirstOrDefault();
                if (ruleAttribute != null)
                {
                    var definition = new RuleTypeDefinition
                    {
                        Name = ruleAttribute.RuleName,
                        RuleType = type,
                        ConfigType = ruleAttribute.ConfigType
                    };
                    ruleTypes.Add(definition.Name.ToLower(), definition);
                }
            }
            return ruleTypes;
        }

        private static IEnumerable<Type> GetTypes(Func<Type, bool> predicate)
        {
            return GetTypes(AppDomain.CurrentDomain.GetAssemblies(), predicate);
        }

        private static IEnumerable<T> GetAttributes<T>(Type type, bool inherit = false)
        {
            return type.GetCustomAttributes(inherit).OfType<T>();
        }

        private static IEnumerable<Type> GetTypes(IEnumerable<Assembly> assemblies, Func<Type, bool> predicate)
        {
            return assemblies.SelectMany(
                a =>
                {
                    try
                    {
                        return a.GetTypes().Where(predicate);
                    }
                    catch
                    {
                        return new Type[] { };
                    }
                });
        }

        private class RuleTypeDefinition
        {
            public string Name { get; set; }
            public Type RuleType { get; set; }
            public Type ConfigType { get; set; }
        }
    }
}