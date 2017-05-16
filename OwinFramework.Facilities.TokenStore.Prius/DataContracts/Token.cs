using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OwinFramework.Facilities.TokenStore.Prius.Interfaces;

namespace OwinFramework.Facilities.TokenStore.Prius.DataContracts
{
    /// <summary>
    /// This is a token from the database that is fully hydrated with
    /// instancecs of token validators attached and ready to handle
    /// token validation operations.
    /// </summary>
    internal class Token
    {
        public string Type;
        public IList<ITokenValidator> Validators;

        public JObject Serialize()
        {
            var json = new JObject();
            json.Add("type", Type);

            if (Validators != null)
            {
                foreach (var validator in Validators)
                {
                    var validatorStatus = validator.Serialize();
                    if (validatorStatus != null)
                        json.Add(validator.Name, validatorStatus);
                }
            }
            return json;
        }

        public void Hydrate(JObject jason)
        {

        }
    }
}
