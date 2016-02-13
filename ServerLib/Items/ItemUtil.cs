using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using GameLib;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Shared
{
    public class ItemUtil
    {
        private static ItemUtil m_Instance;

        public static ItemUtil Instance
        {
            get
            {
                if (m_Instance == null)
                {
                    m_Instance = new ItemUtil();
                }
                return m_Instance;
            }
            set { m_Instance = value; }
        }

        /// <summary>
        /// Creates a new Item in the DB using the Item's template XML file as a basis for stats and properties.  If you want to override any of the default
        /// properties, pass in the appropriate ItemProperties property bag.
        /// </summary>
        /// <param name="msg">an error message, if any</param>
        /// <returns></returns>
        public bool PersistNewItem(ServerGameObject ci, string owner, ref string msg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;

            if (ci != null && ci.IsTransient)
            {
                msg = "Transient objects can't be saved.";
                return false;
            }

            try
            {
                if (ValidateItemCreateRequest(ci, ref msg))
                {
                    if (DB.Instance.Item_Create(ci, owner, out msg, out tran, out con))
                    {
                        tran.Commit();
                        return true;
                    }
                    else
                    {
                        if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                        {
                            if (tran != null)
                            {
                                tran.Rollback();
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                {
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                }
                return false;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }

                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates the item, based on the template, but doesn't register it with the object manager, nor does it persist it
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public ServerGameObject CreateItemShell(string template)
        {
            Template t = null;
            if (!Template.Templates.TryGetValue(template.ToLower(), out t))
            {
                return null;
            }

            uint hash = Factory.GetStableHash(t.Class);
            ServerGameObject sgo = Factory.Instance.CreateObject(hash) as ServerGameObject;
            if (sgo == null)
            {
                Log1.Logger("Server").Error("Tried to instantiate game object of type " + t.Class + ", but the Factory could not find that type.  Was it registered?");
                return null;
            }

            foreach (uint script in t.Scripts)
            {
                GameObjectScript gos = GameObjectScript.GetScript(script);
                if(gos == null)
                {
                    Log1.Logger("ObjectError").Error("Unable to instantiate GameObjectScript [" + script + "] for template [" + t.Name + "]. On game object [" + sgo.UID.ToString() + "].");
                    continue;
                }

                sgo.Scripts.AttachScript(gos);
            }

            sgo.Properties.UpdateWithValues(t.Properties);
            sgo.Stats.UpdateWithValues(t.Stats);
            sgo.ItemTemplate = t.Name;
            sgo.GameObjectType = t.GameObjectType;
            sgo.IsStatic = t.IsStatic;
            sgo.StackCount = t.StackCount;
            sgo.IsTransient = t.IsTransient;
            
            return sgo;
        }

        /// <summary>
        /// creates a Item object, but does not persist it.
        /// </summary>
        /// <param name="properties">Item properties to add</param>
        /// <param name="owner">the owning user</param>
        /// <param name="context">Any user defined context - could be a game room, an owning server, etc.  GameObjectManager keeps sub-lists of games based on context ID. Use GUID.Empty for none. </param>
        /// <returns></returns>
        public ServerGameObject CreateNewItem(string itemTemplate, Guid context, ServerGameObjectManager gom, string owningServer)
        {
            ServerGameObject sgo = CreateItemShell(itemTemplate);
            if (sgo == null) return null;

            sgo.IsGhost = true;
            // Instantiate the inner GameObject
            sgo.Context = context;
            // Add the needed bits
            sgo.UID = Guid.NewGuid();
            sgo.CreatedOn = DateTime.UtcNow;
            sgo.OwningServer = owningServer;
            gom.RegisterGameObject(sgo, context);
            return sgo;
        }

        /// <summary>
        /// Gets called as the Item is being created in the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the creation to be committed or not.
        /// </summary>
        /// <param name="ItemId">the id of the Item to delete</param>
        /// <param name="owner">the owner of the Item to delete</param>
        /// <param name="permaPurge">should the data be permanently purged</param>
        /// <param name="reason">reason for service log</param>
        /// <param name="rsltMsg">a message saying why deletion failed, if any</param>
        /// <param name="con">the connection object to use for additional database work in relation to Item creation</param>
        /// <param name="tran">the transaction object to use for additional database work in relation to Item creation</param>
        /// <returns>return false if you do not want the transaction to be committed, which will also cause Item creation to fail</returns>
       /*
        public bool DeleteItem(Guid ItemId, ServerUser player, bool permaPurge, string reason, ref string rsltMsg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;

            try
            {
                if (DB.Instance.Item_Delete(player.ID, ItemId, permaPurge, reason, out tran, out con))
                {
                    tran.Commit();
                    return true;
                }
                else
                {
                    if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                    {
                        if (tran != null)
                        {
                            tran.Rollback();
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                {
                    if (tran != null)
                    {
                        tran.Rollback();
                    }
                }
                return false;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }

                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }

            return false;
        }
*/

        /// <summary>
        /// Saves/updates the Item to the DB
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the Item to get</param>
        /// <returns></returns>
        public bool SaveItem(ServerGameObject item, string owner, ref string rsultMsg)
        {
            if(item == null)
            { 
                return false; 
            }

            SqlConnection con = null;
            SqlTransaction tran = null;
            if(item.IsTransient)
            {
                rsultMsg = "Transient objects can't be saved.";
                return false;
            }
            try
            {
                Guid cown = Guid.Empty;
                if (DB.Instance.Item_Save(item, owner, out rsultMsg, out tran, out con))
                {
                    tran.Commit();
                }
                else
                {
                    if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                    {
                        if (tran != null)
                        {
                            tran.Rollback();
                        }
                    }
                    rsultMsg = "Can't save Item. " + rsultMsg;
                    return false;
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }
            return true;
        }

        /// <summary>
        /// Gets a reference to a server game object currently in memory, i.e. previously loaded from the DB.
        /// </summary>
        /// <param name="id">id of the object being returned</param>
        /// <returns></returns>
        public ServerGameObject GetItem(Guid id, ServerGameObjectManager gom, bool includeDeleted = false)
        {
            ServerGameObject sgo = gom.GetGameObjectFromId(id) as ServerGameObject;
            if (sgo == null || (!includeDeleted && sgo.IsDeleted))
            {
                return null;
            }

            return sgo;
        }

        /// <summary>
        /// Delete an object from the world.
        /// </summary>
        /// <param name="id"></param>
        public void DeleteItem(Guid id, Guid accountDeleting, string reason, ServerGameObjectManager gom)
        {
            ServerGameObject sgo = null;

            if (gom != null)
            {
                GetItem(id, gom);
            }
            else
            {
                string msg = "";
                sgo = LoadItem(id, true, "", ref msg, null);
            }
            
            if (sgo != null)
            {
                sgo.IsDeleted = true;
                sgo.DeleteReason = reason;
                sgo.AccountDeleted = accountDeleting;
            }
        }
        
        /*
        /// <summary>
        /// Saves all currently loaded objects that are dirty to the database
        /// </summary>
        public void RunSaveCycle(string serverID, GameObjectManager gom)
        {
            if (1 == Interlocked.Exchange(ref m_UpdatingSaveQueue, 1))
            {
                // Failed to get the lock.  Process is already/still running.
                return;
            }

            DateTime start = DateTime.UtcNow;
            IGameObject[] items = gom.AllObjects;
            int processed = 0;
         
            for(int i = 0; i < items.Length; i++)
            {
                string msg = "";
                ServerGameObject go = items[i] as ServerGameObject;
                if (go == null) continue;

                if (go.IsGhost) // never before written to DB
                {
                    go.IsSaving = true;
                    if (PersistNewItem(go, serverID, ref msg))
                    {
                        processed++;
                        go.IsSaving = false;
                        go.IsGhost = false;
                        go.IsDirty = false;
                    }
                }
                else if(go.IsDirty)
                {
                    go.IsSaving = true;
                    if (SaveItem(go, serverID, ref msg))
                    {
                        processed++;
                        go.IsSaving = false;
                        go.IsDirty = false;
                    }
                }
              
                if(go.IsDeleted)
                {
                    PresistDeleteItem(go.UID, go.AccountDeleted, false, go.DeleteReason, ref msg);
                    gom.RemoveGameObject(go);
                    processed++;
                }               
            }
            
            DateTime fin = DateTime.UtcNow;
            TimeSpan len = fin - start;
            Log1.Logger("Server").Info("Game object save cycle wrote  [" + processed.ToString() + " objects in " + len.ToString() +"]");
            Log1.Logger("Server").Info("Server is now tracking [" + gom.ObjectCount.ToString() + " game objects].");

            // Release the lock
            StartSaveCycle(gom, serverID);
            Interlocked.Exchange(ref m_UpdatingSaveQueue, 0);
        }
         */

        /// <summary>
        /// Deleted the item from the DB
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the Item to get</param>
        /// <returns></returns>
        public bool PersistDeleteItem(Guid item, Guid accountDoingDeleting, bool permaPurge, string serviceLogReason, ref string rsultMsg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;
            try
            {
                Guid cown = Guid.Empty;
                if (DB.Instance.Item_Delete(accountDoingDeleting, item, permaPurge, serviceLogReason, out tran, out con))
                {
                    tran.Commit();
                }
                else
                {
                    if (con.State != System.Data.ConnectionState.Closed && con.State != System.Data.ConnectionState.Connecting)
                    {
                        if (tran != null)
                        {
                            tran.Rollback();
                        }
                    }
                    rsultMsg = "Can't delete Item. " + rsultMsg;
                    return false;
                }
            }
            catch (Exception e)
            {
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }
            return true;
        }
       
        /// <summary>
        /// Loads an Item from the DB 
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="FAST_FORWARD">the id of the context for the Items to get</param>
        /// <returns></returns>
        public List<IGameObject> LoadItems(Guid context, bool includeDeleted, string lockedByServerId, ref string rsultMsg, ServerGameObjectManager gom)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;
            List<IGameObject> items = new List<IGameObject>();
            try
            {
                Guid cown = Guid.Empty;
                items = DB.Instance.Item_LoadBatchForContext(context, includeDeleted, lockedByServerId, out tran, out con);
                if (items.Count > 0)
                {
                    foreach (IGameObject sgo in items)
                    {
                        ((ServerGameObject)sgo).IsDirty = false;
                        ((ServerGameObject)sgo).IsGhost = false;
                        if (!((ServerGameObject)sgo).IsDeleted && lockedByServerId != null && lockedByServerId.Length > 0)
                        {
                            gom.RegisterGameObject(sgo, sgo.Context);
                        }
                    }
                    tran.Commit();
                }
                else
                {
                    rsultMsg = "Item doesn't exist.";
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }
            return items;
        }

        /// <summary>
        /// Loads an Item from the DB 
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the Item to get</param>
        /// <returns></returns>
        public List<IGameObject> LoadItems(List<Guid> ids, bool includeDeleted, string lockedByServerId, ref string rsultMsg, ServerGameObjectManager gom)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;
            List<IGameObject> items = new List<IGameObject>();
            try
            {
                Guid cown = Guid.Empty;
                items = DB.Instance.Item_LoadBatch(ids, includeDeleted, lockedByServerId, out tran, out con);
                if (items.Count > 0)
                {
                    foreach (IGameObject sgo in items)
                    {
                        ((ServerGameObject)sgo).IsDirty = false;
                        ((ServerGameObject)sgo).IsGhost = false;
                        if (!((ServerGameObject)sgo).IsDeleted && lockedByServerId != null && lockedByServerId.Length > 0)
                        {
                            gom.RegisterGameObject(sgo, sgo.Context);
                        }                            
                    }
                    tran.Commit();
                }
                else
                {
                    rsultMsg = "Item doesn't exist.";
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }
            return items;
        }

        /// <summary>
        /// Loads an Item from the DB 
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the Item to get</param>
        /// <returns></returns>
        public ServerGameObject LoadItem(Guid id, bool includeDeleted, string lockedByServerId, ref string rsultMsg, ServerGameObjectManager gom)
        {
            if(id == Guid.Empty)
            {
                return null;
            }

            

            SqlConnection con = null;
            SqlTransaction tran = null;
            ServerGameObject sgo = null;
            if(gom != null)
            {
                sgo = gom.GetGameObjectFromId(id) as ServerGameObject;
                if(sgo != null)
                {
                    return sgo;
                }
            }


            try
            {
                Guid cown = Guid.Empty;
                if (DB.Instance.Item_Load(out sgo, id, includeDeleted, lockedByServerId, out tran, out con))
                {
                   
                    ((ServerGameObject)sgo).IsDirty = false;
                    ((ServerGameObject)sgo).IsGhost = false;

                    tran.Commit();

                    bool isDeleted = false;
                    isDeleted = ((ServerGameObject)sgo).IsDeleted;

                    if(!isDeleted && lockedByServerId != null && lockedByServerId.Length > 0)
                    {
                        if (gom != null)
                        {
                            gom.RegisterGameObject(sgo, sgo.Context);
                        }
                    }
                }
                else
                {
                    rsultMsg = "Item doesn't exist.";
                    return null;
                }
            }
            catch (Exception e)
            {
                return null;
            }
            finally
            {
                if (con != null)
                {
                    con.Close();
                    con.Dispose();
                    con = null;
                }
                if (tran != null)
                {
                    tran.Dispose();
                    tran = null;
                }
            }

            return sgo;
        }

        /// <summary>
        /// Checks all characte properties against the Item template xml
        /// </summary>
        /// <param name="Item">The Item that will be created.</param>
        /// <param name="msg">anything you want the player to know</param>
        /// <returns></returns>
        public bool ValidateItemCreateRequest(IGameObject Item, ref string msg)
        {
            return true;
        }

        

    }
}
