/* 
Copyright (c) 2015, 2016 Robert R Schomburg
Licensed under terms of the MIT License, which is given at
https://github.com/bobbyray/MitLicense/releases/tag/v1.0
*/

/* Prototype for L.LatLng object in LeafLetJs provided by 
Gregor the Map Guy Blog
--------------------------------------------------------------
*/
// Returns bearing to other destination from this object.
// Arg:
//  other: L.LatLng object for the other destination.
// Returns:
//  bearing: floating point number in degrees for bearing wrt to North.
//           value constrainted to 0 to 360 degrees.
L.LatLng.prototype.bearingTo = function(other) {
    var d2r  = L.LatLng.DEG_TO_RAD;
    var r2d  = L.LatLng.RAD_TO_DEG;
    var lat1 = this.lat * d2r;
    var lat2 = other.lat * d2r;
    var dLon = (other.lng-this.lng) * d2r;
    var y    = Math.sin(dLon) * Math.cos(lat2);
    var x    = Math.cos(lat1)*Math.sin(lat2) - Math.sin(lat1)*Math.cos(lat2)*Math.cos(dLon);
    var brng = Math.atan2(y, x);
    //brng = parseInt( brng * r2d ); // Use floating point for the bearing angle.
    brng = brng * r2d;
    brng = (brng + 360) % 360;
    return brng;
};

// Returns word abbreviation for bearing to other destination.
// Arg:
//  other: L.LatLng destination object.
// Returns:
//  word: string for abbreviation of compass bearing.
L.LatLng.prototype.bearingWordTo = function(other) {
    var bearing = this.bearingTo(other);
    var bearingword = '';
    if      (bearing >=  22 && bearing <=  67) bearingword = 'NE';
    else if (bearing >=  67 && bearing <= 112) bearingword =  'E';
    else if (bearing >= 112 && bearing <= 157) bearingword = 'SE';
    else if (bearing >= 157 && bearing <= 202) bearingword =  'S';
    else if (bearing >= 202 && bearing <= 247) bearingword = 'SW';
    else if (bearing >= 247 && bearing <= 292) bearingword =  'W';
    else if (bearing >= 292 && bearing <= 337) bearingword = 'NW';
    else if (bearing >= 337 || bearing <=  22) bearingword =  'N';
    return bearingword;
};
/* -------------------------------------------------------------------*/

// Object for showing geo path map.
// Object can be shared by view objects of pages, 
// for example GeoPaths.html and Trail.html.
// Constructor Arg:
//  bShowMapCtrls: boolean. Indicates if zoom and map-type controls are shown
//                 on google map. Defaults to true;
//                 Note: The map controls are not large enough on a mobile phone.
//                       Maybe need to find out how to scale the map controls.
//                       For now, provide option of not showing any of the map controls.
function wigo_ws_GeoPathMap(bShowMapCtrls) {
    var that = this;
    var map = null;     // Underlying map object.
    var mapPath = null; // Map overlay for current path.

    if (typeof (bShowMapCtrls) !== 'boolean')
        bShowMapCtrls = true;

    // Initialize to use Open Streets Map once browser has initialized.
    // Event handler for window loaded can be set to this function.
    this.InitializeMap = function () {
        var latlngMtHood = new L.LatLng(45.3736111111111, -121.695833333333);
        map = L.map('map-canvas').setView(latlngMtHood, 13);
        // add an OpenStreetMap tile layer
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', { ////20170604 change osm to openstreetmap.
            attribution: '&copy; <a href="http://osm.org/copyright">OpenStreetMap</a> contributors'
        }).addTo(map);

        /* 404 error when trying to load tiles (not found) May be find another topo layer later.
        var OpenTopoMap = L.tileLayer('http://{s}.tile.opentopomap.org/{z}/{x}/{y}.png', {
            opacity: 0.35,
            maxZoom: 16,
            attribution: 'Map data: &copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>, <a href="http://viewfinderpanoramas.org">SRTM</a> | Map style: &copy; <a href="https://opentopomap.org">OpenTopoMap</a> (<a href="https://creativecommons.org/licenses/by-sa/3.0/">CC-BY-SA</a>)'
        });
        OpenTopoMap.addTo(map);
        */

        // Add a listener for the click event and call getElevation on that location
        map.on('click', onMapClick);
    }

    // Error message that methods may set on an error.
    this.sError = "";

    var curPath = null; // Ref to current path drawn.
    // Draws geo path on the Google map object.
    // Args:
    //  path: wigo_ws_GpxPath object for the path.
    this.DrawPath = function (path) {
        if (!IsMapLoaded())
            return; // Quit if map has not been loaded.
        //var polyline = L.polyline(latlngs, { color: 'red' }).addTo(map);
        // Clear any current path before drawing another path.
        this.ClearPath();

        var pathCoords = new Array();
        var gpt;
        var mapCoord;
        for (var i = 0; i < path.arGeoPt.length; i++) {
            gpt = path.arGeoPt[i];
            mapCoord = L.latLng(gpt.lat, gpt.lon);
            pathCoords.push(mapCoord);
        }
        mapPath = L.polyline(pathCoords, { color: 'red', opacity: 1.0 });
        mapPath.addTo(map);

        curPath = path; // Save current gpx path object.
        this.PanToPathCenter();

        // mapPath.redraw(); did not help panning to new path on same screen.
    };

    // Sets geo location circle and arrow for shortest distance to geo path.
    // Arg:
    //  location: Map LatLng object for location off the geo path.
    this.SetGeoLocationCircleAndArrow = function (location) {
        if (!IsMapLoaded())
            return; // Quit if map has not been loaded.
        // Draw geo location circle (green circle) on the map.
        SetGeoLocationCircle(location, 10);
        if (geolocCircle) {
            SetGeoLocToPathArrow(location);
            var center = geolocCircle.getLatLng();
            map.panTo(center);
        }
    };

    // Pan to point on the map.
    // Arg:
    // gpt: wigo_ws_GeoPt obj to which to pan. 
    this.PanTo = function (gpt) {
        if (!IsMapLoaded())
            return; // Quit if map has not been loaded.
        var ll = L.latLng(gpt.lat, gpt.lon);
        // Note: If new gpt for center of path is not off the current screen, 
        // the path may not be redrawn centered.
        // map.setView(ll) was no different than map.panTo(ll) in this regard. 
        // Note: {animate: false} option seems to fix problem of panning within same screen.
        map.panTo(ll, { animate: false });
        // Note: panBy(0,-5) avoids problem when nearby lat/lng is on same scren.
        //       panBy(..) is necessary for web page, but not for cordova android app.
        map.panBy(L.point(0, -5));
    };

    // Pan to center of path on the map.
    // Returns true for success, false if current path is null.
    this.PanToPathCenter = function () {
        var bOk = true;
        if (curPath) {
            // Set zoom so that trail fits.
            var sw = L.latLng(curPath.gptSW.lat, curPath.gptSW.lon);
            var ne = L.latLng(curPath.gptNE.lat, curPath.gptNE.lon);
            var bounds = L.latLngBounds(sw, ne);
            map.fitBounds(bounds);
            var gpt = curPath.gptCenter();
            this.PanTo(gpt);
        } else
            bOk = false;
        return bOk;
    };

    // Clears current path and geo location circle and arrow from the map.
    this.ClearPath = function () {
        if (!IsMapLoaded())
            return; // Quit if map has not been loaded.
        if (mapPath)
            map.removeLayer(mapPath);
        curPath = null;        
        ClearGeoLocationCircle();
        ClearGeoLocationToPathArrow();
    }

    // Returns a reference to underlaying Google map object.
    this.getMap = function () { return map; };

    // ** Events fired by map for container (view) to handle.
    // Click current on the map.
    // Signature of handler:
    //  llAt: Google LatLng object for the click.
    this.onMapClick = function (llAt) { };

    // Event handler for click on map.
    function onMapClick(e) {
        that.onMapClick(e.latlng);
    }

    // ** More private members
    // Returns true if map is loaded.
    // For false, sets this.sError to indicate map is not loaded.
    function IsMapLoaded() {
        var bYes = map != null;
        if (!bYes) 
            this.sError = "Map is not loaded."
        return bYes;
    }

    // Clears from map the geo location circle.
    function ClearGeoLocationCircle() {
        // Remove existing geolocation circle, if there is one, from the map.
        if (geolocCircle)
            map.removeLayer(geolocCircle);    }

    var geolocCircle = null;
    // Set (draws) circle on map centered at geo location.
    // Arguments:
    //  latlng is L.LatLng object for center of circle.
    //  r is radius in meters of circle.
    function SetGeoLocationCircle(latlng, r) {
        ClearGeoLocationCircle();
        var circleOptions = {
            color: '#00FF00',
            opacity: 1.0,
            fill: true,
            fillOpacity: 1.0,
            weight: 5
        };
        geolocCircle = L.circle(latlng, r, circleOptions);
        geolocCircle.addTo(map);
    }

    var curLocToPathArrow = null; // Current arrow from location to path drawn on map.
    // Clears from map the current location to geo path arrow.
    function ClearGeoLocationToPathArrow() {
        // Remove existing geolocation circle, if there is one, from the map.
        if (curLocToPathArrow)
            map.removeLayer(curLocToPathArrow);
    }

    // Sets (draws) arrow for a location to near point on geo path (the trail).
    // Arg:
    //  location: Map L.LatLng object for location on map.
    function SetGeoLocToPathArrow(location) {
        ClearGeoLocationToPathArrow();
        var llAt = FindNearestPointOnGeoPath(location);
        if (llAt) {
            var mapCoords = [location, llAt];

            var options = {
                color: '#0000FF',
                lineCap: 'butt',  /* in place of an arrow */
                weight: 5,
                opacity: 1.0
            }
            var mapPath = L.polyline(mapCoords, options)
            mapPath.addTo(map);
            // Note: Leaflet has no easy way to put arrow on polylines.
            // Save ref to the current path.
            curLocToPathArrow = mapPath;
        }
    }

    // Searchs through all segments of curPath to find shortest distance
    // to the path from a location off the path.
    // Returns: 
    //  Map L.LatLng obj for nearest point on the the path.
    //  Returns null if curPath is not valid.
    // Arg: 
    //      llLoc: Map L.LatLng object for location for which search is done.
    function FindNearestPointOnGeoPath(llLoc) {
        var llFound = null;
        if (curPath && curPath.arGeoPt) {
            var result;
            var minD = 0.0;
            var llSeg0, llSeg1, gpt, gptNext;
            for (var i = 0; i < curPath.arGeoPt.length; i++) {
                gpt = curPath.arGeoPt[i];
                llSeg0 = L.latLng(gpt.lat, gpt.lon);
                if (curPath.arGeoPt.length == 1) {
                    llFound = llSeg0;
                } else if (i < curPath.arGeoPt.length - 1) {
                    gptNext = curPath.arGeoPt[i + 1];
                    llSeg1 = L.latLng(gptNext.lat, gptNext.lon);
                    result = LocationToSegment(llLoc, llSeg0, llSeg1);
                    if (llFound === null) {
                        llFound = result.at;
                        minD = result.d;
                    } else if (result.d < minD) {
                        llFound = result.at;
                        minD = result.d;
                    }
                }
            }
        }
        return llFound;
    }

    // Calculates distance to from a location point to a line segment.
    // Returns literal object:
    //  d: floating point number for distance to path in meters.
    // llAt: L.LatLng object for point on the segment.
    // Args:
    //  llLoc: L.LatLng object for location.
    //  llSeg0: L.LatLng object for starting point of segment.
    //  llSeg1: L.LatLng object for ending point of segment.
    function LocationToSegment(llLoc, llSeg0, llSeg1) {
        var hdLoc = llSeg0.bearingTo(llLoc);
        var dLoc = llSeg0.distanceTo(llLoc);
        var hdSeg = llSeg0.bearingTo(llSeg1);
        var dSeg = llSeg0.distanceTo(llSeg1);

        // Calc angle phi between heading hdLoc and heading hdSeg.
        var phi = hdSeg - hdLoc;
        if (phi < 0)
            phi = -phi;
        phi = phi * L.LatLng.DEG_TO_RAD;  // Convert degrees to radians.

        // I think simple planar geometry approx for interpolation should be good enough.
        var llAt = L.latLng(0,0);
        var dOnSeg = dLoc * Math.cos(phi);
        if (dOnSeg < 0.0) {
            // Vector location projects before starting point of segment, so
            // truncate to starting point of the segment.
            llAt = llSeg0;
        } else if (dOnSeg > dSeg) {
            // Vector to location point projects beyong segment, so truncate 
            // to segment end point.
            llAt = llSeg1;
        } else {
            // Vector to location point projects onto segment.
            var fraction = dOnSeg / dSeg;
            var delLng = fraction * (llSeg1.lng - llSeg0.lng);
            var delLat = fraction * (llSeg1.lat - llSeg0.lat);
            llAt.lng = llSeg0.lng + delLng;
            llAt.lat = llSeg0.lat + delLat;
        }

        // Calculate distance from location to path.
        var dToPath = llLoc.distanceTo(llAt);
        var result = { d: dToPath, at: llAt };
        return result;
    }
}

