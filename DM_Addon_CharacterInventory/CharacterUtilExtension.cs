using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;
using System.Data.SqlClient;

namespace DM_Addon_CharacterInventory
{
    public static class CharacterUtilExtension
    {
        public static bool PersistNewCharacter_Inventory(this CharacterUtil util, ICharacterInfo ci, SqlConnection con, SqlTransaction tran)
        {
            DB.Instance.Character_Create_Inventory(con, tran);
            return true;
        }

    }
}
