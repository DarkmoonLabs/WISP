using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Shared;
using System.Xml.XPath;
using System.Xml;

namespace Shared
{
    /// <summary>
    /// Represents a user database stored in a local file, as opposed to a SQL database
    /// </summary>
    public class UserFile
    {
        public class AccountInfo
        {
            public Guid AccountID { get; set; }
            public string AccountName { get; set; }
            public string Password { get; set; }
            public string Email { get; set; }
            public bool Locked { get; set; }
            public List<string> Roles { get; set; }

            public AccountInfo()
            {
                Roles = new List<string>();
            }
        }

        public static IEnumerable<AccountInfo> AllAccounts
        {
            get
            {
                return Accounts.Values.AsEnumerable();
            }
        }

        private static Dictionary<string, AccountInfo> Accounts = new Dictionary<string, AccountInfo>();
        private static string CurrentPath = "";

        /// <summary>
        /// Creates an empty file with the requried structure
        /// </summary>
        /// <param name="path">where we want to create the file</param>
        /// <returns></returns>
        public static bool CreateClean(string path, ref string msg)
        {
            msg = "";
            StreamWriter sw = null;
            try
            {
                /*
                  See Character.Xml for the structure
                 */
                string dat = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n<Accounts/>";
                sw = new StreamWriter(path, false, Encoding.UTF8);
                sw.Write(dat);
                return true;
            }
            catch (Exception e)
            {
                msg = "Failed to create clean User File at " + path + ". " + e.Message;
                Log.LogMsg("Failed to create clean User File at " + path + ". " + e.Message);
                return false;
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
            }
        }

        /// <summary>
        /// Loads an XML document from disk and returns the appropriate Linq node for query, or returns null on error
        /// </summary>
        /// <param name="filePath">fully qualified path to the file</param>
        /// <returns></returns>
        private static XPathDocument LoadDocument(string filePath, bool preserveWhiteSpace)
        {
            try
            {
                XPathDocument doc = new XPathDocument(filePath, preserveWhiteSpace ? XmlSpace.Preserve : XmlSpace.None);
                return doc;
            }
            catch (Exception e)
            {
                Log.LogMsg("Failed to load XML file " + filePath + ". " + e.Message);
            }

            return null;
        }

        public static bool LoadFromFile(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    string msg = "";
                    CreateClean(path, ref msg);
                }
                CurrentPath = path;
                Accounts.Clear();
                XPathDocument doc = LoadDocument(path, true);
                XPathNavigator nav = doc.CreateNavigator();


                XPathNodeIterator iter = nav.Select(@"//Account");
                while (iter.MoveNext())
                {
                    Guid id = Guid.Empty;
                    try
                    {
                        AccountInfo ai = new AccountInfo();
                        id = new Guid(iter.Current.GetAttribute("ID", ""));
                        string name = iter.Current.GetAttribute("name", "");
                        if (name.Length < 1)
                        {
                            Log.LogMsg("Error loading account data from file. " + id.ToString() + ". Account name can't be blank.");
                            continue;
                        }

                        if (Accounts.ContainsKey(name))
                        {
                            Log.LogMsg("Error loading account data from file. " + id.ToString() + ". Account name already exists.");
                            continue;
                        }

                        string pw = iter.Current.GetAttribute("password", "");
                        string email = iter.Current.GetAttribute("email", "");
                        bool locked = false;
                        bool.TryParse(iter.Current.GetAttribute("locked", ""), out locked);

                        ai.AccountID = id;
                        ai.AccountName = name;
                        ai.Email = email;
                        ai.Locked = locked;
                        ai.Password = pw;
                        


                        // get roles
                        XPathNodeIterator roles = iter.Current.Select("Roles/Role");
                        while (roles.MoveNext())
                        {
                            string rname = roles.Current.GetAttribute("name", "");
                            if (rname.Length > 0)
                            {
                                if (ai.Roles.IndexOf(rname) < 0)
                                {
                                    ai.Roles.Add(rname);
                                }
                            }
                        }

                        Accounts.Add(ai.AccountName, ai);
                    }
                    catch(Exception ie)
                    {
                        Log.LogMsg("Error loading account data from file. " + (id == Guid.Empty? "Account Id is badly formatted. " : id.ToString()) + "_" + ie.Message);
                        continue;
                    }
                }
                return true;
            }
            catch(Exception e)
            {
                return false;
            }
            finally
            {
            }
        }

        public static bool Login(string accountName, string password, ref string msg)
        {
            AccountInfo ai = null;
            if (Accounts.Count == 0)
            {
                // Built in account only works when no other accounts have been specified
                if (accountName == "WispAdmin" && password == "wisp123")
                {
                    return true;
                }

                msg = "Account doesn't exist.";
                return false;
            }

            if (!Accounts.TryGetValue(accountName, out ai))
            {
                msg = "Account doesn't exist.";
                return false;
            }

            string hashed = CryptoManager.GetSHA256Hash(password);
            if (hashed != ai.Password)
            {
                msg = "Account name / password combination doesn't exist.";
                return false;
            }

            return true;
        }

        public static bool IsInRole(string accountName, string role)
        {
            AccountInfo ai = null;
            if (!Accounts.TryGetValue(accountName, out ai))
            {
                if (Accounts.Count == 0 && accountName == "WispAdmin" && role == "Administrator")
                {
                            return true;
                }
                return false;
            }


            return ai.Roles.IndexOf(role) >= 0;
        }

        public static Guid GetUserId(string accountName)
        {
            AccountInfo ai = null;
            if (!Accounts.TryGetValue(accountName, out ai))
            {
                return Guid.Empty;
            }
            return ai.AccountID;
        }

        public static bool IsLocked(string accountName)
        {
            AccountInfo ai = null;
            if (!Accounts.TryGetValue(accountName, out ai))
            {
                return false;
            }

            return ai.Locked;
        }

        public static Guid AddUser(string accountName, string password, string email, bool locked, string[] roles, ref string msg)
        {
            if (Accounts.ContainsKey(accountName))
            {
                msg = "Account name already exists.";
                return Guid.Empty;
            }

            AccountInfo ai = new AccountInfo();
            ai.AccountName = accountName;
            ai.AccountID = Guid.NewGuid();
            ai.Email = email;
            ai.Locked = locked;
            ai.Password = CryptoManager.GetSHA256Hash(password);
            ai.Roles = new List<string>(roles);

            Accounts.Add(accountName, ai);

            if (CurrentPath.Length != 0)
            {
                SaveFile(CurrentPath);
            }
            return ai.AccountID;
        }

        public static bool SaveFile(string path)
        {
            StreamWriter w = null;
            try
            {
/*
                <?xml version="1.0" encoding="utf-8" ?>
                <Accounts>
                  <Account ID="9834-3483743-98349834-3434" name="myaccountname" password="hashed" email="me@home.com" locked="False">
                    <Roles>
                      <Role name="ActiveUser"/>
                    </Roles>
                  </Account>
                </Accounts>
*/
                w = new StreamWriter(path, false, Encoding.UTF8);
                w.WriteLine("<?xml version=\"1.0\" encoding=\"utf-8\" ?>");
                Dictionary<string, AccountInfo>.Enumerator enu = Accounts.GetEnumerator();
                w.WriteLine("<Accounts>");
                while (enu.MoveNext())
                {
                    string[] parts = new string[5];
                    parts[0] = enu.Current.Value.AccountID.ToString();
                    parts[1] = enu.Current.Value.AccountName;
                    parts[2] = enu.Current.Value.Password;
                    parts[3] = enu.Current.Value.Email;
                    parts[4] = enu.Current.Value.Locked.ToString();
                    string account = string.Format("<Account ID=\"{0}\" name=\"{1}\" password=\"{2}\" email=\"{3}\" locked=\"{4}\">", parts);
                    w.WriteLine(account);
                    w.WriteLine("<Roles>");
                    for (int i = 0; i < enu.Current.Value.Roles.Count; i++)
                    {
                        w.WriteLine(string.Format("<Role name=\"{0}\"/>", enu.Current.Value.Roles[i]));
                    }
                    w.WriteLine("</Roles>");
                    w.WriteLine("</Account>");
                }
                ;
                w.WriteLine("</Accounts>");
                return true;
            }
            catch (Exception e)
            {
                Log.LogMsg("Failed to save user file. " + e.Message);
                return false;
            }
            finally
            {
                if (w != null)
                {
                    w.Close();
                    w.Dispose();
                }
            }
        }

        public static string[] GetRoles(string accountName)
        {

            AccountInfo ai = null;
            if (!Accounts.TryGetValue(accountName, out ai))
            {
                return new string[0];
            }

            return ai.Roles.ToArray();
        }

        /// <summary>
        /// Total users currently loaded from file
        /// </summary>
        public static int TotalUsers 
        {
            get
            {
                return Accounts.Count;
            }
        }
    }
}
