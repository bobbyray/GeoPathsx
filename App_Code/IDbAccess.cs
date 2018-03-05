/* 
Copyright (c) 2015 - 2017 Robert R Schomburg
Licensed under terms of the MIT License, which is given at
https://github.com/bobbyray/MitLicense/releases/tag/v1.0
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// A callback to do work upon successfully connection to database.
/// </summary>
/// <returns></returns>
public delegate DbResult DoConnectionCallback();

/// <summary>
/// Summary description for Class1
/// </summary>
public interface IDbAccess
{
    // ** Connection methods
    DbResult DoConnection(DoConnectionCallback fnCallback);

    /// <summary>
    /// Opens connection to database.
    /// </summary>
    /// <returns></returns>
    DbResult OpenConnection();
    
    /// <summary>
    /// Closes database connection releasing all associated resources.
    /// </summary>
    /// <returns></returns>
    DbResult CloseConnection();

    // ** Data Access Methods
    /// <summary>
    /// Store gpx data in database.
    /// </summary>
    /// <param name="gpx">Object to be stored.</param>
    /// <returns></returns>
    DbResult GpxPut(Gpx gpx);

    /// <summary>
    /// Deletes gpx object from database.
    /// Returns result of database access.
    /// </summary>
    /// <param name="sOwnerId">
    /// Owner id for the record to delete. 
    /// </param>
    /// <param name="nId">Unique record id for the record to delete.</param>
    /// <returns></returns>
    DbResult GpxDelete(string sOwnerId, int nId); 

    /// <summary>
    /// Sets a list of Gpx obj found in database.
    /// Returns result of of database access.
    /// </summary>
    /// <param name="sOwnerId">Id of owner of Gpx records in database.
    /// If null, any owner record is valid to included.</param>
    /// <param name="eShare">Indicates sharing of record: public, private or protected.</param>
    /// <param name="list">Ref to list [out] that is filled from database.</param>
    /// <returns></returns>
    DbResult GetGpxList(string sOwnerId, byte eShare, GpxList list);

    /// <summary>
    /// Sets a list of Gpx obj found in database that are within a geo rectangle.
    /// Returns result of of database access.
    /// </summary>
    /// <param name="sOwnerId">Id of owner of Gpx records in database.
    /// If null, any owner record is valid to included.</param>
    /// <param name="eShare">Indicates sharing of record: public, private or protected.</param>
    /// <param name="gptSW">SouthWest corner of rectangle.</param>
    /// <param name="gptNE">NorthEast corner of rectangle.</param>
    /// <param name="list">Ref to list [out] that is filled from database.</param>
    /// <returns></returns>
    DbResult GetGpxListByLatLon(string sOwnerId, byte eShare, GeoPt gptSW, GeoPt gptNE, GpxList list);
    

    /// <summary>
    /// Validates access to database for an owner.
    /// Returned DbResult.bOk is true if access is valid.
    /// </summary>
    /// <param name="sOwnerId">owner id to validate.</param>
    /// <param name="accessHandle">Access handle that owner claims is valid.</param>
    /// <returns></returns>
    DbResult ValidateAccess(string sOwnerId, string accessHandle);

    /// <summary>
    /// Updates owner information. If sOwnerId does not exist, creates a new owner.
    /// </summary>
    /// <param name="sOwnerId">Unique id for the owner.</param>
    /// <param name="sName">Name of the owner.</param>
    /// <param name="accessHandle"></param>
    /// <returns></returns>
    DbResult UpdateOwner(string sOwnerId, string sName, string accessHandle);

    /// <summary>
    /// Sets record for owner to indicate logged out, i.e. no longer authenticated.
    /// </summary>
    /// <param name="sOwnerId">Owner id.</param>
    /// <param name="accessHandle">Access handle for verification.</param>
    /// <returns></returns>
    DbResult Logout(string sOwnerId, string accessHandle);

    ////20180226 **** Additions for Record Stats

    /// <summary>
    /// Stores list of GeoTrailRecordStats object in database.
    /// Replaces an existing record in the database, or creates a new one
    /// when there is no existing record.
    /// </summary>
    /// <param name="sOwnerId">Owner id to identify to whom the stats belong.</param>
    /// <param name="statsList">List of stats objects to store in database.</param>
    /// <param name="bOverWrite">true to always replace stats in database even if stats element in statsList is same as database record.</param>
    /// <returns></returns>
    DbResult UploadRecordStatsList(string sOwnerId, GeoTrailRecordStatsList statsList, bool bOverWrite = false);


}
