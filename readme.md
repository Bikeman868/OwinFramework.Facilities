# OwinFramework.Facilities

The OWIN Framework defines facilities that are useful for middleware developers. Facilities are
just interface definitions in the OWIN Framework, this project contains concrete implementations
for some of those interfaces.

Note that these libraries are available as NuGet packages as follows:

## Owin.Framework.Facilities.Cache.Local
Caches data in process memory. Mostly useful for testing and experimenting.

## Owin.Framework.Facilities.TokenStore.Cache
Persists tokens using the configured cache facility.

## Owin.Framework.Facilities.TokenStore.Prius
Persists tokens to a database using the Prius ORM. Has token types with configurable business rules

## Owin.Framework.Facilities.IdentityStore.Prius
Uses the Prius ORM to store identity information in a database.
