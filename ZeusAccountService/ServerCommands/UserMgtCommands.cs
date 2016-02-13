using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web.Security;

namespace Shared
{
    public class UserMgtCommands
    {
        public string UpdateCharacterStringProperty(string characterId, string propertyId, string newValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = -1;
                if (characterId.Trim().Length > 0)
                {
                    charId = int.Parse(characterId);
                }

                if (!DB.Instance.Character_UpdateStringProperty(charId, propId, "", newValue))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to update property on character. " + e.Message;
            }

            return msg;
        }

        public string InsertCharacterStringProperty(string characterId, string propertyId, string propertyName, string newValue, string executor)
        {
            string msg = "";

            try
            {
                if (propertyName == null || propertyName.Length < 1)
                {
                    msg = "Property name can't be blank.";
                    return msg;
                }
                
                int propId = int.Parse(propertyId);
                int charId = -1;
                if (characterId.Trim().Length > 0)
                {
                    charId = int.Parse(characterId);
                }

                if (!DB.Instance.Character_UpdateStringProperty(charId, propId, propertyName, newValue))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId +"] to [" + newValue +"].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId +"] to [" + newValue +"].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to set Property on character. " + e.Message;
            }

            return msg;
        }

        public string UpdateCharacterFloatProperty(string characterId, string propertyId, string newValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                float fval = float.Parse(newValue);

                if (!DB.Instance.Character_UpdateFloatProperty(charId, propId, "", fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to update property on character. " + e.Message;
            }

            return msg;
        }
        
        public string InsertCharacterFloatProperty(string characterId, string propertyId, string propertyName, string newValue, string executor)
        {
            string msg = "";

            try
            {
                if (propertyName == null || propertyName.Length < 1)
                {
                    msg = "Property name can't be blank.";
                    return msg;
                }

                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                float fval = float.Parse(newValue);

                if (!DB.Instance.Character_UpdateFloatProperty(charId, propId, propertyName, fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to set Property on character. " + e.Message;
            }

            return msg;
        }

        public string UpdateCharacterLongProperty(string characterId, string propertyId, string newValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                long fval = long.Parse(newValue);

                if (!DB.Instance.Character_UpdateLongProperty(charId, propId, "", fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to update property on character. " + e.Message;
            }

            return msg;
        }

        public string InsertCharacterLongProperty(string characterId, string propertyId, string propertyName, string newValue, string executor)
        {
            string msg = "";

            try
            {
                if (propertyName == null || propertyName.Length < 1)
                {
                    msg = "Property name can't be blank.";
                    return msg;
                }

                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                long fval = long.Parse(newValue);

                if (!DB.Instance.Character_UpdateLongProperty(charId, propId, propertyName, fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to set Property on character. " + e.Message;
            }

            return msg;
        }

        public string UpdateCharacterIntProperty(string characterId, string propertyId, string newValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                int fval = int.Parse(newValue);

                if (!DB.Instance.Character_UpdateIntProperty(charId, propId, "", fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to update property on character. " + e.Message;
            }

            return msg;
        }

        public string InsertCharacterIntProperty(string characterId, string propertyId, string propertyName, string newValue, string executor)
        {
            string msg = "";

            try
            {
                if (propertyName == null || propertyName.Length < 1)
                {
                    msg = "Property name can't be blank.";
                    return msg;
                }

                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (newValue == null || newValue.Length < 1)
                {
                    msg = "Value must be formatted as a floating point number.";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                int fval = int.Parse(newValue);

                if (!DB.Instance.Character_UpdateIntProperty(charId, propId, propertyName, fval))
                {
                    msg = "DB Error setting character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully set character [" + charId + "] property ID [" + propId + "] to [" + newValue + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to set Property on character. " + e.Message;
            }

            return msg;
        }

        public string InsertCharacterStat(string characterId, string statId, string minValue, string maxValue, string curValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(statId);
                int charId = int.Parse(characterId);

                if (curValue == null || curValue.Length < 1 || maxValue == null || maxValue.Length < 1 || minValue == null || minValue.Length < 1)
                {
                    msg = "Add failed: Must enter all three values [Current, Maximum, Minimum]";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                float mnValue = float.Parse(minValue);
                float mxValue = float.Parse(maxValue);
                float cValue = float.Parse(curValue);

                if (!DB.Instance.Character_UpdateStat(charId, propId, mnValue, mxValue, cValue))
                {
                    msg = "DB Error adding character [" + charId + "] Stat ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully added character [" + charId + "] Stat ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to add Stat on character. " + e.Message;
            }

            return msg;
        }

        public string UpdateCharacterStat(string characterId, string statId, string minValue, string maxValue, string curValue, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(statId);
                int charId = int.Parse(characterId);

                if (curValue == null || curValue.Length < 1 || maxValue == null || maxValue.Length < 1 || minValue == null || minValue.Length < 1)
                {
                    msg = "Update failed: Must enter all three values [Current, Maximum, Minimum]";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                float mnValue = float.Parse(minValue);
                float mxValue = float.Parse(maxValue);
                float cValue = float.Parse(curValue);

                if (!DB.Instance.Character_UpdateStat(charId, propId, mnValue, mxValue, cValue))
                {
                    msg = "DB Error Updating character [" + charId + "] Stat ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully updated character [" + charId + "] Stat ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to update Stat on character. " + e.Message;
            }

            return msg;
        }

        public string DeleteCharacterStringProperty(string characterId, string propertyId, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (!DB.Instance.Character_DeleteStringProperty(charId, propId))
                {
                    msg = "DB Error deleting character [" + charId + "] property ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully deleted character [" + charId + "] property ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to delete Property on character. " + e.Message;
            }

            return msg;
        }

        public string DeleteCharacterFloatProperty(string characterId, string propertyId, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (!DB.Instance.Character_DeleteFloatProperty(charId, propId))
                {
                    msg = "DB Error deleting character [" + charId + "] property ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully deleted character [" + charId + "] property ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to delete Property on character. " + e.Message;
            }

            return msg;
        }

        public string DeleteCharacterIntProperty(string characterId, string propertyId, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (!DB.Instance.Character_DeleteIntProperty(charId, propId))
                {
                    msg = "DB Error deleting character [" + charId + "] property ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully deleted character [" + charId + "] property ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to delete Property on character. " + e.Message;
            }

            return msg;
        }

        public string DeleteCharacterLongProperty(string characterId, string propertyId, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (!DB.Instance.Character_DeleteLongProperty(charId, propId))
                {
                    msg = "DB Error deleting character [" + charId + "] property ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully deleted character [" + charId + "] property ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to delete Property on character. " + e.Message;
            }

            return msg;
        }

        public string DeleteCharacterStat(string characterId, string propertyId, string executor)
        {
            string msg = "";

            try
            {
                int propId = int.Parse(propertyId);
                int charId = int.Parse(characterId);

                if (!DB.Instance.Character_DeleteStat(charId, propId))
                {
                    msg = "DB Error deleting character [" + charId + "] stat ID [" + propId + "].";
                    Log1.Logger("Server.Commands").Info(msg);
                    return msg;
                }

                msg = "[" + executor + "] successfully deleted character [" + charId + "] stat ID [" + propId + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to delete Stat on character. " + e.Message;
            }

            return msg;
        }
        
        public string UnsuspendAccount(string guidUser, string serviceMessage, string characterId, string executor)
        {
            string msg = "";

            try
            {
                if (serviceMessage.Trim().Length < 1)
                {
                    return "Must enter reason into service note field.";
                }

                int charId = -1;
                if (characterId.Trim().Length > 0)
                {
                    charId = int.Parse(characterId);
                }

                Guid id = new Guid(guidUser);
                if (!DB.Instance.User_Unsuspend(id, executor, DateTime.UtcNow, serviceMessage, charId))
                {
                    return "Error in DB when trying to unsuspend account [" + id.ToString() + "].";
                }

                msg = "Account [" + guidUser + "] unsuspended.";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to unsuspend user. " + e.Message;
            }

            return msg;
        }

        public string SuspendAccount(string guidUser, string longDurationHours, string serviceMessage, string characterId, string executor)
        {
            string msg = "";

            try
            {
                int charId = -1;
                if (characterId.Trim().Length > 0)
                {
                    charId = int.Parse(characterId);
                }

                Guid id = new Guid(guidUser);
                long durationHours = long.Parse(longDurationHours);

                //MembershipUser user = Membership.GetUser(id);
                //if (user == null)
                //{
                //    msg = "Lockout failed. Unable to locate user [" +  id.ToString() + "] in the database.";
                //    return msg;
                //}

                //Roles.RemoveUserFromRole(user.UserName, "ActiveUser");
                DateTime end = DateTime.UtcNow;
                if (!DB.Instance.User_Suspend(id, executor, serviceMessage, durationHours, charId, out end))
                {
                    return "Error in DB when trying to suspend account [" + id.ToString() + "].";
                }

                msg = "Account [" + guidUser + "] locked out until " + end.ToString("g");
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to lockout user. " + e.Message;
            }

            return msg;
        }

        public string AddRole(string guidUser, string role, string executor)
        {
            string msg = "";

            try
            {
                if (role.Trim().Length < 1)
                {
                    return "Role can't be blank.";
                }

                Guid id = new Guid(guidUser);
                MembershipUser usr = Membership.GetUser(id, false);

                if (usr == null)
                {
                    return "User not found.";
                }

                Roles.AddUserToRole(usr.UserName, role);
                if (!Roles.IsUserInRole(usr.UserName, role))
                {
                    return "Failed to add role [" + role + "] to account [" + usr.UserName + "].";
                }

                msg = "Account [" + usr.UserName + "] was added to role [" + role +"].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to add role to user. " + e.Message;
            }

            return msg;
        }

        public string RemoveRole(string guidUser, string role, string executor)
        {
            string msg = "";

            try
            {
                if (role.Trim().Length < 1)
                {
                    return "Role can't be blank.";
                }

                Guid id = new Guid(guidUser);
                MembershipUser usr = Membership.GetUser(id, false);

                if (usr == null)
                {
                    return "User not found.";
                }

                Roles.RemoveUserFromRole(usr.UserName, role);
                if (Roles.IsUserInRole(usr.UserName, role))
                {
                    return "Failed to remove role [" + role + "] to account [" + usr.UserName + "].";
                }

                msg = "Account [" + usr.UserName + "] was removed from role [" + role + "].";
            }
            catch (Exception e)
            {
                msg = "Error occurred trying to add role to user. " + e.Message;
            }

            return msg;
        }
    }
}
