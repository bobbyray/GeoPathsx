'use strict';
// wigo_ws_Model oject is in js/Model.js.

// Object for View present by page.
function wigo_ws_View() {
    // ** Events fired by the view for controller to handle.
    // Note: Controller needs to set these onHandler functions.
    
    // File selected for uploading Gpx file.
    // Handler Signature:
    //  file: file javascript object obtained by user selecting a file.
    //        file is null for invalid file selection.
    //  nMode: this.eMode enumeration for current view mode.
    this.onFileUploadPath = function (file, nMode) { };

    // File upload clicked.
    // Handler Signature:
    //  nMode: byte value of this.eMode enumeration.
    //  ixGeoPathList: integer index for selected item in a geo path array
    //      which was given by calling this.setPathList(..). -1 indicates
    //      no item selected.
    this.onUpload = function (nMode, ixPathArray) { };

    // Delete button was clicked.
    // Handler Signature:
    //  nMode: byte value of this.eMode enumeration.
    //  ixGeoPathList: integer index for selected item in a geo path array
    //      which was given by calling this.setPathList(..). -1 indicates
    //      no item selected.
    this.onDelete = function (nMode, ixPathArray) { };


    // Get list of paths from server.
    // Handler Signature:
    //  nMode: byte value of this.eMode enumeration.
    //  sPathOwnerId: string for path owner id for getting the paths from server.
    this.onGetPaths = function (nMode, sPathOwnerId) { };

    // User selected a geo path from the list of paths.
    // Handler Signature:
    //  nMode: byte value of this.eMode enumeration.
    //  nIx: integer for selection in the list. 
    this.onPathSelected = function (nMode, nIx) { };

    // The view mode has changed.
    // Handler Signature:
    //  nMode: byte value of this.eMode enumeration for the new mode.
    this.onModeChanged = function (nMode) { };

    // Login authentication has completed.
    // Handler Signature:
    //  result: json {userName, userID, accessToken, nAuthResult}
    //    userID: Facebook user id or empty string when authentication fails.
    //    accessToken: access token string acquired from FaceBook, or empty string
    //      when athentication fails or is cancelled.
    //    nAuthResult: integer result of authentication, value of which is given 
    //      by EAuthStatus in Service.cs.
    this.onAuthenticationCompleted = function (result) { };

    // ** Public members

    // Enumeration of Authentication status (login result)
    this.eAuthStatus = function () {
        return fb.EAuthResult;
    };

    // Enumeration of mode for processing geo paths.
    this.eMode = {
        edit: 0, upload: 1, view: 2, define: 3, about: 4,
        toNum: function(sMode) { // Returns byte value for sMode property name.
            var nMode = this[sMode];
            if (nMode === undefined)
                nMode = this.define; 
            return nMode;
        },
        toStr: function (nMode) { // Returns string for property name of nMode byte value.
            var sMode;
            switch (nMode) {
                case this.edit: sMode = 'edit'; break;
                case this.upload: sMode = 'upload'; break;
                case this.view: sMode = 'view'; break;
                default: sMode = 'define'; 
            }
            return sMode;
        },
        tabToNum: function(sPanelSelector) { // Returns byte value for tab panel selector id string.
            var nMode;
            if (sPanelSelector === 'tabs-upload')
                nMode = this.upload;
            else if (sPanelSelector === 'tabs-edit')
                nMode = this.edit;
            else if (sPanelSelector === 'tabs-view')
                nMode = this.view;
            else if (sPanelSelector === 'tabs-define')
                nMode = this.define;
            else if (sPanelSelector === 'tabs-about')
                nMode = this.about;
            else
                nMode = this.define;
            return nMode;
        }
    };

    // Returns current mode for processing geo paths.
    this.curMode = function() {
        return nMode;
    };

    // Returns OwnerId string of signed-in user.
    this.getOwnerId = function () {
        return _ownerId;
    };
    var _ownerId = "";

    // Sets OwnerId of signed-in user to string given by sOwnerId.
    this.setOwnerId = function (sOwnerId) {
        _ownerId = sOwnerId;
        txbxPathOwnerId.value = sOwnerId;
    };

    // Returns Owner Name string of signed in user.
    this.getOwnerName = function () {
        return txbxOwnerName.value;
    }
    
    // Sets Owner Name string for signed in user.
    this.setOwnerName = function (sOwnerName) {
        txbxOwnerName.value = sOwnerName;
    }

    // Clears owner Id and owner Name for signed in user.
    this.clearOwner = function () {
        this.setOwnerId("");
        this.setOwnerName("");
    }

    // Return string value of currently selected Share state:
    // public, protected, or private.
    this.getShare = function () {
        return selectShare.value;
    };

    // Displays share value for the gpx path data.
    // Arg: 
    //  sShare: string value: public, protected, or private.
    this.setShare = function (sShare) {
        selectShare.value = sShare;
    }

    // Returns string for the path name (description) of the gpx path.
    this.getPathName = function () {
        return txbxPathName.value.trim();
    };

    // Displays Path Name (description) for gpx path.
    // Arg: sPathName is string for the path name.
    this.setPathName = function (sPathName) {
        txbxPathName.value = sPathName;
    };

    // Fill the list of paths that user can select.
    // Arg:
    //  arPath is an array of strings for geo path names.
    //  bSort is optional boolean to display sorted version of arPath.
    //        Defaults to true if not given.
    this.setPathList = function (arPath, bSort) {
        if (typeof(bSort) !== 'boolean')
            bSort = true;

        // For arSelect to use as a sorted version of arPath.
        var arSelect = new Array();
        for (var i = 0; i < arPath.length; i++ ) {
            arSelect.push({s: arPath[i], i: i});
        }
        if (arSelect.length > 1 && bSort) {
            // Do a case insensitive sort.
            arSelect.sort(function (a, b) {
                var n = a.s.toLowerCase().localeCompare(b.s.toLowerCase());
                return n;
            });
        }

        InitPathList("Select a Geo Path");
        // Add the list of geo paths.
        var name, iStr;
        for (var i = 0; i < arSelect.length; i++) {
            name = arSelect[i].s;
            iStr = arSelect[i].i.toString();
            var option = new Option(name, iStr);
            selectGeoPath.add(option);
        }
    };


    // Returns integer index to path array for selected geo path.
    // The index is that of selected item as given by this.setPathList(..).
    // -1 indicates path in the list is selected.
    this.getSelectedPathIx = function () {
        var ix = parseInt(selectGeoPath.value);
        return ix;
    };

    // Set the user interface for a new mode.
    // Arg:
    //  newMode: eMode enumeration value for the new mode.
    this.setModeUI = function (newMode) {
        nMode = newMode;
        MinimizeMap(); // Note: Minimize map shows edit ctrls except for define or about mode.
        switch (nMode) {
            case this.eMode.edit:
                $(buUpload).prop('disabled', true);       // Disable upload. Enable when upload file is selected.
                buUpload.style.display = 'block';         // Show upload button.
                $(buDelete).prop('disabled', true);       // Disable delete button.
                buDelete.style.display = 'block';         // Show delete button.
                $(selectShare).prop('disabled', false);    // Enable share droplist.
                selectShare.value = 'public';
                $(selectGeoPath).prop('disabled', false);  // Enable droplist of GeoPaths (new path being uploaded).
                InitPathList("Select Geo Path");
                selectGeoPath.selectedIndex = 0;
                ShowFileUploadPath(true);
                $(txbxPathOwnerId).prop('disabled', true);  // Disable PathOwnerId.
                txbxPathOwnerId.value = this.getOwnerId();  // Set PathOwnerId to signed-in user.
                $(txbxPathName).prop('disabled', false);    // Enable editing PathName.
                txbxPathName.value = "";
                $(buLoadFromServer).prop('disabled', true); // Disable loading list of paths from server.
                buLoadFromServer.style.display = 'none';
                $(txbxBegin).prop('disabled', true);   // Disable begin geo pt.
                txbxBegin.value = '';
                $(txbxEnd).prop('disabled', true);     // Disable end geo pt.
                txbxEnd.value = '';
                $(txbxNECorner).prop('disabled', true);     // Disable NE corner geo pt.
                txbxNECorner.value = '';
                $(txbxSWCorner).prop('disabled', true);     // Disable SW corner geo pt.
                txbxSWCorner.value = '';
                break;
            case this.eMode.upload:
                $(buUpload).prop('disabled', true);        // Disable upload. Enable when upload file is selected.
                buUpload.style.display = 'block';          // Show upload button.  
                $(buDelete).prop('disabled', true);        // Disable delete button.
                buDelete.style.display = 'none';           // Hide delete button.
                $(selectShare).prop('disabled', false);    // Enable share droplist.
                selectShare.value = 'public';
                $(selectGeoPath).prop('disabled', true);     // Disable droplist of GeoPaths (new path being uploaded).
                InitPathList("New Geo Path");
                selectGeoPath.selectedIndex = 0;
                ShowFileUploadPath(true);
                $(txbxPathOwnerId).prop('disabled', true);  // Disable PathOwnerId.
                txbxPathOwnerId.value = this.getOwnerId();  // Set PathOwnerId to signed-in user.
                $(txbxPathName).prop('disabled', false);    // Enable editing PathName.
                txbxPathName.value = '';
                $(buLoadFromServer).prop('disabled', true); // Disable loading list of paths from server.
                buLoadFromServer.style.display = 'none';
                $(txbxBegin).prop('disabled', true);     // Disable begin geo pt.
                txbxBegin.value = '';
                $(txbxEnd).prop('disabled', true);     // Disable end geo pt.
                txbxEnd.value = '';

                $(txbxNECorner).prop('disabled', true);     // Disable NE corner geo pt.
                txbxNECorner.value = '';
                $(txbxSWCorner).prop('disabled', true);     // Disable SW corner geo pt.
                txbxSWCorner.value = '';
                break;
            case this.eMode.view:
                $(buUpload).prop('disabled', true);       // Disable upload. Enable when upload file is selected.
                buUpload.style.display = 'none';          // Hide upload button.
                $(buDelete).prop('disabled', true);       // Disable delete. 
                buDelete.style.display = 'none';          // Hide delete button.
                $(selectShare).prop('disabled', true);    // Disable share droplist.
                selectShare.value = 'public';
                $(selectGeoPath).prop('disabled', false);  // Enable droplist of GeoPaths (new path being uploaded).
                InitPathList("Select Geo Path");
                selectGeoPath.selectedIndex = 0;
                ShowFileUploadPath(false);
                $(txbxPathOwnerId).prop('disabled', true);  // Disable PathOwnerId.
                txbxPathOwnerId.value = '';  // Set PathOwnerId to signed-in user.
                $(txbxPathName).prop('disabled', true);     // Disable editing PathName.
                txbxPathName.value = '';
                $(buLoadFromServer).prop('disabled', true); // Disable loading list of paths from server.
                buLoadFromServer.style.display = 'none';
                $(txbxBegin).prop('disabled', true);     // Disable begin geo pt.
                txbxBegin.value = '';
                $(txbxEnd).prop('disabled', true);     // Disable end geo pt.
                txbxEnd.value = '';
                $(txbxNECorner).prop('disabled', true);     // Disable NE corner geo pt.
                txbxNECorner.value = '';
                $(txbxSWCorner).prop('disabled', true);     // Disable SW corner geo pt.
                txbxSWCorner.value = '';
                break;
            case this.eMode.define:
                $(buLoadFromServer).prop('disabled', true); // Disable loading list of paths from server.
                buLoadFromServer.style.display = 'none';
                break;
            case this.eMode.about:
                break;
        }
    };
    
    // Displays a status message.
    // Arg:
    //  sStatus: string of html to display.
    //  bError: boolean, optional. Indicates an error msg. Default to true.
    this.ShowStatus = function (sStatus, bError) {
        if (typeof (bError) === 'undefined')
            bError = true;
        if (bError)
            divStatus.className = 'ErrorMsg';
        else
            divStatus.className = 'NormalMsg';
        divStatus.style.display = "block";
        sStatus = sStatus.replace("\n", "<br/>");
        divStatus.innerHTML = sStatus;
        SetMapPanelTop();
        window.scrollTo(0, 0);
    };

    // Clears the status message.
    this.ClearStatus = function () {
        divStatus.innerHTML = "";
        divStatus.style.display = 'none'; 
        SetMapPanelTop();
    };


    // Returns args to use when calling ShowPathInfo(args).
    // Note: caller fills in the argument value.
    this.PathInfoArgs = function () {
        var args = { bShow: true, sOwnerId: "", name: "", sShare: null, path: null};
        return args;
    };

    // Shows path information.
    // args object: (See this.PathInfoArgs())
    //  bShow: boolean. Show or hide displaying the path info.
    //  sOwnerId: string for id of person that defined the path.
    //  name: string for name (description) of path. null indicates do not set.
    //  sShare: string for sharing state of geo path. null indicates do not set.
    //  path: wigo_ws_GpxPath object defining the path. null indicates do not set. 
    this.ShowPathInfo = function (args) {
        ShowPathInfoDiv(args.bShow);

        if (args.name !== null)
            txbxPathName.value = args.name;

        txbxPathOwnerId.value = args.sOwnerId;
        if (args.sShare != null)
            selectShare.value = args.sShare;
        if (args.path) {
            txbxBegin.value = GeoPtToStr(args.path.gptBegin);
            txbxEnd.value = GeoPtToStr(args.path.gptEnd);

            txbxSWCorner.value = GeoPtToStr(args.path.gptSW);
            txbxNECorner.value = GeoPtToStr(args.path.gptNE);
        } else {
            txbxBegin.value = "";
            txbxEnd.value = "";

            txbxSWCorner.value = "";
            txbxNECorner.value = "";
        }

        map.DrawPath(args.path);
    };

    // ** Private members for controls

    var tabs = $('#tabs')[0];
    var divOwnerId = $('#divOwnerId')[0];
    var txbxOwnerName = $('#txbxOwnerName')[0];
    var selectSignIn = $('#selectSignIn')[0];
    var divFbLogin = $('#divFbLogin')[0]
    var divFileUploadPath = $('#divFileUploadPath')[0];
    var fileUploadPath = $('#fileUploadPath')[0];
    var divStatus = $('#divStatus')[0];

    var divPathInfo = $('#divPathInfo')[0];
    var buLoadFromServer = $('#buLoadFromServer')[0];
    var selectGeoPath = $('#selectGeoPath')[0];
    var selectShare = $('#selectShare')[0];
    var txbxPathName = $('#txbxPathName')[0];
    var txbxPathOwnerId = $('#txbxPathOwnerId')[0];

    var txbxBegin = $('#txbxBegin')[0];
    var txbxEnd = $('#txbxEnd')[0];

    var txbxSWCorner = $('#txbxSWCorner')[0];
    var txbxNECorner = $('#txbxNECorner')[0];
    var buUpload = $('#buUpload')[0];
    var buDelete = $('#buDelete')[0];
    var panel = $('#panel')[0];
    var buGeoLocate = $('#buGeoLocate')[0];
    var buGoToPath = $('#buGoToPath')[0];
    var buMinMaxMap = $('#buMinMaxMap')[0];

    var that = this;
    // ** Use jquery to attach event handler for controls.
    // Note: All the control event handlers clear status first thing.

    $(selectSignIn).bind('change', function (e) {
        that.ClearStatus();
        var val = this.selectedValue;
        if (this.selectedIndex > 0) {
            var option = this[this.selectedIndex];
            if (option.value === 'facebook') {
                fb.Authenticate();
            } else if (option.value === 'logout') {
                fb.LogOut();
            }
            this.selectedIndex = 0;
        }
    });

    $(fileUploadPath).bind('change', function (e) {   
        that.ClearStatus();
        var bOk = this.files.length > 0;
        var file;
        if (bOk) {
            if (that.curMode() === that.eMode.upload) {
                // Enable upload button.
                $(buUpload).prop('disabled', false);
            }
            file = this.files[0];
            that.onFileUploadPath(file, that.curMode());
        }
        // Note: this.files.length == 0 is true when switching to view upload mode
        //       because fileUploadPath.value is initialized to empty string
        //       causing this change handler to be entered.
        // else
        // {
        //    file = null;
        //     that.ShowStatus("Error selecting path for a GPX file to upload.");
        // }
    });


    $(fileUploadPath).bind('click', function (e) {
        this.value = "";
    });


    $(buUpload).bind('click', function (e) {
        that.ClearStatus();
        // Validate the input fields
        var curMode = that.curMode(); 
        var sError = "";
        if (!txbxPathName.value.trim())
            if (!sError) 
                sError += "Path Name must be given.<br/>";
        if (curMode === that.eMode.upload && !fileUploadPath.value.trim()) {
            sError += "GPX file must be selected for uploading.<br/>";
        } else if (curMode === that.eMode.edit && selectGeoPath.selectedIndex < 1) {
            sError += "A geo path must be selected for editing.<br/>";
        } else {
            if (!txbxPathOwnerId.value.trim()) {
                sError += "Owner must be logged in to upload a geo path.<br/>";
            } else {
                if (!txbxBegin.value.trim())
                    sError += "Begin point is not known.<br/>"
                if (!txbxEnd.value.trim())
                    sError += "End point is not known.<br/>"

                if (!txbxSWCorner.value.trim())
                    sError += "SW Corner is not known.<br/>";
                if (!txbxNECorner.value.trim())
                    sError += "NE Corner is not known.<br/>";
            }
        }
        if (sError)
            that.ShowStatus(sError);
        else {
            // Disable upload button so that same file is not uploaded again when mode is upload.
            if (curMode === that.eMode.upload)
                $(this).prop('disabled', true); // Disable this upload ctrl.
            var ix = parseInt(selectGeoPath.value);
            that.onUpload(curMode, ix);
        }
    });


    $(buDelete).bind('click', function (e) {
        that.ClearStatus();
        var nCurMode = that.curMode();
        var ix = parseInt(selectGeoPath.value);
        var iSelect = selectGeoPath.selectedIndex;
        if (iSelect > 0 ) {
            var sName = selectGeoPath[iSelect].text;
            var sPrompt = "Ok to delete {0}?".format(sName);
            var bOk = confirm(sPrompt);
            if (bOk) {
                that.onDelete(nCurMode, ix);
            }
        } else {
            that.ShowStatus("No item is selected for deletion.");
            alert("No item is selected for deletion.");
        }
    });


    $(selectGeoPath).bind('change', function (e) {
        that.ClearStatus();
        // Clear drawn map.
        map.ClearPath(); 
        if (that.curMode() === that.eMode.edit) {
            if (this.selectedIndex > 0) {
                // Enable upload when a Geo Path is selected in edit mode.
                $(buUpload).prop('disabled', false);
                $(buDelete).prop('disabled', false);  
            } else {
                // Disable upload when no Geo Path is selected in edit mode.
                $(buUpload).prop('disabled', true);
                $(buDelete).prop('disabled', true);  
            }
        }
        if (this.selectedIndex >= 0) {
            window.scrollTo(0, divPathInfo.offsetTop);  
            var iList = parseInt(this.value);
            that.onPathSelected(that.curMode(), iList);
        }
    });

    $(buGeoLocate).bind('click', function (e) {
        that.ShowStatus("Getting Geo Location ...", false);
        var geoLocationOptions = { enableHighAccuracy: true, timeout: 10000, maximumAge: 0 };
        navigator.geolocation.getCurrentPosition(
        function (position) {
            // Successfully obtained location.
            //  position is a Position object:
            //      .coords is a coordinates object:
            //          .coords.latitude  is latitude in degrees
            //          .coords.longitude is longitude in degrees 
            //      position has other members too. See spec on web for navigator.geolocation.getCurrentPosition.
            var location = L.latLng(position.coords.latitude, position.coords.longitude);
            map.SetGeoLocationCircleAndArrow(location);
            that.ClearStatus();
        },
        function (postionError) {
            // Error occurred trying to get location.
            var sMsg = "Geolocation Failed! Check your browser options to enable Geolocation.\n" + positionError.message;
            that.ShowStatus(sMsg);
        },
        geoLocationOptions);
    });

    $(buGoToPath).bind('click', function (e) {
        that.ClearStatus();
        var bOk = map.PanToPathCenter();
        if (!bOk) {
            that.ShowStatus("No Geo Path currently defined to pan-to.");
        }

    });

    $(buMinMaxMap).bind('click', function (e) {
        that.ClearStatus();
        // Toggle minimum/maximum display of map.
        var minState = $(this).prop('data-minState')
        if (minState === undefined)
            minState = 'true';
        var bMin = minState === 'true';
        bMin = !bMin;
        // For bMin true, show the edit mode and path info so that map is shown
        // only in a small portion at bottom of the screen.
        // Otherwise, hide edit mode and path info so map is shown full screen.
        // Set the value of this button, which is the button caption, to be opposite of 
        // bMin because pressing the button toggles the current state.
        if (bMin) {
            MinimizeMap();
            this.value = 'Full Screen';
        } else {
            MaximizeMap();
            this.value = 'Reduce';
        }
        // Save the current minState.
        $(this).prop('data-minState', bMin.toString());
    });

    $(tabs).tabs();

    $(tabs).on("tabsactivate", function (event, ui) {
        that.ClearStatus();
        if (ui && ui.newPanel.length > 0) {
            var newMode = that.eMode.tabToNum(ui.newPanel[0].id);
            // Set view for the selected mode.
            that.setModeUI(newMode);
            // Inform controller of the mode change.
            that.onModeChanged(newMode);
            that.onGetPaths(newMode, that.getOwnerId());
        }
        // Clear drawn map.
        map.ClearPath();
        SetMapPanelTop();
    });


    // ** Private members for open streets map
    var map = new wigo_ws_GeoPathMap();
    /* // Map click no longer used.
    map.onMapClick = function (llAt) {
        that.ClearStatus();
        map.SetGeoLocationCircleAndArrow(llAt);
    };
    */

    // Returns true if the edit controls for path info and sign-in should be displayed.
    function AreEditCtrlsNeeded() {
        var bShowEditCtrls = nMode === that.eMode.upload ||
                             nMode === that.eMode.view ||
                             nMode === that.eMode.edit;
        return bShowEditCtrls;

    }

    // Display map below divPath info rather than at the top of the screen.
    // Also positions panel to be over the top of the map.
    function MinimizeMap() {
        var bShowEditCtrls = AreEditCtrlsNeeded();
        ShowEditCtrls(bShowEditCtrls);
        SetMapPanelTop();
    }

    // Display map at top of screen by hiding edit mode and path info.
    // Also positions panel to be over the top of the map.
    function MaximizeMap() {
        ShowEditCtrls(false);
        SetMapPanelTop();
    }

    function SetMapPanelTop() {
        var mapcanvas = document.getElementById("map-canvas");
        var y = mapcanvas.offsetTop;
        panel.style.top = y + 'px';
        // Only show the panel when edit controls need to be shown.
        // Note: This avoids a problem when the page is loaded because
        //       y for the mapcanvas is 0 initially before the map has been
        //       fully initialized. This hides the panel when the 
        //       Define and About tabs are visible, which is good, and the page
        //       is initially in Define mode so the problem of y == 0 is avoided.
        var bShowPanel = AreEditCtrlsNeeded();
        panel.style.display = bShowPanel ? 'block' : 'none';
    }

    // ** More private members
    var nMode = this.eMode.edit; // Current mode for processing geo paths.

    // Initialize selectGeoPath droplist to empty list of path.
    // Arg:
    //  sHeader: string for item 0 in the droplist, which is a header desribing the list.
    function InitPathList(sHeader) {
        // Remove any existing elements from selectGeoPath.
        var nCount = selectGeoPath.length;
        for (var i = 0; i < nCount; i++) {
            selectGeoPath.remove(0);
        }
        // Add header element.
        var option = new Option(sHeader, "-1");
        selectGeoPath.add(option);

    }

    // Returns a string for displaying a GeoPt obj.
    //  Arg: geopt: a wigo_ws_GeoPt objt.
    function GeoPtToStr(geopt) {
        var s = "({0}, {1})".format(geopt.lat, geopt.lon);
        return s;
    }

    // Shows/hides fileUploadPath ctrl and its label. Initializes path to empty string.
    // Arg: bShow: boolean to show the ctrl and its label.
    function ShowFileUploadPath(bShow) {
        var sShow = bShow ? 'block' : 'none';
        divFileUploadPath.style.display = sShow;
    }

    // Shows or hides divPathInfo.
    // Arg:
    //  bShow: boolean indicating to show.
    function ShowPathInfoDiv(bShow) {
        if (bShow)
            divPathInfo.style.display = 'block';
        else
            divPathInfo.style.display = 'none';
    }
    
    // Shows or hides edit controls that are above the map.
    // Arg:
    //  bShow: boolean indicating to show.
    function ShowEditCtrls(bShow) {
        if (bShow) {
            divOwnerId.style.display = 'block';
        } else {
            divOwnerId.style.display = 'none';
        }

        ShowPathInfoDiv(bShow);
    }

    // Callback after Facebook authentication has completed.
    function cbFbAuthenticationCompleted(result) {
        if (that.onAuthenticationCompleted)
            that.onAuthenticationCompleted(result);
    }

    // ** Constructor initialization.
    // Set current mode for processing geo paths based on tabs ctrl.
    this.setModeUI(this.eMode.define);

    $(window).load(map.InitializeMap)

    // Set Facebook login.
    // NOTE: appid must be in sync for private\appSettings.config
    var fb = new wigo_ws_FaceBookAuthentication('694318660701967'); //MainAppId 
    // var fb = new wigo_ws_FaceBookAuthentication('870220976445067'); //LocalTestingAppId.

    fb.callbackAuthenticated = cbFbAuthenticationCompleted;
}

// Object for controller of the page.
function wigo_ws_Controller() {
    var that = this;
    var view = new wigo_ws_View();
    var model = new wigo_ws_Model();

    // ** Handlers for events fired by view.

    view.onModeChanged = function (nNewMode) {
        // Clear xmlGpx, the xml data saved when a gpx file is read.
        // A new gpx file must be read for upload mode.
        // For edit edit mode a the exist xml data is used.
        // If the different gpx file is read for an item being edit,
        // the xml data has been updated when the file was read.
        xmlGpx = "";
        gpxArray = null;
    };

    // Read gpx file from disk.
    view.onFileUploadPath = function (file, nMode) {
        if (!file) {
            view.ShowStatus("No valid file selected to upload!");
            return;
        }

        var sOwnerId = view.getOwnerId();
        if (!sOwnerId) {
            view.ShowStatus("Owner must be logged in before file can be uploaded.<br.>");
            return;
        }

        // Read the text from the file.
        var bOk = model.readTextFile(file, function (bOk, sResult) {
            // Async callback when reading file is done.
            if (bOk) {
                view.ShowStatus("Loaded file " + file.name, false); 
                // Parse result, the xml in the file read.
                var gpxPath = model.ParseGpxXml(sResult);
                if (gpxPath.ok) {
                    // Save the xml read from the file.
                    xmlGpx = sResult;
                    if (nMode === view.eMode.edit) {
                        // Update the xmlData for geo path being edited.
                        var ix = view.getSelectedPathIx();
                        if (gpxArray && ix >= 0 && ix < gpxArray.length) {
                            gpxArray[ix].xmlData = xmlGpx;
                        }
                    }

                    // Show the path info read from the xml file.
                    var args = view.PathInfoArgs();
                    args.bShow = true;
                    if (nMode === view.eMode.upload) {
                        args.name = ""; // Clear path name. User needs to enter path name later.
                        args.sOwnerId = view.getOwnerId(); // Signed in user is owner of the path.
                        args.sSharing = null; // Do not change geo path change sharing state
                    } else if (nMode === view.eMode.edit) {
                        args.name = null;    // Leave geo path name as is.
                        args.sOwnerId = view.getOwnerId();
                        args.sShare = null; // Do not change geo path sharing state.
                    }
                    
                    args.path = gpxPath;
                    view.ShowPathInfo(args);
                } else {
                    view.ShowStatus("Failed to parse GPX file.");
                }
            } else {
                var sError = "Cannot read file " + file.name + " because reading another file is already in progress."
                view.ShowStatus(sError);
            }
        });

    };

    // Upload Gpx object to server based on GPX file read.
    view.onUpload = function (nMode, ixGpxArray) {
        var gpx;
        var bOk = false;
        if (nMode === view.eMode.upload) {
            // Form Gpx oject from file text and put to server.
            bOk = true;
            gpx = new wigo_ws_Gpx();
            gpx.nId = 0; // New record being uploaded.
            gpx.xmlData = xmlGpx; // Global set by reading gpx file for upload.
            if (!xmlGpx) {
                bOk = false;
                view.ShowStatus("No valid GPX data found to upload.<br/>");
            }
        } else if (nMode === view.eMode.edit) {
            // Upload the edited geo path to the server.
            if (gpxArray && ixGpxArray < gpxArray.length && ixGpxArray >= 0) {
                bOk = true;
                gpx = gpxArray[ixGpxArray];
                // Note: If a different gpx file has been read, the item gpxArray has been edited.
            } else {
                view.ShowStatus("Error trying to edit geo path.<br/>");
            }
        } else {
            view.ShowStatus("Error trying to edit geo path, invalid view mode:" + nMode +".<br/>");
        }
        if (bOk) {
            if ( model.IsOwnerAccessValid()) {
                // Note: Setting these fields is same for view mode of upload or edit.
                gpx.sOwnerId = view.getOwnerId();
                var sShare = view.getShare();
                gpx.eShare = model.eShare().toNum(view.getShare());
                gpx.sName = view.getPathName();
                var gpxPath = model.ParseGpxXml(gpx.xmlData);
                gpx.gptBegin = gpxPath.gptBegin;
                gpx.gptEnd = gpxPath.gptEnd;
                gpx.gptSW = gpxPath.gptSW;
                gpx.gptNE = gpxPath.gptNE;
                // gpx.tModified is dont care because server sets tModified when storing record to database.
                // Put Gpx object to server via the model.
                bOk = model.putGpx(gpx,
                    // Async callback upon storing record at server.
                    function (bOk, sStatus) {
                        if (bOk) {
                            var oStatus = JSON.parse(sStatus);
                            var eDuplicate = model.eDuplicate();
                            if (oStatus.eDup === eDuplicate.Renamed) {
                                // Set changed path name due to auto rename.
                                view.setPathName(oStatus.sName);
                                gpx.sName = oStatus.sName;
                                if (nMode === view.eMode.edit) {
                                    // Reload the list of paths since the name has changed.
                                    ReloadPathList();
                                }
                                // Show message about 
                                view.ShowStatus(oStatus.sMsg, true); // true shows message hightlighted.
                            } else if (oStatus.eDup === eDuplicate.Match) {
                                // gpx obj has same name as its record in database so there is no name change.
                                // No need to reload the list of paths.
                                view.ShowStatus("Successfully uploaded GPX path.", false);
                            } else if (oStatus.eDup === eDuplicate.NotDup) {
                                if (nMode === view.eMode.edit) {
                                    // Reload the list of paths because edited the path name.
                                    ReloadPathList();
                                }
                                view.ShowStatus("Successfully uploaded GPX path.", false);
                            } else {
                                view.ShowStatus("Error occurred uploading GPX path.");
                            }
                        } else {
                            // Show error message.
                            view.ShowStatus(sStatus, !bOk)
                        }

                    });
                if (!bOk) {
                    var sError = "Cannot upload GPX path to server because another transfer is already in progress."
                    view.ShowStatus(sError, !bOk);
                }

            } else {
                ShowStatus("Owner must be logged in to upload GPX path.");
            }
        }
    }



    // Upload Gpx object to server based on GPX file read.
    view.onDelete = function (nMode, ixGpxArray) {
        var gpx = null;
        var bOk = false;
        if (nMode === view.eMode.edit) {
            // Delete the geo path record on the server.
            if (gpxArray && ixGpxArray < gpxArray.length && ixGpxArray >= 0) {
                bOk = true;
                gpx = gpxArray[ixGpxArray];
            } else {
                view.ShowStatus("Error trying to edit geo path.<br/>");
            }
        } else {
            view.ShowStatus("Error trying to edit geo path, invalid view mode:" + nMode + ".<br/>");
        }
        if (bOk) {
            if (model.IsOwnerAccessValid()) {
                var gpxId = { sOwnerId: gpx.sOwnerId, nId: gpx.nId };
                bOk = model.deleteGpx(gpxId,
                    // Async callback upon storing record at server.
                    function (bOk, sStatus) {
                        var sMsg = bOk ? "Successfully deleted GPX data record at server.<br/>" : sStatus + "<br/>";
                        view.ShowStatus(sMsg, !bOk);
                        // Remove gpx element from gpxArray.
                        gpxArray.splice(ixGpxArray, 1);
                        // Reload list of paths.
                        ReloadPathList();
                    });
                if (!bOk) {
                    var sError = "Cannot delete GPX data at server because another transfer is already in progress."
                    view.ShowStatus(sError, !bOk);
                }
            } else {
                ShowStatus("Owner must be signed in to delete GPX path.");
            }
        }
    }



    // Get paths from server and load the list paths in the view.
    view.onGetPaths = function (nMode, sPathOwnerId) {

        gpxArray = new Array(); // Clear existing gpxArray.
        var arPath = new Array(); // Lost of path names to show in view.

        // Local helper function to get all geo paths for owner.
        // On error shows status in view.
        // Args
        //  onDone: asynchronous callback when done, Signature:
        //      bOk: boolean indicating success.
        function GetAllGeoPathsForOwner(onDone) {
            // Get all geo paths for the owner.
            var nShare = model.eShare().any;
            model.getGpxList(sPathOwnerId, nShare, function (bOk, gpxList, sStatus) {
                if (bOk) {
                    for (var i = 0; i < gpxList.length; i++) {
                        arPath.push(gpxList[i].sName);
                        gpxArray.push(gpxList[i]);
                    }
                } else {
                    view.ShowStatus(sStatus);
                }
                if (onDone)
                    onDone(bOk);
            });
        }

        // Local helper function to get public geo paths.
        // On error shows status in view.
        // Args:
        //  bExcludeOwner: boolean to exclude owner paths from the list.
        //  onDone: asynchronous callback when done, Signature:
        //      bOk: boolean indicating success.
        function GetPublicGeoPaths(bExcludeOwner, onDone) {
            // Also include all public paths.
            var nShare = model.eShare().public;
            model.getGpxList("any", nShare, function (bOk, gpxList, sStatus) {
                if (bOk) {
                    for (var i = 0; i < gpxList.length; i++) {
                        if (bExcludeOwner) {
                            if (gpxList[i].sOwnerId === sPathOwnerId)
                                continue; // Item with same owner id as sOwnerId is already in list.
                        }
                        gpxArray.push(gpxList[i]); // Add to array of Gpx objects that correspond to list of path names to be set in the view. 
                        arPath.push(gpxList[i].sName);
                    }
                } else {
                    view.ShowStatus(sStatus);
                }
                if (onDone)
                    onDone(bOk);
            });
        }


        // var nShare = model.eShare().toNum(sShare);
        if (nMode === view.eMode.view && !sPathOwnerId) {
            // View mode, no owner signed in. Get all public geo paths.
            GetPublicGeoPaths(false, function (bOk) {
                // Show path list obtained even if there is an error. (Likely empty on error).
                view.setPathList(arPath);
            });
        } else if (nMode === view.eMode.view) {
            // View mode with signed in user.
            // Get all geo paths for owner.
            GetAllGeoPathsForOwner(function (bOk) {
                if (bOk) {
                    // Get public geo paths excluding owner.
                    GetPublicGeoPaths(true, function (bOk) {
                        // Show path list obtained even if error has occured.
                        view.setPathList(arPath);
                    });
                } else {
                    // Show paths obtained before error, likely empty list.
                    view.setPathList(arPath); 
                }
            });
        } else if (nMode === view.eMode.edit) {
            // Edit mode, owner must be signed in.
            if (!sPathOwnerId) {
                view.ShowStatus("Owner must be signed in to edit geo paths.");
                // Clear the path list in the view.
                view.setPathList(arPath);
            } else {
                // Get all geo paths for the owner.
                GetAllGeoPathsForOwner(function (bOk) {
                    // Show path list obtained, even if there is an error.
                    view.setPathList(arPath);
                });
            }
        } else if (nMode === view.eMode.upload) {
            // Upload mode, owner must be signed in.
            // Note: Should not happen because view.onUpload(..) should be called instead.
            if (!sPathOwnerId) {
                view.ShowStatus("Owner must be signed in to upload geo paths.")
                // Clear the path list in the view.
                view.setPathList(arPath); 
            } 
        }
    };

    view.onPathSelected = function (nMode, iPathList) {
        if (iPathList >= 0 && iPathList < gpxArray.length) {
            var gpx = gpxArray[iPathList];
            // Show the geo path info.
            var args = view.PathInfoArgs();
            args.sOwnerId = gpx.sOwnerId;
            args.name = gpx.sName;
            args.sShare = model.eShare().toStr(gpx.eShare);
            args.path = model.ParseGpxXml(gpx.xmlData); // Parse the xml to get path data.
            view.ShowPathInfo(args);
        }
    };

    view.onAuthenticationCompleted = function (result) {
        // result = {userName: _userName, userID: _userID, accessToken: _accessToken, status: nStatus}
        var eStatus = view.eAuthStatus();
        if (result.status === eStatus.Ok) {
            // Show success, refine later.
            view.ShowStatus("Successfully authenticated by OAuth.", false);
            // Update database for authenticated owner.
            model.authenticate(result.accessToken, result.userID, result.userName, function (result) {
                // Save user info to localStorage.
                model.setOwnerId(result.userID);
                model.setOwnerName(result.userName);
                model.setAccessHandle(result.accessHandle);
                view.setOwnerName(result.userName); 
                view.setOwnerId(result.userID); 
                if (result.status === model.eAuthStatus().Ok) {
                    view.ShowStatus("User successfully logged in.", false);
                    // Cause geo paths to be displayed for user.
                    view.onGetPaths(view.curMode(), view.getOwnerId());
                } else {
                    // var sMsg = "Authentication failed:{0}status: {1}{0}UserID: {2}{0}User Name: {3}{0}AccessHandle: {4}{0}msg: {5}".format("<br/>",result.status, result.userID, result.userName, result.accessHandle, result.msg);
                    // Note: result has info for debug.
                    var sMsg = "Server-side authentication failed.<br/>" +
                               "Please go to Facebook and Log Out<br/>" +
                               "so that your old authentication is reset.";
                    view.ShowStatus(sMsg);
                }
            });
        } else if (result.status === eStatus.Logout) {
            // Note: result not meaningful on logout completed because 
            //       result.userID, result.accessToken have been set to empty.
            // Successfully logged out of OAuth provider (Facebook).
            view.ShowStatus("Successfully logged out by OAuth.", false);
            var sOwnerId = model.getOwnerId();
            var bOwnerIdValid = sOwnerId.length > 0;
            if (bOwnerIdValid) {
                model.logout(function (bOk, sMsg) {
                    if (bOk) {
                        view.ShowStatus("Successfully logged out.", false);
                        // Show geo path for no user logged in.
                        view.onGetPaths(view.curMode(), view.getOwnerId());
                    } else {
                        var sError = "Error logging out: {0}".format(sMsg);
                        view.ShowStatus(sError);
                    }
                });
            } else {
                view.ShowStatus("No owner logged in.");
            }

            // Clear user info in localStorage.
            model.setAccessHandle("");
            model.setOwnerId("");
            model.setOwnerName("");
            // Clear textbox and id view for owner.
            view.clearOwner();
        } else if (result.status === eStatus.Canceled) {
            view.ShowStatus("Login cancelled.", false);
        } else {
            // Show error.
            view.ShowStatus("Authentication failed.");
        }
    };

    // ** Private members
    var xmlGpx = ""; // xml string from a gpx file.

    var gpxArray = null; // Array of wigo_ws_Gpx object obtained from server.

    // Reloads the list of path names in the view. gpxArray has a list 
    // of Gpx objs and is used to form the list of path names.
    function ReloadPathList() {
        if (gpxArray) {
            // Reload the list of paths since the name has changed.
            var arPath = new Array();
            for (var i = 0; i < gpxArray.length; i++) {
                arPath.push(gpxArray[i].sName);
            }
            view.setPathList(arPath);
        }
    }


    // ** Constructor initialization
    view.setOwnerName(model.getOwnerName());
    view.setOwnerId(model.getOwnerId());
    // Need to set mode UI after owner name and id have been set.
    view.setModeUI(view.curMode()); 
}

// Set global var for the controller and therefore the view and model.
window.app = {};
window.app.OnDocReady = function (e) {
    // Create the controller and therefore the view and model therein.
    // Redirect if not https. 
    // Attribution: Thanks to stack overflow, https://stackoverflow.com/questions/4723213/detect-http-or-https-then-force-https-in-javascript
    if (location.protocol !== 'https:')
    {
     location.href = 'https:' + window.location.href.substring(window.location.protocol.length);
    }    
    window.app.ctlr = new wigo_ws_Controller();
};

// Handle DOMCententLoaded event to create the model, view and controller. 
$(document).ready(window.app.OnDocReady);
