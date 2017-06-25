# OwinFramework.Facilities

The OWIN Framework defines facilities that are useful for middleware developers. Facilities are
just interface definitions in the OWIN Framework, this project contains concrete implementations
for some of those interfaces.

Note that these libraries are available as NuGet packages as follows:

| NuGet Package | Facility | Description |
|---------------|----------|-------------|
| Owin.Framework.Facilities.Cache.Local | Cache | Caches data in process memory. Mostly useful for testing and experimenting. |
| Owin.Framework.Facilities.TokenStore.Cache | Token store | Persists tokens using the configured cache facility |
| Owin.Framework.Facilities.TokenStore.Prius | Token store | Persists tokens to a database using the Prius ORM. Has token types with configurable business rules |
| Owin.Framework.Facilities.IdentityStore.Prius | Identity store | Uses the Prius ORM to store identity information in a database |
