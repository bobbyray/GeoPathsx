/* 
Copyright (c) 2015 - 2017 Robert R Schomburg
Licensed under terms of the MIT License, which is given at
https://github.com/bobbyray/MitLicense/releases/tag/v1.0
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Activation;
using System.ServiceModel.Web;
using System.Text;

using System.Web.Compilation;
using System.Web.Configuration;

[ServiceContract(Namespace = "")]
[AspNetCompatibilityRequirements(RequirementsMode = AspNetCompatibilityRequirementsMode.Allowed)]
public class Service
{
	// To use HTTP GET, add [WebGet] attribute. (Default ResponseFormat is WebMessageFormat.Json)
	// To create an operation that returns XML,
	//     add [WebGet(ResponseFormat=WebMessageFormat.Xml)],
	//     and include the following line in the operation body:
	//         WebOperationContext.Current.OutgoingResponse.ContentType = "text/xml";
	[OperationContract]
	public void DoWork()
	{
		// Add your operation implementation here
		return;
	}
    
    /// <summary>
    /// Store record for gpx data into database.
    /// On success, response text is string of JSON indicating result for name duplication:
    ///     {eDup: integer, sName: string, sMsg: string}
    ///         eDup: Duplication result values: 
    ///             NotDup = 0: Not a duplicate. No record with gpx.sName in database. 
    ///             Match = 1: Matched record in database by gpx.sName and gpx.nId. 
    ///             Renamed = 2: Auto renamed to avoid duplication of name in database. 
    ///             Dup = 3: Auto renamed failed. gpx.sName would be a duplicate. No update done. 
    ///             Error = 4: Database access error. 
    ///         sName: For eDup = Renamed, renamed gpx.sName to avoid duplication,
    ///                otherwise gpx.sName.
    ///         sMsg: describes the result.
    ///     NOTE: The response string must be parsed twice by JSON.parse(..).
    ///           The first parse returns a string. The second parse returns 
    ///           the javascript object.
    /// On failure, response text is simply a string describing the error.    
    /// </summary>
    /// <param name="gpx">The data to be stored. 
    /// For gpx.nId == 0, a new record is created, otherwise existing record is updated.</param>
    /// <returns></returns>
    [OperationContract]
    [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json, UriTemplate = "gpxput?ah={accessHandle}")]
    public string GpxPut(Gpx gpx, string accessHandle)
    {
        IDbAccess acc = MyDbAccess.Get();
        DoConnectionCallback oCode = delegate()
        {
            DbResult oDbResult = acc.ValidateAccess(gpx.sOwnerId, accessHandle);
            if (oDbResult.bOk)
            {
                oDbResult = acc.GpxPut(gpx); 
            }
            return oDbResult;
        };
        DbResult result = acc.DoConnection(oCode);

        if (!result.bOk)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }

        return result.sMsg;
    }

    /// <summary>
    /// Deletes record for gpx data from database.
    /// </summary>
    /// <param name="sOwnerId">The owner id of the record to delete.</param>
    /// <param name="sId">The unique record id for the data record to delete. 
    /// <param name="accessHandle">Access handle to validate deletion.</param>
    /// <returns></returns>
    [OperationContract]  
    [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json, UriTemplate = "gpxdelete?ah={accessHandle}")]
    public string GpxDelete(GpxId gpxId, string accessHandle)
    {
        IDbAccess acc = MyDbAccess.Get();
        DoConnectionCallback oCode = delegate()
        {
            DbResult oDbResult = acc.ValidateAccess(gpxId.sOwnerId, accessHandle);
            if (oDbResult.bOk)
            {
                if (gpxId.nId > 0)
                    oDbResult = acc.GpxDelete(gpxId.sOwnerId, gpxId.nId);
                else
                    oDbResult.SetError(DbResult.EResult.ERROR, "Record id number for deletion must be greater than 0.");
            }
            return oDbResult;
        };
        DbResult result = acc.DoConnection(oCode);

        if (!result.bOk)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }

        return result.sMsg;
    }

    /// <summary>
    /// Gets a list of Gpx records from the database.
    /// </summary>
    /// <param name="sOwnerId">owner id of gpx records.</param>
    /// <param name="sShare">Type of sharing with other owners allowed.</param>
    /// <returns></returns>
    /// <remarks>
    /// The http request status code:
    ///     200 for ok.
    ///     403 for forbidden, authentication failed.
    ///     500 for internal server error, database access error.
    /// </remarks>
    [OperationContract]
    [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "gpxgetlist/{sOwnerId}/{sShare}?ah={accessHandle}")]
    public GpxList GpxGetList(string sOwnerId, string sShare, string accessHandle)
    {
        GpxList list = new GpxList();
        byte eShare = Gpx.ShareValue(sShare);
        System.Net.HttpStatusCode httpStatusError = System.Net.HttpStatusCode.InternalServerError;
       
        IDbAccess acc = MyDbAccess.Get();
        DoConnectionCallback oCode = delegate()
        {
            // Do not validate accessHandle if request is for any owner and only public geo paths.
            bool bValidationNeeded = !(Gpx.IsAnyOwnerId(sOwnerId) && 
                                       Gpx.ShareValue("public") == Gpx.ShareValue(sShare));
            DbResult oResult = new DbResult();
            if (bValidationNeeded)
            {
                oResult  = acc.ValidateAccess(sOwnerId, accessHandle);
                if (oResult.nResult == DbResult.EResult.ERROR)
                    httpStatusError = System.Net.HttpStatusCode.Forbidden;
            }
            if (oResult.bOk)
            {
                oResult = acc.GetGpxList(sOwnerId, eShare, list);
            }

            return oResult;
        };
        DbResult result = acc.DoConnection(oCode);

        if (!result.bOk)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = httpStatusError; 
        }

        return list;
    }

    /// <summary>
    /// Gets a list of Gpx records from the database.
    /// </summary>
    /// <param name="sOwnerId">owner id of gpx records.</param>
    /// <param name="sShare">Type of sharing with other owners allowed.</param>
    /// <returns></returns>
    /// <remarks>
    /// The http request status code:
    ///     200 for ok.
    ///     403 for forbidden, authentication failed.
    ///     500 for internal server error, database access error.
    /// </remarks>
    [OperationContract]
    [WebInvoke(Method = "GET", ResponseFormat = WebMessageFormat.Json, UriTemplate = "gpxgetlistbylatlon/{sOwnerId}/{sShare}?latSW={latSW}&lonSW={lonSW}&latNE={latNE}&lonNE={lonNE}&ah={accessHandle}")]
    public GpxList GpxGetListByLatLon(string sOwnerId, string sShare, double latSW, double lonSW, double latNE, double lonNE, string accessHandle)
    {
        GpxList list = new GpxList();
        byte eShare = Gpx.ShareValue(sShare);
        System.Net.HttpStatusCode httpStatusError = System.Net.HttpStatusCode.InternalServerError;

        IDbAccess acc = MyDbAccess.Get();
        DoConnectionCallback oCode = delegate()
        {
            // Do not validate accessHandle if request is for any owner and only public geo paths.
            bool bValidationNeeded = !(Gpx.IsAnyOwnerId(sOwnerId) &&
                                       Gpx.ShareValue("public") == Gpx.ShareValue(sShare));
            DbResult oResult = new DbResult();
            if (bValidationNeeded)
            {
                oResult = acc.ValidateAccess(sOwnerId, accessHandle);
                if (oResult.nResult == DbResult.EResult.ERROR)
                    httpStatusError = System.Net.HttpStatusCode.Forbidden;
            }
            if (oResult.bOk)
            {
                GeoPt gptSW = new GeoPt();
                gptSW.lat = latSW; 
                gptSW.lon = lonSW; 
                GeoPt gptNE = new GeoPt();
                gptNE.lat = latNE; 
                gptNE.lon = lonNE; 
                oResult = acc.GetGpxListByLatLon(sOwnerId, eShare, gptSW, gptNE, list);
            }

            return oResult;
        };
        DbResult result = acc.DoConnection(oCode);

        if (!result.bOk)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = httpStatusError;
        }

        return list;
    }


	/// <summary>
	/// Authenticates a user.
	/// </summary>
	/// <param name="auth">Data for user to be authenticated</param>
	/// <returns>Json for the result.</returns>
    [OperationContract]
    [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json, UriTemplate = "authenticate")]
    public AuthResult Authenticate(AuthData auth)
    {
        FbServerAuth verify = new FbServerAuth();
        verify.appID = WebConfigurationManager.AppSettings["appWigoGeoTrailID"];
        verify.appSecret = WebConfigurationManager.AppSettings["appWigoGeoTrailSecret"]; // need to securely get via web.config backing file.

        FbServerAuth.Result verifyResult = verify.Authenticate(auth);
        AuthResult authResult = new AuthResult(); // Initialized to indicate failure.
        authResult.status = (int)verifyResult.status;
        authResult.msg = verifyResult.msg;
        if (verifyResult.status == FbServerAuth.EAuthStatus.Ok)
        {
            // Note: Currently using auth.accessToken as the accessHandle in OwnerRec.
            //       If later decide a shorter access handle is desirable, need to
            //       generate a random accessHandle here instead of using accessToken.
            //       I think using the accessToken, which came from OAuth provider
            //       (Facebook) should be fine. I think accessToken length is 221 chars.
            IDbAccess acc = MyDbAccess.Get();
            DoConnectionCallback oCode = delegate()
            {
                DbResult oResult = acc.UpdateOwner(auth.userID, auth.userName, auth.accessToken);
                return oResult;
            };
            DbResult dbResult = acc.DoConnection(oCode);
            if (dbResult.bOk)
            {
                authResult.userID = auth.userID;
                authResult.userName = auth.userName;
                authResult.accessHandle = auth.accessToken;
            }
            else
            {
                authResult.status = (int)FbServerAuth.EAuthStatus.Error;
            }
        }
        
        return authResult;
    }

    // Clears authentication for a user, which is a owner record in the database.
    /// <summary>
    /// Clears authentication for a user, which refers to an owner record.
    /// Returns string indicating the result.
    /// </summary>
    /// <param name="authData">Info to logout authenticated owner.</param>
    /// <returns></returns>
    [OperationContract]
    [WebInvoke(Method = "POST", RequestFormat = WebMessageFormat.Xml, ResponseFormat = WebMessageFormat.Json, UriTemplate = "logout")]
    public string Logout(LogoutData authData)
    {
        IDbAccess acc = MyDbAccess.Get();
        DoConnectionCallback oCode = delegate()
        {
            DbResult oResult = acc.Logout(authData.userID, authData.accessHandle);
            return oResult;
        };
        DbResult dbResult = acc.DoConnection(oCode);

        if (!dbResult.bOk)
        {
            WebOperationContext.Current.OutgoingResponse.StatusCode = System.Net.HttpStatusCode.InternalServerError;
        }

        return dbResult.sMsg;
    }

    
    // Add more operations here and mark them with [OperationContract]

}

/// <summary>
/// Defines accessor for database. 
/// To use a different kind of database, change this class to
/// get the accessor for the appropriate class for the database.
/// Should be only place in code to change, except of course to
/// implement the class derived from IDbAccess for the diffent 
/// kind of database.
/// </summary>
/// <remarks>Note: Need to define a class to Get() the accessor
/// rather using a simple function because a simple function cannot
/// be defined at name space level.</remarks>
public class MyDbAccess
{
    public static IDbAccess Get()
    {
        // Change next statement to use some other kind of database access.
        // The class for the other kind of database access must implement IDbAccess.
        return new DbMySqlAccess();
    }
}


/// <summary>
/// Class for exhanging GPX data.
/// GPX is standard using XML to define paths of track points in 
/// lattitude/longitude coordinates. 
/// </summary>
[DataContract]
public class Gpx 
{
    /// <summary>
    /// Returns true is sOwnerId have a value that indicates any owner.
    /// </summary>
    /// <param name="sOwnerId">Owner id to check.</param>
    /// <returns></returns>
    static public bool IsAnyOwnerId(string sOwnerId)
    {
        string sAny = sOwnerId.ToLower().Trim();
        bool bYes = sAny == "any";
        return bYes;
    }

    /// <summary>
    /// Returns byte value for enumeration of sShares.
    /// sShare indicated how a gpx database reccord is shared.
    /// </summary>
    /// <param name="sShare">public->0, protected->1, private->2</param>
    /// <returns></returns>
    /// <remarks>
    /// Share indicates how a record in the gpx database is shared with other people.
    /// public indicates anyone can access the record.
    /// protected indicates only the owner and friends of the owner can access the record.
    /// private indicates only the owner can access the record.
    /// any indicates don't care about share state when reading a record.
    ///     Note: Do not use any for writing a record.
    /// </remarks>
    static public byte ShareValue(string sShare)
    {
        byte eShare = 2; // private.
        sShare = sShare.ToLower().Trim();
        if (sShare == "public")
            eShare = 0;
        else if (sShare == "protected")
            eShare = 1;
        else if (sShare == "any")
            eShare = 3;
        return eShare;
    }

    // Returns true if eShare indicates.
    // Note: any should only be used for reading, never for writing a record.
    static public bool IsAnyShare(byte eShare)
    {
        bool bYes = eShare == 3;
        return bYes;
    }

    
    /// <summary>
    /// Id for the GPX data.
    /// </summary>
    [DataMember]
    public int nId 
    {
        get { return _nId; }
        set { _nId = value;}
    }
    int _nId;

    /// <summary>
    /// Id for owner of the GPX data.
    /// </summary>
    [DataMember]
    public string sOwnerId
    {
        get {return _sOwnerId;}
        set {_sOwnerId = value;}
    }
    string _sOwnerId;

    /// <summary>
    /// Indicate if GPX is public available to anyone.
    /// If false, can only be accessed by sOwnerId.
    /// </summary>
    [DataMember]
    public byte eShare 
    {
        get { return _eShare; }
        set { _eShare = value; }
    }   
    byte _eShare;

    /// <summary>
    /// Name of path, short description.
    /// </summary>
    [DataMember]
    public string sName
    {
        get { return _sName; }
        set { _sName = value; }
    }
    string _sName;

    /// <summary>
    /// Latitude/longitude for beginning point of path.
    /// </summary>
    [DataMember]
    public GeoPt gptBegin
    {
        get { return _gptBegin; }
        set { _gptBegin = value; }
    }
    GeoPt _gptBegin = new GeoPt();

    /// <summary>
    /// Latitude/longitude for ending point of path.
    /// </summary>
    [DataMember]
    public GeoPt gptEnd
    {
        get { return _gptEnd; }
        set { _gptEnd = value; }
    }
    GeoPt _gptEnd = new GeoPt();

    /// <summary>
    /// SouthWest corner of point of rect containing the path.
    /// </summary>
    [DataMember]
    public GeoPt gptSW
    {
        get { return _gptSW; }
        set { _gptSW = value; }
    }
    GeoPt _gptSW = new GeoPt();

    /// <summary>
    /// NorthEast corner of point of rect containing the path.
    /// </summary>
    [DataMember]
    public GeoPt gptNE
    {
        get { return _gptNE; }
        set { _gptNE = value; }
    }
    GeoPt _gptNE = new GeoPt();

    /// <summary>
    /// Date/Time record was last modified.
    /// </summary>
    [DataMember]
    public DateTime tModified
    {
        get { return _tModified; }
        set { _tModified = value; }
    }
    DateTime _tModified = new DateTime(0);

    /// <summary>
    /// XML for the GPX data.
    /// </summary>
    [DataMember]
    public string xmlData
    {
        get { return _xmlData; }
        set { _xmlData = value; }
    }
    string _xmlData;
}

/// <summary>
/// Class to identity a Gpx data record to delete.
/// </summary>
[DataContract]
public class GpxId
{
    /// <summary>
    /// Owner id for a Gpx data record.
    /// </summary>
    [DataMember]
    public string sOwnerId
    {
        get { return _sOwnerId; }
        set { _sOwnerId = value; }
    }
    string _sOwnerId = "";

    /// <summary>
    /// Unique record id for a Gpx data record.
    /// </summary>
    [DataMember]
    public int nId
    {
        get { return _nId; }
        set { _nId = value; }
    }
    int _nId = 0;
}

/// <summary>
/// Geolocation, ie Latitude and Longitude, for point in a path.
/// </summary>
[DataContract]
public class GeoPt
{
    /// <summary>
    ///  Latitude for a geo location point.
    /// </summary>
    [DataMember]
    public double lat 
    {
        get { return _lat; }
        set { _lat = value; }
    }
    double _lat;

    /// <summary>
    ///  Longitude for a geo location point.
    /// </summary>
    [DataMember]
    public double lon
    {
        get { return _lon; }
        set { _lon = value; }
    }
    double _lon;
}

/// <summary>
/// Json array for list of Gpx elements.
/// </summary>
/// <remarks>
/// Base List is serialized to json array.
/// Note: Serialization fails if data members are added to GpxList.
/// Instead form a class with [DataContract] attribute which has a data member of 
/// of type GpxList (this class) with attribute of [DataContract]
/// (the attribute of the data member is NOT [CollectionDataContract]).
/// </remarks>
[CollectionDataContract]
public class GpxList : List<Gpx>
{
    public GpxList() { }
    public GpxList(List<Gpx> list) : base(list) { }
}

/// <summary>
/// Data to verify authentication deserialized from json.
/// </summary>
[DataContract]
public class AuthData
{
    /// <summary>
    /// Access token obtained by client.
    /// </summary>
    [DataMember]
    public string accessToken
    {
        get { return _accessToken;}
        set {_accessToken = value;}
    }
    string _accessToken;

    /// <summary>
    /// User id obtained by client used as owner id at server.
    /// </summary>
    [DataMember]
    public string userID
    {
        get { return _userID; }
        set { _userID = value; }
    }
    string _userID;

    // User name obtained by client.
    [DataMember]
    public string userName 
    {
        get { return _userName; }
        set { _userName = value; }
    }
    string _userName;
}

/// <summary>
/// Logout data to revoke authentication in server database for an owner.
/// </summary>
[DataContract]
public class LogoutData
{
    /// <summary>
    /// Access handle obtained by client from the server.
    /// </summary>
    [DataMember]
    public string accessHandle
    {
        get { return _accessHandle; }
        set { _accessHandle = value; }
    }
    string _accessHandle;

    /// <summary>
    /// User id obtained by client used as owner id at server.
    /// </summary>
    [DataMember]
    public string userID
    {
        get { return _userID; }
        set { _userID = value; }
    }
    string _userID;
    
}


/// <summary>
/// Result for verifying authentication serialized to json.
/// </summary>
[DataContract]
public class AuthResult
{
    /// <summary>
    /// Result code:
    ///     enum EAuthStatus cast as int.
    /// </summary>
    [DataMember]
    public int status
    {
        get { return _status; }
        set { _status = value; }
    }
    int _status = (int)FbServerAuth.EAuthStatus.Failed;

    /// <summary>
    /// Id to access data for the authenticated user.
    /// </summary>
    [DataMember]
    public string accessHandle {
        get { return _accessHandle; }
        set { _accessHandle = value; }
    }
    string _accessHandle = String.Empty;

    /// <summary>
    /// User id of authenticated user.
    /// </summary>
    [DataMember]
    public string userID
    {
        get { return _userID; }
        set { _userID = value; }
    }
    string _userID = String.Empty;

    // User Name of authenticated user.
    [DataMember]
    public string userName
    {
        get { return _userName; }
        set { _userName = value; }
    }
    string _userName = String.Empty;

    // Message describing the result.
    [DataMember]
    public string msg 
    {
        get {return _msg;}
        set {_msg = value;}

    }
    string _msg = String.Empty;
}
