using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace DM_Addon_CharacterInventory
{
    public static class DBExtension
    {
        public static bool Character_Create_Inventory(this DB db, SqlConnection con, SqlTransaction t)
        {            
            return true;
        }

        public static bool Character_Delete_Inventory(this DB db, SqlConnection con, SqlTransaction t)
        {
            return true;
        }
    }
}
