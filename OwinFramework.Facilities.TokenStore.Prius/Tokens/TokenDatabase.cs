using System;
using System.Data;
using System.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;
using OwinFramework.Facilities.TokenStore.Prius.Records;
using OwinFramework.Interfaces.Builder;
using Prius.Contracts.Interfaces;
using Urchin.Client.Interfaces;
using ParameterDirection = Prius.Contracts.Attributes.ParameterDirection;

namespace OwinFramework.Facilities.TokenStore.Prius.Tokens
{
    internal class TokenDatabase : ITokenDatabase
    {
        private readonly IContextFactory _contextFactory;
        private readonly ICommandFactory _commandFactory;
        private readonly IDisposable _configurationChangeRegistration;

        private string _repositoryName;

        public TokenDatabase(
            IContextFactory contextFactory,
            ICommandFactory commandFactory,
            IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _commandFactory = commandFactory;

            _configurationChangeRegistration = configuration.Register(
                "/owinFramework/facility/tokenStore.Prius/PriusRepositoryName",
                r => _repositoryName = r,
                "TokenStore");
        }

        public TokenRecord AddToken(
            string token, 
            string tokenType, 
            string state)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_AddToken"))
                {
                    command.AddParameter("token", token);
                    command.AddParameter("type", tokenType);
                    command.AddParameter("state", state);
                    using (var records = context.ExecuteEnumerable<TokenRecord>(command))
                    {
                        return records.FirstOrDefault();
                    }
                }
            }
        }
    
        public TokenRecord GetTokenById(long tokenId)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetTokenById"))
                {
                    command.AddParameter("token_id", tokenId);
                    using (var records = context.ExecuteEnumerable<TokenRecord>(command))
                    {
                        return records.FirstOrDefault();
                    }
                }
            }
        }

        public TokenRecord GetToken(string token)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_GetTokenById"))
                {
                    command.AddParameter("token", token);
                    using (var records = context.ExecuteEnumerable<TokenRecord>(command))
                    {
                        return records.FirstOrDefault();
                    }
                }
            }
        }

        public bool UpdateToken(long tokenId, string state)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_UpdateTokenState"))
                {
                    command.AddParameter("token_id", tokenId);
                    command.AddParameter("state", state);
                    return context.ExecuteNonQuery(command) == 1;
                }
            }
        }

        public bool DeleteToken(string token)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_DeleteToken"))
                {
                    command.AddParameter<long?>("tokenId", null);
                    command.AddParameter("token", token);
                    return context.ExecuteNonQuery(command) == 1;
                }
            }
        }

        public bool DeleteToken(long tokenId)
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_DeleteToken"))
                {
                    command.AddParameter("tokenId", tokenId);
                    command.AddParameter<string>("token", null);
                    return context.ExecuteNonQuery(command) == 1;
                }
            }
        }

        /// <summary>
        /// Deletes old data from the database
        /// </summary>
        public long Clean()
        {
            using (var context = _contextFactory.Create(_repositoryName))
            {
                using (var command = _commandFactory.CreateStoredProcedure("sp_Clean"))
                {
                    return context.ExecuteNonQuery(command);
                }
            }
        }
    }
}