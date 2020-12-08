'use strict';
// Also need to use client side config.js that is configured for local debug or GoDaddy
// Use www.wigo.ws at GoDaddy with https
// const wigo_ws_auth_api_sBaseUri = "https://www.wigo.ws/wigoauth/Service.svc/";
// const wigo_ws_auth_page_uri = "https://www.wigo.ws/wigoauth/wigoauth.html";
// const wigo_ws_geopaths_api_sBaseUri = "https://www.wigo.ws/WalkingMap/Service.svc/";

// For local debugging on desktop, use local host with relative url.   
// Note: This does not work for an Android web view (mobile app) because https is required.
//       the mobile app can debug the javascript at the remote host using Chrome debug,
//       but not the server side code.
const wigo_ws_auth_api_sBaseUri = "../wigoauth1/Service.svc/";
const wigo_ws_auth_page_uri = "../wigoauth1/wigoauth.html";
const wigo_ws_geopaths_api_sBaseUri = "../WalkingMap/Service.svc/";

// Note: At GoDaddy, the wigo auth virtual directory is wigoauth.
//       Locally the wigo auth virtual directory is wigoauth1.
//       An Android web view (mobile app) needs to specify http or https
//       and cannot use relative url. 
//       A web page can use relative url for http or https without
//       specifying http or https.