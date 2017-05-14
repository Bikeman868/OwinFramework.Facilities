# OWIN Prius TokenStore Facility

Add this package to your solution to provide the OwinFramework with the `ITokenStore` facility.

This implementation of `ITokenStore` stores tokens using the Prius ORM. Every time a token is updated
it is read from the database, updated and written back. This means that there is potentially a lot of
load on the database, but the token store can be distributed accross many machines that share the
same database. 

This implementation is good for tokens that are long lived and checked infrequently - for example
password reset tokens. This implementation is not good for tokens that are checked very frequently
(like for example a token that rate limits callers of an API to a few hundred calls per second) because
each time the token is checked it is read from the database, updated and written back. This implementation
does provide rate limiting tokens, and they are great for things like limiting how often a user can change
their profile image.

This token store implements the following business rules:

* Only tokens that have been created are valid. Checking a random token string will always result in a 'not valid' response.
* Tokens are no longer valid after they have expired.
* Tokens are only valid for the purpose, identity and token type they were created with.
* If a token is created with no purpose then it is valid for any purpose.
* If a token is created with no identity than it is valid for all identities.
* It is not valid to create a token with no token type.
* This middleware must be configured with the business rules for the token types you will use in your application.
* The token itself and the token identity are case sensitive. The token type and purpose are case insentitive.
* When tokens are deleted they are deleted from the cache and immediately become invalid.

To be useful this token store must be configured with token types. To do this register an implementation of `IConfiguration` 
using the IoC Modules package, then create an object in your configuration data with the path `/owinFramework/facility/tokenStore.Prius` 
that contains the following properties:

`TokenTypes` defines the rules that are applied to each type of token. When creating a token the `tokenType` property
must be one of the token types defined in this configuration property.

Token types are defined by a token type name and a list of rules to apply. If you don't define any
rules then the token will only implement the general business rules defined above. This package
includes some of the most common rules, you can also add application specific rules and this package
will find your rules at startup through reflection. The built-in rules are:

`Expiry` specifies that the token has a limited lifetime and should no longer be valid after this much time has elapsed.

`Rate` specifies that the token can not be used more than the specified number of times within a defined time window.
For example the token can not be used more than 2 times in any 24 hour period.

`UseCount` specifies the total number of times that the token can be used before it becomes invalid.

For example a password reset token might be configured to expire in 7 days and can only be used once. This is how to
configure that in Urchin.

   {
     "owinFramework": {
       "facility": {
         "tokenStore.Prius": {
           "tokenTypes": [
             { 
               "name": "PasswordReset", 
               "rules": [
                 "type": "Expiry", "config": "{ \"expiryTime\": 7 }",
                 "type": "UseCount", "config": "{ \"maxUseCount\": 1 }"
               ]
             }
           ]
         }
       }
     }
   }
