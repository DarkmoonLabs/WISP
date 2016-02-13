using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using System.Data.SqlClient;
using System.Data;
using System.Configuration;

namespace Shared
{
    public static class LobbyDBExtensions
    {
        static LobbyDBExtensions()        
        {
            m_LobbyDataConnectionString = ConfigurationManager.ConnectionStrings["LobbyDataConnectionString"].ConnectionString;  
        }

        public delegate bool OnTrackGameDelegate(string serverId, IGame gs, ServerUser createdBy, SqlConnection con, SqlTransaction tran);
        public delegate bool OnUntrackGameDelegate(Guid game, SqlConnection con, SqlTransaction tran);
        public delegate bool OnUntrackGamesForServerDelegate(string serverId, SqlConnection con, SqlTransaction tran);
        public delegate void OnGameSearchDelegate(int page, int numPerPage, bool includeInProgressGames, int maxPlayersAllowed, ref string sql, PropertyBag additionalParamters);
        public delegate bool OnGameUpdateDelegate(IGame gs, SqlConnection con, SqlTransaction tran);

        /// <summary>
        /// Fires just before a game listing gets updated in the DB.  Use this event to add additional data int the DB if you want.  
        /// </summary>
        public static event OnGameUpdateDelegate OnGameUpdate;

        /// <summary>
        /// Fires just before a game gets tracked in the DB.  Use this event to add additional data in the DB if you want.  Return false to prevent the game from being tracked in the DB.
        /// Returning false from this method does not prevent the game from being created - just tracked in the DB.
        /// </summary>
        public static event OnTrackGameDelegate OnTrackGame;

        /// <summary>
        /// Fires just before a game gets untracked in the DB.  Use this event to remove any additional data from the DB if you previously added it during the OnTrackGame event.  
        /// Return false to prevent the game from being untracked in the DB.
        /// Returning false from this method does not prevent the game from being destroyed - just being untracked in the DB.
        /// </summary>
        public static event OnUntrackGameDelegate OnUntrackGame;

        /// <summary>
        /// Fires just before all games assigned to the given server get untracked in the DB.  
        /// Use this event to remove any additional data from the DB if you previously added it during the OnTrackGame event.  
        /// Return false to prevent the games from being untracked in the DB.
        /// Returning false from this method does not prevent the games from being destroyed - just being untracked in the DB.
        /// This method is most often only called when a server goes down, so we don't want games for that server showing up in searches.
        /// </summary>
        public static event OnUntrackGamesForServerDelegate OnUntrackGamesForServer;

        /// <summary>
        /// Fires when a search for games is being submitted.  Modify the SQL to change the query to include any search terms
        /// you might want to add, given any additional data you might have stored previously during the OnTrackGame event.
        /// </summary>
        public static event OnGameSearchDelegate OnGameSearch;        

        /// <summary>
        /// Fires when ALL game tracking information is cleared from the DB.  Use this event to delete any additional game tracking data you might have added during
        /// the OnTrackGame event.
        /// </summary>
        public static event EventHandler OnClearGameMap;

        private static string m_LobbyDataConnectionString = "";

        /// <summary>
        /// Returns the primary connection string for the lobby DB
        /// </summary>
        public static string LobbyDataConnectionString
        {
            get
            {
                return m_LobbyDataConnectionString;
            }
            set
            {
                m_LobbyDataConnectionString = value;
            }
        }

        /// <summary>
        /// Gets a new SqlConnection object, based on the LobbyDataConnectionString.
        /// Caller is responsible for cleaning it up
        /// </summary>
        /// <returns></returns>
        public static SqlConnection LobbyDataConnection
        {
            get
            {
                return new SqlConnection(LobbyDataConnectionString);
            }
        }

        public static bool Chat_DoesAliasExist(this DB db, string alias)
        {
            SqlConnection con = DB.UserDataConnection;

            SqlCommand cmd = DB.GetCommand(con, "chatAliasExists", true);

            cmd.Parameters.Add(new SqlParameter("@userAlias", alias));
            SqlParameter parm = new SqlParameter("@retVal", 0);
            parm.Direction = ParameterDirection.Output;
            cmd.Parameters.Add(parm);

            try
            {
                con.Open();
                cmd.Connection = con;
                cmd.ExecuteNonQuery();
                long val = (long)cmd.Parameters["@retVal"].Value;
                bool res = val > 0;

                return res;
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed to get alias status info for [" + alias + "] in database.", e);
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
            }

            return false;
        }

        /// <summary>
        /// Updates an entry int the DB listing of current games.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="owningClusterServerId"></param>
        /// <param name="gameId"></param>
        /// <param name="createdOnUTC"></param>
        /// <param name="createdByCharacter"></param>
        /// <returns></returns>
        public static bool Lobby_UpdateGameForServer(this DB db, IGame game)
        {
            bool result = true;
            SqlConnection con = DB.SessionDataConnection;

            SqlCommand cmd = DB.GetCommand(con, "Lobby_UpdateGameToServerMap", true);

            cmd.Parameters.Add(new SqlParameter("@GameID", game.GameID));

            if (game.Name.Length > 512)
            {
                game.Name = game.Name.Substring(0, 512);
            }

            cmd.Parameters.Add(new SqlParameter("@GameName", game.Name));
            cmd.Parameters.Add(new SqlParameter("@MaxPlayers", game.MaxPlayers));
            cmd.Parameters.Add(new SqlParameter("@CurPlayers", game.AllPlayers.Count));
            cmd.Parameters.Add(new SqlParameter("@InProgress", game.Started));
            cmd.Parameters.Add(new SqlParameter("@IsPrivate", false));

            SqlTransaction tran = null;

            try
            {
                con.Open();
                tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Connection = con;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();

                if (OnGameUpdate != null && !OnGameUpdate(game, con, tran))
                {
                    tran.Rollback();
                    return false;
                }

                tran.Commit();
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed to update game info for [" + game.GameID + "] [" + game.Name + "] in database.", e);
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

            return result;
        }

        /// <summary>
        /// Adds an entry to the DB listing of which servers are hosting which game.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="owningClusterServerId"></param>
        /// <param name="gameId"></param>
        /// <param name="createdOnUTC"></param>
        /// <param name="createdByCharacter"></param>
        /// <returns></returns>
        public static bool Lobby_TrackGameForServer(this DB db, string owningClusterServerId, IGame game, DateTime createdOnUTC, ServerUser createdByCharacter)
        {
            bool result = true;
            SqlConnection con = DB.SessionDataConnection;

            SqlCommand cmd = DB.GetCommand(con, "Lobby_AddGameToServerMap", true);

            cmd.Parameters.Add(new SqlParameter("@ClusterServerID", owningClusterServerId));
            cmd.Parameters.Add(new SqlParameter("@GameID", game.GameID));
            cmd.Parameters.Add(new SqlParameter("@CreatedOn", createdOnUTC));
            cmd.Parameters.Add(new SqlParameter("@CreatedByCharacter", createdByCharacter.CurrentCharacter.CharacterInfo.ID));

            if (game.Name.Length > 512)
            {
                game.Name = game.Name.Substring(0, 512);
            }

            cmd.Parameters.Add(new SqlParameter("@GameName", game.Name));
            cmd.Parameters.Add(new SqlParameter("@MaxPlayers", game.MaxPlayers));

            SqlTransaction tran = null;

            try
            {
                con.Open();
                tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Connection = con;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();

                if (OnTrackGame != null && !OnTrackGame(owningClusterServerId, game, createdByCharacter, con, tran))
                {
                    tran.Rollback();
                    return false;
                }

                tran.Commit();
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("Failed to trac new game for server [" + owningClusterServerId + "], create by character ID [" + createdByCharacter + "] in database.", e);
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

            return result;
        }

        /// <summary>
        /// Remove an entry from the DB listing of which servers are hosting which game.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="gameId"></param>
        /// <returns></returns>
        public static bool Lobby_UntrackGameForServer(this DB db, Guid gameId)
        {
            bool result = true;
            SqlConnection con = DB.SessionDataConnection;

            SqlCommand cmd = DB.GetCommand(con, "Lobby_DeleteGameFromServerMap", true);
            cmd.Parameters.Add(new SqlParameter("@GameID", gameId));
            SqlTransaction tran = null;

            try
            {
                con.Open();
                tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Connection = con;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();

                if (OnUntrackGame != null && !OnUntrackGame(gameId, con, tran))
                {
                    tran.Rollback();
                    return false;
                }

                tran.Commit();
            }
            catch (Exception e)
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

            return result;
        }

        /// <summary>
        /// Removes all entries for a given server from the DB listing of which servers are hosting which game.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="owningClusterServerId"></param>
        /// <returns></returns>
        public static bool Lobby_UntrackGamesForServer(this DB db, string owningClusterServerId)
        {
            bool result = true;
            SqlConnection con = DB.SessionDataConnection;
            
            SqlCommand cmd = DB.GetCommand(con, "Lobby_DeleteGamesForServerFromServerMap", true);
            cmd.Parameters.Add(new SqlParameter("@OwningClusterServerID", owningClusterServerId));

            SqlTransaction tran = null;

            try
            {
                con.Open();
                tran = con.BeginTransaction(IsolationLevel.ReadCommitted);
                cmd.Connection = con;
                cmd.Transaction = tran;
                cmd.ExecuteNonQuery();

                if (OnUntrackGamesForServer != null && !OnUntrackGamesForServer(owningClusterServerId, con, tran))
                {
                    tran.Rollback();
                    return false;
                }

                tran.Commit();
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
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

            return result;
        }

        /// <summary>
        /// Searches the list of registered games and returns a table with the given values.  If you wish to alter the SQL generated for this method 
        /// (perhaps because you want to include additional search paramteres), subscribe to the LobbyDBExtensions.OnGameSearch event. Method will
        /// return null if there was an error, or an empty table if no results where found.  If null is returned, check @msg paramter to see what
        /// went wrong - it was most likely a problem with the query.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="page">which page, zero-based</param>
        /// <param name="numPerPage">number of records per page</param>
        /// <param name="excludeServerId">Server ID to not include listing for. This is usually the calling server (since that server has a listing of its own games already), or empty string.</param>
        /// <param name="includeInProgressGames">include games that are already in progress?</param>
        /// <param name="maxPlayersAllowed">all games returned allow at least this many players to participate</param>
        /// <param name="msg"></param>
        /// <param name="additionalParamters">Any additional paramters that the client sent up.  you have to handle the OnGameSearch event and modify the T-SQL to make use of these parameters.</param>
        /// <returns></returns>
        public static DataTable Lobby_GetGameListing(this DB db, int page, int numPerPage, string excludeServerId, bool includeInProgressGames, int maxPlayersAllowed, PropertyBag additionalParamters, out string msg)
        {
            msg = "";
            SqlConnection con = DB.SessionDataConnection;
            
            string ip = "";
            if(!includeInProgressGames)
            {
                ip = "Lobby_tblGameToServerMap.InProgress = 0 AND";
            }
            string where = ip + "Lobby_tblGameToServerMap.MaxPlayersAllowed >= " + maxPlayersAllowed;
            
            if (excludeServerId.Length > 0)
            {
                where += " AND Lobby_tblGameToServerMap.ClusterServerID != " + excludeServerId;
            }

            where += " AND Lobby_tblGameToServerMap.ClusterServerID IN (SELECT ClusterServerID FROM Lobby_tblServers)";

            string sql = "SELECT TOP " + numPerPage + " * FROM (SELECT ROW_NUMBER() OVER (ORDER BY GameName DESC) AS RowNumber, *, TotalRows=Count(*) OVER() FROM Lobby_tblGameToServerMap WHERE " + where + ") _tmpInlineView WHERE RowNumber > " + (numPerPage * page).ToString() + " ORDER BY RowNumber ASC";

            if (OnGameSearch != null)
            {
                OnGameSearch(page, numPerPage, includeInProgressGames, maxPlayersAllowed, ref sql, additionalParamters);
            }

            SqlCommand cmd = DB.GetCommand(con, sql, false);
            DataTable dt = new DataTable();
            try
            {
                con.Open();
                SqlDataReader r = cmd.ExecuteReader();
                dt.Load(r);
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
                msg = e.Message;
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
            }

            return dt;
        }

        /// <summary>
        /// Searches the list of registered games and other quickmatch seekers and returns a table with the given values.  If you wish to alter the SQL generated for this method 
        /// (perhaps because you want to include additional search paramteres), subscribe to the LobbyDBExtensions.OnGameSearch event. Method will
        /// return null if there was an error, or an empty table if no results where found.  If null is returned, check @msg paramter to see what
        /// went wrong - it was most likely a problem with the query.  Only the TOP 1 games are returned.
        /// </summary>
        /// <param name="db"></param>
        /// <param name="additionalParamters">Any additional paramters that the client sent up.  you have to handle the OnGameSearch event and modify the T-SQL to make use of these parameters.</param>
        /// <returns></returns>
        public static DataTable Lobby_GetQuickMatch(this DB db, PropertyBag additionalParamters, out string msg)
        {
            msg = "";
            SqlConnection con = DB.SessionDataConnection;

            string sql = "SELECT TOP 1 FROM Lobby_tblGameToServerMap WHERE Lobby_tblGameToServerMap.InProgress = 0 AND Lobby_tblGameToServerMap.ClusterServerID IN (SELECT ClusterServerID FROM Lobby_tblServers)";

            if (OnGameSearch != null)
            {
                OnGameSearch(0, 1, false, int.MaxValue, ref sql, additionalParamters);
            }

            SqlCommand cmd = DB.GetCommand(con, sql, false);
            DataTable dt = new DataTable();
            try
            {
                con.Open();
                SqlDataReader r = cmd.ExecuteReader();
                dt.Load(r);
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
                msg = e.Message;
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
            }

            return dt;
        }

        /// <summary>
        /// Gets the connection information for the server that is currently hosting a particular game.  Will return false if either the game or the server
        /// is no longer registered (i.e. the game is inaccessible).
        /// </summary>
        /// <param name="db"></param>
        /// <param name="gameId"></param>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool Lobby_GetGameServer(this DB db, Guid gameId, out string address, out int port, out string owningServer)
        {
            SqlConnection con = DB.SessionDataConnection;
            address = "";
            port = 0;
            owningServer = "";

            SqlCommand cmd = DB.GetCommand(con, "Lobby_GetGameServer", true);
            cmd.Parameters.Add(new SqlParameter("@GameID", gameId));

            DataTable dt = new DataTable();
            try
            {
                con.Open();
                SqlDataReader r = cmd.ExecuteReader();

                if (!r.Read())
                {
                    return false;
                }

                address = r.GetString(r.GetOrdinal("Address"));
                port = r.GetInt32(r.GetOrdinal("Port"));
                owningServer = r.GetString(r.GetOrdinal("ClusterServerID"));
            }
            catch (Exception e)
            {
                Log1.Logger("Server").Error("[DATABASE ERROR] : " + e.Message);
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
            }

            return true;
        }

        /// <summary>
        /// Removes all entries in the games to server mapping table.
        /// </summary>
        /// <param name="owningClusterServerId"></param>
        /// <returns></returns>
        public static bool Lobby_ClearGamesMap(this DB db)
        {
            bool result = true;
            SqlConnection con = DB.SessionDataConnection;

            SqlCommand cmd = DB.GetCommand(con, "Lobby_DeleteGamesServerMap", true);            

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
    }
}
