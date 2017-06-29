/* 
Copyright (c) 2015 - 2017 Robert R Schomburg
Licensed under terms of the MIT License, which is given at
https://github.com/bobbyray/MitLicense/releases/tag/v1.0
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Wigo.MySqlAccess;
using System.Runtime.Serialization;
using MySql.Data.MySqlClient;

/// <summary>
/// Access to MySql database for the GeoPaths.
/// </summary>
public class DbMySqlAccess : IDbAccess
{
	public DbMySqlAccess()
	{
		//
		// TODO: Add constructor logic here
		//
        Wigo.MySqlAccess.MySqlHelper.ConnectionName = "GeoPathString";

	}

    // ** Connection methods
    /// <summary>
    /// Open a connection with the database, calls a callback function, then 
    /// closes the connection releasing associated resources.
    /// Returns result from the callback function. 
    /// </summary>
    /// <param name="fnCallback">
    /// The callback function with signature: DbResult function ().
    /// </param>    
    /// <returns></returns>
    public DbResult DoConnection(DoConnectionCallback fnCallback)
    {
        DbResult result = null;
        string sDsn = Wigo.MySqlAccess.MySqlHelper.GetDsnForSqlConnection();
        if (String.IsNullOrEmpty(sDsn))
        {
            result = new DbResult();
            SetError(result, DbResult.EResult.DSN_ERROR);
        }
        else
        {
            using (conn = new MySqlConnection(sDsn))
            {
                conn.Open();
                result = fnCallback();
                conn.Close();
            }
            conn = null;
        }
        if (result == null)
        {
            // Should not happen. fnCallback() should return result. 
            result = new DbResult();
            result.SetError(DbResult.EResult.ERROR, "Error during DoConnection() callback for MySql.");
        }
        return result;
    }

    /// <summary>
    /// Not used. Use DoConnection(..) instead.
    /// </summary>
    /// <returns></returns>
    public DbResult OpenConnection() 
    {
        DbResult result = new DbResult();
        result.SetError(DbResult.EResult.ERROR, "OpenConnection() not supported. Use DoConnection(..) instead.");
        return result; 
    }

    /// <summary>
    /// Not used. Use DoConnection(..) instead.
    /// </summary>
    /// <returns></returns>
    public DbResult CloseConnection() 
    {
        DbResult result = new DbResult();
        result.SetError(DbResult.EResult.ERROR, "CloseConnection() not supported. Use DoConnection(..) instead.");
        return result;
    }

    // ** Data Access Methods
    /// <summary>
    /// Stores gpx object into database.
    /// Returns result of database access.
    /// </summary>
    /// <param name="gpx">Object to store. 
    /// For gpx.nId == 0:
    ///     Creates (inserts) new record.
    /// For gpx.nId > 0:
    ///     Updates existing record.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// Checks for duplicate path name and renames path name (gpx.sName) to avoid
    /// duplicate path in database.
    /// For return object DbResult:
    ///     For .bOk false, sMsg is a string describing the error.
    ///     For .bOk true, sMsg is JSON describing the duplication state:
    ///         {eDup: (int)EDuplicate, sMsg: string}
    /// </remarks>
    public DbResult GpxPut(Gpx gpx)  
    {
        gpx.sName = gpx.sName.Trim(); // White space trimmed to aid checking for duplicate names in database.
        DbResult result = new DbResult(); // result is initially ok.
        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            bool bOkToPut = false;
            CheckDuplicateResult dupResult = new CheckDuplicateResult(gpx.sName, gpx.nId);
            CheckForDuplicateName(gpx, gpx.sName, 100, dupResult); // 100 is max recursion to rename for duplicate.
            switch (dupResult.eDup)
            {
                case EDuplicate.NotDup:
                case EDuplicate.Match:
                case EDuplicate.Renamed:
                    bOkToPut = true;
                    result.sMsg = dupResult.json(); 
                    gpx.sName = dupResult.Name; // Note: Name may be auto renamed because of duplication.
                    break;
                case EDuplicate.Dup:
                    // Note: auto rename failed trying to avoid name duplication.
                    bOkToPut = false;
                    result.sMsg = dupResult.json(); 
                    break;
                case EDuplicate.Error:
                default:
                    bOkToPut = false; 
                    result.bOk = false;
                    result.nResult = DbResult.EResult.ERROR;
                    result.sMsg = "Database error occurred checking for duplicate name.";
                    break;
            }

            if (bOkToPut)
            {
                // Ok to update/insert database record.
                GeoPathRec rec = new GeoPathRec(gpx);
                rec.bDeleted = false; 
                rec.tModified = DateTime.Now;
                // Ensure any share state is not saved in database.
                if (Gpx.IsAnyShare(rec.eShare))
                    rec.eShare = Gpx.ShareValue("public");

                MySqlTableAccess.EOpResult nOpResult = MySqlTableAccess.EOpResult.FAILED;
                if (rec.nId == 0)
                {
                    // Create new record.
                    nOpResult = rec.Insert(conn);
                    if (!MySqlTableAccess.IsOpOk(nOpResult))
                    {
                        SetError(result, DbResult.EResult.INSERT_FAILED, nOpResult);
                    }
                    else 
                    {
                        // Set result.sMsg to indicate new record sq id.
                        dupResult.nId = rec.nId;
                        result.sMsg = dupResult.json(); 
                    }
                }
                else
                {
                    // Update existing record.
                    nOpResult = rec.Update(conn);
                    if (!MySqlTableAccess.IsOpOk(nOpResult))
                        SetError(result, DbResult.EResult.UPDATE_FAILED, nOpResult);
                }
            }
        }

        return result; 
    }


    /// <summary>
    /// Deletes gpx object from database.
    /// Returns result of database access.
    /// </summary>
    /// <param name="sOwnerId">
    /// Owner id for the record to delete. 
    /// </param>
    /// <param name="nId">Unique record id for the record to delete.</param>
    /// <returns></returns>
    public DbResult GpxDelete(string sOwnerId, int nId) 
    {
        DbResult result = new DbResult(); // result is initially ok.
        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            GeoPathRec rec = new GeoPathRec();
            MySqlTableAccess.EOpResult accResult = rec.Select(conn, nId);
            if (accResult == MySqlTableAccess.EOpResult.SUCEEDED) {
                if (rec.sOwnerId == sOwnerId) {
                    rec.bDeleted = true;
                    rec.tModified = DateTime.Now;
                    accResult = rec.Update(conn);
                }
                else
                {
                    accResult = MySqlTableAccess.EOpResult.NOT_FOUND;
                }
            }
            if (accResult != MySqlTableAccess.EOpResult.SUCEEDED)
            {
                SetError(result, DbResult.EResult.DELETE_FAILED);
            }
        }

        return result;
    }




    /// <summary>
    /// Gets a list of GeoPathRec records found in database.
    /// Returns result of database access.
    /// </summary>
    /// <param name="sOwnerId">Id of owner of Gpx records in database.
    /// If sOwner == any, any record matching eShare is valid to included.</param>
    /// <param name="eShare">Indicates sharing of record: public, private or protected.
    /// Note: static Gpx.ShareValue(sShare) in the web service enumerates the sharing values.</param>
    /// <param name="list">Ref list [out] that is filled from database.</param>
    /// <returns></returns>
    public DbResult GetGpxList(string sOwnerId, byte eShare, GpxList list)
    {
        DbResult result = new DbResult();
        // Ensure output list is empty before filling it.
        list.Clear();

        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            // Find list of record in database.
            GeoPathRec rec = new GeoPathRec();
            string sExpr = String.Format("sOwnerId = '{0}' and eShare = {1} and bDeleted = FALSE", sOwnerId, eShare);
            if (Gpx.IsAnyOwnerId(sOwnerId))
                sExpr = String.Format("eShare = {0} and bDeleted = FALSE", eShare);  // Don't care what sOwnerId is.
            if (Gpx.IsAnyShare(eShare))
                sExpr = String.Format("sOwnerId = '{0}' and bDeleted = FALSE", sOwnerId); // Don't care what eShare is.
            List<MySqlTableAccess> liFound = new List<MySqlTableAccess>();
            MySqlTableAccess.EOpResult opResult = rec.SelectByExpr(conn, sExpr, liFound);
            // Fill list of Gpx elements to return.
            foreach (MySqlTableAccess recFound in liFound)
            {
                GeoPathRec elFound = recFound as GeoPathRec; // Element to add to output list.
                if (elFound == null)
                {
                    // Cast failed. Should not happen, code error.
                    result.SetError(DbResult.EResult.ERROR, "GeoPathRec in database cast incorrectly.");
                    break;
                }
                
                list.Add(elFound.ToGpx());
            }
        }
        return result;
    }

    /// <summary>
    /// Sets a list of Gpx obj found in database that are within a geo rectangle.
    /// Returns result of of database access.
    /// </summary>
    /// <param name="sOwnerId">Id of owner of Gpx records in database.
    /// If null, any owner record is valid to included.</param>
    /// <param name="eShare">Indicates sharing of record: public, private or protected.</param>
    /// <param name="gptSW">SouthWest corner of rectangle.</param>
    /// <param name="gptNE">NorthEast corner of rectangle.</param>
    /// <param name="list">Ref to list [out] that is filled database.</param>
    /// <returns></returns>
    public DbResult GetGpxListByLatLon(string sOwnerId, byte eShare, GeoPt gptSW, GeoPt gptNE, GpxList list)
    {
        DbResult result = new DbResult();
        // Ensure output list is empty before filling it.
        list.Clear();

        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            // Find list of record in database.
            GeoPathRec rec = new GeoPathRec();
            // Form where part of expression to select by owner id or sharing.
            string sExpr = String.Format("sOwnerId = '{0}' and eShare = {1} and bDeleted = FALSE", sOwnerId, eShare);
            if (Gpx.IsAnyOwnerId(sOwnerId))
                sExpr = String.Format("eShare = {0} and bDeleted = FALSE", eShare);  // Don't care what sOwnerId is.
            if (Gpx.IsAnyShare(eShare))
                sExpr = String.Format("sOwnerId = '{0}' and bDeleted = FALSE", sOwnerId); // Don't care what eShare is.

            // Form where part of expression to find begin point or end point of trail.
            string sBeginPtExpr = FormLatLonExpr("latBegin", "lonBegin", gptSW, gptNE);
            string sEndPtExpr = FormLatLonExpr("latEnd", "lonEnd", gptSW, gptNE);
            string sLatLonExpr = String.Format("(({0}) or ({1}))", sBeginPtExpr, sEndPtExpr);
            // Form total where part of expression by anding latlon part of expression.
            sExpr += " and " + sLatLonExpr;
            
            List<MySqlTableAccess> liFound = new List<MySqlTableAccess>();
            MySqlTableAccess.EOpResult opResult = rec.SelectByExpr(conn, sExpr, liFound);
            // Fill list of Gpx elements to return.
            foreach (MySqlTableAccess recFound in liFound)
            {
                GeoPathRec elFound = recFound as GeoPathRec; // Element to add to output list.
                if (elFound == null)
                {
                    // Cast failed. Should not happen, code error.
                    result.SetError(DbResult.EResult.ERROR, "GeoPathRec in database cast incorrectly.");
                    break;
                }

                list.Add(elFound.ToGpx());
            }
        }
        return result;
    }


    /// <summary>
    /// Validates access to database for an owner.
    /// </summary>
    /// <param name="sOwnerId">owner id to validate.</param>
    /// <param name="accessHandle">Access handle that owner claims is valid.</param>
    /// <returns></returns>
    public DbResult ValidateAccess(string sOwnerId, string accessHandle)
    {
        DbResult result = null;

        if (conn == null)
        {
            result = new DbResult();
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else 
        {
            OwnerRec rec = new OwnerRec();
            MySqlTableAccess.EOpResult status = rec.ValidateAccess(conn, sOwnerId, accessHandle);
            result = new DbResult();
            if (status != MySqlTableAccess.EOpResult.SUCEEDED)
            {
                result.SetError(DbResult.EResult.ERROR, "Database access denied.");
            }
        }
        
        return result;
    }

    /// <summary>
    /// Updates owner information. If sOwnerId does not exist, creates a new owner.
    /// </summary>
    /// <param name="sOwnerId">Unique id for the owner.</param>
    /// <param name="sName">Name of the owner.</param>
    /// <param name="accessHandle">Access handle to validate database accesss is valid.</param>
    /// <returns></returns>
    public DbResult UpdateOwner(string sOwnerId, string sName, string accessHandle)
    {
        DbResult result = new DbResult(); // Initialized for success.
        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            OwnerRec rec = new OwnerRec();
            MySqlTableAccess.EOpResult status = rec.SelectByOwnerId(conn, sOwnerId);
            if (status == MySqlTableAccess.EOpResult.SUCEEDED ||
                status == MySqlTableAccess.EOpResult.NOT_FOUND)
            {
                rec.sOwnerId = sOwnerId;
                rec.sName = sName;
                rec.accessHandle = accessHandle;
                rec.tModified = DateTime.Now;
                if (status == MySqlTableAccess.EOpResult.NOT_FOUND)
                {
                    rec.nId = 0;
                    status = rec.Insert(conn);
                    if (status != MySqlTableAccess.EOpResult.SUCEEDED)
                        SetError(result, DbResult.EResult.INSERT_FAILED);
                }
                else
                {
                    status = rec.Update(conn);
                    if (status != MySqlTableAccess.EOpResult.SUCEEDED)
                        SetError(result, DbResult.EResult.UPDATE_FAILED);
                }
            }
            else
            {
                // Unknown error.
                SetError(result, DbResult.EResult.ERROR);
            }
        }
        
        
        
        return result;
    }


    /// <summary>
    /// Sets record for owner to indicate logged out, i.e. no longer authenticated.
    /// </summary>
    /// <param name="sOwnerId">Owner id.</param>
    /// <param name="accessHandle">Access handle for verification.</param>
    /// <returns></returns>
    public DbResult Logout(string sOwnerId, string accessHandle)
    {
        DbResult result = new DbResult();
        if (conn == null)
        {
            SetError(result, DbResult.EResult.CONNECTION_INACTIVE);
        }
        else
        {
            OwnerRec rec = new OwnerRec();
            MySqlTableAccess.EOpResult status = rec.SelectByOwnerId(conn, sOwnerId);
            if (status == MySqlTableAccess.EOpResult.SUCEEDED)
            {
                if (rec.accessHandle == accessHandle)
                {
                    rec.accessHandle = String.Empty;
                    rec.tModified = DateTime.Now;
                    status = rec.Update(conn);
                    if (status != MySqlTableAccess.EOpResult.SUCEEDED)
                    {
                        result.SetError(DbResult.EResult.UPDATE_FAILED, "Logout Update failed.");
                    }
                }
                else
                {
                    result.SetError(DbResult.EResult.ERROR, "Logout verification failed.");
                }
            }
            else
            {
                result.SetError(DbResult.EResult.UPDATE_FAILED, "Logout owner not found.");
            }
        }
        return result;
    }



    // ** Other Members 
    protected MySqlConnection conn = null; // Active connection to mysql database.

    /// <summary>
    /// Sets resultInfo to indicate an error.
    /// </summary>
    /// <param name="resultInfo">ref to object set.</param>
    /// <param name="nResult">enum value indicating the result.</param>
    /// <remarks>
    /// resultInfo.sMsg is set to text describing nResult.
    /// </remarks>
    protected void SetError(DbResult resultInfo, DbResult.EResult nResult)
    {
        resultInfo.bOk = false;
        resultInfo.nResult = nResult;
        resultInfo.sMsg = resultInfo.GetResultText();
    }

    /// <summary>
    /// Sets resultInfo to indicate an error.
    /// </summary>
    /// <param name="resultInfo">ref to object set.</param>
    /// <param name="nResult">enum value indicating the result.</param>
    /// <param name="opResult">enum value for MySql table access op.</param>
    /// <remarks>
    /// resultInfo.sMsg is set to text describing nResult, followed by string giving enum name of opResult.
    /// </remarks>
    protected void SetError(DbResult resultInfo, DbResult.EResult nResult, MySqlTableAccess.EOpResult opResult)
    {
        resultInfo.bOk = false;
        resultInfo.nResult = nResult;
        string sMsg = String.Format("{0}<br/>MySql Result Code: {1}", resultInfo.GetResultText(), opResult.ToString());
        resultInfo.sMsg = sMsg;

    }

    /// <summary>
    /// Enumeration for CheckDuplicateResult(..) class.
    ///     NotDup: Not a duplicate. No record found with the name.
    ///     Match: Matched an existing record by id and name. 
    ///     Renamed: Renamed with #n suffix to form name that is not a duplicate.
    ///     Dup: Duplicate name, a different record with same name already exists.
    ///     Error: Database access error occurred trying to check for duplicate.
    /// Note: When auto renamed is used, Dup should not be possible possible state.
    /// </summary>
    protected enum EDuplicate {NotDup = 0, Match = 1, Renamed = 2, Dup = 3, Error = 4};

    // Class for result of CheckForDuplicateName(..) method.
    protected class CheckDuplicateResult
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sName">Name being check for duplication.</param>
        public CheckDuplicateResult(string sName, int nId) 
        {
            if (String.IsNullOrEmpty(sName))
                throw new ArgumentException("sName cannot be null or empty string.");
            this.sName = sName;
            this.nId = nId;
        }

        /// <summary>
        /// Indicates if there is duplicate name, or not, or an error accessing database to check.
        /// </summary>
        public EDuplicate eDup = EDuplicate.Error;
        
        /// <summary>
        /// Returns true if name has been renamed because of duplication.
        /// Note: May true even if eDup is EDuplicate.Error.
        /// </summary>
        /// <returns></returns>
        public bool IsRenamed()
        {
            bool bYes = iRename > 0;
            return bYes;
        }

        public string RenameDup(string sName) {
            this.sReName = String.Format("{0} #{1}", sName, ++iRename);
            return this.sReName;
        }

        // Readonly property for name.
        public string Name
        {
            get 
            {
                string s = String.IsNullOrEmpty(sReName) ? sName : sReName;
                return s;
            }
        }

        // Read/write property for database record id.
        public int nId 
        {
            get 
            { 
                return nRecId; 
            }
            set 
            { 
                nRecId = value; 
            }
        }

        /// <summary>
        /// Returns a JSON string for this object.
        /// JSON {eDup: int, sName: string, sMsg: string}
        ///     eDup: integer for EDuplicate values:
        ///         NotDup = 0, Match = 1, Renamed = 2, Dup = 3, Error = 4
        ///     sName: string for name. Original name, unless eDup is Renamed, in which
        ///         case the renamed name.
        ///     sMsg: description for eDup.
        /// </summary>
        /// <returns></returns>
        public string json()
        {
            string s;
            string jsObj;
            switch (eDup)
            {
                case EDuplicate.Dup:
                    s = String.Format("{0} is a duplicate name in database.", sName);
                    jsObj = MsgJson(this.sName, s);
                    break;
                case EDuplicate.NotDup:
                    s = String.Format("{0} is not a duplicate name in database.", sName);
                    jsObj = MsgJson(this.sName, s);
                    break;
                case EDuplicate.Renamed:
                    s = String.Format("{0} renamed to {1} to avoid duplicate name in database.", sName, sReName);
                    jsObj = MsgJson(sReName, s);
                    break;
                case EDuplicate.Match:
                    s = String.Format("{0} is same name as its database record name.", sName);
                    jsObj = MsgJson(this.sName, s);
                    break;
                case EDuplicate.Error:
                default:
                    s = String.Format("Database access error checking for duplicate name {0}.", sName);
                    jsObj = MsgJson(this.sName, s);
                    break;
            }
            return jsObj;
        }

        protected int iRename = 0;                // Count of renaming.
        protected string sName = String.Empty;    // Original name set by constructor.
        protected string sReName = String.Empty;  // Rename for a duplicate name.
        protected int nRecId = 0;  // Record id.

        /// <summary>
        /// Retuns a JSON string for the duplication state:
        ///     {eDup: this.eDup, sMsg: sMsg_arg}
        /// </summary>
        /// <param name="sName">Name used. (Either this.sName or this.sReName.</param>
        /// <param name="sMsg">Message describing the state.</param>
        /// <returns></returns>
        protected string MsgJson(string sName, string sMsg)
        {
            string jsObj = String.Format("{0}\"eDup\": {1}, \"sName\": \"{2}\", \"sMsg\": \"{3}\", \"nId\": {4}{5}",
                                         "{", (int)eDup, sName, sMsg, nId, "}");
            return jsObj;
        }
    }
    
    /// <summary>
    /// Checks if there is already a record in the database with the same
    /// sName field as gpx.sName. For duplicate name found, renames the path name
    /// and tries again.
    /// </summary>
    /// <param name="gpx">Gpx obj to check if there is duplicate name in datebase.</param>
    /// <param name="sName">Path name being checked.</param>
    /// <param name="iRecursion">Recursion count. 
    /// Recursion occurs on duplicate name as long as iRecursion is >= 0.</param>
    /// To check without recursion for renaming, set to 0.
    /// <param name="dupResult">
    /// [in] Construct with path name.
    /// [out] Indicates the result of checking.
    /// </param>
    /// <returns></returns>
    protected void CheckForDuplicateName(Gpx gpx, string sName, int iRecursion, CheckDuplicateResult dupResult)
    {
        // Check for excessive recurion for trying to find duplicate name.
        if (iRecursion-- < 0)
        {
            dupResult.eDup = EDuplicate.Dup;
            return;
        }
        // Do not allow duplicate records with same path name.
        GeoPathRec recDup = new GeoPathRec();
        string sExpr = String.Format("sName = '{0}' and bDeleted = FALSE", sName);
        List<MySqlTableAccess> liDup = new List<MySqlTableAccess>();
        MySqlTableAccess.EOpResult opResult = recDup.SelectByExpr(conn, sExpr, liDup);
                
        switch (opResult)
        {
            case MySqlTableAccess.EOpResult.SUCEEDED:
                // Check if any record found by path name is the same as the gpx record.
                bool bSameRec = false;
                foreach (MySqlTableAccess el in liDup)
                {
                    GeoPathRec dupRec = el as GeoPathRec;
                    if (dupRec == null)
                        break; // Should not happen. 
                    bSameRec = dupRec.sName == gpx.sName && dupRec.nId == gpx.nId;
                    if (bSameRec)
                        break;
                }
                if (bSameRec)
                {
                    // Updating same record so ok to update record.                     
                    dupResult.eDup = EDuplicate.Match;
                }
                else
                {
                    // Duplicate record found. Rename and check again for duplicate.
                    dupResult.eDup = EDuplicate.Dup;
                    string sReName = dupResult.RenameDup(gpx.sName);
                    CheckForDuplicateName(gpx, sReName, iRecursion, dupResult);

                }
                break;
            case MySqlTableAccess.EOpResult.NOT_FOUND:
                dupResult.eDup = dupResult.IsRenamed() ? EDuplicate.Renamed : EDuplicate.NotDup;
                break;
            default: // Database access error
                dupResult.eDup = EDuplicate.Error;
                break;
        }      
        return;        
    }

    /// Returns expression for finding a geo pt within a geo rectangle.
    /// <param name="sLat">Name of the field (column) for latitude of point to find.</param>
    /// <param name="sLon">Name of the field (column) for longitude of point to find.</param>
    /// <param name="gptSW">Southwest corner of bounding rectangle.</param>
    /// <param name="gptNE">Northeast corner of bounding rectangle.</param>
    protected string FormLatLonExpr(string sLat, string sLon, GeoPt gptSW, GeoPt gptNE)
    {
        string sLatBegin = String.Format("{0} > {1} and {0} < {2}", sLat, gptSW.lat, gptNE.lat);
        string sLonBegin = String.Format("{0} > {1} and {0} < {2}", sLon, gptSW.lon, gptNE.lon);
        string s = String.Format("{0} and {1}", sLatBegin, sLonBegin);
        return s;
    }

}

/// <summary>
/// Record for MySql table geopath.
/// </summary>
public class GeoPathRec : MySqlTableAccess
{
    /// <summary>
    /// Id for the GPX data.
    /// </summary>
    public int nId
    {
        get { return svcObj.nId; }
        set { svcObj.nId = value; }
    }

    /// <summary>
    /// Id for owner of the GPX data.
    /// </summary>
    public string sOwnerId
    {
        get { return svcObj.sOwnerId; }
        set { svcObj.sOwnerId = value; }
    }

    /// <summary>
    /// Indicate if GPX is public available to anyone.
    /// If false, can only be accessed by sOwnerId.
    /// </summary>
    public byte eShare
    {
        get { return svcObj.eShare; }
        set { svcObj.eShare = value; }
    }

    /// <summary>
    /// Name of path, short description.
    /// </summary>
    public string sName
    {
        get { return svcObj.sName; }
        set { svcObj.sName = value; }
    }

    /// <summary>
    /// Latitude for beginning point of path.
    /// </summary>
    public double latBegin
    {
        get { return svcObj.gptBegin.lat; }
        set { svcObj.gptBegin.lat = value;}

    }
    
    /// <summary>
    /// Longitude for beginning point of path.
    /// </summary>
    public double lonBegin
    {
        get { return svcObj.gptBegin.lon; }
        set { svcObj.gptBegin.lon = value; }
    }

    /// <summary>
    /// Latitude for ending point of path.
    /// </summary>
    public double latEnd
    {
        get { return svcObj.gptEnd.lat; }
        set { svcObj.gptEnd.lat = value; }
    }
    
    /// <summary>
    /// Longitude for ending point of path.
    /// </summary>
    public double lonEnd
    {
        get { return svcObj.gptEnd.lon; }
        set { svcObj.gptEnd.lon = value; }
    }

    /// <summary>
    /// Latitude of SouthWest corner of rect containing the path.
    /// </summary>
    public double latSW
    {
        get { return svcObj.gptSW.lat; }
        set { svcObj.gptSW.lat = value; }
    }
    
    /// <summary>
    /// Longitude of SouthWest corner of rect containing the path.
    /// </summary>
    public double lonSW
    {
        get { return svcObj.gptSW.lon; }
        set { svcObj.gptSW.lon = value; }
    }

    /// <summary>
    /// Latitude of NorthEast corner of rect containing the path.
    /// </summary>
    public double latNE
    {
        get { return svcObj.gptNE.lat; }
        set { svcObj.gptNE.lat = value; }
    }


    /// <summary>
    /// Longitude of NorthEast corner of rect containing the path.
    /// </summary>
    public double lonNE
    {
        get { return svcObj.gptNE.lon; }
        set { svcObj.gptNE.lon = value; }
    }


    /// <summary>
    /// Date/Time record was last modified.
    /// </summary>
    public DateTime tModified
    {
        get { return svcObj.tModified; }
        set { svcObj.tModified = value; }
    }

    /// <summary>
    /// XML for the GPX data.
    /// </summary>
    public string xmlData
    {
        get { return svcObj.xmlData; }
        set { svcObj.xmlData = value; }
    }

    /// <summary>
    /// Flag indicating record is deleted.
    /// </summary>
    public bool bDeleted 
    {
        get { return _bDeleted; }
        set { _bDeleted = value; }
    }
    protected bool _bDeleted = false;


    // ** Helper Methods
    // Returns the underlying Gpx object for this record.
    public Gpx ToGpx()
    {
        return this.svcObj;
    }

    // ** Specific field information that a derived class provides.
    // Derived class must add record type to table type map by calling MySqlTableAccess.AddTableTypeToMap(string sTableName, Type typeof(class_name)) 

    /// <summary>
    /// Readonly property for name of the table.
    /// </summary>
    override public string TableName 
    { 
        get { return sTABLE_NAME; } 
    }

    /// <summary>
    /// Read/Write property for the identity field of the record.
    /// </summary>
    override public int IdSq 
    { 
        get { return this.nId; } 
        set { this.nId = value; } 
    }

    /// <summary>
    /// Readonly property for column name of IdSq field.
    /// </summary>
    override public string IdName 
    { 
        get { return "nId";} 
    }

    /// <summary>
    /// Indexer of a column value within this object.
    /// Returns object value for column i. 
    /// Returns null if column i index is out of range.
    /// </summary>
    /// <param name="iCol">Index for column value, origin 0.</param>
    /// <returns></returns>
    override protected Column this[int iCol] 
    {
        get
        {
            Column oCol = new Column();
            switch (iCol)
            {
                case 0: oCol.oValue = nId; oCol.sName = "nId"; oCol.oType = typeof(int); break;
                case 1: oCol.oValue = sOwnerId; oCol.sName = "sOwnerId"; oCol.oType = typeof(string); break;
                case 2: oCol.oValue = eShare; oCol.sName = "eShare"; oCol.oType = typeof(byte); break;
                case 3: oCol.oValue = sName; oCol.sName = "sName"; oCol.oType = typeof(string); break;
                case 4: oCol.oValue = latBegin; oCol.sName = "latBegin"; oCol.oType = typeof(double); break;
                case 5: oCol.oValue = lonBegin; oCol.sName = "lonBegin"; oCol.oType = typeof(double); break;
                case 6: oCol.oValue = latEnd; oCol.sName = "latEnd"; oCol.oType = typeof(double); break;
                case 7: oCol.oValue = lonEnd; oCol.sName = "lonEnd"; oCol.oType = typeof(double); break;
                case 8: oCol.oValue = latSW; oCol.sName = "latSW"; oCol.oType = typeof(double); break;
                case 9: oCol.oValue = lonSW; oCol.sName = "lonSW"; oCol.oType = typeof(double); break;
                case 10: oCol.oValue = latNE; oCol.sName = "latNE"; oCol.oType = typeof(double); break;
                case 11: oCol.oValue = lonNE; oCol.sName = "lonNE"; oCol.oType = typeof(double); break;
                case 12: oCol.oValue = tModified; oCol.sName = "tModified"; oCol.oType = typeof(DateTime); break;
                case 13: oCol.oValue = xmlData; oCol.sName = "xmlData"; oCol.oType = typeof(string); break;
                case 14: oCol.oValue = bDeleted; oCol.sName = "bDeleted"; oCol.oType = typeof(bool); break; 
                default:
                    oCol = null;
                    break;
            }
            return oCol;
        }

        set
        {
            switch (iCol)
            {
                case 0: nId = (int)value.oValue; break;
                case 1: sOwnerId = (string)value.oValue; break;
                case 2: eShare = (byte)value.oValue; break;
                case 3: sName = (string)value.oValue; break;
                case 4: latBegin = (double)value.oValue; break;
                case 5: lonBegin = (double)value.oValue; break;
                case 6: latEnd = (double)value.oValue; break;
                case 7: lonEnd = (double)value.oValue; break;
                case 8: latSW = (double)value.oValue; break;
                case 9: lonSW = (double)value.oValue; break;
                case 10: latNE = (double)value.oValue; break;
                case 11: lonNE = (double)value.oValue; break;
                case 12: tModified = (DateTime)value.oValue; break;
                case 13: xmlData = (string)value.oValue; break;
                case 14: bDeleted = (bool)value.oValue; break;  
            }
        }
    }

    // No ovrride.
    // virtual protected string ColumnValue(int iCol, out MySqlParameter oParam)
    // virtual protected bool ReadColumn(int iCol, MySqlDataReader r)

    /// <summary>
    /// Returns a new record object of the derived class.
    /// Note: This useful for getting a working obj that does 
    ///       not disturb this ojbject.
    /// </summary>
    /// <returns></returns>
    override protected MySqlTableAccess New()
    {
        return new GeoPathRec();
    }
    
    /// <summary>
    /// Sets foreign key map needed by HistRec to save change history.
    /// </summary>
    override public void SetForeignKeyMap()
    {
        // No foreign keys.
        mapForeignKey.Clear();
    }


    /// Readonly property that indicates if change history
    /// is saved when database record is changed. 
    /// GeoPathRec's are not saved in the hist table.
    /// </summary>
    protected override bool bSaveHistory
    {
        get { return false; }
    }

    protected const string sTABLE_NAME = "geopath";
    static private bool bTableTypeMapInited = MySqlTableAccess.AddTableTypeToMap(sTABLE_NAME, typeof(GeoPathRec));

    // ** ISerializable
    /// <summary>
    /// Implementation for ISerializable to serialize this obj.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    public void GetObjectData(SerializationInfo info, StreamingContext context) // Implementation of ISerialize.
    {
        GetObjectDataHelper(info, context);       
    }

    // ** Constructors
    /// <summary>
    /// Constructor required when restoring this object from a serialized stream saved via ISerializable.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected GeoPathRec(SerializationInfo info, StreamingContext context)
    {
        this.svcObj = new Gpx();
        DeserializeHelper(info, context);
    }

    /// <summary>
    /// Default constructor.
    /// </summary>
    public GeoPathRec()
    {
        svcObj = new Gpx(); // Object for web service exchange.    
    }

    /// <summary>
    /// Contructor given a Gpx obj.
    /// </summary>
    /// <param name="svcObj">Ref to Gpx object for this record.</param>
    public GeoPathRec (Gpx svcObj) //20120113
    {
        if (svcObj != null)
            this.svcObj = svcObj;
        else
            this.svcObj = new Gpx();
    }
    
    // ** More members
    protected Gpx svcObj = null; // Object for web service exchange. Set by constructor.

}

public class OwnerRec  : MySqlTableAccess
{
    /// <summary>
    /// Constructor.
    /// </summary>
    public OwnerRec() 
    {
        Init();
    }

    /// <summary>
    /// Record unique sqequence id.
    /// </summary>
    public int nId;

    /// <summary>
    /// Unique owner id.
    /// </summary>
    public string sOwnerId;

    /// <summary>
    ///  Name of owner
    /// </summary>
    public string sName;

    /// <summary>
    /// Access handle used to validate that owner can access database records.
    /// </summary>
    public string accessHandle;

    public DateTime tModified;

    // ** Other Methods 
    // 
    /// <summary>
    /// Fills this object from database for specified owner id.
    /// </summary>
    /// <param name="conn">Active database connection.</param>
    /// <param name="sOwnerId">Specified owner id.</param>
    /// <returns></returns>
    /// <remarks>
    /// If record is not found for the specified owner id, this object is
    /// initialized to same state as when selected.
    /// </remarks>
    public EOpResult SelectByOwnerId(MySqlConnection conn, string sOwnerId)
    {
        bool bError = false;

        List<MySqlTableAccess> liResult = new List<MySqlTableAccess>();
        string sExpr = String.Format("sOwnerId = '{0}'", sOwnerId);
        EOpResult result = this.SelectByExpr(conn, sExpr, liResult);
        if (result == EOpResult.SUCEEDED && liResult.Count > 0)
        {
            OwnerRec found = liResult[0] as OwnerRec;
            if (found != null)
                Copy(found);
            else
                bError = true;
        }
        else
        {
            bError = true;
        }
        if (bError)
        {
            if (result == EOpResult.SUCEEDED)
            {
                result = EOpResult.NOT_FOUND;
            }
            Init(); // Initialize this record on error.
        }
        return result;
    }

    /// <summary>
    /// Fills this object with record for specified owner id from database 
    /// and checks if database access is valid for the owner.
    /// Returned EOpResult.SUCCEEDED if access is valid.
    /// </summary>
    /// <param name="conn">Active database connection.</param>
    /// <param name="sOwnerId">Specified owner id.</param>
    /// <param name="accessHandle">Access handle to be validated.</param>
    /// <returns></returns>
    public EOpResult ValidateAccess(MySqlConnection conn, string sOwnerId, string accessHandle)
    {
        EOpResult result = this.SelectByOwnerId(conn, sOwnerId);
        if (result == EOpResult.SUCEEDED)
        {
            bool bOk = this.sOwnerId == sOwnerId && this.accessHandle == accessHandle;
            if (!bOk)
            {
                result = EOpResult.FAILED;
            }
        }

        return result;
    }

    // ** Private Members
    /// <summary>
    /// Copies other OwnerRec to this object.
    /// </summary>
    /// <param name="other">other record to copy.</param>
    private void Copy(OwnerRec other)
    {
        this.nId = other.nId;
        this.sOwnerId = other.sOwnerId;
        this.sName = other.sName;
        this.accessHandle = other.accessHandle;
        this.tModified = other.tModified;
    }

    /// <summary>
    /// Initializes this record to same state as when constructed.
    /// </summary>
    private void Init()
    {
        this.nId = 0;
        this.sOwnerId = String.Empty;
        this.sName = String.Empty;
        this.accessHandle = String.Empty;
        this.tModified = DateTime.Now;
    }

    // ** Specific field information that a derived class provides.
    // Derived class must add record type to table type map by calling MySqlTableAccess.AddTableTypeToMap(string sTableName, Type typeof(class_name)) 

    /// <summary>
    /// Readonly property for name of the table.
    /// </summary>
    override public string TableName
    {
        get { return sTABLE_NAME; }
    }

    /// <summary>
    /// Read/Write property for the identity field of the record.
    /// </summary>
    override public int IdSq
    {
        get { return this.nId; }
        set { this.nId = value; }
    }

    /// <summary>
    /// Readonly property for column name of IdSq field.
    /// </summary>
    override public string IdName
    {
        get { return "nId"; }
    }

    /// <summary>
    /// Indexer of a column value within this object.
    /// Returns object value for column i. 
    /// Returns null if column i index is out of range.
    /// </summary>
    /// <param name="iCol">Index for column value, origin 0.</param>
    /// <returns></returns>
    override protected Column this[int iCol]
    {
        get
        {
            Column oCol = new Column();
            switch (iCol)
            {
                case 0: oCol.oValue = nId; oCol.sName = "nId"; oCol.oType = typeof(int); break;
                case 1: oCol.oValue = sOwnerId; oCol.sName = "sOwnerId"; oCol.oType = typeof(string); break;
                case 2: oCol.oValue = sName; oCol.sName = "sName"; oCol.oType = typeof(string); break;
                case 3: oCol.oValue = accessHandle; oCol.sName = "accessHandle"; oCol.oType = typeof(string); break;
                case 4: oCol.oValue = tModified; oCol.sName = "tModified"; oCol.oType = typeof(DateTime); break;
                default:
                    oCol = null;
                    break;
            }
            return oCol;
        }

        set
        {
            switch (iCol)
            {
                case 0: nId = (int)value.oValue; break;
                case 1: sOwnerId = (string)value.oValue; break;
                case 2: sName = (string)value.oValue; break;
                case 3: accessHandle = (string)value.oValue; break;
                case 4: tModified = (DateTime)value.oValue; break;
            }
        }
    }

    // No ovrride.
    // virtual protected string ColumnValue(int iCol, out MySqlParameter oParam)
    // virtual protected bool ReadColumn(int iCol, MySqlDataReader r)

    /// <summary>
    /// Returns a new record object of the derived class.
    /// Note: This useful for getting a working obj that does 
    ///       not disturb this ojbject.
    /// </summary>
    /// <returns></returns>
    override protected MySqlTableAccess New()
    {
        return new OwnerRec();
    }

    /// <summary>
    /// Sets foreign key map needed by HistRec to save change history.
    /// </summary>
    override public void SetForeignKeyMap()
    {
        // No foreign keys.
        mapForeignKey.Clear();
    }

    /// Readonly property that indicates if change history
    /// is saved when database record is changed. 
    /// OwnerRec's are not saved in the hist table.
    /// </summary>
    protected override bool bSaveHistory
    {
        get { return false; }
    }

    protected const string sTABLE_NAME = "owner";

}


