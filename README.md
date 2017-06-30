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
