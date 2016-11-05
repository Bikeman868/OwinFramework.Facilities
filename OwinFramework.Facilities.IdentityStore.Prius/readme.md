# OWIN Identity Store Facility

Add this package to your solution to provide the OwinFramework with the `IIdentityStore` facility.

This implementation of `IIdentityStore` stores user identitifcation information in a database
using the Prius ORM. Prius can connect to a number of database back-ends including SqlServer,
MySQL and Postgresql. You will need to set up a database and configure Prius to connect to it
in order to use this facility.
