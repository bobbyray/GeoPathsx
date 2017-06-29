/* 
Copyright (c) 2015 - 2017 Robert R Schomburg
Licensed under terms of the MIT License, which is given at
https://github.com/bobbyray/MitLicense/releases/tag/v1.0
*/
using System;
using System.Collections.Generic;
using System.Web;
using MySql.Data.MySqlClient;
using System.Runtime.Serialization;
using System.Xml.Serialization; 
using System.Data; 
using System.Text; // For StringBuilder
using System.IO;   // For TextWriter
using System.Configuration; 

namespace Wigo.MySqlAccess
{
    /// <summary>
    /// Delegate type for a callback function that executes using an active database connection.
    /// </summary>
    /// <param name="conn">Active database connection provided in the callback.</param>
    public delegate void DoConnectionDelegate(MySqlConnection conn);

    /// <summary>
    /// Delegate type for a callback to get a connection string.
    /// Used by CDataAccessBase, which provides a default implementation
    /// to return the ConnectionStringSettings from the Web.Config file.
    /// The ReminderWS (get reminders web service) provides an implementation
    /// that returns a ConnectionString from the Web.Config file from
    /// a schedule site folder.
    /// </summary>
    public delegate ConnectionStringSettings ConnectionStringDelegate();


    public class MySqlHelper
    {
        /// <summary>
        /// Returns a time cast for use in a MySql statement.
        /// </summary>
        /// <param name="t">The timne to be cast.</param>
        /// <returns>String cast in MySql datetime format.</returns>
        static public string MySqlCast(DateTime t) //20100530
        {
            string sCast = String.Format("CAST('{0}' as datetime)", MySql(t));
            return sCast;
        }

        /// <summary>
        /// Return DateTime constrained to limits imposed by MySql for the MySql datetime format.
        /// </summary>
        /// <param name="t">The DateTime to constrain.</param>
        /// <returns></returns>
        /// <remarks>
        /// MySql datetime valid range inclusive: 1000-01-01 00:00:00 to 9999-12-31 23:59:59.
        /// </remarks>
        static public DateTime MySqlDateTimeContraint(DateTime t) //20110228
        {
            if (t < new DateTime(1000, 1, 1, 0, 0, 0))
                t = new DateTime(1000, 1, 1, 0, 0, 0);
            else if (t > new DateTime(9999, 12, 31, 23, 59, 59))
                t = new DateTime(9999, 12, 31, 23, 59, 59);
            return t;
        }

        /// <summary>
        /// Returns formatted string of time in MySql format (yyyy-mm-dd hh:mm:ss).
        /// </summary>
        /// <param name="t">DateTime object to format.</param>
        /// <returns>Date time string in MySql format.</returns>
        static public string MySql(DateTime t)
        {
            string s = t.ToString("yyyy-MM-dd HH':'mm':'ss");
            return s;
        }


        static string _sConnectionError = String.Empty;
        /// <summary>
        /// Returns error message if there is an error getting a database connection.
        /// Returns empty string for no error.
        /// </summary>
        static public string sConnectionError
        {
            get { return _sConnectionError; } 
        }

        protected static string _connStringName = "ScheduleConnectionString";
        /// <summary>
        /// Property for database connection string name.
        /// </summary>
        static public string ConnectionName
        {
            get { return _connStringName; }
            set { _connStringName = value; }
        }

        /// <summary>
        /// A delegate function to return a connection string for sql database access.
        /// Default is to get the connection string settings from the Web.config 
        /// file for the site.
        /// Note: The ReminderWS (web service) sets the delegate in order to get the
        /// connection string setting for various schedule sites.
        /// </summary>
        static public ConnectionStringDelegate GetConnStringFcn = DefaultConnectionStringSetting;

        /// <summary>
        /// Gets the connection strings settings from the config.web file for the site.
        /// </summary>
        /// <returns>Connection settings.</returns>
        static private ConnectionStringSettings DefaultConnectionStringSetting()
        {
            ConnectionStringSettings ConnSettings = ConfigurationManager.ConnectionStrings[_connStringName];
            return ConnSettings;
        }

        /// <summary>
        /// Gets the connection string for the sql database.
        /// Logs an event warning message on failure to get connection string configuration manager.
        /// </summary>
        /// <returns>The connection string (data set name); null on failure.</returns>
        static public string GetDsnForSqlConnection()
        {
            // Get the dataset name to connect to.
            string dsn = null;
            ConnectionStringSettings ConnSettings = GetConnStringFcn();

            if (ConnSettings == null)
            {
                _sConnectionError = String.Format("Configuration Manager can not find this connection string: {0}", _connStringName);
            }
            else
            {
                _sConnectionError = String.Empty;
                dsn = ConnSettings.ConnectionString;
            }
            return dsn;
        }


        /// <summary>
        /// Gets an new open database connection and executes a callback function.
        /// Returns true when active database connection is obtained.
        /// </summary>
        /// <remarks>
        /// The connection is opened, then the called function, which provided the 
        /// connection, is executed.  Finally the connection is closed.
        /// The callback function is not called if opening a database connection 
        /// fails, in which case false if returned.
        /// Exception are NOT caught, which follows the practice of reporting 
        /// exceptions by logging and/or email and displaying a html error page.
        /// </remarks>
        /// <param name="aCallback">The callback function that is executed.</param>
        /// <returns></returns>
        static public bool DoConnection(DoConnectionDelegate aCallback)
        {
            string sConn = GetDsnForSqlConnection();
            if (String.IsNullOrEmpty(sConn))
                return false;
            using (MySqlConnection conn = new MySqlConnection(sConn))
            {
                conn.Open();
                aCallback(conn);
                conn.Close();
            }
            return true;
        }

    }



    /// <summary>
    /// Implements the pattern for insert and updating records keeping history
    /// in a MySql Database.  
    /// </summary>
    /// <remarks>
    /// This class applies to a single table.
    /// A record class (a row) derives from this class and implements the 
    /// abstract properties and functions relating to specific fields 
    /// of the record about which the details this base class cannot know.
    /// A derived class can keep change history by doing the following:
    ///  -  Implement ISerialize. 
    ///  -  Specifying the [Serialize] attribute for the class record.
    ///  -  Call GetObjectDataHelper() and DeserializeHelper() to easily implement
    ///  -  members needed by ISerialize.
    ///  -  Implement SetForeignKeyMap() to add elements to the ForeignKeyMap member 
    ///  -  for the foreign keys in its record.
    /// </remarks>
    public abstract class MySqlTableAccess //20100604 
    {
        public const char MYSQL_PARAM_PREFIX_CHAR = '?';

        public enum EOpResult
        {
            FAILED, SUCEEDED, SAME, NOT_FOUND, HIST_FAILED, COLUMN_VALUE_ERROR
        }

        public class Column
        {
            public string sName;   // Name of column in database.
            public object oValue;  // Value of object in a column in database.
            public Type oType;     // Type for the oValue;
        }

        public class ForeignKey
        {
            public string sName; // Column name of foreign key for this record.
            public int nSq;      // Column value of foreign key for this record. 

            /// <summary>
            /// Constructor.
            /// </summary>
            /// <param name="sName"></param>
            /// <param name="nSq"></param>
            public ForeignKey(string sName, int nSq)
            {
                this.sName = sName;
                this.nSq = nSq;
            }
        }

        public class ForeignKeyMap : List<ForeignKey>
        {
            /// <summary>
            /// Finds the index (key in this map) given a foreign key name.
            /// Returns index for success, or -1 for foreign key name not found.
            /// </summary>
            /// <param name="sName"></param>
            /// <returns></returns>
            public int FindKeyIx(string sName)
            {
                ForeignKey FKey = null;
                int nFound = -1;

                for (int i = 0; i < this.Count; i++)
                {
                    FKey = this[i];
                    if (String.Compare(FKey.sName, sName, true) == 0)
                    {
                        nFound = i;
                        break;
                    }
                }

                return nFound;
            }

            /// <summary>
            /// Return Foreign Key element from this map for a given index.
            /// Return null if index is out of bounds.
            /// </summary>
            /// <remarks>
            /// Same as this[i], except checks for bounds and return null if out of bounds.
            /// </remarks>
            /// <param name="i">The index in this map.</param>
            /// <returns></returns>
            public ForeignKey FindForeignKey(int i)
            {
                ForeignKey Fkey = null;
                if (i >= 0 && i < this.Count)
                {
                    Fkey = this[i];
                }
                return Fkey;
            }

            /// <summary>
            /// Return the name of a foreign key set in the HistRec for element i of this map.
            /// </summary>
            /// <param name="i"></param>
            /// <returns></returns>
            static public string ForeignKeyHistName(int i)
            {
                return HistRec.ForeignKeyHistName(i);
            }
        }

        // ** Specifics that a derived class provides.

        // Derived class must add code to base class static protected Dictionary<String, Type> InitTableTypeMap().

        /// <summary>
        /// Readonly property for name of the table.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        abstract public string TableName { get; }

        /// <summary>
        /// Read/Write property for the identity field of the record.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        abstract public int IdSq { get; set; }

        /// <summary>
        /// Readonly property for column name of IdSq field.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        abstract public string IdName { get; }

        /// <summary>
        /// Indexer of a column value within this object.
        /// Get returns object for column i. 
        /// Set only sets Column.oValue for column i.  The other 
        /// Column members, sName and oType, are read only.
        /// Returns null if column i index is out of range.
        /// </summary>
        /// <param name="iCol">Index for column value, origin 0.</param>
        /// <returns></returns>
        [System.Xml.Serialization.XmlIgnore]
        abstract protected Column this[int iCol] { get; set; }

        /// <summary>
        /// Sets foreign key map for derived record obj.
        /// </summary>
        abstract public void SetForeignKeyMap();

        /// <summary>
        /// Returns a new record object of the derived class.
        /// Note: This useful for getting a working obj that does 
        ///       not disturb this ojbject.
        /// </summary>
        /// <returns></returns>
        abstract protected MySqlTableAccess New();

        // ** Helper members for derived record class.

        /// <summary>
        /// Derived fills this member if it has foreign keys.
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public ForeignKeyMap mapForeignKey = new ForeignKeyMap();

        /// <summary>
        /// Helper for derived record that calls to implement the GetObjectData member of ISerializable.
        /// </summary>
        /// <remarks>
        /// Loops through all columns of the derived record using the indexer to get the data to serialize.
        /// </remarks>
        /// <param name="info">Information provide when serializing the derived record.</param>
        /// <param name="context">Not used.</param>
        protected void GetObjectDataHelper(SerializationInfo info, StreamingContext context) // Implementation of ISerialize.
        {
            int iCol = 0;
            Column oCol = this[iCol++];
            while (oCol != null)
            {
                info.AddValue(oCol.sName, oCol.oValue);
                oCol = this[iCol++];
            }
        }

        /// <summary>
        /// Helper for derived class to call from its constructor(SerializationInfo info, StreamingContext context)
        /// that is needed to create the object from serialized bytes.
        /// </summary>
        /// <remarks>
        /// Loops through each column indexer using each element indexed in the derived
        /// class to restore the indexed element.
        /// </remarks>
        /// <param name="info">Serialozed info provided when deserializing.</param>
        /// <param name="context">Not used.</param>
        protected void DeserializeHelper(SerializationInfo info, StreamingContext context)
        {
            int iCol = 0;
            Column oCol = this[iCol];
            while (oCol != null)
            {
                iCol++;
                oCol.oValue = info.GetValue(oCol.sName, oCol.oType);
                this[iCol] = oCol;
            }
        }

        /// <summary>
        /// Readonly property that indicates if change history.  Defaults to true.
        /// is save when database record is changed.
        /// </summary>
        protected virtual bool bSaveHistory
        {
            get { return true; }
        }


        /// <summary>
        /// Returns string for value of column i.  Return null if column i is out of range.
        /// </summary>
        /// <remarks>
        /// It is expected that this function can correctly return a string for a column value indexed
        /// within this object.  However, if the type of the indexed column is unusual,
        /// this function can be overridden to return the correct value string and optional 
        /// MySqlParamter object.
        /// Returns string for value of column i.  Return null if column i is out of range.
        /// May return a parameter name for a value, in which case a MySqlParameter object must be set.
        /// A parameter name begins with the parameter prefix character (a '?').
        /// Note: I think that a parameter is not used very often.  However it is useful when a string
        ///       may have characters that could affect the syntax of a MySql statement.  Sometimes hacks 
        ///       are done by users by putting characters that change the syntax of an sql statement in
        ///       a string field.  Providing a string as a parameter avoids this problem.  (Also, the user
        ///       input is usually checked to avoid such a hack, but it may be that a character is
        ///       legitimate, such as an apostrophe, so the using a parameter is the solution.
        ///       This function return a string using a MySqlParameter object.
        /// </remarks>
        /// <param name="iCol">Index of column origin 0.</param>
        /// <param name="oParam">Object set to attributes of a parameter. Null if parameter is not used.</param>
        /// <returns></returns>
        protected string ColumnValue(int iCol, out MySqlParameter oParam)
        {
            oParam = null;
            Column col = this[iCol];
            if (col == null)
                return null;

            string sResult = ColumnValue(col, out oParam);
            return sResult;
        }

        protected virtual string ColumnValue(Column oCol, out MySqlParameter oParam)
        {
            oParam = null;
            string sResult = null;
            object oValue = oCol.oValue;
            Type oValueType = oValue.GetType();
            if (oValueType == typeof(String))
            {
                // Use parameter for string to avoid special chars in a string 
                // perturbing sql statement syntax.
                oParam = new MySqlParameter();
                oParam.DbType = DbType.String;
                oParam.Value = oValue.ToString();
                oParam.Direction = ParameterDirection.Input;
                oParam.ParameterName = String.Format("{0}{1}", MYSQL_PARAM_PREFIX_CHAR, oCol.sName);
                sResult = oParam.ParameterName;
            }
            else if (oValueType == typeof(DateTime))
            {
                // Convert DateTime to string format needed by MySql.
                DateTime t = (DateTime)oValue;
                sResult = MySqlHelper.MySqlCast(t);
            }
            else
            {
                sResult = oValue.ToString();
            }

            return sResult;
        }

        /// <summary>
        /// Assigns a value for a column i from the database.
        /// Returns true when value is successfully assigned.
        /// </summary>
        /// <remarks>
        /// It is expected that this base implementation typically is adequate.
        /// However, if the type of a value for column is unusual, 
        /// this function can be overridden to assign the 
        /// column from the data reader that is accessing the database.
        /// Another reason to override is the value for a null value 
        /// in the database may need to be assigned in a special way.
        /// This member is public because this record may need to be joined
        /// to a record to another table.
        /// </remarks>
        /// <param name="iCol"></param>
        /// <param name="r"></param>
        virtual public bool ReadColumn(int iCol, MySqlDataReader r, int iRdr)
        {
            Column oCol = this[iCol];
            if (oCol == null)
                return false; // iCol is out range.  Used to end looping thru columns.

            bool bOk = true;
            Type oValueType = oCol.oType;
            if (oValueType == typeof(int))
            {
                oCol.oValue = r.GetInt32(iRdr);
            }
            else if (oValueType == typeof(DateTime))
            {
                if (r.IsDBNull(iRdr))
                    oCol.oValue = DateTime.MinValue;
                else
                    oCol.oValue = r.GetDateTime(iRdr);
            }
            else if (oValueType == typeof(short))
            {
                oCol.oValue = r.GetInt16(iRdr);
            }
            else if (oValueType == typeof(long))
            {
                oCol.oValue = r.GetInt64(iRdr);
            }
            else if (oValueType == typeof(string))
            {
                oCol.oValue = r.GetString(iRdr);
            }
            else if (oValueType == typeof(bool))
            {
                oCol.oValue = r.GetBoolean(iRdr);
            }
            else if (oValueType == typeof(byte))
            {
                oCol.oValue = r.GetByte(iRdr);
            }
            else if (oValueType == typeof(uint))
            {
                oCol.oValue = r.GetUInt32(iRdr);
            }
            else if (oValueType == typeof(ushort))
            {
                oCol.oValue = r.GetUInt16(iRdr);
            }
            else if (oValueType == typeof(ulong))
            {
                oCol.oValue = r.GetUInt64(iRdr);
            }
            else if (oValueType == typeof(double))
            {
                oCol.oValue = r.GetDouble(iRdr);
            }
            else
            {
                bOk = false; // Did not assign.
            }
            // Set the result in derived class obj. 
            if (bOk)
                this[iCol] = oCol;
            return bOk;
        }

        /// <summary>
        /// Returns true if ith column has same value as some another Column object.
        /// </summary>
        /// <remarks>
        /// This base implementation assumes a column in a derived object has Object.Equals() 
        /// provided as a value comparison.  If the comparison is by reference instead, 
        /// the derived class needs to over-ride this function.  Most types will 
        /// have the equal comparison by value, but a custom class is by reference by default.
        /// </remarks>
        /// <param name="iCol">Index of column in this object.</param>
        /// <param name="oOther">Some other Column object to compare to.</param>
        /// <returns></returns>
        virtual protected bool IsColumnSameValue(int iCol, Column oOther)
        {
            Column oMe = this[iCol];
            if (oMe == null && oOther == null)
                return true;
            if (oMe == null || oOther == null)
                return false;

            //DoesNotWork bool bSame = oMe.oType == oOther.oType && oMe.sName == oOther.sName && oMe.Equals(oOther);
            // Column.Equals(oOther) appears to use Object.Equals(oOther) which probably compares refs 
            // resulting in false for different Column objects even though the values are the save.

            // Compare type and name fields.
            bool bSame = oMe.oType == oOther.oType && oMe.sName == oOther.sName;
            if (bSame)
            {
                // Compare column value.
                MySqlParameter paramMe = null;
                MySqlParameter paramOther = null;
                string sValueMe = ColumnValue(oMe, out paramMe);
                string sValueOther = ColumnValue(oOther, out paramOther);
                if (paramMe != null && paramOther != null)
                {
                    string sMe = paramMe.Value as string;
                    string sOther = paramOther.Value as string;
                    bSame = sMe == sOther;
                }
                else
                    bSame = sValueMe == sValueOther;

            }
            return bSame;
        }

        // ** Serializaton / Deserialization of derived class. Used by HistRec, probably not used by elsewhere.

        /// <summary>
        /// Serializes this obj to an xml string, returning the xml string.
        /// </summary>
        /// <returns></returns>
        public string SerializeXml()
        {
            StringBuilder sbXml = new StringBuilder(1000);
            Type myType = GetType();
            XmlSerializer serializer = new XmlSerializer(myType);
            TextWriter writer = new StringWriter(sbXml);
            serializer.Serialize(writer, this);
            writer.Close();

            return sbXml.ToString();
        }

        /// <summary>
        /// Deserializes an xml string for a table record.
        /// Returns object of the type of the table record.
        /// Cast the returned obj to type of the table record.
        /// </summary>
        /// <param name="sTableName">Table name of record to deserialize.</param>
        /// <param name="sXml">String of xml to deserialize.</param>
        /// <returns></returns>
        static public MySqlTableAccess DeserializeXml(string sTableName, String sXml) //20100912
        {
            _sSerializationError = String.Empty;
            // Create an instance of the XmlSerializer class;
            // specify the type of object to be deserialized.
            Type typeTable = TableType(sTableName);
            if (typeTable == null)
                return null; // Type for table is not known.
            XmlSerializer serializer = new XmlSerializer(typeTable);
            // If the XML document has been altered with unknown 
            // nodes or attributes, handle them with the 
            // UnknownNode and UnknownAttribute events.*
            serializer.UnknownNode += new
            XmlNodeEventHandler(serializer_UnknownNode);
            serializer.UnknownAttribute += new
            XmlAttributeEventHandler(serializer_UnknownAttribute);

            TextReader reader = new StringReader(sXml);
            MySqlTableAccess oRec = serializer.Deserialize(reader) as MySqlTableAccess;
            if (oRec != null)
                oRec.SetForeignKeyMap();
            return oRec;
        }

        [System.Xml.Serialization.XmlIgnore]
        static protected Dictionary<string, Type> _mapTableType = new Dictionary<string, Type>(); // Map of table name to record type for each derived class.

        /// <summary>
        /// Adds a record to table type map. 
        /// </summary>
        /// <param name="rec">A derived record to add to the table type map.</param>
        /// <remarks>
        /// May be called more than once for same record type. 
        /// Subsequent calls after the first are ignored.
        /// </remarks>
        static public bool AddTableTypeToMap(string sTableName, Type type)  
        {
            Type typeRec; 
            bool bFound  = _mapTableType.TryGetValue(sTableName, out typeRec);
            if (!bFound)
                _mapTableType.Add(sTableName, type);
            
            return true;
        }

        /// <summary>
        /// Returns class type of record given a TableName. Returns null if TableName is not known.
        /// Note: Each TableName must have a definition set by InitTableMap().
        /// </summary>
        /// <param name="sTableName">MySql table name for the record class type.</param>
        /// <returns></returns>
        static protected Type TableType(string sTableName)
        {
            Type typ = _mapTableType[sTableName];
            return typ;
        }

        [System.Xml.Serialization.XmlIgnore]
        static protected string _sSerializationError = String.Empty;

        /// <summary>
        /// Event handler for deserializing unknown know.
        /// Saves error message in _sSerializationError.
        /// </summary>
        /// <param name="sender">XmlSerializer obj</param>
        /// <param name="e">Event args.</param>
        static protected void serializer_UnknownNode //20100912
        (object sender, XmlNodeEventArgs e)
        {
            _sSerializationError += ("Unknown Node:" + e.Name + "\t" + e.Text + "<br>");
        }

        /// <summary>
        /// Event handler for deserializing an unknown attribute.
        /// Saves error message in _sSerializationError.
        /// </summary>
        /// <param name="sender">XmlSerializer obj</param>
        /// <param name="e"></param>
        static private void serializer_UnknownAttribute //20100912
        (object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            _sSerializationError += ("Unknown attribute " + attr.Name + "='" + attr.Value + "'" + "<br>");
        }

        // ** Public methods directly from base class.
        /// <summary>
        /// Returns text string describing the derived record.
        /// </summary>
        /// <param name="bLineEnd">Line ending to use, typically Environbment.NewLine.</param>
        /// <returns></returns>
        public string RecDescr(string sLineEnd)
        {
            string sText = String.Format("Table Name: {0} {1}", TableName, sLineEnd);
            Column oCol = null;
            for (int i = 0; i < 1000; i++) // Note: 1000 should never be reach. It a safety limit for looping.
            {
                oCol = this[i];
                if (oCol == null)
                    break;
                sText += String.Format("{0}: {1}{2}", oCol.sName, oCol.oValue.ToString(), sLineEnd);
            }
            return sText;
        }

        // ** Public database access methods for user of this class.

        /// <summary>
        /// Updates a record in the database for this object keeping history.
        /// Returns SUCEEDED for for succes, SAME if update would cause no change,
        /// NOT_FOUND if record does not exist in database, or some other error.
        /// </summary>
        /// <remarks>
        /// If an existing record in the database is the same, does nothing.
        /// If an existing record in the database is not found, does nothing.
        /// Otherwise a new record is inserted in the database, marked as 
        /// the current record, the previous record is marked an not currrent.
        /// Returns: 
        ///     SUCEEDED, update successful.
        ///     SAME, existing record is same, no update done.
        ///     NOT_FOUND, record does exist in database so update is not done.
        ///     other, some kind of error, update is not done.
        /// Note: tCreated and sEditorName should be set in this record object to indicate when 
        ///       and who made the change.
        /// </remarks>
        /// <param name="conn">Active connection to database.</param>
        /// <returns></returns>
        public EOpResult Update(MySqlConnection conn)
        {
            bool bHistOk = true;
            bool bError = false;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                MySqlParameter oParam = null;
                System.Text.StringBuilder s = new System.Text.StringBuilder();
                s.AppendFormat("UPDATE {0} SET ", TableName);
                bool bFirst = true;
                int iCol = 0;
                Column col = this[iCol++];
                while (col != null)
                {
                    if (col.sName == IdName)
                    {
                        col = this[iCol++];
                        continue;
                    }
                    if (!bFirst)
                        s.Append(", ");
                    bFirst = false;
                    string sValue = ColumnValue(col, out oParam);
                    bError = String.IsNullOrEmpty(sValue);
                    if (bError)
                        break;
                    s.AppendFormat("{0} = {1}", col.sName, sValue);
                    if (oParam != null)
                        cmdSql.Parameters.Add(oParam);

                    col = this[iCol++];
                }
                if (!bError)
                {
                    s.AppendFormat(" WHERE {0} = {1};", IdName, IdSq);
                    cmdSql.CommandText = s.ToString();
                    int nRows = cmdSql.ExecuteNonQuery();
                    bError = nRows != 1;
                    if (bSaveHistory && !bError)
                    {
                        HistRec oHist = new HistRec();
                        bHistOk = oHist.SaveHistory(conn, this, HistRec.EKind.Updated);
                    }
                }
            }
            EOpResult nResult = bError ? EOpResult.FAILED : bHistOk ? EOpResult.SUCEEDED : EOpResult.HIST_FAILED;
            return nResult;
        }

        /// <summary>
        /// Inserts a record in database for this object setting up to use history.
        /// Returns SUCEEDED for success.
        /// Note: tCreated and sEditorName should be set in this record object to indicate when 
        ///       and who made the change.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <returns></returns>
        public EOpResult Insert(MySqlConnection conn)
        {
            EOpResult nResult = InsertRec(conn);
            if (nResult == EOpResult.SUCEEDED && bSaveHistory)
            {
                HistRec oHist = new HistRec();
                bool bHistOk = oHist.SaveHistory(conn, this, HistRec.EKind.Inserted);
                if (!bHistOk)
                    nResult = EOpResult.HIST_FAILED;
            }

            return nResult;
        }

        /// <summary>
        /// Clears the bCurrent flag in the database record for the record indicated by this identity number.
        /// In addition to updating a previous record as not current, another record is inserted 
        /// into the database to indicate the time of deletion and the user who made the 
        /// change.  
        /// Note: tCreated and sEditorName should be set in this object to indicate when 
        ///       and who made the change.
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        public EOpResult Delete(MySqlConnection conn)
        {
            EOpResult nResult = EOpResult.FAILED;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                cmdSql.CommandText = String.Format("DELETE FROM {0} WHERE {1} = {2};",
                                                    TableName, IdName, IdSq);
                int nRows = cmdSql.ExecuteNonQuery();
                nResult = nRows == 1 ? EOpResult.SUCEEDED : EOpResult.FAILED;
                if (nRows == 1 && bSaveHistory)
                {
                    HistRec oHist = new HistRec();
                    bool bHistOk = oHist.SaveHistory(conn, this, HistRec.EKind.Deleted);
                    if (!bHistOk)
                        nResult = EOpResult.HIST_FAILED;
                }
            }
            return nResult;
        }

        /// <summary>
        /// Fills this record object from the database of a record.
        /// Returns: 
        ///     SUCEEDED,  successfully loaded this object from database.
        ///     NOT_FOUND, record does not exist in database.
        ///     other, some other kind error.  This object is not loaded from database.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="nId">Identity number of the record to select.</param>
        /// <returns></returns>
        public EOpResult Select(MySqlConnection conn, int nId)
        {
            EOpResult nResult = SelectById(conn, nId, this);
            return nResult;
        }

        /// <summary>
        /// Sets liResult to list of records found in database that match a MySql Where expression.
        /// Returns result of datebase access.  liResult is empty if no records are found.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="sExpr">Where expression for selection.</param>
        /// <param name="liResult">[out] Records found. [in] Must not be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// The Where expression is a string that goes inside the Where( ) clause on the sql statment. 
        /// The sql statement: SELECT * FROM table_name WHERE(expr)
        /// table_name: this.TableName
        /// Typical expr: col_name = n
        /// col_name: name of a column whose value is an integer.
        /// n: the integer value to match for the column value.
        /// </remarks>
        public EOpResult SelectByExpr(MySqlConnection conn, string sExpr, List<MySqlTableAccess> liResult) //20110221
        {
            return SelectByExpr(conn, sExpr, null, liResult);
        }

        /// <summary>
        /// Sets liResult to list of records found in database that match a MySql Where expression.
        /// Returns result of datebase access.  liResult is empty if no records are found.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="sExpr">Where expression for selection.</param>
        /// <param name="sOrderBy">Order expression for ordering (sorting) results. May be null for no ordering.</param>
        /// <param name="liResult">[out] Records found. [in] Must not be null.</param>
        /// <returns></returns>
        /// <remarks>
        /// The Where expression is a string that goes inside the Where( ) clause on the sql statment. 
        /// The sql statement: SELECT * FROM table_name WHERE(expr) ORDER BY (order_expr)
        /// table_name: this.TableName
        /// Typical expr: col_name = n
        ///   col_name: name of a column whose value is an integer.
        ///   n: the integer value to match for the column value.
        /// Typical order_expr: col_name DESC
        ///   col_name: name of column on which to order (sort)
        ///   DESC: literal indicating to sort in descending order.  Default is omitted is ASC for sorting up.
        /// </remarks>
        public EOpResult SelectByExpr(MySqlConnection conn, string sExpr, string sOrderBy, List<MySqlTableAccess> liResult)
        {
            liResult.Clear();
            EOpResult nResult = EOpResult.NOT_FOUND;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                if (sOrderBy == null)
                    cmdSql.CommandText = String.Format("SELECT * FROM {0} WHERE ({1})", this.TableName, sExpr);
                else
                    cmdSql.CommandText = String.Format("SELECT * FROM {0} WHERE ({1}) ORDER BY ({2})", this.TableName, sExpr, sOrderBy);

                using (MySqlDataReader r = cmdSql.ExecuteReader())
                {
                    const int iLoopMax = 10000; // Used to check for excessive looping.
                    MySqlTableAccess oRec = null;
                    for (int iLoop = iLoopMax; iLoop > 0 && r.Read(); iLoop--)
                    {
                        oRec = New();
                        nResult = ReadAllColumns(r, oRec);
                        if (!IsOpOk(nResult))
                            break; // Quit on error.
                        liResult.Add(oRec);
                    }
                }
            }
            return nResult;
        }

        /// <summary>
        /// Sets liResult to list of records that match a foreign key value.
        /// Returns result of datebase access.  liResult is empty if no records are found.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="sKeyName">Name of key field.</param>
        /// <param name="nKeyValue">Value of key to match.</param>
        /// <param name="liResult">[out] List of records found. [in] Must not be null.</param>
        /// <returns></returns>
        public EOpResult SelectByForeignKey(MySqlConnection conn, string sKeyName, int nKeyValue, List<MySqlTableAccess> liResult) //20110221
        {
            string sExpr = String.Format("{0} = {1}", sKeyName, nKeyValue);
            EOpResult nResult = SelectByExpr(conn, sExpr, liResult);
            return nResult;
        }

        /// <summary>
        /// Sets liResult to list of records that match a foreign key value.
        /// Returns result of datebase access.  liResult is empty if no records are found.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="sKeyName">Name of key field.</param>
        /// <param name="nKeyValue">Value of key to match.</param>
        /// <param name="sOrderBy">Express for ordering result (typically a column name).</param>
        /// <param name="liResult">[out] List of records found. [in] Must not be null.</param>
        /// <returns></returns>
        public EOpResult SelectByForeignKey(MySqlConnection conn, string sKeyName, int nKeyValue, string sOrderBy, List<MySqlTableAccess> liResult) //20111031
        {
            string sExpr = String.Format("{0} = {1}", sKeyName, nKeyValue);
            EOpResult nResult = SelectByExpr(conn, sExpr, sOrderBy, liResult);
            return nResult;
        }

        /// <summary>
        /// Callback function used by SelectStmt(..) for each row selected.
        /// </summary>
        /// <param name="r">MySql data reader.</param>
        public delegate void SelectDelegate(MySqlDataReader r);

        /// <summary>
        /// Executes any sql select statment calling back for each row selected.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="sSqlCmd">Valid sql select statement.</param>
        /// <param name="oCallback">Callback for each row selected.  Not called if no row is selected.</param>
        /// <remarks>
        /// The sql select statement can be any kind statment and is especially useful for joins across multiple tables.
        /// </remarks>
        static public void SelectStmt(MySqlConnection conn, string sSqlCmd, SelectDelegate oCallback)
        {
            using (MySqlCommand cmdSql = new MySqlCommand(sSqlCmd, conn))
            {
                MySqlDataReader r = cmdSql.ExecuteReader();
                while (r.Read())
                {
                    oCallback(r); // r has items to get.
                }
                r.Close();
            }
        }

        /// <summary>
        /// Returns true of EOpResult is SUCEEDED.
        /// </summary>
        /// <param name="nResult"></param>
        /// <returns></returns>
        static public bool IsOpOk(EOpResult nResult)
        {
            bool bOk = nResult == EOpResult.SUCEEDED;
            return bOk;
        }

        /// <summary>
        /// Returns true if all the other column values of another record are the same as for this object.
        /// </summary>
        /// <param name="oOther">The other record to compare to by value.</param>
        /// <returns></returns>
        /// <remarks>
        /// Throws ApplciationException on excessive looping (GT 1000 loops).
        /// </remarks>
        public bool IsSameValue(MySqlTableAccess oOther)
        {
            int iMaxLoop = 1000;
            bool bSame = false;
            Column oColOther = null;
            Column oColMe = null;
            for (int i = 0; i < iMaxLoop + 1; i++)
            {
                oColMe = this[i];
                oColOther = oOther[i];
                bSame = IsColumnSameValue(i, oColOther);
                if (oColMe == null && oColOther == null)
                    break;
                if (!bSame)
                    break;

                if (i == iMaxLoop)
                    throw new System.ApplicationException("Excessive looping in MySqlTableAccess.IsSameValue().");
            }
            return bSame;
        }

        /// <summary>
        /// Gets an new open database connection and executes a callback function.
        /// Returns true when active database connection is obtained.
        /// </summary>
        /// <param name="aCallback">The callback function that is executed.</param>
        /// <returns></returns>
        public bool DoConnection(DoConnectionDelegate aCallback)
        {
            return MySqlHelper.DoConnection(aCallback);
        }

        // ** Helpers for data access.
        /// <summary>
        /// Fills members of record by reading all columns for a row from database.
        /// Returns EOpResult.SUCEEDED for success.
        /// </summary>
        /// <remarks>
        /// Other classes may do sql selection of rows including all columns and call 
        /// this function to fill records that the have been selected.
        /// Data reader is advanced by column position but not closed upon return.
        /// Returns MySqlTableAccess.EOpResult:
        ///     SUCEEDED, success.
        ///     NOT_FOUND, no columns available in datareader.
        /// </remarks>
        /// <param name="r">Open database reader position for column 0 of the table.</param>
        /// <param name="oRec">The record filled for reading all columns of a table row.</param>
        /// <returns></returns>
        public static EOpResult ReadAllColumns(MySqlDataReader r, MySqlTableAccess oRec)
        {
            int iRdr = 0;
            return ReadAllColumns(r, oRec, ref iRdr);
        }

        /// <summary>
        /// Fills members of record by reading all columns for a row from database.
        /// Returns EOpResult.SUCEEDED for success.
        /// </summary>
        /// <remarks>
        /// Other classes may do sql selection of rows including all columns and call 
        /// this function to fill records that the have been selected.
        /// Data reader is advanced by column position but not closed upon return.
        /// Returns MySqlTableAccess.EOpResult:
        ///     SUCEEDED, success.
        ///     NOT_FOUND, no columns available in datareader.
        /// </remarks>
        /// <param name="r">Open database reader position for column 0 of the table.</param>
        /// <param name="oRec">The record filled for reading all columns of a table row.</param>
        /// <param name="iRdr">[in] starting field for reader. [out] Incremented for each field read.</param>
        /// <returns></returns>
        public static EOpResult ReadAllColumns(MySqlDataReader r, MySqlTableAccess oRec, ref int iRdr)
        {
            int i = 0;
            while (oRec.ReadColumn(i++, r, iRdr))
            {
                iRdr++; // Note: iRdr++ in while() caused extra increment of iRdr cause of ReadColumn(...) reads until column i is null.
            }
            EOpResult nResult = i <= 1 ? EOpResult.NOT_FOUND : EOpResult.SUCEEDED;
            return nResult;
        }

        /// <summary>
        /// Fills a record object from the database.
        /// Returns EpResult.SUCEEDED for success.
        /// </summary>
        /// <remarks>
        /// Returns MySqlTableAccess.EOpResult: 
        ///     SUCEEDED, successfully filled record from database.
        ///     NOT_FOUND, record does not exist in database.
        ///     other, some other kind of error.  Record object is not filled successfully.
        /// </remarks>
        /// <param name="conn">Active database connection.</param>
        /// <param name="nId">Identity number of record to select.</param>
        /// <param name="oRec">The record object that is to filled.</param>
        /// <returns></returns>
        EOpResult SelectById(MySqlConnection conn, int nId, MySqlTableAccess oRec)
        {
            EOpResult nResult = EOpResult.NOT_FOUND;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                cmdSql.CommandText = String.Format("SELECT * FROM {0} WHERE {1} = {2} ", oRec.TableName, oRec.IdName, nId);
                using (MySqlDataReader r = cmdSql.ExecuteReader())
                {
                    if (r.Read())
                    {
                        nResult = ReadAllColumns(r, oRec);
                    }
                }
            }

            return nResult;
        }

        /// <summary>
        /// Inserts this record into the database.  Also sets
        /// the history field to identity of the new record,
        /// and marks the new record as current, in the database
        /// and in this record object.
        /// Returns:
        ///     SUCEEDED, successfully inserted record.
        ///     FAILED, insert failed.
        ///     other, some other kind of failure. (Probably not used).
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <returns></returns>
        EOpResult InsertRec(MySqlConnection conn)
        {
            EOpResult nResult = EOpResult.FAILED;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                System.Text.StringBuilder s = new System.Text.StringBuilder();
                s.AppendFormat("INSERT INTO {0} VALUES(", TableName);

                bool bOk = false;
                Column col = null;
                MySqlParameter oParam = null;
                string sValue = null;
                this.IdSq = 0; // Ensure inserted identity value is 0 which is needed for server to auto asign next number.
                for (int i = 0; i < 100; i++)
                {
                    col = this[i];
                    if (col == null)
                        break; // Normal way to end the loop when column is beyond bounds.
                    sValue = ColumnValue(i, out oParam);

                    if (sValue == null)
                    {
                        bOk = false;
                        nResult = EOpResult.COLUMN_VALUE_ERROR;
                        break; // Quit on error.
                    }

                    bOk = true;
                    if (i > 0)
                        s.Append(", ");
                    s.Append(sValue);
                    if (oParam != null)
                        cmdSql.Parameters.Add(oParam);
                }
                s.Append(");");
                if (bOk)
                {
                    cmdSql.CommandText = s.ToString();
                    int nRows = cmdSql.ExecuteNonQuery();
                    if (nRows == 1)
                    {
                        nResult = EOpResult.SUCEEDED;
                        this.IdSq = (int)cmdSql.LastInsertedId;
                    }
                }
            }
            return nResult;
        }
    }

    /// <summary>
    /// Database record to save change history.
    /// </summary>
    /// <remarks>
    /// The record for which history is being saved must be implemented as follows:
    ///   * The record's class is derived from MySqlTableAccess.
    ///   * The MySqlTableAccess has serveral abstruct functions that must be implemented
    ///     so that MySqlTableAccess can access the MySql database in a generic way.
    ///   * The record's class must implement the ISerializable interface which has 
    ///     only one member:
    ///         [SecurityPermissionAttribute(SecurityAction.Demand, SerializationFormatter = true)]
    ///         public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
    ///         The record's class should call GetObjectDataHelper(SerializationInfo info, StreamingContext context)
    ///         in its base class.
    ///   * For deserialization to restore the record, a constructor must also be provided:
    ///         public constructor(SerializationInfo info, StreamingContext context)
    ///         The record's constructor should call the base class 
    ///         DeserializeHelper(SerializationInfo info, StreamingContext context).
    /// When a page is loaded that might update the data, the page should set HistRec.sCurEditor, a public
    /// static string, to the Page.User.Identity.Name, which is the user id of the signed user.
    /// </remarks>
    public class HistRec //20100615
    {
        public const int nTABLE_NAME_MAX_LEN = 20;

        public enum EKind { Inserted = 0, Updated = 1, Deleted = 2 }
        public int nHistId;    // Identity Sq of record.
        public int nRecId;     // Identity Sq of record for which history is being saved.
        public string sTableName; // Table name to which nRecId applies.
        public DateTime tStamp;// Time stamp when this record is created.
        public int nFKey0;     // Foreign key 1 that xml history may have.  0 if no foreign key. Used to find records associated to different kind of HistRec.
        public int nFKey1;     // Foreign key 2 that xml history may have.
        public int nFKey2;     // Foreign key 3 that xml history may have.
        public int nFKey3;     // Foreign key 4 that xml history may have.
        public byte nKind;     // Kind of history as given by EKind.
        public string sEditor; // User name of editor of record given nRecId.
        public string xmlRec;   // Text array representing state of record given by nRecId.

        // ** Dabase access members
        public const int FOREIGN_KEY_LIMIT = 4; // Maximum nuber of foreign keys that can in a HistRec.

        public static string sCurEditor = "Unknown";
        /// <summary>
        /// Returns column name in Hist table for foreign key i.
        /// Returns empty string if i is out of range.
        /// </summary>
        /// <param name="i">Index for key name, 0 ... 3.</param>
        /// <returns></returns>
        static public string ForeignKeyHistName(int i)
        {
            string sName = String.Empty;
            if (i >= 0 && i < FOREIGN_KEY_LIMIT)
            {
                sName = String.Format("nFKey{0}", i);
            }
            return sName;
        }

        /// <summary>
        /// Loads this obj from database.
        /// Returns true for success.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="nHistId">Identity sq of HistRec to load.</param>
        /// <returns></returns>
        public bool Select(MySqlConnection conn, int nHistId)
        {
            bool bOk = false;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                cmdSql.CommandText = String.Format("SELECT * FROM hist WHERE nHistId = {0};", nHistId);
                using (MySqlDataReader r = cmdSql.ExecuteReader())
                {
                    while (r.Read())
                    {
                        Fill(r);
                    }
                    r.Close();
                }
            }
            return bOk;
        }

        /// <summary>
        /// Saves a change history record to database for a record being changed.
        /// Returns true for success.
        /// </summary>
        /// <param name="conn">Active database connection.</param>
        /// <param name="oRec">The record that has been changed.</param>
        /// <returns></returns>
        public bool SaveHistory(MySqlConnection conn, MySqlTableAccess oRec, EKind nKind)
        {
            bool bOk = false;
            using (MySqlCommand cmdSql = new MySqlCommand())
            {
                cmdSql.Connection = conn;
                this.nRecId = oRec.IdSq;
                this.sTableName = oRec.TableName;
                this.tStamp = DateTime.Now;
                this.nKind = (byte)nKind;
                this.sEditor = sCurEditor;

                System.Text.StringBuilder s = new System.Text.StringBuilder();
                s.Append("INSERT INTO hist SET ");
                s.AppendFormat("nRecId = {0}, ", nRecId);
                int nLength = sTableName.Length > nTABLE_NAME_MAX_LEN ? nTABLE_NAME_MAX_LEN : sTableName.Length;
                s.AppendFormat("sTableName = '{0}', ", sTableName.Substring(0, nLength));
                s.AppendFormat("tStamp = {0}, ", MySqlHelper.MySqlCast(tStamp));

                // Set FKeyi field in this obj and gen sql to set the foreign key fields for this HistRec.
                oRec.SetForeignKeyMap();
                MySqlTableAccess.ForeignKey keyForeign = null;
                for (int i = 0; oRec.mapForeignKey != null && i < oRec.mapForeignKey.Count; i++)
                {
                    keyForeign = oRec.mapForeignKey[i];
                    if (i == 0)
                        this.nFKey0 = keyForeign.nSq;
                    else if (i == 1)
                        this.nFKey1 = keyForeign.nSq;
                    else if (i == 2)
                        this.nFKey2 = keyForeign.nSq;
                    else if (i == 3)
                        this.nFKey3 = keyForeign.nSq;
                    s.AppendFormat("{0} = {1}, ", ForeignKeyHistName(i), keyForeign.nSq);
                }

                s.AppendFormat("nKind = {0}, ", this.nKind);
                s.AppendFormat("sEditor = '{0}', ", this.sEditor);
                s.Append(String.Format("xmlRec = {0}xmlRec;", MySqlTableAccess.MYSQL_PARAM_PREFIX_CHAR));

                // Set parameter for the data for xmlRec.
                this.xmlRec = oRec.SerializeXml();
                string sParamName = String.Format("{0}xmlRec", MySqlTableAccess.MYSQL_PARAM_PREFIX_CHAR);
                MySqlParameter oParam = cmdSql.Parameters.Add(sParamName, MySqlDbType.VarString);
                oParam.Value = this.xmlRec;

                cmdSql.CommandText = s.ToString();
                int nRows = cmdSql.ExecuteNonQuery();
                bOk = nRows == 1;
            }
            return bOk;
        }

        /// <summary>
        /// Fills this obj from database reader.
        /// </summary>
        /// <param name="r">Database reader</param>
        protected void Fill(MySqlDataReader r)
        {
            nHistId = r.GetInt32(0);
            nRecId = r.GetInt32(1);
            sTableName = r.GetString(2);
            tStamp = r.GetDateTime(3);
            nFKey0 = r.GetInt32(4);
            nFKey1 = r.GetInt32(5);
            nFKey2 = r.GetInt32(6);
            nFKey3 = r.GetInt32(7);
            nKind = r.GetByte(8);
            sEditor = r.GetString(9);
            long nRecLen = r.GetBytes(10, 0, null, 0, int.MaxValue); // Null buffer returns length of data.
            xmlRec = r.GetString(10);
        }
    }

}