# GeoPathsx
## Web Server for [GeoTrail Project](https://github.com/bobbyray/geotrail)
The web server code is written in C# and implements a Windows Communification Foundation (WCF) Service running under the Microsoft Internet Information Services (IIS). MySql is used for the database that stores data by user id for the geo paths. The user id is the Facebook user id, which a user provides by authentication via Facebook. The code is currently running in this web hosting enviroment: 
* Microsoft .Net 4
* Microsoft IIS 7
* MySql 5.5
### RESTful API for Web Server Accessing Database
A client accesses the web server via a RESTful api. The GeoTrail mobile app or the GpxPaths.html page are the expected clients. The https protocol is used to exhange data between a client and web server.

In file js/GeoPathsApi2.js, see [properties of wigo_ws_GeoPathsRESTfulApi object](../master/js/GeoPathsApi2.js) for a description of the api that a client uses.

The file App_Code/Service.cs has the [implementation of the server side code](../master/App_Code/Service.cs) for the api.
## Web Page GpxPaths.html for Uploading Gpx Files to Web Server Database
Rather than uploading to the web server database a path defined by using the GeoTrail mobile app, one may want to upload a gpx file for a path from some other source. For example the site [www.hillmap.com](http://www.hillmap.com) lets you draw a path and down load a gpx file for the path. Other sites may have paths (trails) that can be download as gpx files. The [GpxPaths.html page](../master/GpxPaths.html) provides a means for uploading a gpx file. View the [live GpxPaths page](https://www.wigo.ws/geopathsx/gpxpaths.html).

Some sites may download KML files (Google format). Currently a KML file cannot be uploaded by the GeoPaths.html page. However, sites that convert a KML file to GPX for free can be found.
## Customization of Secret Values and Web Server Domain
* [Web.Example.config](../master/Web.Example.config) needs database connection passwords set and file needs to be renamed Web.config
* [Private/appSettings.Example.config](../master/Private/appSettings.Example.config) needs Facebook app values set and file needs to be renamed appSettings.config.  
The Facebook app id and secret are set by a developer when logged into one's Facebook account. The developer needs to set up an app in Facebook, using the Facebook Developer Console, that is allowed to use Facebook authentication. The GeoTrail mobile app and the GpxPaths.html use the same Facebook app id for user authentication via Facebook.
* Domain and URI for Web Server in [js/GeoPathsApi2.js](../master/js/GeoPathsApi2.js).  
Search for: *base = new wigo_ws_Ajax(*.  
Set the constructor to specify your_domain in the URI as indicated below:  
`var base = new wigo_ws_Ajax("https://your_domain/geopathsx/Service.svc/"); // Remote host`  
The default is:  
`var base = new wigo_ws_Ajax("https://www.wigo.ws/geopathsx/Service.svc/"); // Remote host`
* The MySql database needs to be initially created.  
[MySqlInitialTables/MySqlInitialTable.sql](../master/MySqlInitialTables/MySqlInitialTable.sql) is a file that can be imported into MySql that defines the database and two tables, which are empty.  
**Change the database name of some_geopath in the file to match the database name used at the hosting site.**
However, do **not** change the table name of geopath in the file.
## Deployment to Remote Host
Deploy the directory structure of this repository to a remote host for which IIS is configured to have a virtual directory named geopathsx corresponding to root of this repository structure.
## Local Debugging
Since it likely not possible to debug at the remote server, one can debug on a local machine. Currently I am using Microsoft Visuals Studio 2013 Express for Web to debug. However, Visual Studio 2013 Express for Web is no longer available and is replaced by Visual Studio Community Edition.
### Local Debugging, Preliminaries To Do Once
* Install Microsoft Visual Studio Community Edition  
If not using Visual Studio 2013 Express for Web, you can install the [Microsoft Visual Studio Community Edition](https://www.visualstudio.com/vs/community/), which is the replacement for Visual Studio Express 203 for Web.
Since the following description was written for using Visual Studio 2013 Express for Web, it could be outdated for later versions.  
The Visual Studio sets up an IIS Express automatically.
Locally download and extract this respository. Using Visual Studio, open the geopathsx folder as a website.
You should be ready to go, except you may need to install MySql database locally described next.
* Install MySql WorkBench Comnunity Edition if Needed  
See [Instructions for installing MySql WorkBench](http://dev.mysql.com/doc/refman/5.7/en/windows-installation.html). The following description pertains to using the the MySql Installer Method, selecting mysql-installer-web-community-5.7.11.0.msi for the installer.  
Notes for installing, which obviously may be outdated for a different installer version, follow:
  <pre>
  I did not sign up for an Oracle account, I just downloaded.
  
  Installer file downloaded to downloads folder.
  
  Right-click on file | Install to start (MySql Installer 1.4).
  
  I clicked Yes to install available upgrade.
    Requirements check indicated requirements not met for:
    MySql for Excel 1.3f.6
    MySql for Visual Studio 1.2.6
    Connector for Python (3.4) 2.1.3
    I proceeded without updating for these.
  
  Ready to download:
    MySql Server 5.7.11
    MySql Workbench 6.3.6
    MySql Notifier 1.1.6
    MySql Fabric 1.5.6 &amp; utilities
    Connector ODBC 5.3.4
    Connector/C++ 1.1.7
    Connector/J 5.1.38
    Connector/NET 6.9.8
    MySql Documentation 5.7.11
    Samples &amp; Examples 5.7.11
    All downloaded and installed automatically (after clicking Execute).
    The status is shown during the process.
  
  Next installer goes through configuration of MySql Server.
    Config Type: Development Machine
    Mark TCP/IP
    Port Number: 3306 (default)
    Clear Open Firewall port for network access check box
    Pipe Name: MYSQL
    Memory Name: MYSQL
    Other check boxes are unmarked.
  
  Next installer goes through Accounts and Roles
    Root Account Password: *******
      NOTE: password must match that used in the database connection string 
            in passkeeper/web.config on the IIS server and the database password 
            set in the C# HtmlPassKeeperMySqlBackup app.
    MySql User Accounts: did not add any.
  
  Next installer goes through Window Services
    Mark MySql Server as a Windows Service
    Mark Start the MySql Server at System Startup
    Mark Standard Account type.
    The above are the defaults.
  
  Next Apply Server Configuration
    Click Execute.
    Status shows progress of configuring.
    Click Finish when done.
    Next Check that Connect to Service
    User: root
    Password: *******
      NOTE: Use root password set above.
    (The above are set by default.)
    Click Check.  Connection successful.
  
  Next installer shows Apply Server Configuration (again)
    Looks like just checking configuration.
    Click Execute
    Successfully runs through Steps.
    Click Finish
  
  Next installer shows Product Configuration
    MySql Server 5.7.11 – complete.
    Samples &amp; Examples 5.7.11 – complete.
    Seems to be completed immediately, probably already done.
    Click Next
  
  Next installer shows Installation Complete
    Installation procedure completed.
    Mark Start MySql Workbench after Setup
    Click Copy Log to Clipboard
    Click Finish
  
  Installer ends and Workbench opens.
  </pre>  
* Initialize MsSql Database and Table  
In MySql Workbench prepare the database schema:</p>
  * **NOTE: Do not import database if it already exists because it will be over-written with empty tables.**  
  * If geopath database does not exist, import it from [MySqlInitialTables/MySqlInitialTable.sql](../master/MySqlInitialTables/MySqlInitialTable.sql).  
  Customize the MySqlInitialTable.sql file to use the database name you want instead of some_geopath.  
  Note: Do **not** change the table name of geopath in the file.
  * The database name and password must be set in the local web.config file to match.  
### Local Debugging, Temporary Changes
* Set URI for web server in [js/GeoPathsApi2.js](../master/js/GeoPathsApi2.js).  
Search for "base = new wigo_ws_Ajax(". Set the constructor arg to the URI for the web server's Service.svc file.  
var base = new wigo_ws_Ajax("Service.svc/");  // Should work for local debug.  
Remember to comment out statement setting URI for remote server.
* May need to disable redirection for https protocol in [js/GpxPaths2.js](../master/js/GpxPaths2.js).  
Near bottom of file, comment out the redirection for https if local server does not have a https certificate.  
Note: GpxPaths2.js is javascript code for the GpxPaths.html page.
