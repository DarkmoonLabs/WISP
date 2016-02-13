using System;
using System.Data;
using System.Data.SqlClient;
using System.Collections.Generic;
using Shared;
using System.Configuration;
using System.Threading;
using ServerLib;
using System.Linq;

/// <summary>
/// Adds some helper methods to the Data reader.
/// </summary>
public static class SqlDataReaderExtensions 
{ 
    public static DateTime GetDateTimeUtc(this SqlDataReader reader, string name) 
    {
        int fieldOrdinal = reader.GetOrdinal(name);
        return GetDateTimeUtc(reader, fieldOrdinal);
    }

    public static DateTime GetDateTimeUtc(this SqlDataReader reader, int fieldOrdinal)
    {
        DateTime unspecified = reader.GetDateTime(fieldOrdinal);
        return DateTime.SpecifyKind(unspecified, DateTimeKind.Utc);
    } 
}  

/// <summary>
/// Database access methods for various features.
/// </summary>
public class DB
{
    private DB()
    {
    }

    private static DB m_Instance;
    public static DB Instance
    {
        get 
        {
            if (m_Instance == null)
            {
                m_Instance = new DB();
            }

            return m_Instance;
        }
    }
    
    #region Utility

    public DataTable IntArrayToTable(IEnumerable<int> input)
    {
        DataTable table = new DataTable("IntArray");

        table.Columns.Add("Item", typeof(int));

        IEnumerator<int> enu = input.GetEnumerator();
        while (enu.MoveNext())
        {
            DataRow r = table.NewRow();
            r["Item"] = enu.Current;
            table.Rows.Add(r);
        }

        return table;
    }

    public DataTable GuidArrayToTable(IEnumerable<Guid> input)
    {
        DataTable table = new DataTable("GuidArray");

        table.Columns.Add("UID", typeof(Guid));

        IEnumerator<Guid> enu = input.GetEnumerator();
        while (enu.MoveNext())
        {
            DataRow r = table.NewRow();
            r["UID"] = enu.Current;
            table.Rows.Add(r);
        }

        return table;
    }

    private DataTable PropertiesToIntArrayTable(IEnumerable<Property> input)
    {
        DataTable table = new DataTable("IntArray");

        table.Columns.Add("Item", typeof(int));

        IEnumerator<Property> enu = input.GetEnumerator();
        while (enu.MoveNext())
        {
            DataRow r = table.NewRow();
            r["Item"] = enu.Current.PropertyId;
            table.Rows.Add(r);
        }

        return table;
    }

    private DataTable IntPropertiesToTable(IEnumerable<Property> input)
    {
        DataTable table = new DataTable("PropertyTable");

        table.Columns.Add("PropertyOwner", typeof(int));
        table.Columns.Add("PropertyId", typeof(int));
        table.Columns.Add("PropertyValue", typeof(int));
        table.Columns.Add("PropertyName", typeof(string));
        
        IEnumerator<Property> enu = input.GetEnumerator();
        Int32Property prop = null;
        while(enu.MoveNext())
        {
            prop = enu.Current as Int32Property;
            DataRow r = table.NewRow();
            r["PropertyOwner"] = -1;
            r["PropertyId"] = prop.PropertyId;
            r["PropertyValue"] = prop.Value;
            r["PropertyName"] = prop.Name;
            table.Rows.Add(r);
        }

        return table;
    }

    public DataTable StringPropertiesToTable(IEnumerable<Property> input)
    {
        DataTable table = new DataTable("PropertyTable");

        table.Columns.Add("PropertyOwner", typeof(int));
        table.Columns.Add("PropertyId", typeof(int));
        table.Columns.Add("PropertyValue", typeof(string));
        table.Columns.Add("PropertyName", typeof(string));

        IEnumerator<Property> enu = input.GetEnumerator();
        StringProperty prop = null;
        while (enu.MoveNext())
        {
            prop = enu.Current as StringProperty;
            DataRow r = table.NewRow();
            r["PropertyOwner"] = -1;
            r["PropertyId"] = prop.PropertyId;
            r["PropertyValue"] = prop.Value;
            r["PropertyName"] = prop.Name;
            table.Rows.Add(r);
        }

        return table;
    }

    private DataTable FloatPropertiesToTable(IEnumerable<Property> input)
    {
        DataTable table = new DataTable("PropertyTable");

        table.Columns.Add("PropertyOwner", typeof(int));
        table.Columns.Add("PropertyId", typeof(int));
        table.Columns.Add("PropertyValue", typeof(float));
        table.Columns.Add("PropertyName", typeof(string));

        IEnumerator<Property> enu = input.GetEnumerator();
        SingleProperty prop = null;
        while (enu.MoveNext())
        {
            prop = enu.Current as SingleProperty;
            DataRow r = table.NewRow();
            r["PropertyOwner"] = -1;
            r["PropertyId"] = prop.PropertyId;
            r["PropertyValue"] = prop.Value;
            r["PropertyName"] = prop.Name;
            table.Rows.Add(r);
        }

        return table;
    }

    private DataTable LongPropertiesToTable(IEnumerable<Property> input)
    {
        DataTable table = new DataTable("PropertyTable");

        table.Columns.Add("PropertyOwner", typeof(int));
        table.Columns.Add("PropertyId", typeof(int));
        table.Columns.Add("PropertyValue", typeof(long));
        table.Columns.Add("PropertyName", typeof(string));

        IEnumerator<Property> enu = input.GetEnumerator();
        Int64Property prop = null;
        while (enu.MoveNext())
        {
            prop = enu.Current as Int64Property;
            DataRow r = table.NewRow();
            r["PropertyOwner"] = -1;
            r["PropertyId"] = prop.PropertyId;
            r["PropertyValue"] = prop.Value;
            r["PropertyName"] = prop.Name;
            table.Rows.Add(r);
        }

        return table;
    }

    private DataTable StatsToTable(IEnumerable<Stat> input)
    {
        DataTable table = new DataTable("StatTable");

        table.Columns.Add("StatOwner", typeof(int));
        table.Columns.Add("StatId", typeof(int));
        table.Columns.Add("StatValue", typeof(float));
        table.Columns.Add("StatMaxValue", typeof(float));
        table.Columns.Add("StatMinValue", typeof(float));

        IEnumerator<Stat> enu = input.GetEnumerator();
        Stat stat = null;
        while (enu.MoveNext())
        {
            stat = enu.Current;
            DataRow r = table.NewRow();
            r["StatOwner"] = -1;
            r["StatId"] = stat.StatID;
            r["StatValue"] = stat.CurrentValue;
            r["StatMaxValue"] = stat.MaxValue;
            r["StatMinValue"] = stat.MinValue;
            table.Rows.Add(r);
        }

        return table;
    }
     
    public SqlCommand GetProc(string commandName)
    {
        return GetCommand(GameDataConnection, "GetCharacterNamesForUser", true);
    }
    
    static DB()
    {
        if (ConfigHelper.GetStringConfig("DatabaseConnectivity", "TRUE").ToLower() == "true")
        {
            m_GameDataConnectionString = ConfigurationManager.ConnectionStrings["GameDataConnectionString"].ConnectionString;
            m_UserDataConnectionString = ConfigurationManager.ConnectionStrings["AccountDataConnectionString"].ConnectionString;
            m_SessionDataConnectionString = ConfigurationManager.ConnectionStrings["SessionDataConnectionString"].ConnectionString;

            if (m_GameDataConnectionString.Length < 1)
            {
                throw new ArgumentNullException("!!!!Failed to get the DataConnectionString for the database!!!!");
            }
        }
    }

    private static string m_SessionDataConnectionString = "";

    /// <summary>
    /// Returns the primary connection string for the application
    /// </summary>
    public static string SessionDataConnectionString
    {
        get
        {
            return m_SessionDataConnectionString;
        }
        set
        {
            m_SessionDataConnectionString = value;
        }
    }

    private static string m_GameDataConnectionString = "";

    /// <summary>
    /// Returns the primary connection string for the application
    /// </summary>
    public static string GameDataConnectionString
    {
        get
        {
            return m_GameDataConnectionString;
        }
        set
        {
            m_GameDataConnectionString = value;
        }
    }

    private static string m_UserDataConnectionString = "";

    /// <summary>
    /// Returns the primary connection string for the application
    /// </summary>
    public static string UserDataConnectionString
    {
        get
        {
            return m_UserDataConnectionString;
        }
        set
        {
            m_UserDataConnectionString = value;
        }
    }

    /// <summary>
    /// Gets a new SqlConnection object, based on the default SessionDataConnectionString in the App.Config.
    /// Caller is responsible for cleaning it up
    /// </summary>
    /// <returns></returns>
    public static SqlConnection SessionDataConnection
    {
        get
        {
            return new SqlConnection(SessionDataConnectionString);
        }
    }

    private Dictionary<string, string> m_SessionConnectionStrings = new Dictionary<string, string>();
    
    /// <summary>
    /// Adds a server group specific Session database connection string
    /// </summary>
    /// <param name="serverKey"></param>
    /// <param name="conString"></param>
    public void AddServerGroupSessionConnection(string serverKey, string conString)
    {
        m_SessionConnectionStrings.Remove(serverKey);
        m_SessionConnectionStrings.Add(serverKey, conString);
    }

    /// <summary>
    /// Removes a server group specific session connection string
    /// </summary>
    /// <param name="serverKey"></param>
    public void RemoveSessionConnection(string serverKey)
    {
        m_SessionConnectionStrings.Remove(serverKey);
    }

    /// <summary>
    /// In some service setups, it is advantageous to have seperate session databases, one per server group.  
    /// You can set a specific session storage data string per server group in the App.Config OutboundConnections section.
    /// </summary>
    /// <param name="serverKey"></param>
    /// <returns></returns>
    public SqlConnection GetSessionDataConnectionForServer(string serverKey)
    {
        string conString = "";
        if (!m_SessionConnectionStrings.TryGetValue(serverKey, out conString))
        {
            return null;
        }
        return new SqlConnection(conString);
    }

    /// <summary>
    /// Gets a new SqlConnection object, based on the GameDataConnectionString.
    /// Caller is responsible for cleaning it up
    /// </summary>
    /// <returns></returns>
    public static SqlConnection GameDataConnection
    {
        get
        {
            return new SqlConnection(GameDataConnectionString);
        }
    }

    /// <summary>
    /// Gets a new SqlConnection object, based on the UserDataConnectionString.
    /// Caller is responsible for cleaning it up
    /// </summary>
    /// <returns></returns>
    public static SqlConnection UserDataConnection
    {
        get
        {
            return new SqlConnection(UserDataConnectionString);
        }
    }

    /// <summary>
    /// Returns an SQL command object, based on the givven command text
    /// </summary>
    /// <param name="storedProcName"></param>
    /// <returns></returns>
    public static SqlCommand GetCommand(SqlConnection con, string commandText, bool isStoredProc, int commandTimeout = 0)
    {
        SqlCommand cmd = new SqlCommand(commandText, con);
        if (isStoredProc)
        {
            cmd.CommandType = CommandType.StoredProcedure;
        }
        else
        {
            cmd.CommandType = CommandType.Text;
        }

        cmd.CommandTimeout = commandTimeout;
        return cmd;
    }
    #endregion

    public bool Character_Delete(Guid player, int id, bool permaPurge, string serviceLogReason, out SqlTransaction tran, out SqlConnection con)
    {
        bool result = true;
        con = GameDataConnection;
        tran = null;
        SqlCommand cmd = GetCommand(con, "Character_Delete", true);
        cmd.Parameters.Add(new SqlParameter("@owner", player));
        cmd.Parameters.Add(new SqlParameter("@CharacterID", id));
        cmd.Parameters.Add(new SqlParameter("@permaPurge", permaPurge));

        serviceLogReason += " [Purged = " + permaPurge.ToString() + "]";
        cmd.Parameters.Add(new SqlParameter("@deleteReason", serviceLogReason));
        SqlParameter pout = new SqlParameter("@numAffected", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        try
        {
            con.Open();

            tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
            cmd.Connection = con;
            cmd.Transaction = tran;

            cmd.ExecuteNonQuery();
            result = (long)cmd.Parameters["@numAffected"].Value > 0;
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            /*
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
             * Handled by the caller. */
        }

        return result;
    }

    public bool Character_LoadAny(Guid player, ICharacterInfo characterContainer)
    {
        bool result = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_GetAny", true);
        cmd.Parameters.Add(new SqlParameter("@user", player));

        SqlParameter pout = new SqlParameter("@resultCode", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        SqlDataReader reader = null;
        try
        {
            int idColumn = 1;
            int valueColumn = 2;
            int nameColumn = 3;
            con.Open();
            reader = cmd.ExecuteReader();
            int count = 0;
            if (!reader.HasRows)
            {
                return false;
            }

            if (reader.Read())
            {
                Guid ownerAccount = reader.GetGuid(0);
                DateTime createdOn = reader.GetDateTimeUtc(1);
                bool isActive = reader.GetBoolean(2);
                bool isDeleted = reader.GetBoolean(3);
            }

            //reader.NextResult();
            while (reader.NextResult())
            {
                count++;
                if (!reader.HasRows)
                {
                    continue;
                }
                ICharacterInfo ci = characterContainer;
                switch (count)
                {
                    case 1: // Float
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), (int)reader.GetDouble(valueColumn));
                        }
                        break;
                    case 2: // Int
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt32(valueColumn));
                        }
                        break;
                    case 3: // Long
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt64(valueColumn));
                        }
                        break;
                    case 4: // String
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetString(valueColumn));
                        }
                        break;
                    case 5: // Stats
                        //reader.NextResult(); // read past the character id
                        while (reader.Read())
                        {
                            Stat s = new Stat();
                            s.StatID = reader.GetInt32(reader.GetOrdinal("StatID"));

                            double cValue = reader.GetDouble(reader.GetOrdinal("CurrentValue"));
                            double mValue = reader.GetDouble(reader.GetOrdinal("MaxValue"));
                            double mnValue = reader.GetDouble(reader.GetOrdinal("MinValue"));

                            Stat proto = StatManager.Instance[s.StatID];
                            if (proto == null)
                            {
                                Log1.Logger("Server.Stats").Error("Character_Load attempted to read stat ID [" + s.StatID + "] which was not defined in the Stats.xml config file. Stat not added to character.");
                                continue;
                            }

                            s.MinValue = (float)mnValue;
                            s.MaxValue = (float)mValue;
                            s.ForceValue((float)cValue);

                            s.Description = proto.Description;
                            s.DisplayName = proto.DisplayName;
                            s.Group = proto.Group;

                            ci.Stats.AddStat(s);
                        }
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            result = false;
        }
        finally
        {
            if (reader != null && !reader.IsClosed)
            {
                reader.Close();
            }
        }
        return result;
    }

    /*public void Test(int newCharacterId, PropertyBag props)
    {
        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertStringProperties", true);
        SqlParameter strings = new SqlParameter("@InputTable", StringPropertiesToTable(props.GetAllPropertiesOfKind(PropertyKind.String)));
        strings.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(strings);

        cmd.Parameters.Add("@charID", newCharacterId);

        try
        {
            con.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            int x = 0;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return;
    } */

    public bool Character_Create(Guid owner, StatBag stats, PropertyBag properties, int namePropertyId, string characterName, bool enforceUniqueName, int maxCharactersForUser, bool isTempCharacter, out string msg, out SqlTransaction tran, out SqlConnection con, out int newCharacterId)
    {
        tran = null;
        newCharacterId = 0;
        bool result = true;
        msg = "";

        con = GameDataConnection;        
        SqlCommand cmd = GetCommand(con, "Character_Create", true);

        SqlParameter pout = new SqlParameter("@resultCode", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        cmd.Parameters.Add(new SqlParameter("@owner", owner));
        cmd.Parameters.Add(new SqlParameter("@enforceUniqueName", enforceUniqueName));
        cmd.Parameters.Add(new SqlParameter("@namePropertyId", namePropertyId));
        cmd.Parameters.Add(new SqlParameter("@characterName", characterName));
        cmd.Parameters.Add(new SqlParameter("@maxCharacters", maxCharactersForUser));

        SqlParameter ints = new SqlParameter("@intProperties", IntPropertiesToTable(properties.GetAllPropertiesOfKind(PropertyKind.Int32)));
        ints.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(ints);

        SqlParameter floats = new SqlParameter("@floatProperties", FloatPropertiesToTable(properties.GetAllPropertiesOfKind(PropertyKind.Single)));
        floats.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(floats);

        SqlParameter longs = new SqlParameter("@longProperties", LongPropertiesToTable(properties.GetAllPropertiesOfKind(PropertyKind.Int64)));
        longs.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(longs);

        SqlParameter strings = new SqlParameter("@stringProperties", StringPropertiesToTable(properties.GetAllPropertiesOfKind(PropertyKind.String)));
        strings.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(strings);

        cmd.Parameters.Add(new SqlParameter("@isTemp", isTempCharacter));

        SqlParameter statsParm = new SqlParameter("@stats", StatsToTable(stats.AllStats));
        statsParm.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(statsParm);

        SqlParameter charIdParm = new SqlParameter("@characterId", 0);
        charIdParm.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(charIdParm);

        try
        {
            con.Open();
            tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
            cmd.Connection = con;
            cmd.Transaction = tran;

            cmd.ExecuteNonQuery();
            long val = (long)cmd.Parameters[0].Value;
            result = val > 0;

            switch (val)
            {
                case -2:
                    msg = string.Format("That character name is already taken. Choose another.");
                    break;
                case -9:
                    msg = string.Format("You can't create another character until you delete one first. You may have up to {0} characters on your account.", maxCharactersForUser);
                    break;
                case -1:
                case 0:
                    msg = "Server was unable to created character.";
                    break;
                case 1:
                    msg = "Character crated.";
                    newCharacterId = (int)(long)charIdParm.Value;
                    break;
            }

            // -9 = adding a character would exceed max characters allowed for this account
            // -2 = character name taken
            // -1 = unknown error creating character
            //  0 = unknown error crating character starting stats
            //  1 = character created successfully
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            /*
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
            
             * Calling method must close connection. this is to allow API users to append to the character creation method and commit the transaction
             before closing the connection.*/
        }

        return result;
    }

    public bool Character_GetAll(Guid player, LinkedList<Property> intPropsToInclude, LinkedList<Property> longPropsToInclude, LinkedList<Property> floatPropsToInclude, LinkedList<Property> stringPropsToInclude, Dictionary<int, ICharacterInfo> charactersContainer)
    {
        if (charactersContainer == null)
        {
            return false;
        }
        bool result = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_GetPropertiesFromAllCharacters", true);
        cmd.Parameters.Add(new SqlParameter("@user", player));

        SqlParameter intprops = new SqlParameter("@intProps", PropertiesToIntArrayTable(intPropsToInclude));
        intprops.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(intprops);

        SqlParameter longprops = new SqlParameter("@longProps", PropertiesToIntArrayTable(longPropsToInclude));
        longprops.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(longprops);

        SqlParameter floatprops = new SqlParameter("@floatProps", PropertiesToIntArrayTable(floatPropsToInclude));
        floatprops.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(floatprops);

        SqlParameter stringprops = new SqlParameter("@stringProps", PropertiesToIntArrayTable(stringPropsToInclude));
        stringprops.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(stringprops);

        SqlDataReader reader = null;
        try
        {
            int idColumn = 2;
            int valueColumn = 3;
            int nameColumn = 4;

            con.Open();
            reader = cmd.ExecuteReader();
            int count = 0;

            ICharacterInfo ci = null; ;
            do
            {
                ci = null;
                count++;                
                switch (count)
                {
                    case 1: // Float
                        while (reader.Read())
                        {
                            int id = (int)reader.GetInt32(0);
                            if (!charactersContainer.TryGetValue(id, out ci))
                            {
                                ci = new CharacterInfo(id);
                                charactersContainer.Add(id, ci);
                            }
                            
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), (float)reader.GetDouble(valueColumn));
                        }
                        break;
                    case 2: // Int
                        while (reader.Read())
                        {
                            int id = (int)reader.GetInt32(0);
                            if (!charactersContainer.TryGetValue(id, out ci))
                            {
                                ci = new CharacterInfo(id);
                                charactersContainer.Add(id, ci);
                            }
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt32(valueColumn));
                        }
                        break;
                    case 3: // Long
                        while (reader.Read())
                        {
                            int id = (int)reader.GetInt32(0);
                            if (!charactersContainer.TryGetValue(id, out ci))
                            {
                                ci = new CharacterInfo(id);
                                charactersContainer.Add(id, ci);
                            }
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt64(valueColumn));
                        }
                        break;
                    case 4: // String
                        while (reader.Read())
                        {
                            int id = (int)reader.GetInt32(0);
                            if (!charactersContainer.TryGetValue(id, out ci))
                            {
                                ci = new CharacterInfo(id);
                                charactersContainer.Add(id, ci);
                            }
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetString(valueColumn));
                            //Log.LogMsg(player.ToString() + " : " + id.ToString() + " : " + ci.CharacterName);
                        }
                        break;
                }

            } while (reader.NextResult());
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
        }
        finally
        {
            if (reader != null && !reader.IsClosed)
            {
                reader.Close();
            }
            /*
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
            */
        }


        return result;
    }

    public bool Character_Load(Guid player, ICharacterInfo characterContainer, int characterId, ref Guid owner, out SqlTransaction tran, out SqlConnection con)
    {
        tran = null;
        con = null;
        if (characterContainer == null)
        {
            return false;
        }

        bool result = true;

        con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_Get", true);
        cmd.Parameters.Add(new SqlParameter("@user", player));

        SqlParameter pout = new SqlParameter("@resultCode", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        cmd.Parameters.Add(new SqlParameter("@character", characterId));

        SqlDataReader reader = null;
        try
        { 
            int idColumn = 1;
            int valueColumn = 2;
            int nameColumn = 3;

            con.Open();

            tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
            cmd.Connection = con;
            cmd.Transaction = tran;

            reader = cmd.ExecuteReader();
            int count = 0;
            if (!reader.HasRows)
            {
                return false;
            }

            if (reader.Read())
            {
                owner = reader.GetGuid(0);
                DateTime createdOn = reader.GetDateTimeUtc(1);
                bool isActive = reader.GetBoolean(2);
                bool isDeleted = reader.GetBoolean(3);
            }

            reader.NextResult();
            while (reader.NextResult())
            {
                count++;
                if (!reader.HasRows)
                {
                    continue;
                }

                ICharacterInfo ci = characterContainer;
                switch (count)
                {
                    case 1: // Float
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), (float)reader.GetDouble(valueColumn));
                        }
                        break;
                    case 2: // Int
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt32(valueColumn));
                        }
                        break;
                    case 3: // Long
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetInt64(valueColumn));
                        }
                        break;
                    case 4: // String
                        while (reader.Read())
                        {
                            string name = reader.IsDBNull(nameColumn) ? "" : reader.GetString(nameColumn);
                            ci.Properties.SetProperty(name, (int)reader.GetInt32(idColumn), reader.GetString(valueColumn));
                        }
                        break;
                    case 5: // Stats
                        reader.NextResult(); // read past the character id
                        while (reader.Read())
                        {
                            Stat s = new Stat();
                            s.StatID = reader.GetInt32(reader.GetOrdinal("StatID"));
                            
                            double cValue = reader.GetDouble(reader.GetOrdinal("CurrentValue"));
                            double mValue = reader.GetDouble(reader.GetOrdinal("MaxValue"));
                            double mnValue = reader.GetDouble(reader.GetOrdinal("MinValue"));
                            
                            Stat proto = StatManager.Instance[s.StatID];
                            if (proto == null)
                            {
                                Log1.Logger("Server.Stats").Error("Character_Load attempted to read stat ID [" + s.StatID + "] which was not defined in the Stats.xml config file. Stat not added to character.");
                                continue;
                            }

                            s.MinValue = (float)mnValue;
                            s.MaxValue = (float)mValue;
                            s.ForceValue((float)cValue);

                            s.Description = proto.Description;
                            s.DisplayName = proto.DisplayName;
                            s.Group = proto.Group;

                            ci.Stats.AddStat(s);
                        }
                        break;
                }
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            result = false;
        }
        finally
        {
            characterContainer.ID = characterId;
            
            if (reader != null && !reader.IsClosed)
            {
                reader.Close();
            }
        }
        return result;
    }

    public bool Character_Save(Guid owner, ICharacterInfo toon, int namePropertyId, string characterName, bool enforceUniqueName, out string msg, out SqlTransaction tran, out SqlConnection con)
    {
        /*
            WHEN NOT MATCHED [BY TARGET] -- row exists in source but not in target
            WHEN NOT MATCHED BY SOURCE -- row exists in target but not in source
        */
        tran = null;

        bool result = true;
        msg = "";
        con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_Save", true);

        SqlParameter pout = new SqlParameter("@resultCode", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);
        
        cmd.Parameters.Add(new SqlParameter("@charID", toon.ID));
        cmd.Parameters.Add(new SqlParameter("@owner", owner));
        cmd.Parameters.Add(new SqlParameter("@enforceUniqueName", enforceUniqueName));
        cmd.Parameters.Add(new SqlParameter("@namePropertyId", namePropertyId));
        cmd.Parameters.Add(new SqlParameter("@characterName", characterName));

        SqlParameter ints = new SqlParameter("@intProperties", IntPropertiesToTable(toon.Properties.GetAllPropertiesOfKind(PropertyKind.Int32)));
        ints.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(ints);

        SqlParameter floats = new SqlParameter("@floatProperties", FloatPropertiesToTable(toon.Properties.GetAllPropertiesOfKind(PropertyKind.Single)));
        floats.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(floats);

        SqlParameter longs = new SqlParameter("@longProperties", LongPropertiesToTable(toon.Properties.GetAllPropertiesOfKind(PropertyKind.Int64)));
        longs.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(longs);

        SqlParameter strings = new SqlParameter("@stringProperties", StringPropertiesToTable(toon.Properties.GetAllPropertiesOfKind(PropertyKind.String)));
        strings.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(strings);

        SqlParameter statsParm = new SqlParameter("@stats", StatsToTable(toon.Stats.AllStats));
        statsParm.SqlDbType = SqlDbType.Structured;
        cmd.Parameters.Add(statsParm);

        try
        {
            con.Open();
            tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
            cmd.Connection = con;
            cmd.Transaction = tran;

            cmd.ExecuteNonQuery();
            long val = (long)cmd.Parameters[0].Value;
            result = val > 0;

            if (val == -2)
            {
                msg = string.Format("That character name is already taken. Choose another.");
            }
            else if (val == -9)
            {
                msg = string.Format("Character doesn't exist in DB.  Create character first, before updating with a save..");
            }

            // -9 = toon doesnt exist
            // -2 = character name taken
            // -1 = unknown error creating character
            //  0 = unknown error crating character starting stats
            //  1 = character created successfully
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            /*
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
            
             * Calling method must close connection. this is to allow API users to append to the character creation method and commit the transaction
             before closing the connection.*/
        }

        return result;
    }

    public bool Character_UpdateStringProperty(int characterId, int propertyId, string propertyName, string newValue)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertStringProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        StringProperty prop = new StringProperty(propertyName, propertyId, newValue, null);
        List<StringProperty> props = new List<StringProperty>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", StringPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_UpdateFloatProperty(int characterId, int propertyId, string propertyName, float newValue)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertFloatProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        SingleProperty prop = new SingleProperty(propertyName, propertyId, newValue, null);
        List<SingleProperty> props = new List<SingleProperty>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", FloatPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }
   
    public bool Character_UpdateLongProperty(int characterId, int propertyId, string propertyName, long newValue)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertLongProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Int64Property prop = new Int64Property(propertyName, propertyId, newValue, null);
        List<Int64Property> props = new List<Int64Property>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", LongPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_UpdateIntProperty(int characterId, int propertyId, string propertyName, int newValue)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertIntProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Int32Property prop = new Int32Property(propertyName, propertyId, newValue, null);
        List<Int32Property> props = new List<Int32Property>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", IntPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_UpdateStat(int characterId, int statId, float minValue, float maxValue, float curValue)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_UpdateOrInsertStats", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Stat stat = new Stat(statId, "", "", "", curValue, minValue, maxValue);
        List<Stat> stats = new List<Stat>();
        stats.Add(stat);
        cmd.Parameters.Add(new SqlParameter("@InputTable", StatsToTable(stats)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_DeleteStat(int characterId, int statId)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_DeleteStats", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Stat stat = new Stat(statId, "", "", "", 0, 0, 0);
        List<Stat> stats = new List<Stat>();
        stats.Add(stat);
        cmd.Parameters.Add(new SqlParameter("@InputTable", StatsToTable(stats)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_DeleteFloatProperty(int characterId, int propertyDbId)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_DeleteFloatProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        SingleProperty prop = new SingleProperty("", propertyDbId, 0, null);
        List<SingleProperty> props = new List<SingleProperty>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", FloatPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_DeleteStringProperty(int characterId, int propertyDbId)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_DeleteStringProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        StringProperty prop = new StringProperty("", propertyDbId, "", null);
        List<StringProperty> props = new List<StringProperty>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", StringPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_DeleteLongProperty(int characterId, int propertyDbId)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_DeleteLongProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Int64Property prop = new Int64Property("", propertyDbId, 0, null);
        List<Int64Property> props = new List<Int64Property>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", LongPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool Character_DeleteIntProperty(int characterId, int propertyDbId)
    {
        bool rslt = true;

        SqlConnection con = GameDataConnection;
        SqlCommand cmd = GetCommand(con, "Character_DeleteIntProperties", true);
        cmd.Parameters.Add(new SqlParameter("@charID", characterId));

        Int32Property prop = new Int32Property("", propertyDbId, 0, null);
        List<Int32Property> props = new List<Int32Property>();
        props.Add(prop);
        cmd.Parameters.Add(new SqlParameter("@InputTable", IntPropertiesToTable(props)));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;

    }

    public bool User_Suspend(Guid account, string suspendingAuthority, string serviceNote, long suspensionInHours, int characterId, out DateTime suspensionReleaseUTC)
    {
        bool rslt = true;        

        SqlConnection con = UserDataConnection;
        SqlCommand cmd = GetCommand(con, "aspnet_UsersSuspend", true);
        cmd.Parameters.Add(new SqlParameter("@Account", account));
        cmd.Parameters.Add(new SqlParameter("@CharacterId", characterId));        
        cmd.Parameters.Add(new SqlParameter("@EntryBy", suspendingAuthority));

        suspensionReleaseUTC = DateTime.UtcNow + TimeSpan.FromHours(suspensionInHours);
        serviceNote = "Suspended until " + suspensionReleaseUTC.ToString("g") + ".\r\n" + serviceNote;
        
        cmd.Parameters.Add(new SqlParameter("@ServiceNote", serviceNote));        
        cmd.Parameters.Add(new SqlParameter("@ReleaseDateUTC", suspensionReleaseUTC));

        SqlParameter pout = new SqlParameter("@numAffected", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
            long returnCode = (long)pout.Value;
            rslt = returnCode > 0;
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_Unsuspend(Guid account, string unsuspendingAuthority, DateTime effectiveDateUTC, string serviceNote, int characterId)
    {

        bool rslt = true;

        SqlConnection con = UserDataConnection;
        SqlCommand cmd = GetCommand(con, "aspnet_UsersUnsuspend", true);
        cmd.Parameters.Add(new SqlParameter("@Account", account));        
        cmd.Parameters.Add(new SqlParameter("@EntryBy", unsuspendingAuthority));

        serviceNote = "Account unsuspended.\r\n" + serviceNote;
        cmd.Parameters.Add(new SqlParameter("@ServiceNote", serviceNote));
        SqlParameter pout = new SqlParameter("@numAffected", 0);
        pout.Direction = ParameterDirection.Output;
        cmd.Parameters.Add(pout);

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
            object ret = cmd.Parameters["@numAffected"].Value;
            long returnCode = (long)ret;
            rslt = returnCode > 0;
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_CreateServiceLogEntry(Guid account, string entryType, string entryBy, string serviceNote, int characterId)
    {
        bool rslt = true;

        SqlConnection con = UserDataConnection;
        SqlCommand cmd = GetCommand(con, "aspnet_UsersInsertServiceLog", true);
        cmd.Parameters.Add(new SqlParameter("@Account", account));
        cmd.Parameters.Add(new SqlParameter("@Note", serviceNote));
        cmd.Parameters.Add(new SqlParameter("@EntryBy", entryBy));
        cmd.Parameters.Add(new SqlParameter("@EntryType", entryType));
        cmd.Parameters.Add(new SqlParameter("@CharacterID", characterId));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_GetServiceLogEntries(Guid account, string entryBy, List<ServiceLogEntry> entries)
    {
        if (entries == null || (account == Guid.Empty && (entryBy == null || entryBy.Length < 1)))
        {
            return false;
        }
        
        bool result = true;

        SqlConnection con = UserDataConnection;
        SqlCommand cmd = GetCommand(con, "aspnet_UsersGetServiceLog", true);
        if (account == Guid.Empty)
        {
            cmd.Parameters.Add(new SqlParameter("@Account", null));
        }
        else
        {
            cmd.Parameters.Add(new SqlParameter("@Account", account));
        }

        if (entryBy == null || entryBy.Length < 1)
        {
            cmd.Parameters.Add(new SqlParameter("@EntryBy", null));
        }
        else
        {
            cmd.Parameters.Add(new SqlParameter("@EntryBy", entryBy));
        }

        SqlDataReader reader = null;
        try
        {
            con.Open();
            reader = cmd.ExecuteReader();
            if (!reader.HasRows)
            {
                return true;
            }

            while (reader.Read())
            {
                ServiceLogEntry sle = new ServiceLogEntry();
                sle.EntryBy = reader.GetString(0);
                sle.TimeStampUTC = reader.GetDateTimeUtc(1);
                sle.Note = reader.GetString(2);
                sle.EntryType = reader.GetString(3);
                sle.Account = reader.GetGuid(4);
                sle.CharacterId = reader.GetInt32(5);
                entries.Add(sle);
            }

        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            result = false;
        }
        finally
        {
            if (reader != null && !reader.IsClosed)
            {
                reader.Close();
            }
        }
        return result;
    }

    public bool User_ClearSessionsForServer(string targetServer)
    {
        bool result = true;

        SqlConnection con = SessionDataConnection;
        SqlCommand cmd = GetCommand(UserDataConnection, "DELETE FROM Session_tblAuth WHERE AuthorizingServerID = '" + targetServer + "'", false);

        try
        {
            con.Open();
            cmd.Connection = con;

            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    public bool User_AuthorizeSession(string account, Guid accountID, Guid authTicket, string authorizingServer, DateTime authorizedOnUTC, int character, string targetServerID)
    {
        return User_AuthorizeSession("", account, accountID, authTicket, authorizingServer, authorizedOnUTC, character, targetServerID);
    }

    public bool User_AuthorizeSession(string serverGroup, string account, Guid accountID, Guid authTicket, string authorizingServer, DateTime authorizedOnUTC, int character, string targetServerID)
    {
        bool rslt = true;

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }
        SqlCommand cmd = GetCommand(con, "Session_AuthorizeAccount", true);
        cmd.Parameters.Add(new SqlParameter("@AccountName", account));
        cmd.Parameters.Add(new SqlParameter("@AuthorizedOn", authorizedOnUTC));
        cmd.Parameters.Add(new SqlParameter("@AuthorizingServerID", authorizingServer));
        cmd.Parameters.Add(new SqlParameter("@Ticket", authTicket));
        cmd.Parameters.Add(new SqlParameter("@Character", character));
        cmd.Parameters.Add(new SqlParameter("@TargetServerID", targetServerID));
        cmd.Parameters.Add(new SqlParameter("@AccountID", accountID));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_UnauthorizeSession(string account)
    {
        return User_UnauthorizeSession("", account);
    }

    public bool User_UnauthorizeSession(string serverGroup, string account)
    {
        bool rslt = true;

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = GetCommand(con, "Session_UnauthorizeAccount", true);
        cmd.Parameters.Add(new SqlParameter("@AccountName", account));

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_ClearAllSessions()
    {
        return User_ClearAllSessions("");
    }

    public bool User_ClearAllSessions(string serverGroup)
    {
        bool rslt = true;

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = GetCommand(con, "Session_ClearAllSessions", true);

        try
        {
            con.Open();
            int code = cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_GetAuthorizationTicketForAccount(out string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, out int character, out string targetServerID, Guid accountID)
    {
        return User_GetAuthorizationTicketForAccount("", out account, out authorizingServer, out ticket, out authorizationOnUTC, out character, out targetServerID, accountID);
    }

    public bool User_GetAuthorizationTicketForAccount(string serverGroup, out string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, out int character, out string targetServerID, Guid accountID)
    {
        bool rslt = true;
        authorizationOnUTC = DateTime.MinValue;
        authorizingServer = "";
        ticket = Guid.Empty;
        targetServerID = "";
        account = "";
        character = -1;

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = GetCommand(con, "Session_GetAuthorizationTicketForAccount", true);
        cmd.Parameters.Add(new SqlParameter("@AccountID", accountID));

        try
        {
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                ticket = (Guid)r.GetSqlGuid(r.GetOrdinal("Ticket"));
                authorizingServer = r.GetString(r.GetOrdinal("AuthorizingServerID"));
                targetServerID = r.GetString(r.GetOrdinal("TargetServerID"));
                authorizationOnUTC = r.GetDateTimeUtc("AuthorizedOn");
                account = r.GetString(r.GetOrdinal("AccountName"));
                character = r.GetInt32(r.GetOrdinal("Character"));
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }
        
    public bool User_GetAuthorizationTicketForCharacter(out string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, int character, out string targetServerID, out Guid accountID)
    {
        return User_GetAuthorizationTicketForCharacter("", out account, out authorizingServer, out ticket, out authorizationOnUTC, character, out targetServerID, out accountID);
    }

    public bool User_GetAuthorizationTicketForCharacter(string serverGroup, out string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, int character, out string targetServerID, out Guid accountID)
    {
        bool rslt = true;
        authorizationOnUTC = DateTime.MinValue;
        authorizingServer = "";
        ticket = accountID = Guid.Empty;
        targetServerID = "";
        account = "";

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = GetCommand(con, "Session_GetAuthorizationTicketForCharacter", true);
        cmd.Parameters.Add(new SqlParameter("@ToonID", account));

        try
        {
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                ticket = (Guid)r.GetSqlGuid(r.GetOrdinal("Ticket"));
                authorizingServer = r.GetString(r.GetOrdinal("AuthorizingServerID"));
                targetServerID = r.GetString(r.GetOrdinal("TargetServerID"));
                authorizationOnUTC = r.GetDateTimeUtc("AuthorizedOn");
                account = r.GetString(r.GetOrdinal("AccountName"));
                accountID = r.GetGuid(r.GetOrdinal("AccountID"));
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    public bool User_GetAuthorizationTicket(string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, out int character, out string targetServerID, out Guid accountID)
    {
        return User_GetAuthorizationTicket("", account, out authorizingServer, out ticket, out authorizationOnUTC, out character, out targetServerID, out accountID);
    }

    public bool User_GetAuthorizationTicket(string serverGroup, string account, out string authorizingServer, out Guid ticket, out DateTime authorizationOnUTC, out int character, out string targetServerID, out Guid accountID)
    {
        bool rslt = true;
        authorizationOnUTC = DateTime.MinValue;
        authorizingServer = "";
        ticket = accountID = Guid.Empty;
        character = -1;
        targetServerID = "";

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = GetCommand(con, "Session_GetAuthorizationTicket", true);
        cmd.Parameters.Add(new SqlParameter("@AccountName", account));

        try
        {
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                ticket = (Guid)r.GetSqlGuid(r.GetOrdinal("Ticket"));
                authorizingServer = r.GetString(r.GetOrdinal("AuthorizingServerID"));
                targetServerID = r.GetString(r.GetOrdinal("TargetServerID"));
                authorizationOnUTC = r.GetDateTimeUtc("AuthorizedOn");
                character = r.GetInt32(r.GetOrdinal("Character"));
                accountID = r.GetGuid(r.GetOrdinal("AccountID"));
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            int x = 0;
            rslt = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
            }
        }

        return rslt;
    }

    /// <summary>
    /// Registers the address and IP of a game server in the DB
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public bool Server_Register(string clusterServerId, string address, int port, DateTime registeredOnUTC, string serverType, int curConnections, int maxConnections)
    {
        return Server_Register("", clusterServerId, address, port, registeredOnUTC, serverType, curConnections, maxConnections);
    }

    /// <summary>
    /// Registers the address and IP of a game server in the DB
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <param name="address"></param>
    /// <param name="port"></param>
    /// <returns></returns>
    public bool Server_Register(string serverGroup, string clusterServerId, string address, int port, DateTime registeredOnUTC, string serverType, int curConnections, int maxConnections)
    {
        bool result = true;
        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = DB.GetCommand(con, "Lobby_RegisterServer", true);

        cmd.Parameters.Add(new SqlParameter("@ClusterServerID", clusterServerId));
        cmd.Parameters.Add(new SqlParameter("@Address", address));
        cmd.Parameters.Add(new SqlParameter("@Port", port));
        cmd.Parameters.Add(new SqlParameter("@RegisteredOn", registeredOnUTC));
        cmd.Parameters.Add(new SqlParameter("@Type", serverType));
        cmd.Parameters.Add(new SqlParameter("@CurConnections", curConnections));
        cmd.Parameters.Add(new SqlParameter("@MaxConnections", maxConnections));

        try
        {
            con.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Unregisters the address and IP of a game server in the DB
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_Unregister(string clusterServerId)
    {
        return Server_Unregister("", clusterServerId);
    }

    /// <summary>
    /// Unregisters the address and IP of a game server in the DB
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_Unregister(string serverGroup, string clusterServerId)
    {
        bool result = true;
        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = DB.GetCommand(con, "Lobby_UnregisterServer", true);

        cmd.Parameters.Add(new SqlParameter("@ClusterServerID", clusterServerId));

        try
        {
            con.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Clears all entries in the server address/port listing.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public bool Server_ClearRegistrations(string serverType)
    {
        return Server_ClearRegistrations("", serverType);
    }

    /// <summary>
    /// Clears all entries in the server address/port listing. Pass "all" for @serverType to delete all registrations of all types.
    /// </summary>
    /// <param name="db"></param>
    /// <returns></returns>
    public bool Server_ClearRegistrations(string serverGroup, string serverType)
    {
        bool result = true;
        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = DB.GetCommand(con, "Lobby_ClearServerRegistrations", true);
        cmd.Parameters.Add(new SqlParameter("@ServerType", serverType));

        try
        {
            con.Open();
            cmd.ExecuteNonQuery();
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Clears all entries in the server address/port listing.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_GetRegistrations(string serverType, out List<string> addresses, out List<string> ids, out List<int> ports, out List<int> curConnections, out List<int> maxConnections)
    {
        return Server_GetRegistrations("", serverType, out addresses, out ids, out ports, out curConnections, out maxConnections);
    }

    /// <summary>
    /// Clears all entries in the server address/port listing.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_GetRegistrations(string serverGroup, string serverType, out List<string> addresses, out List<string> ids, out List<int> ports, out List<int> curConnections, out List<int> maxConnections)
    {
        bool result = true;
        addresses = new List<string>();
        ids = new List<string>();
        ports = new List<int>();
        curConnections = new List<int>();
        maxConnections = new List<int>();

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }

        SqlCommand cmd = DB.GetCommand(con, "Lobby_GetServerRegistrationsForType", true);
        cmd.Parameters.Add(new SqlParameter("@Type", serverType));

        try
        {
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            while (r.Read())
            {
                string address = r.GetString(r.GetOrdinal("Address"));
                int port = r.GetInt32(r.GetOrdinal("Port"));
                string id = r.GetString(r.GetOrdinal("ClusterServerID"));
                int curConnection = r.GetInt32(r.GetOrdinal("CurConnections"));
                int maxConnection = r.GetInt32(r.GetOrdinal("MaxConnections"));
                addresses.Add(address);
                ports.Add(port);
                ids.Add(id);
                curConnections.Add(curConnection);
                maxConnections.Add(maxConnection);
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets entrt from the server address/port listing.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_GetRegistrations(string serverGroup, string clusterServerId, out string serverType, out string address, out int port, out int curConnections, out int maxConnections)
    {
        bool result = true;
        address = "";
        serverType = "";
        port = -1;
        maxConnections = 0;
        curConnections = 0;

        SqlConnection con = null;
        if (serverGroup == null || serverGroup.Length < 1)
        {
            con = SessionDataConnection;
        }
        else
        {
            con = GetSessionDataConnectionForServer(serverGroup);
            if (con == null)
            {
                return false;
            }
        }
        curConnections = maxConnections = -1;

        SqlCommand cmd = DB.GetCommand(con, "Lobby_GetServerRegistrations", true);
        cmd.Parameters.Add(new SqlParameter("@ClusterServerID", clusterServerId));

        try
        {
            con.Open();
            SqlDataReader r = cmd.ExecuteReader();
            if (r.Read())
            {
                address = r.GetString(r.GetOrdinal("Address"));
                port = r.GetInt32(r.GetOrdinal("Port"));
                serverType = r.GetString(r.GetOrdinal("Type"));
                curConnections = r.GetInt32(r.GetOrdinal("CurConnections"));
                maxConnections = r.GetInt32(r.GetOrdinal("MaxConnections"));
            }
            else
            {
                return false;
            }
        }
        catch (Exception e)
        {
            Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
            result = false;
        }
        finally
        {
            if (con != null)
            {
                con.Close();
                con.Dispose();
                con = null;
            }
        }

        return result;
    }

    /// <summary>
    /// Gets entry from the server address/port listing.
    /// </summary>
    /// <param name="db"></param>
    /// <param name="clusterServerId"></param>
    /// <returns></returns>
    public bool Server_GetRegistrations(string clusterServerId, out string serverType, out string address, out int port, out int curConnections, out int maxConnections)
    {
        return Server_GetRegistrations("", clusterServerId,  out serverType, out address, out port, out curConnections, out maxConnections);
    }

}

