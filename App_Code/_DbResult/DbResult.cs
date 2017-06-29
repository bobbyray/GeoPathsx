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
/// Info about result for a for accessing a database.
/// </summary>
/// <remarks>
/// nResult is 0 for no error, < 0 for an error code, >= 0 for a successful result code.
/// sMsg may be empty, or it may be a description about a sucessful result or about an error.
/// </remarks>
public class DbResult
{
    public bool bOk = true;                // True indicates no error.
    public EResult nResult = EResult.OK;   // Result code.
    public string sMsg = String.Empty;     // Msg about the operation 

    public enum EResult
    {
        OK = 0,
        ERROR = -1,
        CONNECTION_INACTIVE = -2,
        DSN_ERROR = -3,
        INSERT_FAILED = -4,
        DELETE_FAILED = -5,
        UPDATE_FAILED = -6,
    }

    // Returns textual description of nResult.
    public string GetResultText()
    {
        string s = String.Empty;
        switch (nResult)
        {
            case EResult.OK: s = "Successful"; break;
            case EResult.ERROR: s = "Error occurred"; ; break;
            case EResult.CONNECTION_INACTIVE: s = "Database connection is not active."; break;
            case EResult.DSN_ERROR: s = "Error occurred getting dataset name."; break;
            case EResult.INSERT_FAILED: s = "Inserting record into database failed."; break;
            case EResult.DELETE_FAILED: s = "Deleting record from database failed."; break;
            case EResult.UPDATE_FAILED: s = "Updating record in database failed."; break;
        }
        return s;
    }

    /// <summary>
    /// Clears this object to initial state: bOk = true, nResult = 0, sMsg = empty.
    /// </summary>
    public void Clear()
    {
        bOk = true;
        sMsg = String.Empty;
        nResult = EResult.OK;
    }


    /// <summary>
    /// Sets this object to indicate an error.
    /// </summary>
    /// <param name="nResult">Enumeration indicating the error.</param>
    /// <param name="sMsg">Additional msg, may be null.</param>
    public void SetError(EResult nResult, string sMsg)
    {
        this.bOk = false;
        this.nResult = nResult;
        this.sMsg = sMsg == null ? String.Empty : sMsg;
    }

    public void SetError(EResult nResult)
    {
        SetError(nResult, null);
    }

}
