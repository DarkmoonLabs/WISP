using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Threading;

namespace Shared
{
    public class CharacterUtil
    {

        private static CharacterUtil m_Instance;

        public static CharacterUtil Instance
        {
            get 
            {
                if (m_Instance == null)
                {
                    m_Instance = new CharacterUtil();
                }
                return m_Instance; 
            }
            set { m_Instance = value; }
        }
        
        /// <summary>
        /// Template to what a character looks like
        /// </summary>
        private PropertyBag m_CharacterTemplateProperties = new PropertyBag();
        private StatBag m_CharacterTemplateStats = new StatBag();

        // Property IDs belonging to each type category in the character teamplate.  Used for querying the DB about which properties we want to hear about
        private LinkedList<Property> m_CharacterTemplateInts = new LinkedList<Property>();
        private LinkedList<Property> m_CharacterTemplateFloats = new LinkedList<Property>();
        private LinkedList<Property> m_CharacterTemplateLongs = new LinkedList<Property>();
        private LinkedList<Property> m_CharacterTemplateStrings = new LinkedList<Property>();

        /// <summary>
        /// The minimum length that a character name must be.
        /// </summary>
        public int CharacterMinNameLength { get; set; }

        /// <summary>
        /// The path to the character template file
        /// </summary>
        public string CharacterTemplateFile { get; set; }

        /// <summary>
        /// Reads the character XML file into m_CharacterTemplate
        /// </summary>
        public void LoadCharacterTemplate()
        {
            bool result = XMLHelper.Character_GetPropertyTypesFromTemplate(CharacterUtil.Instance.CharacterTemplateFile, ref m_CharacterTemplateProperties, ref m_CharacterTemplateStats);
            if (!result)
            {
                throw new FormatException(CharacterTemplateFile + " couldn't be read.  Server can't run without this. Correct the file and restart server.");
            }
            
            m_CharacterTemplateStrings = m_CharacterTemplateProperties.GetAllPropertiesOfKind(PropertyKind.String);
            m_CharacterTemplateFloats = m_CharacterTemplateProperties.GetAllPropertiesOfKind(PropertyKind.Single);
            m_CharacterTemplateInts = m_CharacterTemplateProperties.GetAllPropertiesOfKind(PropertyKind.Int32);
            m_CharacterTemplateLongs = m_CharacterTemplateProperties.GetAllPropertiesOfKind(PropertyKind.Int64);
        }

        /// <summary>
        /// Gets called as the character is being created in the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the creation to be committed or not.
        /// </summary>
        public event Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool> CharacterPersisting;

        /// <summary>
        /// Gets called as the character is being deleted from the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the deletion to be committed or not.
        /// </summary>
        public event Func<SqlConnection, SqlTransaction, int, ServerUser, bool> CharacterDeleting;

        /// <summary>
        /// Gets called as the character is being loaded from the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the load (and any related writes that you might have added) to be committed or not.
        /// </summary>
        public event Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool> CharacterLoading;

        /// <summary>
        /// Gets called as the character is being saved/updated to the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the load (and any related writes that you might have added) to be committed or not.
        /// </summary>
        public event Func<SqlConnection, SqlTransaction, ServerCharacterInfo, bool> CharacterSaving;

        /// <summary>
        /// Called when the ServerCharacterInfo object is being created.  Catch to override the concrete type created.
        /// </summary>
        public event Func<CharacterInfo, ServerUser, ServerCharacterInfo> CharacterObjectCreate;

       /// <summary>
       /// Gets called as the character is being created in the Database.  If you have additional DB tasks
       /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
       /// be open.
       /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
       /// you want the creation to be committed or not.
       /// </summary>
       /// <param name="con">the connection object to use for additional database work in relation to character creation</param>
        /// <param name="tran">the transaction object to use for additional database work in relation to character creation</param>
       /// <returns>return false if you do not want the transaction to be committed, which will also cause character creation to fail</returns>
        protected virtual bool OnCharacterPersiting(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci)
        {
            if (CharacterPersisting != null)
            {
                return CharacterPersisting(con, tran, ci);
            }
            return true;
        }

        /// <summary>
        /// Gets called as the character is being deleted from the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the deletion to be committed or not.
        /// </summary>
        /// <param name="con">the connection object to use for additional database work in relation to character deletion</param>
        /// <param name="tran">the transaction object to use for additional database work in relation to character deletion</param>
        /// <returns>return false if you do not want the transaction to be committed, which will also cause character deletion to fail</returns>
        protected virtual bool OnCharacterDeleting(SqlConnection con, SqlTransaction tran, int chracterId, ServerUser owner)
        {
            if (CharacterDeleting != null)
            {
                return CharacterDeleting(con, tran, chracterId, owner);
            }
            return true;
        }

        /// <summary>
        /// Creates a new character in the DB using the character template XML file as a basis for stats and properties.  If you want to override any of the default
        /// properties, pass in the appropriate characterProperties property bag.
        /// </summary>
        /// <param name="msg">an error message, if any</param>
        /// <returns></returns>
        public bool PersistNewCharacter(ServerCharacterInfo ci, ServerUser owner, ref string msg, bool isTempCharacter)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;

            try
            {
                if (ValidateCharacterCreateRequest(ci, ref msg))
                {
                    int maxCharacters = (int)owner.Profile.MaxCharacters;
                    if (maxCharacters < 1) maxCharacters = 1;
                    int newCharId = 0;
                    if (DB.Instance.Character_Create(owner.ID, ci.Stats, ci.Properties, (int)PropertyID.Name, ci.CharacterName, true, maxCharacters, isTempCharacter, out msg, out tran, out con, out newCharId))
                    {
                        ci.CharacterInfo.ID = newCharId;
                        if (OnCharacterPersiting(con, tran, ci))
                        {
                            tran.Commit();
                        }
                        else
                        {
                            tran.Rollback();
                        }
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
        /// creates a character object, but does not persist it.
        /// </summary>
        /// <param name="properties">character properties to add</param>
        /// <param name="owner">the owning user</param>
        /// <returns></returns>
        public ServerCharacterInfo CreateNewCharacter(PropertyBag properties, ServerUser owner)
        {
            CharacterInfo shell = CreateNewCharacterShell();
            if (properties != null)
            {
                shell.Properties.UpdateWithValues(properties);
            }

            ServerCharacterInfo ci = new ServerCharacterInfo(shell);
            ci.ID = NextCharId;
            ci.Properties = shell.Properties;
            ci.Stats = shell.Stats;
            ci.OwningAccount = owner;

            return ci;
        }

        /// <summary>
        /// Gets called as the character is being created in the Database.  If you have additional DB tasks
        /// to add, now is the time.  Use the supplied connection and transaction objects.  The connection should already
        /// be open.
        /// Do NOT close the connection and do NOT commit /rollback the transaction.  Simply return true or false if
        /// you want the creation to be committed or not.
        /// </summary>
        /// <param name="characterId">the id of the character to delete</param>
        /// <param name="owner">the owner of the character to delete</param>
        /// <param name="permaPurge">should the data be permanently purged</param>
        /// <param name="reason">reason for service log</param>
        /// <param name="rsltMsg">a message saying why deletion failed, if any</param>
        /// <param name="con">the connection object to use for additional database work in relation to character creation</param>
        /// <param name="tran">the transaction object to use for additional database work in relation to character creation</param>
        /// <returns>return false if you do not want the transaction to be committed, which will also cause character creation to fail</returns>
        public bool DeleteCharacter(int characterId, ServerUser owner, bool permaPurge, string reason, ref string rsltMsg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;

            try
            {
                if (DB.Instance.Character_Delete(owner.ID, characterId, permaPurge, reason, out tran, out con))
                {
                    if (OnCharacterDeleting(con, tran, characterId, owner))
                    {
                        tran.Commit();
                    }
                    else
                    {
                        tran.Rollback();
                        rsltMsg = "Unknown failure deleting character.";
                        return false;

                    }
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

        private static int m_CurFakeCharId = int.MinValue;
        private static int NextCharId
        {
            get
            {
                return Interlocked.Increment(ref m_CurFakeCharId);
            }
        }

        /// <summary>
        /// For clusters that don't use explicit characters, default characters are used for database tracking purposes.
        /// Call this method to get a reference to that default character.  Method will return null if App.Config's UseCharacters 
        /// setting is TRUE
        /// </summary>
        /// <param name="owner"></param>
        /// <returns></returns>
        public ServerCharacterInfo GetOrCreateDefaultCharacter(bool isTempCharacter, ServerUser owner)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;

            try
            {
                // This cluster doesn't use characters.  Create a default one for system purposes if we don't have one already
                ServerCharacterInfo ci = new ServerCharacterInfo();
                if (!DB.Instance.Character_LoadAny(owner.ID, ci)) // see if we can fetch the current default character from the DB.
                {
                    // Nope, couldn't find it. Try creating one.
                    ci = new ServerCharacterInfo(CreateNewCharacterShell(), owner);
                    ci.Properties.SetProperty((int)PropertyID.Name, "Mc_" + CryptoManager.GetSHA256Hash(owner.AccountName));
                    
                    string msgCreate = "";
                    int newCharId = NextCharId;

                    if (DB.Instance.Character_Create(owner.ID, ci.Stats, ci.Properties, (int)PropertyID.Name, ci.CharacterName, true, 1, isTempCharacter, out msgCreate, out tran, out con, out newCharId))
                    {
                        ci.CharacterInfo.ID = newCharId;
                        if (tran != null)
                        {
                            tran.Commit();
                        }
                        return ci;
                    }
                    else
                    {
                        if (tran != null)
                        {
                            tran.Rollback();
                        }
                    }
                }
                else // got the default character
                {
                    return ci;
                }
            }
            catch (Exception e)
            { }
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

            return null;
        }


        protected ServerCharacterInfo OnCharacterObjectCreate(CharacterInfo ci, ServerUser owner)
        {
            if (CharacterObjectCreate != null)
            {
                return CharacterObjectCreate(ci, owner);
            }
            return new ServerCharacterInfo(CreateNewCharacterShell(), owner);
        }

        protected bool OnCharacterLoading(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci, ref string msg)
        {
            if (CharacterLoading != null)
            {
                return CharacterLoading(con, tran, ci);
            }
            return true;
        }

        protected bool OnCharacterSaving(SqlConnection con, SqlTransaction tran, ServerCharacterInfo ci, ref string msg)
        {
            if (CharacterSaving != null)
            {
                return CharacterSaving(con, tran, ci);
            }
            return true;
        }

        /// <summary>
        /// Saves/updates the character to the DB
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the character to get</param>
        /// <param name="enforceUniqueName">You can change a toon's name in the DB. To ensure unique names, set to true. IMPORTANT!!!: If you are saving
        /// , i.e. updating an EXISTING toon without changing his name, set enforceUniqueName to FALSE - otherwise the update will fail since that 
        /// toon's name already exists in the DB, the update will fail.</param>
        /// <returns></returns>
        public bool SaveCharacter(ServerUser owner, ServerCharacterInfo toon, bool enforceUniqueName, ref string rsultMsg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;
            try
            {
                Guid cown = Guid.Empty;
                if (DB.Instance.Character_Save(owner.ID, toon, (int)PropertyID.Name, toon.CharacterName, enforceUniqueName, out rsultMsg, out tran, out con))
                {
                    if (!OnCharacterSaving(con, tran, toon, ref rsultMsg))
                    {
                        tran.Rollback();
                    }
                    else
                    {
                        tran.Commit();
                    }
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
                    rsultMsg = "Can't save character. " + rsultMsg;
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
        /// Gets the character from the DB
        /// </summary>
        /// <param name="owner">owning account</param>
        /// <param name="id">the id for the character to get</param>
        /// <returns></returns>
        public ServerCharacterInfo LoadCharacter(ServerUser owner, int id, ref string rsultMsg)
        {
            SqlConnection con = null;
            SqlTransaction tran = null;
            ServerCharacterInfo ci = OnCharacterObjectCreate(CreateNewCharacterShell(), owner);

            try
            {
                Guid cown = Guid.Empty;
                if (DB.Instance.Character_Load(owner.ID, ci, id, ref cown, out tran, out con))
                {
                    if (!OnCharacterLoading(con, tran, ci, ref rsultMsg))
                    {
                        tran.Rollback();
                    }
                    else
                    {
                        tran.Commit();
                    }
                }
                else
                {
                    rsultMsg = "Character doesn't exist.";
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
            return ci;
        }

        /// <summary>
        /// Creates a default character object with stats and properties based on the XML character template file values.
        /// </summary>
        /// <returns></returns>
        public CharacterInfo CreateNewCharacterShell()
        {
            CharacterInfo toon = new CharacterInfo();
            toon.Stats.UpdateWithValues(m_CharacterTemplateStats);
            toon.Properties.UpdateWithValues(m_CharacterTemplateProperties);
            return toon;
        }

        /// <summary>
        /// Checks all characte properties against the Character template xml
        /// </summary>
        /// <param name="character">The character that will be created.</param>
        /// <param name="msg">anything you want the player to know</param>
        /// <returns></returns>
        private bool ValidateCharacterCreateRequest(ICharacterInfo character, ref string msg)
        {
            // check the properties and stats against the character template. don't allow the client to submit stats or properties that aren't in the template
            Stat[] stats = character.Stats.AllStats;
            for (int i = 0; i < stats.Length; i++)
            {
                if (m_CharacterTemplateStats.GetStat(stats[i].StatID) == null)
                {
                    msg = "Server does not allow characters to be created with Stats of StatID " + stats[i].StatID.ToString();
                    return false;
                }
            }

            Property[] props = character.Properties.AllProperties;
            for (int i = 0; i < props.Length; i++)
            {
                if (m_CharacterTemplateProperties.GetProperty(props[i].PropertyId) == null)
                {
                    msg = "Server does not allow characters to be created with Properties of PropertyId" + props[i].PropertyId.ToString();
                    return false;
                }
            }

            if (!CharacterValidateName(character.CharacterName, ref msg))
            {
                // send error if we can't validate the name
                return false;
            }

            return true;
        }

        /// <summary>
        /// Validate the character name against CharacterMinNameLength
        /// </summary>
        /// <param name="name">the name to check</param>
        /// <param name="msg">failure message, if any</param>
        /// <returns></returns>
        private bool CharacterValidateName(string name, ref string msg)
        {
            if (name.Length < CharacterMinNameLength)
            {
                msg = string.Format("Name must be at least {0} characters long.", CharacterMinNameLength);
                return false;
            }

            return true;
        }

        public List<ICharacterInfo> GetCharacterListing(Guid user)
        {
            Dictionary<int, ICharacterInfo> toons = new Dictionary<int, ICharacterInfo>();
            bool gotListing = DB.Instance.Character_GetAll(user, m_CharacterTemplateInts, m_CharacterTemplateLongs, m_CharacterTemplateFloats, m_CharacterTemplateStrings, toons);
            List<ICharacterInfo> toonsList = new List<ICharacterInfo>(toons.Values);
            
            if (gotListing)
            {
                return toonsList;
            }
            else
            {
                return new List<ICharacterInfo>();
            }
        }


    }
}
