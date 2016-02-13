using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Shared
{
    public static class DBExtension
    {
        public static bool Character_Create_TSRating(this DB db, SqlConnection con, SqlTransaction t)
        {
            return true;
        }

        public static bool Character_Load_TSRating(this DB db, SqlConnection con, SqlTransaction t)
        {
            return true;
        }

        public static bool Character_Delete_TSRating(this DB db, int toon, SqlConnection con, SqlTransaction t)
        {
            return true;
        }

        public static bool Character_Save_TSRating(this DB db, SqlConnection con, SqlTransaction t)
        {
            return true;
        }
    }
}
