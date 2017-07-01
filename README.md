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
## Customization of Secret Values
* [Web.Example.config](../master/Web.Example.config) needs database connection passwords set and file needs to be renamed Web.config
* [Private/appSettings.Example.config](../master/Private/appSettings.Example.config) needs Facebook app values set and file needs to be renamed appSettings.config.  
The Facebook app id and secret are set by a developer when logged into one's Facebook account. The developer needs to set up an app in Facebook, using the Facebook Developer Console, that is allowed to use Facebook authentication. The GeoTrail mobile app and the GpxPaths.html use the same Facebook app id for user authentication via Facebook. 
## Temporary Changes for Local Debug
* Set URI for web server in [js/GeoPathsApi2.js](../master/js/GeoPathsApi2.js).  
Search for "base = new wigo_ws_Ajax(". Set the constructor arg to the URI for the web server's Service.svc file.  
var base = new wigo_ws_Ajax("Service.svc/");  // Should work for local debug.  
Remember to comment out statement setting URI for remote server.
* May need to disable redirection for https protocol in [js/GpxPaths2.js](../master/js/GpxPaths2.js).  
Near bottom of file, comment out the redirection for https if local server does not have a https certificate.  
Note: GpxPaths2.js is javascript code for the GpxPaths.html page.
