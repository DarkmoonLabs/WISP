using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Data;
using GameLib;

namespace Shared
{
    public static class DBExtensionItems
    {
        private static DataTable IntArrayToTable(IEnumerable<int> input)
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

        private static DataTable GuidArrayToTable(IEnumerable<Guid> input, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("GuiArray");
                table.Columns.Add("Item", typeof(Guid));
            }

            IEnumerator<Guid> enu = input.GetEnumerator();
            while (enu.MoveNext())
            {
                DataRow r = table.NewRow();
                r["Item"] = enu.Current;
                table.Rows.Add(r);
            }

            return table;
        }

        private static DataTable PropertiesToIntArrayTable(IEnumerable<Property> input, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("IntArray");
                table.Columns.Add("Item", typeof(int));
            }

            IEnumerator<Property> enu = input.GetEnumerator();
            while (enu.MoveNext())
            {
                DataRow r = table.NewRow();
                r["Item"] = enu.Current.PropertyId;
                table.Rows.Add(r);
            }

            return table;
        }

        private static DataTable ItemIntPropertiesToTable(IEnumerable<Property> input, Guid owningObject, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("PropertyTable");

                table.Columns.Add("PropertyOwner", typeof(Guid));
                table.Columns.Add("PropertyId", typeof(int));
                table.Columns.Add("PropertyValue", typeof(int));
                table.Columns.Add("PropertyName", typeof(string));
            }

            IEnumerator<Property> enu = input.GetEnumerator();
            Int32Property prop = null;
            while (enu.MoveNext())
            {
                prop = enu.Current as Int32Property;
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = prop.PropertyId;
                r["PropertyValue"] = prop.Value;
                r["PropertyName"] = prop.Name;
                table.Rows.Add(r);
            }

            /* wtf is this for?
            if (table.Rows.Count < 1)
            {
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = 0;
                r["PropertyValue"] = 0;
                r["PropertyName"] = "D";
                table.Rows.Add(r);
            }
            */

            return table;
        }

        private static DataTable ItemStringPropertiesToTable(IEnumerable<Property> input, Guid owningObject, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("PropertyTable");

                table.Columns.Add("PropertyOwner", typeof(Guid));
                table.Columns.Add("PropertyId", typeof(int));
                table.Columns.Add("PropertyValue", typeof(string));
                table.Columns.Add("PropertyName", typeof(string));
            }

            IEnumerator<Property> enu = input.GetEnumerator();
            StringProperty prop = null;
            while (enu.MoveNext())
            {
                prop = enu.Current as StringProperty;
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = prop.PropertyId;
                r["PropertyValue"] = prop.Value;
                r["PropertyName"] = prop.Name;
                table.Rows.Add(r);
            }

            /* wtf is this for?
            if (table.Rows.Count < 1)
            {
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = 0;
                r["PropertyValue"] = "";
                r["PropertyName"] = "D";
                table.Rows.Add(r);
            }
            */

            return table;
        }

        private static DataTable GameObjectToDeleteRow(ServerGameObject input, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("DeleteTable");

                table.Columns.Add("ItemID", typeof(Guid));
                table.Columns.Add("PermaPurge", typeof(bool));
                table.Columns.Add("Account", typeof(Guid));
                table.Columns.Add("DeleteReason", typeof(string));
            }
        
            DataRow r = table.NewRow();
            r["ItemID"] = input.UID;
            r["PermaPurge"] = false;
            r["Account"] = input.AccountDeleted;
            r["DeleteReason"] = input.DeleteReason;

            table.Rows.Add(r);

            return table;
        }
        private static DataTable GameObjectToTable(ServerGameObject input, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("ItemTable");

                table.Columns.Add("Template", typeof(string));
                table.Columns.Add("CreatedOn", typeof(DateTime));
                table.Columns.Add("GOT", typeof(int));
                table.Columns.Add("UID", typeof(Guid));
                table.Columns.Add("Owner", typeof(string));
                table.Columns.Add("Context", typeof(Guid));
                table.Columns.Add("TypeHash", typeof(long));
                table.Columns.Add("BinData", typeof(byte[]));
                table.Columns.Add("IsStatic", typeof(byte));
                table.Columns.Add("StackCount", typeof(int));
                table.Columns.Add("ObjectOwner", typeof(Guid));
            }
        
            DataRow r = table.NewRow();
            r["Template"] = input.ItemTemplate;
            r["CreatedOn"] = input.CreatedOn;
            r["GOT"] = (int)input.GameObjectType;
            r["UID"] = input.UID;
            r["Owner"] = input.OwningServer;
            r["Context"] = input.Context;
            r["TypeHash"] = input.TypeHash;

            Pointer dataPointer = new Pointer();
            byte[] bindata = new byte[1024];
            input.Serialize(ref bindata, dataPointer);
            
            // Combine envelope and body into final data gram
            byte[] trimData = new byte[dataPointer.Position];
            Util.Copy(bindata, 0, trimData, trimData.Length, dataPointer.Position);

            r["BinData"] = trimData;

            r["StackCount"] = input.StackCount;
            r["IsStatic"] = input.IsStatic;
            r["ObjectOwner"] = input.Owner;
            table.Rows.Add(r);

            return table;
        }

        private static DataTable ItemFloatPropertiesToTable(IEnumerable<Property> input, Guid owningObject, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("PropertyTable");

                table.Columns.Add("PropertyOwner", typeof(Guid));
                table.Columns.Add("PropertyId", typeof(int));
                table.Columns.Add("PropertyValue", typeof(float));
                table.Columns.Add("PropertyName", typeof(string));
            }

            IEnumerator<Property> enu = input.GetEnumerator();
            SingleProperty prop = null;
            while (enu.MoveNext())
            {
                prop = enu.Current as SingleProperty;
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = prop.PropertyId;
                r["PropertyValue"] = prop.Value;
                r["PropertyName"] = prop.Name;
                table.Rows.Add(r);
            }

            /* wtf is this for?
            if (table.Rows.Count < 1)
            {
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = 0;
                r["PropertyValue"] = 0;
                r["PropertyName"] = "D";
                table.Rows.Add(r);
            }
            */

            return table;
        }

        private static DataTable ItemLongPropertiesToTable(IEnumerable<Property> input, Guid owningObject, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("PropertyTable");

                table.Columns.Add("PropertyOwner", typeof(Guid));
                table.Columns.Add("PropertyId", typeof(int));
                table.Columns.Add("PropertyValue", typeof(long));
                table.Columns.Add("PropertyName", typeof(string));
            }

            IEnumerator<Property> enu = input.GetEnumerator();
            Int64Property prop = null;
            while (enu.MoveNext())
            {
                prop = enu.Current as Int64Property;
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = prop.PropertyId;
                r["PropertyValue"] = prop.Value;
                r["PropertyName"] = prop.Name;
                table.Rows.Add(r);
            }

            /* wtf is this for?
            if (table.Rows.Count < 1)
            {
                DataRow r = table.NewRow();
                r["PropertyOwner"] = owningObject;
                r["PropertyId"] = 0;
                r["PropertyValue"] = 0;
                r["PropertyName"] = "D";
                table.Rows.Add(r);
            }
            */

            return table;
        }

        private static DataTable ItemStatsToTable(IEnumerable<Stat> input, Guid owningObject, DataTable table = null)
        {
            if (table == null)
            {
                table = new DataTable("StatTable");

                table.Columns.Add("StatOwner", typeof(Guid));
                table.Columns.Add("StatId", typeof(int));
                table.Columns.Add("StatValue", typeof(float));
                table.Columns.Add("StatMaxValue", typeof(float));
                table.Columns.Add("StatMinValue", typeof(float));
            }

            IEnumerator<Stat> enu = input.GetEnumerator();
            Stat stat = null;
            while (enu.MoveNext())
            {
                stat = enu.Current;
                DataRow r = table.NewRow();
                r["StatOwner"] = owningObject;
                r["StatId"] = stat.StatID;
                r["StatValue"] = stat.CurrentValue;
                r["StatMaxValue"] = stat.MaxValue;
                r["StatMinValue"] = stat.MinValue;
                table.Rows.Add(r);
            }

            if (table.Rows.Count < 1)
            {
                DataRow r = table.NewRow();
                r["StatOwner"] = owningObject;
                r["StatId"] = 0;
                r["StatValue"] = 0;
                r["StatMaxValue"] = 0;
                r["StatMinValue"] = 0;
                table.Rows.Add(r);
            }

            return table;
        }
     
        public static bool Item_Delete(this DB db, Guid account, Guid itemId, bool permaPurge, string serviceLogReason, out SqlTransaction tran, out SqlConnection con)
        {
            bool result = true;
            con = DB.GameDataConnection;
            tran = null;
            SqlCommand cmd = DB.GetCommand(con, "Items_Delete", true);
            cmd.Parameters.Add(new SqlParameter("@ItemID", itemId));
            cmd.Parameters.Add(new SqlParameter("@permaPurge", permaPurge));
            cmd.Parameters.Add(new SqlParameter("@account", account));

            serviceLogReason += " [Purged = " + (permaPurge? "T" : "F") + "]";
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

        public static bool Item_Create(this DB db, ServerGameObject go, string owner, out string msg, out SqlTransaction tran, out SqlConnection con)
        {            
            if(go.IsTransient)
            {
                con = null;
                tran = null;
                msg = "Transient objects can't be saved to the database.";
                return false;
            }

            tran = null;
            bool result = true;
            msg = "";

            con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_Create", true);

            SqlParameter pout = new SqlParameter("@resultCode", 0);
            pout.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(pout);
            
            cmd.Parameters.Add(new SqlParameter("@GOT", (int)go.GameObjectType));
            cmd.Parameters.Add(new SqlParameter("@UID", go.UID));
            cmd.Parameters.Add(new SqlParameter("@createdOn", go.CreatedOn));
            cmd.Parameters.Add(new SqlParameter("@template", go.ItemTemplate));
            cmd.Parameters.Add(new SqlParameter("@owner", owner));
            cmd.Parameters.Add(new SqlParameter("@context", go.Context));
            cmd.Parameters.Add(new SqlParameter("@typeHash", (long)go.TypeHash));

            Pointer dataPointer = new Pointer();
            byte[] bindata = new byte[1024];
            go.Serialize(ref bindata, dataPointer);

            // Combine envelope and body into final data gram
            byte[] trimData = new byte[dataPointer.Position];
            Util.Copy(bindata, 0, trimData, trimData.Length, dataPointer.Position);

            cmd.Parameters.Add(new SqlParameter("@binData", trimData));
            cmd.Parameters.Add(new SqlParameter("@stackCount", go.StackCount));
            cmd.Parameters.Add(new SqlParameter("@isStatic", go.IsStatic));
            cmd.Parameters.Add(new SqlParameter("@objectOwner", go.Owner));  

            if (!go.IsStatic)
            {
                SqlParameter ints = new SqlParameter("@intProperties", ItemIntPropertiesToTable(go.Properties.GetAllPropertiesOfKind(PropertyKind.Int32), go.UID));
                ints.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(ints);

                SqlParameter floats = new SqlParameter("@floatProperties", ItemFloatPropertiesToTable(go.Properties.GetAllPropertiesOfKind(PropertyKind.Single), go.UID));
                floats.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(floats);

                SqlParameter longs = new SqlParameter("@longProperties", ItemLongPropertiesToTable(go.Properties.GetAllPropertiesOfKind(PropertyKind.Int64), go.UID));
                longs.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(longs);

                SqlParameter strings = new SqlParameter("@stringProperties", ItemStringPropertiesToTable(go.Properties.GetAllPropertiesOfKind(PropertyKind.String), go.UID));
                strings.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(strings);

                SqlParameter statsParm = new SqlParameter("@stats", ItemStatsToTable(go.Stats.AllStats, go.UID));
                statsParm.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(statsParm);
            }

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
                    case -1:
                    case 0:
                        msg = "Server was unable to created Item.";
                        break;
                    case 1:
                        msg = "Item created.";
                        break;
                }

                // -1 = unknown error creating Item
                //  0 = unknown error crating Item starting stats
                //  1 = Item created successfully
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
            
                 * Calling method must close connection. this is to allow API users to append to the Item creation method and commit the transaction
                 before closing the connection.*/
            }

            return result;
        }
        //Items_GetBatchForContext
       
        public static List<IGameObject> Item_LoadBatchForContext(this DB db, Guid context, bool includeDeleted, string lockedBy, out SqlTransaction tran, out SqlConnection con)
        {
            tran = null;
            con = null;
            SqlCommand cmd = DB.GetCommand(null, "Items_GetBatchForContext", true);
            cmd.Parameters.Add(new SqlParameter("@context", context));
            return Item_LoadBatchWorker(cmd, includeDeleted, lockedBy, out tran, out con);
        }
        /// <summary>
        /// Read an item from the database.  if @lockedBy is set to null or empty string it is assumed that you are not trying to take ownership of the loaded object and are opening it for read-only 
        /// purposes.  Setting the @lockedBy parameter to a server ID indicates that that server has ownership of the object.  Objects cannot be saved except by the lock's owner.  
        /// </summary>
        /// <param name="db"></param>
        /// <param name="item"></param>
        /// <param name="ItemId"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="loadedBy"></param>
        /// <param name="tran"></param>
        /// <param name="con"></param>
        /// <returns></returns>
        public static List<IGameObject> Item_LoadBatch(this DB db, List<Guid> items, bool includeDeleted, string lockedBy, out SqlTransaction tran, out SqlConnection con)
        {
            tran = null;
            con = null;
            if (items == null || items.Count < 1)
            {
                return new List<IGameObject>();
            }

            SqlCommand cmd = DB.GetCommand(null, "Items_GetBatch", true);

            cmd.Parameters.Add(new SqlParameter("@ItemIDs", GuidArrayToTable(items)));
            return Item_LoadBatchWorker(cmd, includeDeleted, lockedBy, out tran, out con);
        }

        private static List<IGameObject> Item_LoadBatchWorker(SqlCommand cmd, bool includeDeleted, string lockedBy, out SqlTransaction tran, out SqlConnection con)
        {
            tran = null;
            con = DB.GameDataConnection;
            cmd.Connection = con;
            cmd.CommandTimeout = 0;

            List<IGameObject> result = new List<IGameObject>();

            cmd.Parameters.Add(new SqlParameter("@includeDeleted", includeDeleted));
            cmd.Parameters.Add(new SqlParameter("@lockedBy", lockedBy));

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

                tran = con.BeginTransaction(IsolationLevel.Serializable);
                cmd.Connection = con;
                cmd.Transaction = tran;

                reader = cmd.ExecuteReader();
                bool isStatic = false;

                while (reader.HasRows && reader.Read()) // any items to read?
                {
                    IGameObject loadedItem = null;

                    string templateNum = reader.GetString(1);
                    loadedItem = ItemUtil.Instance.CreateItemShell(templateNum);

                    bool isDeleted = reader.GetByte(2) == 1;
                    ((ServerGameObject)loadedItem).IsDeleted = isDeleted;
                    int GOT = reader.GetInt32(3);
                    loadedItem.UID = reader.GetGuid(0);
                    // loadedItem.ItemTemplate = templateNum; // set by template in Create item shell
                    // loadedItem.GameObjectType = (GOT)GOT; // set by template in Create item shell
                    loadedItem.CreatedOn = reader.GetDateTimeUtc(4);
                    // isStatic = reader.GetByte(9) == 1; // set by template in Create item shell

                    loadedItem.StackCount = reader.GetInt32(9);
                    loadedItem.Owner = reader.GetGuid(10);

                    if (loadedItem is ServerGameObject && !reader.IsDBNull(5))
                    {
                        ((ServerGameObject)loadedItem).OwningServer = reader.GetString(5);
                    }

                    loadedItem.Context = reader.GetGuid(6);
                    if (loadedItem.TypeHash != (uint)reader.GetInt64(7))
                    {
                        Log1.Logger("Server").Error("Tried loading game object [" + loadedItem.UID.ToString() + "]. Type hash was inconsistent with database.)");
                    }

                    if (!isStatic && !reader.IsDBNull(8))
                    {
                        byte[] binData = (byte[])reader["BinData"]; // works good if there's not a ton of data
                        loadedItem.Deserialize(binData, new Pointer());
                        
                        /*
                        long dataSize = reader.GetBytes(8, 0, null, 0, 0);
                        
                        byte[] buffer = new byte[1024];
                        var dataRemaining = dataSize;
                        while (dataRemaining > 0)
                        {
                            int bytesToRead = (int)(buffer.Length < dataRemaining ? buffer.Length : dataRemaining);
                            //fill the buffer
                            reader.GetBytes(1, dataSize - dataRemaining, buffer, 0, bytesToRead);
                            Util.Copy(buffer, 0, binData, (int)dataSize - (int)dataRemaining, bytesToRead);
                            dataRemaining -= bytesToRead;
                        }
                        */
                    }


                   // if (!isStatic)
                    {
                        reader.NextResult(); // grab item properties
                        bool attribsDone = false;
                        int count = 0;
                        while (!attribsDone)
                        {
                            //reader.NextResult();
                            count++;

                            if (count == 5)
                            {
                                // Finished reading item attributes.  Go on to the next item.
                                attribsDone = true;
                            }

                            if (!reader.HasRows)
                            {
                                reader.NextResult();
                                continue;
                            }

                            IGameObject ci = loadedItem;
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
                                    //reader.NextResult(); // read past the Item id
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
                                            Log1.Logger("Server.Stats").Error("Item_Load attempted to read stat ID [" + s.StatID + "] which was not defined in the Stats.xml config file. Stat not added to Item.");
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

                            reader.NextResult();
                        } // while reader has next result for item properties
                    } // if not static item

                    result.Add(loadedItem);
                    if(isStatic)
                    {
                        reader.NextResult();
                    }
                } // while reader has rows
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR Item_Load] : " + e.Message);
                 result.Clear(); // return all or nothing.
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


        /// <summary>
        /// Read an item from the database.  if @lockedBy is set to null or empty string it is assumed that you are not trying to take ownership of the loaded object and are opening it for read-only 
        /// purposes.  Setting the @lockedBy parameter to a server ID indicates that that server has ownership of the object.  Objects cannot be saved except by the lock's owner.  
        /// </summary>
        /// <param name="db"></param>
        /// <param name="item"></param>
        /// <param name="ItemId"></param>
        /// <param name="includeDeleted"></param>
        /// <param name="loadedBy"></param>
        /// <param name="tran"></param>
        /// <param name="con"></param>
        /// <returns></returns>
        public static bool Item_Load(this DB db, out ServerGameObject item, Guid ItemId, bool includeDeleted, string lockedBy, out SqlTransaction tran, out SqlConnection con)
        {
            tran = null;
            con = null;
            
            bool result = true;

            con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_Get", true);
            
            cmd.Parameters.Add(new SqlParameter("@ItemId", ItemId));
            cmd.Parameters.Add(new SqlParameter("@includeDeleted", includeDeleted));
            cmd.Parameters.Add(new SqlParameter("@lockedBy", lockedBy));
            
            SqlParameter pout = new SqlParameter("@resultCode", 0);
            pout.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(pout);

            SqlDataReader reader = null;
            Guid context = Guid.Empty;
            item = null;

            try
            {
                int idColumn = 1;
                int valueColumn = 2;
                int nameColumn = 3;
                con.Open();

                tran = con.BeginTransaction(IsolationLevel.Serializable);
                cmd.Connection = con;
                cmd.Transaction = tran;

                reader = cmd.ExecuteReader();
                int count = 0;
                bool isStatic = false;

                if (reader.HasRows) // any items to read?
                {
                    ServerGameObject loadedItem = null;
                    if (reader.Read()) // read item data from Item Master Rable  
                    {
                        string templateNum = reader.GetString(1);
                        loadedItem = ItemUtil.Instance.CreateItemShell(templateNum);

                        bool isDeleted = reader.GetByte(2) == 1;
                        loadedItem.IsDeleted = isDeleted;
                        // int GOT = reader.GetInt32(3); // set by template in CreateItemShell
                        loadedItem.UID = reader.GetGuid(0);
                        //loadedItem.ItemTemplate = templateNum; // set by template in CreateItemShell
                        // loadedItem.GameObjectType = (GOT)GOT;// set by template in CreateItemShell
                        loadedItem.CreatedOn = reader.GetDateTimeUtc(4);
                        //isStatic = reader.GetByte(9) == 1; // set by template in CreateItemShell
                        loadedItem.StackCount = reader.GetInt32(9);
                        loadedItem.Owner = reader.GetGuid(10);

                        if (!reader.IsDBNull(5))
                        {
                            loadedItem.OwningServer = reader.GetString(5);
                        }

                        loadedItem.Context = reader.GetGuid(6);
                        if (loadedItem.TypeHash != (uint)reader.GetInt64(7))
                        {
                            Log1.Logger("Server").Error("Tried loading game object [" + loadedItem.UID.ToString() + "]. Type hash was inconsistent with database.)");
                        }

                        if (!isStatic && !reader.IsDBNull(8))
                        { 
                            byte[] binData = (byte[])reader["BinData"]; // works good if there's not a ton of data
                            loadedItem.Deserialize(binData, new Pointer());
                            
                            /*
                            long dataSize = reader.GetBytes(8, 0, null, 0, 0);
                        
                            byte[] buffer = new byte[1024];
                            var dataRemaining = dataSize;
                            while (dataRemaining > 0)
                            {
                                int bytesToRead = (int)(buffer.Length < dataRemaining ? buffer.Length : dataRemaining);
                                //fill the buffer
                                reader.GetBytes(1, dataSize - dataRemaining, buffer, 0, bytesToRead);
                                Util.Copy(buffer, 0, binData, (int)dataSize - (int)dataRemaining, bytesToRead); 
                                dataRemaining -= bytesToRead;
                            }
                            */
                        }
                    }

                    //if (!isStatic)
                    {
                        reader.NextResult(); // grab item properties
                        bool attribsDone = false;

                        while (!attribsDone)
                        {
                            //reader.NextResult();
                            count++;

                            if (count == 5)
                            {
                                // Finished reading item attributes.  Go on to the next item.
                                attribsDone = true;
                            }

                            if (!reader.HasRows)
                            {
                                reader.NextResult();
                                continue;
                            }

                            IGameObject ci = loadedItem;
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
                                    //reader.NextResult(); // read past the Item id
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
                                            Log1.Logger("Server.Stats").Error("Item_Load attempted to read stat ID [" + s.StatID + "] which was not defined in the Stats.xml config file. Stat not added to Item.");
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

                            reader.NextResult();
                        } // while reader has next result for item properties
                    } // if not static item
                    item = loadedItem;
                } // while reader has rows
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR Item_Load] : " + e.Message);
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

        public static bool Item_Save(this DB db, IGameObject item, string owner, out string msg, out SqlTransaction tran, out SqlConnection con)
        {
            /*
                WHEN NOT MATCHED [BY TARGET] -- row exists in source but not in target
                WHEN NOT MATCHED BY SOURCE -- row exists in target but not in source
            */

            if(((ServerGameObject)item).IsTransient)
            {
                tran = null;
                con = null;
                msg = "Transient objects can't be saved.";
                return false;
            }

            tran = null;

            bool result = true;
            msg = "";
            con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_Save", true);

            SqlParameter pout = new SqlParameter("@resultCode", 0);
            pout.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(pout);

            cmd.Parameters.Add(new SqlParameter("@itemID", item.UID));

            cmd.Parameters.Add(new SqlParameter("@owner", owner));
            cmd.Parameters.Add(new SqlParameter("@context", item.Context));
            cmd.Parameters.Add(new SqlParameter("@typeHash", item.TypeHash));
            cmd.Parameters.Add(new SqlParameter("@typeHash", item.TypeHash));
            cmd.Parameters.Add(new SqlParameter("@stackCount", item.StackCount));
            cmd.Parameters.Add(new SqlParameter("@template", item.ItemTemplate));
            cmd.Parameters.Add(new SqlParameter("@objectOwner", item.Owner));
            cmd.Parameters.Add(new SqlParameter("@typeHash", (long)item.TypeHash));
            cmd.Parameters.Add(new SqlParameter("@createdOn", item.CreatedOn));
            cmd.Parameters.Add(new SqlParameter("@got", (int)item.GameObjectType));  

            if (!((ServerGameObject)item).IsStatic)
            {
                Pointer dataPointer = new Pointer();
                byte[] bindata = new byte[1024];
                item.Serialize(ref bindata, dataPointer);

                // Combine envelope and body into final data gram
                byte[] trimData = new byte[dataPointer.Position];
                Util.Copy(bindata, 0, trimData, trimData.Length, dataPointer.Position);

                cmd.Parameters.Add(new SqlParameter("@binData", trimData));

                SqlParameter ints = new SqlParameter("@intProperties", ItemIntPropertiesToTable(item.Properties.GetAllPropertiesOfKind(PropertyKind.Int32), item.UID));
                ints.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(ints);

                SqlParameter floats = new SqlParameter("@floatProperties", ItemFloatPropertiesToTable(item.Properties.GetAllPropertiesOfKind(PropertyKind.Single), item.UID));
                floats.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(floats);

                SqlParameter longs = new SqlParameter("@longProperties", ItemLongPropertiesToTable(item.Properties.GetAllPropertiesOfKind(PropertyKind.Int64), item.UID));
                longs.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(longs);

                SqlParameter strings = new SqlParameter("@stringProperties", ItemStringPropertiesToTable(item.Properties.GetAllPropertiesOfKind(PropertyKind.String), item.UID));
                strings.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(strings);

                SqlParameter statsParm = new SqlParameter("@stats", ItemStatsToTable(item.Stats.AllStats, item.UID));
                statsParm.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(statsParm);
            }
            try
            {
                con.Open();
                tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Connection = con;
                cmd.Transaction = tran;

                cmd.ExecuteNonQuery();
                long val = (long)cmd.Parameters[0].Value;
                result = val > 0;

                // -1 = unknown error creating Item
                //  0 = unknown error crating Item starting stats
                //  1 = Item created successfully
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
            
                 * Calling method must close connection. this is to allow API users to append to the Item creation method and commit the transaction
                 before closing the connection.*/
            }

            return result;
        }

        private static int ItemBatchUpdateStep(List<ServerGameObject> done, DataTable itemTable, DataTable intTable, DataTable floatTable, DataTable longTable, DataTable stringTable, DataTable statTable, DataTable deleteTable, ServerGameObjectManager gom)
        {
            int processed = 0;
            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_BatchUpdateOrInsert", true, 60);

            SqlParameter pout = new SqlParameter("@resultCode", 0);
            pout.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(pout);

            SqlParameter items = new SqlParameter("@Items", itemTable);
            items.SqlDbType = SqlDbType.Structured;
            cmd.Parameters.Add(items);

            if (intTable != null)
            {
                SqlParameter ints = new SqlParameter("@ItemPropertyInts", intTable);
                ints.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(ints);
            }

            if (floatTable != null)
            {
                SqlParameter floats = new SqlParameter("@ItemPropertyFloats", floatTable);
                floats.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(floats);
            }

            if (longTable != null)
            {
                SqlParameter longs = new SqlParameter("@ItemPropertyLongs", longTable);
                longs.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(longs);
            }

            if (stringTable != null)
            {
                SqlParameter strings = new SqlParameter("@ItemPropertyStrings", stringTable);
                strings.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(strings);
            }
            
            if (statTable != null)
            {
                SqlParameter statsParm = new SqlParameter("@ItemPropertyStats", statTable);
                statsParm.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(statsParm);
            }

            if (deleteTable != null)
            {
                SqlParameter deleteParm = new SqlParameter("@DeleteItems", deleteTable);
                deleteParm.SqlDbType = SqlDbType.Structured;
                cmd.Parameters.Add(deleteParm);
            }

            try
            {
                con.Open();
                cmd.Connection = con;

                cmd.ExecuteNonQuery();
                long val = (long)cmd.Parameters[0].Value;
                bool result = val > 0;
                if (!result)
                {
                    processed = 0;
                }
                else
                {
                    foreach (ServerGameObject go in done)
                    {
                        processed++;
                        if(go.IsDeleted)
                        {
                            gom.RemoveGameObject(go);
                        }                        
                        go.IsGhost = false;
                        go.IsDirty = false;
                        go.IsSaving = false;
                    }
                }

                BatchTimeoutErrs--;
                if(MaxBatchItemCountMod < 0 && BatchTimeoutErrs < 0)
                {
                    MaxBatchItemCountMod += 0.5f;
                }
                // -1 = unknown error creating Item
                //  0 = unknown error crating Item starting stats
                //  1 = Item created successfully
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
                if(e.Message.ToLower().Contains("timeout"))
                {
                    BatchTimeoutErrs+=20;                    
                    MaxBatchItemCountMod -= 0.1f;
                }
                processed = 0;
                foreach (ServerGameObject go in done)
                {
                    go.IsSaving = false;
                }
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
            return processed;
        }

        public static int Item_BatchUpdate(this DB db, List<IGameObject> objects, out string msg, ServerGameObjectManager gom)
        {
            /*
                WHEN NOT MATCHED [BY TARGET] -- row exists in source but not in target
                WHEN NOT MATCHED BY SOURCE -- row exists in target but not in source
            */

            msg = "";
         
            int processed = 0;
            // Gather the data tables
            DataTable intTable = null;
            DataTable floatTable = null;
            DataTable longTable = null;
            DataTable stringTable = null;
            DataTable statTable = null;
            DataTable itemTable = null;
            DataTable deleteTable = null;

            List<ServerGameObject> done = new List<ServerGameObject>();
            int maxCount = GetMaxBatchItemCount();
            for (int i = 0; i < objects.Count; i++)
            {
                ServerGameObject sgo = (ServerGameObject) objects[i];

                // Transient objects only exist in memory and are not persisted in the database
                if(sgo == null || sgo.IsTransient)
                {
                    continue;
                }

                bool added = false;

                // if the object is deleted, add it to the deleted table and add it to the list of objects that have been processed (are @done)
                if (sgo.IsDeleted)
                {
                    // add to deleted table
                    deleteTable = GameObjectToDeleteRow(sgo);
                    done.Add(sgo);
                    added = true; // we added something to the done list in the delete block
                }  

                // if the object has new/changed data (isDirty), or the object is a ghost, i.e. only exists in memory because it has not been written to the db yet and the
                // item is a valid item (ValidateITemCreateRequest), then we add this item's information to the data that will be scheduled to be written to the db
                if (sgo.IsDirty || (sgo.IsGhost && ItemUtil.Instance.ValidateItemCreateRequest(sgo, ref msg))) // never before written to DB
                {
                    // Add object data to the item data table
                    itemTable = GameObjectToTable(sgo, itemTable);

                    // if th item is not static, it might have individual, i.e. instance data like properties and stats.  Therefore, if the item is not static
                    // we write the object's instance data to the tables which will be cached to be written to the DB
                    if (!sgo.IsStatic)
                    {
                        intTable = ItemIntPropertiesToTable(sgo.Properties.GetAllPropertiesOfKind(PropertyKind.Int32), sgo.UID, intTable);
                        floatTable = ItemFloatPropertiesToTable(sgo.Properties.GetAllPropertiesOfKind(PropertyKind.Single), sgo.UID, floatTable);
                        longTable = ItemLongPropertiesToTable(sgo.Properties.GetAllPropertiesOfKind(PropertyKind.Int64), sgo.UID, longTable);
                        stringTable = ItemStringPropertiesToTable(sgo.Properties.GetAllPropertiesOfKind(PropertyKind.String), sgo.UID, stringTable);
                        statTable = ItemStatsToTable(sgo.Stats.AllStats, sgo.UID, statTable);
                        sgo.IsSaving = true;
                    }

                    // if the object has not been previously added to the "@done", i.e. processed, item list, add it now.
                    if (!added)
                    {
                        done.Add(sgo);
                    }
                }

                // If we've processed the maximum number of objects that we are configured to process in one pass, or if we are the end of the list of objects to be processed,
                // then we issue a command to the DB to write all of the item's data that we have thus far cached.
                if(done.Count >= maxCount || i >= objects.Count-1)
                {
                    // do we have anything to process?  Could have passed in an object list with zero objects.
                    if (done.Count > 0)
                    {
                        // Issue the DB persist command
                        processed += ItemBatchUpdateStep(done, itemTable, intTable, floatTable, longTable, stringTable, statTable, deleteTable, gom);
                    }

                    // Clear out the item table caches and the "@done", i.e. processed items list.
                    itemTable = intTable = floatTable = longTable = stringTable = statTable = null;
                    done.Clear();

                    maxCount = GetMaxBatchItemCount();
                }

                if(sgo.IsZombie & gom != null)
                {
                    gom.RemoveGameObject(sgo);
                }  
            }

            return processed;
        }

        private static int GetMaxBatchItemCount()
        {
            int baseC = ConfigHelper.GetIntConfig("MAX_BATCH_ITEM_SAVE_COUNT", 500);
            if(MaxBatchItemCountMod < -0.95f)
            {
                MaxBatchItemCountMod = -0.95f;
            }

            int modCount = (int)Math.Ceiling( (float)baseC + (float)baseC * MaxBatchItemCountMod);
            if(modCount < 1)
            {
                modCount = 1;
            }

            return modCount;
        }

        private static float MaxBatchItemCountMod = 0;
        private static int BatchTimeoutErrs = 0;

        public static StatBag GetStaticStats(this DB db)
        {
            StatBag rslt = new StatBag();

            SqlConnection con = new SqlConnection(DB.GameDataConnectionString);
            SqlCommand cmd = DB.GetCommand(con, "GetStaticStats", true);

            try
            {
                con.Open();
                SqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Stat s = (Stat)Enum.Parse(typeof(Stat), reader["StatName"] as string);
                    Stat stat = StatManager.Instance[s.StatID];
                    rslt.AddStat(new Stat(stat.StatID, stat.DisplayName, stat.Description, reader["StatGroup"] as string, (int)reader["StatValue"], stat.MinValue, stat.MaxValue));
                }
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

            return rslt;
        }

        public static bool Item_UpdateStringProperty(this DB db, int ItemId, int propertyId, string propertyName, string newValue)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_UpdateOrInsertStringProperties", true);
            cmd.Parameters.Add(new SqlParameter("@charID", ItemId));

            StringProperty prop = new StringProperty(propertyName, propertyId, newValue, null);
            List<StringProperty> props = new List<StringProperty>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", DB.Instance.StringPropertiesToTable(props)));

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

        public static bool Item_UpdateFloatProperty(this DB db, Guid ItemId, int propertyId, string propertyName, float newValue)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_UpdateOrInsertFloatProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemId", ItemId));

            SingleProperty prop = new SingleProperty(propertyName, propertyId, newValue, null);
            List<SingleProperty> props = new List<SingleProperty>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemFloatPropertiesToTable(props, ItemId)));

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

        public static bool Item_UpdateLongProperty(this DB db, Guid ItemId, int propertyId, string propertyName, long newValue)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_UpdateOrInsertLongProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemId", ItemId));

            Int64Property prop = new Int64Property(propertyName, propertyId, newValue, null);
            List<Int64Property> props = new List<Int64Property>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemLongPropertiesToTable(props, ItemId)));

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

        public static bool Item_UpdateIntProperty(this DB db, Guid ItemId, int propertyId, string propertyName, int newValue)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_UpdateOrInsertIntProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            Int32Property prop = new Int32Property(propertyName, propertyId, newValue, null);
            List<Int32Property> props = new List<Int32Property>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemIntPropertiesToTable(props, ItemId)));

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

        public static bool Item_UpdateStat(this DB db, Guid ItemId, int statId, float minValue, float maxValue, float curValue)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_UpdateOrInsertStats", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            Stat stat = new Stat(statId, "", "", "", curValue, minValue, maxValue);
            List<Stat> stats = new List<Stat>();
            stats.Add(stat);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemStatsToTable(stats, ItemId)));

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

        public static bool Item_DeleteStat(this DB db, Guid ItemId, int statId)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_DeleteStats", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            Stat stat = new Stat(statId, "", "", "", 0, 0, 0);
            List<Stat> stats = new List<Stat>();
            stats.Add(stat);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemStatsToTable(stats, ItemId)));

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

        public static bool Item_DeleteFloatProperty(this DB db, Guid ItemId, int propertyDbId)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_DeleteFloatProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            SingleProperty prop = new SingleProperty("", propertyDbId, 0, null);
            List<SingleProperty> props = new List<SingleProperty>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemFloatPropertiesToTable(props, ItemId)));

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

        public static bool Item_DeleteStringProperty(this DB db, Guid ItemId, int propertyDbId)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_DeleteStringProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            StringProperty prop = new StringProperty("", propertyDbId, "", null);
            List<StringProperty> props = new List<StringProperty>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemStringPropertiesToTable(props, ItemId)));

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

        public static bool Item_DeleteLongProperty(this DB db, int ItemId, int propertyDbId)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Items_DeleteLongProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            Int64Property prop = new Int64Property("", propertyDbId, 0, null);
            List<Int64Property> props = new List<Int64Property>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemLongPropertiesToTable(props, Guid.Empty)));

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

        public static bool Item_DeleteIntProperty(this DB db, int ItemId, int propertyDbId)
        {
            bool rslt = true;

            SqlConnection con = DB.GameDataConnection;
            SqlCommand cmd = DB.GetCommand(con, "Item_DeleteIntProperties", true);
            cmd.Parameters.Add(new SqlParameter("@itemID", ItemId));

            Int32Property prop = new Int32Property("", propertyDbId, 0, null);
            List<Int32Property> props = new List<Int32Property>();
            props.Add(prop);
            cmd.Parameters.Add(new SqlParameter("@InputTable", ItemIntPropertiesToTable(props, Guid.Empty)));

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


    }
}
