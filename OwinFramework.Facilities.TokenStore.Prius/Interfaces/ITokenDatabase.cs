using OwinFramework.Facilities.TokenStore.Prius.Records;

namespace OwinFramework.Facilities.TokenStore.Prius.Interfaces
{
    internal interface ITokenDatabase
    {
        TokenRecord AddToken(
            string token, 
            string tokenType, 
            string state);

        TokenRecord GetTokenById(long tokenId);
        TokenRecord GetToken(string token);

        bool UpdateToken(long tokenId, string state);

        bool DeleteToken(string token);
        bool DeleteToken(long tokenId);

        /// <summary>
        /// Deletes old data from the database
        /// </summary>
        long Clean();
    }
}