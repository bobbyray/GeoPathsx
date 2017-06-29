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
/// Class to verify Facebook authentication of a client of a server.
/// </summary>
public class FbServerAuth
{
	public FbServerAuth()
	{
		//
		// TODO: Add constructor logic here
		//
	}

    /// <summary>
    /// Enumeration for Result.status.
    /// </summary>
    public enum EAuthStatus
    {
        Ok = 1,                // User authentication succeeded.
        Failed = 0,            // User authentication failed.
        Canceled = -1,         // User authentication canceled (not used by server.)
        Error = -2,            // Error occurred trying to authenticate.
        Expired = -3,          // Time allowed for user to access data has expired (used by server).
        // Client needs to authenticate user again.
    }

    /// <summary>
    /// Result from calling Authenticate(..) method.
    /// </summary>
    public class Result
    {
        public Result() { }

        // Result code indicating ok or some kind of error.
        public EAuthStatus status = EAuthStatus.Failed;

        ///<summary>
        /// Message describing the result.
        ///</summary>
        public string msg = String.Empty; 
    }

    // ** Properties 
    /// <summary>
    /// Facebook app ID of app requesting info. Must be set.
    /// </summary>
    public string appID = "";
    /// <summary>
    /// Facebook app secrect string. Must be set. (Must keep private. Only app should know.)
    /// </summary>
    public string appSecret = "";
    
    // ** Methods

    /// <summary>
    /// Authenticates Facebook access token received from client with Facebook.
    /// Returns Result object. 
    /// </summary>
    /// <param name="auth">Authentication data that client has received from Facebook.
    /// Caller provides the class named AuthData, which must have at least 3 public
    /// string properties named: userID, userName, and accessToken.
    /// </param>
    /// <returns></returns>
    /// <remarks>
    /// Client (web browser) uses Facebook javascript SDK to athenticate with Facebook
    /// receiving an access token from Facebook. Facebook allows this token to be 
    /// sent to server for server to access Facebook for information about authenticated
    /// user. However Facebook recommends a procedure to verify that the access token
    /// has not been forged, which this method implements.
    /// </remarks>
    public Result Authenticate(AuthData auth) {
        Result result = new Result();

        if (appID == null)
        {
            result.status = EAuthStatus.Error;
            result.msg = "Authentication error, app id is invalid.";
            return result;
        }
        if (appSecret == null)
        {
            result.status = EAuthStatus.Error;
            result.msg = "Authentication error, app verification failed.";
            return result;
        }

        var fb = new Facebook.FacebookClient();

        dynamic responseAppToken = fb.Get("oauth/access_token",
            new
            {
                client_id = appID, // App id
                client_secret = appSecret, 
                grant_type = "client_credentials"
            });
        dynamic appAccessToken = responseAppToken["access_token"];
        if (appAccessToken == null)
        {
            // App id or secret invalid. Should not happen.
            result.msg = "Failed to get app access token.";
            result.status = EAuthStatus.Error;
        }
        else
        {
            dynamic response = fb.Get("debug_token",
            new
            {
                input_token = auth.accessToken,
                access_token = appAccessToken,
            });

            dynamic data = response["data"];
            if (data == null)
            {
                // debug_token for user access verification is invalid. Should not happen.
                result.msg = "Failed to user access token verification data.";
                result.status = EAuthStatus.Error;
            }
            else
            {
                if (!data["is_valid"])
                {
                    result.msg = "User authentication is invalid.";
                    result.status = (int)EAuthStatus.Failed;
                }
                else
                {
                    // Verify that userID sent from client and appID match.
                    bool bAppOk = (data["app_id"] == appID);
                    bool bUserOk = data["user_id"] == auth.userID;
                    if (bUserOk && bAppOk)
                    {
                        // Successfully verified authentication of user.
                        result.status = EAuthStatus.Ok;
                        result.msg = "User authentication verification valid.";
                    }
                    else
                    {
                        string sError = "Authentication verification failed:";
                        if (!bAppOk)
                            sError += " app id is invalid.";
                        if (!bUserOk)
                            sError += " User id is invalid.";
                        result.status = (int)EAuthStatus.Failed;
                        result.msg = sError;
                    }
                }
            }
        }

        return result;
    }

}