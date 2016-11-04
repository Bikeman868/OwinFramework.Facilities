# OWIN Cache TokenStore Facility

Add this package to your solution to provide the OwinFramework with the `ITokenStore` facility.

This implementation of `ITokenStore` stores tokens using the `ICache` facility. Select an implementation of `ICache` that
meets your needs in terms of sharing information across server farms, performance, scaleability and persistence.

This token store implements the following business rules:

* Only tokens that have been created are valid. Checking a random token string will always result in a 'not valid' response.
* Tokens are no longer valid after they have expired.
* Tokens are only valid for the purpose, identity and token type they were created with.
* If a token is created with no purpose then it is valid for any purpose.
* If a token is created with no identity than it is valid for all identities.
* It is not valid to create a token with no token type.
* All token types are treated identically.
* The token itself is case sensitive.
* The token type, purpose and identity are case insentitive.
* When tokens are deleted they are deleted from the cache and immediately become invalid

This token store can be configured. To do this register an implementation of `IConfiguration` using the IoC Modules package, then
create an object in your configuration data with the path `/OwinFramework/TokenStore.Cache` that contains the following properties:

`Lifetime` is a `TimeSpan` that specifies how long tokens live for. The cache will be asked to cache the tokens for this duration.
The default value for lifetime is 1 hour.

`CachePrefix` is a prefix that is added to the front of the token to create a unique location in cache where the token is stored.
The default prefix is `/tokens/`.
