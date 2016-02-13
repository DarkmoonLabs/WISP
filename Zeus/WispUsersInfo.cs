using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    /// Wisp object that lists all of the config settings available on the server
    /// </summary>
    public class WispUsersInfo : ISerializableWispObject
    {
        public class User
        {
            public User(string name, string[] roles, bool isLocked, Guid id, string email, DateTime lastLogin)
            {                
                if (roles == null)
                {
                    roles = new string[0];
                }
                Username = name;
                Roles = roles;
                IsLocked = IsLocked;
                ID = id;
                Email = email;
                LastLogin = lastLogin;
            }

            public DateTime LastLogin { get; set; }
            public string Email { get; set; }
            public string[] Roles { get; set; }
            public string Username { get; set; }
            public bool IsLocked { get; set; }
            public Guid ID { get; set; }
        }

        public List<User> Users = new List<User>();
        public string UserDataStore = "";
        public bool AllowRemoteConnections = false;
        public int TotalUserCount = 0;

        private static uint m_TypeHash = 0;
        public virtual uint TypeHash
        {
            get
            {
                if (m_TypeHash == 0)
                {
                    m_TypeHash = Factory.GetTypeHash(this.GetType());
                }

                return m_TypeHash;
            }
        }

        public void Serialize(ref byte[] buffer, Pointer p)
        {
            BitPacker.AddBool(ref buffer, p, AllowRemoteConnections);
            BitPacker.AddInt(ref buffer, p, TotalUserCount);
            BitPacker.AddString(ref buffer, p, UserDataStore);            
            BitPacker.AddInt(ref buffer, p, Users.Count);
            
            foreach(User su in Users)
            {
                BitPacker.AddString(ref buffer, p, su.Username);
                BitPacker.AddStringList(ref buffer, p, new List<string>(su.Roles));
                BitPacker.AddBool(ref buffer, p, su.IsLocked);
                BitPacker.AddString(ref buffer, p, su.ID.ToString());
                BitPacker.AddString(ref buffer, p, su.Email);
                BitPacker.AddLong(ref buffer, p, su.LastLogin.Ticks);
            }
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            AllowRemoteConnections = BitPacker.GetBool(data, p);
            TotalUserCount = BitPacker.GetInt(data, p);
            UserDataStore = BitPacker.GetString(data, p);

            int count = BitPacker.GetInt(data, p);
            for (int i = 0; i < count; i++)
            {
                string username = BitPacker.GetString(data, p);
                string[] roles = BitPacker.GetStringList(data, p).ToArray();
                bool isLocked = BitPacker.GetBool(data, p);
                Guid id = new Guid(BitPacker.GetString(data, p));
                string email = BitPacker.GetString(data, p);
                DateTime lastLogin = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
                User urs = new User(username, roles, isLocked, id, email, lastLogin);
                Users.Add(urs);
            }
        }


    }
}
