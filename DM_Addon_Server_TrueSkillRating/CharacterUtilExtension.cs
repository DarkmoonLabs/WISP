using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using System.Data.SqlClient;

namespace Shared
{
    public static class CharacterUtilExtension
    {
        public static bool PersistNewCharacter_TSRating(this CharacterUtil util, ICharacterInfo ci, SqlConnection con, SqlTransaction tran)
        {
            DB.Instance.Character_Create_TSRating(con, tran);
            return true;
        }

        public static bool LoadCharacter_TSRating(this CharacterUtil util, ICharacterInfo ci, SqlConnection con, SqlTransaction tran)
        {
            DB.Instance.Character_Load_TSRating(con, tran);
            return true;
        }

        public static bool DeleteCharacter_TSRating(this CharacterUtil util, int toon, SqlConnection con, SqlTransaction tran)
        {
            DB.Instance.Character_Delete_TSRating(toon, con, tran);
            return true;
        }

        public static bool SaveCharacter_TSRating(this CharacterUtil util, ICharacterInfo ci, SqlConnection con, SqlTransaction tran)
        {
            DB.Instance.Character_Save_TSRating(con, tran);
            return true;
        }


    }
}
