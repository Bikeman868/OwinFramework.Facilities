using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Rules;
using OwinFramework.InterfacesV1.Facilities;
using Prius.Contracts.Interfaces;
using Urchin.Client.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.Tokens
{
    internal class TokenDatabase
    {
        private readonly ITokenFactory _tokenFactory;
        private readonly IContextFactory _contextFactory;
        private readonly ICommandFactory _commandFactory;

        public TokenDatabase(
            ITokenFactory tokenFactory,
            IContextFactory contextFactory,
            ICommandFactory commandFactory)
        {
            _tokenFactory = tokenFactory;
            _contextFactory = contextFactory;
            _commandFactory = commandFactory;
        }

        public string CreateToken(string token, string purpose, string identity)
        {
            token = token.ToLower();

            ITokenType tokenType;
            if (_tokenTypes.TryGetValue(type, out tokenType))
            {
                var key = Guid.NewGuid().ToShortString();
                var token = new Token
                    {
                        Type = type,
                        Validators = tokenType.GetValidators(purposes)
                    };
                lock (_tokens) _tokens.Add(key, token);
                return key;
            }

            return null;
        }

        public bool DeleteToken(string token)
        {
            lock (_tokens)
            {
                try
                {
                    return _tokens.Remove(token);
                }
                catch
                {
                    return false;
                }
            }
        }

        public TokenStatus CheckToken(string tokenKey, string type, string purpose)
        {
            Token token;
            lock(_tokens)
                if (!_tokens.TryGetValue(tokenKey, out token)) 
                    return TokenStatus.Deleted;

            if (token.Type != type.ToLower())
                return TokenStatus.NotAllowed;

            var result = CheckResult.Valid;
            foreach (var validator in token.Validators)
            {
                switch (validator.CheckIsValid(purpose))
                {
                    case CheckResult.TemporaryInvalid:
                        if (result == CheckResult.Valid)
                            result = CheckResult.TemporaryInvalid;
                        break;
                    case CheckResult.PermenantInvalid:
                        result = CheckResult.PermenantInvalid;
                        break;
                }
            }

            switch (result)
            {
                case CheckResult.Valid: 
                    return TokenStatus.Allowed;
                case CheckResult.TemporaryInvalid:
                    return TokenStatus.NotAllowed;
                case CheckResult.PermenantInvalid:
                    DeleteToken(tokenKey);
                    return TokenStatus.Deleted;
                default:
                    throw new Exception("Unknown token status");
            }
        }

        public float CleanupSeconds { get; private set; }
        public int CleanupCount { get; private set; }
        public Dictionary<string, int> TokenTypeCounts { get; private set; }
        
        public string GetTokensForPersistence()
        {
            var tokensToPersist = new List<TokenSettings>();
            
            lock (_tokens)
            {
                foreach (var token in _tokens)
                {
                    var expired = token.Value.Validators.Any(validator => validator.CheckIsExpired());

                    if (!expired)
                    {
                        var tokenSettings = new TokenSettings
                        {
                            Key = token.Key,
                            Type = token.Value.Type
                        };

                        var validatorSettings =
                            token.Value.Validators.Select(
                                v => new Tuple<string, string>(v.GetType().FullName.Split('+')[0], v.Serialize()))
                                .ToList();
                        tokenSettings.ValidatorSettings = JsonConvert.SerializeObject(validatorSettings);

                        tokensToPersist.Add(tokenSettings);
                    }
                }
            }

            return JsonConvert.SerializeObject(tokensToPersist);
        }

        public void RestoreTokens(string tokensFromCache)
        {
            lock (_tokens)
            {

                var cachedTokens = JsonConvert.DeserializeObject<List<TokenSettings>>(tokensFromCache);

                foreach (var cachedToken in cachedTokens)
                {
                    Token foundToken;
                    if (_tokens.TryGetValue(cachedToken.Key, out foundToken))
                        continue;

                    ITokenType tokenType;
                    if (!_tokenTypes.TryGetValue(cachedToken.Type, out tokenType)) 
                        continue;

                    var validators = new List<ITokenValidator>();

                    var cachedValidators = JsonConvert.DeserializeObject<List<Tuple<string,string>>>(cachedToken.ValidatorSettings);
                    foreach (var cachedValidator in cachedValidators)
                    {
                        var className = cachedValidator.Item1;
                        //construct TokenRule types
                        var tokenRule = _dependencyResolver.Construct(ReflectionHelper.GetType(className)) as TokenRule;
                        if (tokenRule != null)
                        {
                            var validator = tokenRule.GetInstance();
                            validator.Hydrate(cachedValidator.Item2);
                            validators.Add(validator);
                        }
                        else
                        {
                            //construct purpose rule
                            var tokenValidatorRule = _dependencyResolver.Construct(ReflectionHelper.GetType(className)) as ITokenValidator;
                            var validator = new TokenPurposeRule();
                            validator.Hydrate(cachedValidator.Item2);
                        }
                    }

                    var token = new Token
                    {
                        Type = cachedToken.Type,
                        Validators = validators
                    };

                    var expired = token.Validators.Any(validator => validator.CheckIsExpired());
                    if (!expired)
                    {
                        _tokens.Add(cachedToken.Key, token);
                    }

                }
            }
        }

    }
}