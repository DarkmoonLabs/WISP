using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Configuration;

namespace Shared
{
    public static class ZeusDBExtension
    {
        public static bool User_SearchByIP(this DB db, string IP, List<WispUsersInfo.User> userContainer)
        {
            if (userContainer == null)
            {
                return false;
            }

            bool result = true;

            string cons = ConfigurationManager.ConnectionStrings["DataConnectionString"].ConnectionString;
            SqlConnection con = new SqlConnection(cons);
            SqlCommand cmd = DB.GetCommand(con, "aspnet_Users_FindByIP", true);
            cmd.Parameters.Add(new SqlParameter("@IP", IP));

            SqlDataReader reader = null;
            try
            {
                con.Open();
                cmd.Connection = con;
                reader = cmd.ExecuteReader();
                
                if (!reader.HasRows)
                {
                    return false;
                }
                
                while (reader.Read())
                {
                    Guid id = reader.GetGuid(0);
                    bool isLocked = reader.IsDBNull(1)? false : reader.GetBoolean(1);
                    string userName = reader.GetString(2);
                    string email = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    DateTime lastLogin = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                    userContainer.Add(new WispUsersInfo.User(userName, new string[0], isLocked, id, email, lastLogin));
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


       
    }
}
