﻿# OWIN Framework Facilities Sample Web Site

## Reading the source code

My recommended reading order is this

`Package.cs` defines all of the interfaces implemented in this application, and all of the interfaces that it depends on from other packages.

'packages.config' lists all the NuGet packages that this application needs to build and run. Some of these 
packages implement ths interfaces that this application directly depends on, others provide OWIN hosting, OWIN middleware or provide something
that one of the other packages needs.

'Startup.cs' configures everything. It specifies how IOC is set up, where the application gets its configuration
from and how the middleware components are chained into the OWIN pipeline. You can change everything about how the
application is wired up by changing just this file. All other classes depend only on the interface implementations
that are injected into there constructors by IOC.

The `Middleware` folder contains all of the application specific OWIN middleware components.

The `assets` folder contains static files that are served without any security

The 'pages' folder contains static html files that require the user to create an account and login to access.

## Running this sample web site

To try this sample on your local machine follow these steps:

1. If you have not installed IIS already then install it now.

2. If you do not have MySQL installed then install it now.

3. Run the MySQL script in OwinFramework.Facilities.IdentityStore.Prius\SqlScripts\MySql.sql to create an identity store database.

4. Edit the config.json file and modify the connection string to one that will connect the MySQL database you just created.

5. In the IIS Services Manager create a new web site with the host name `sample.facilities.owin.local` and the physical path set 
to the folder containing this readme file. Make sure the AppPool is set to .Net 4 Integrated mode.

6. Make sure that IIS has permission to access this location in your file system.

7. Edit your `hosts` file on your computer to include this line `27.0.0.1 sample.facilities.owin.local`. Note that the `hosts`
file is usually in `C:\Windows\System32\drivers\etc'

8. In Visual Studio, right click the 'SampleWebSite' project and choose `Debug|Start new instance`.

That's it. Now try some of these URLs in your browser:

http://sample.facilities.owin.local/config/assets
http://sample.facilities.owin.local/config/pages
http://sample.facilities.owin.local/config/defaultdocument
http://sample.facilities.owin.local/
http://sample.facilities.owin.local/home.html
http://sample.facilities.owin.local/blah.html
http://sample.facilities.owin.local/calculator.html
http://sample.facilities.owin.local/api/add?a=12&b=65

## What this site does

This sample site contains some static html pages and some static assets (like css and images).

This site requires the user to create an account and sign in when they access any any page except the home page. The
home page can be viewed anonymously and contains the register/login UI.

This site does not check user identity when serving static files like css and images so these are served as
fast as possible. Note that frameworks like ASP.NET establish session for each request, and the session 
server does not allow multiple requests to own the sesion at the same time which effectively serializes 
these requests. We can avoid these problems using OWIN framework by configuring different OWIN pipelines
for different types of request so that each request is processed with as few resources and as little blocking
as possible.

This site contains an API that is called from Javascript. The API has a different security scheme than the html pages.

## Why is this sample site here

Just as a picture tells a thousand words, a working example beats documentation every time. You can copy this whole
project as a starting point for your own application, or modify the code to see what happens. Note that if you use this
as a starting point for your application, you should remove references to other projects in this solution and add the
NuGet packages instead.
