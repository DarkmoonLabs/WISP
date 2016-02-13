using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ServerLib;
#if !IS_CLIENT
using ServerLib;
#endif

namespace Shared
{
    public class WispUserDetail : ISerializableWispObject
    {
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
            BitPacker.AddLong(ref buffer, p, LastLogin.Ticks);
            BitPacker.AddLong(ref buffer, p, UserSince.Ticks);
            BitPacker.AddLong(ref buffer, p, LastPasswordChange.Ticks);

            BitPacker.AddString(ref buffer, p, ID.ToString());
            
            BitPacker.AddString(ref buffer, p, Email);
            BitPacker.AddString(ref buffer, p, Username);

            BitPacker.AddBool(ref buffer, p, IsLocked);
            BitPacker.AddBool(ref buffer, p, IsOnline);
            BitPacker.AddBool(ref buffer, p, IsApproved);

            BitPacker.AddStringList(ref buffer, p, Roles);

            BitPacker.AddInt(ref buffer, p, ServiceNotes.Count);
            for (int i = 0; i < ServiceNotes.Count; i++)
            {
                ServiceLogEntry sle = ServiceNotes[i];
                BitPacker.AddString(ref buffer, p, sle.EntryBy);
                BitPacker.AddString(ref buffer, p, sle.Note);
                BitPacker.AddString(ref buffer, p, sle.EntryType);
                BitPacker.AddInt(ref buffer, p, sle.CharacterId);
                BitPacker.AddLong(ref buffer, p, sle.TimeStampUTC.Ticks);
            }

            BitPacker.AddPropertyBag(ref buffer, p, AddedProperties);

            BitPacker.AddInt(ref buffer, p, LoginSessions.Count);
            for (int i = 0; i < LoginSessions.Count; i++)
            {
                AccountProfile.Session s = LoginSessions[i];
                BitPacker.AddLong(ref buffer, p, s.LoginUTC.Ticks);
                BitPacker.AddLong(ref buffer, p, s.LogoutUTC.Ticks);
                BitPacker.AddString(ref buffer, p, s.IP);
            }

            BitPacker.AddLong(ref buffer, p, CurrentLoginTime.Ticks);

            BitPacker.AddInt(ref buffer, p, Characters.Count);
            for (int i = 0; i < Characters.Count; i++)
            {
                ICharacterInfo ci = Characters[i];
                BitPacker.AddComponent(ref buffer, p, ci);
            }
        }

        public void Deserialize(byte[] data, Pointer p)
        {
            LastLogin = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
            UserSince = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
            LastPasswordChange = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);

            ID = new Guid(BitPacker.GetString(data, p));

            Email = BitPacker.GetString(data, p);
            Username = BitPacker.GetString(data, p);

            IsLocked = BitPacker.GetBool(data, p);
            IsOnline = BitPacker.GetBool(data, p);
            IsApproved = BitPacker.GetBool(data, p);

            Roles = BitPacker.GetStringList(data, p);

            int notes = BitPacker.GetInt(data, p);
            for (int i = 0; i < notes; i++)
            {
                ServiceLogEntry sle = new ServiceLogEntry();
                sle.Account = ID;
                sle.EntryBy = BitPacker.GetString(data, p);
                sle.Note = BitPacker.GetString(data, p);
                sle.EntryType = BitPacker.GetString(data, p);
                sle.CharacterId = BitPacker.GetInt(data, p);
                sle.TimeStampUTC = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
                ServiceNotes.Add(sle);
            }

            AddedProperties = BitPacker.GetPropertyBag(data, p);

            int numSessions = BitPacker.GetInt(data, p);
            for (int i = 0; i < numSessions; i++)
            {
                DateTime login = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
                DateTime logout = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);
                string ip = BitPacker.GetString(data, p);
                ip = ip.Substring(0, ip.LastIndexOf("]") + 1);
                AccountProfile.Session s = new AccountProfile.Session(login, logout, ip);
                LoginSessions.Add(s);
            }

            //LoginSessions = LoginSessions.OrderBy(session => session.LogoutUTC).ToList();   
            LoginSessions.Reverse();
            CurrentLoginTime = new DateTime(BitPacker.GetLong(data, p), DateTimeKind.Utc);

            int characters = BitPacker.GetInt(data, p);
            for (int i = 0; i < characters; i++)
            {
                ICharacterInfo ci = BitPacker.GetComponent(data, p) as ICharacterInfo;
                Characters.Add(ci);
            }
        }

        public WispUserDetail()
        {
            ServiceNotes = new List<ServiceLogEntry>();
            Roles = new List<string>();
            AddedProperties = new PropertyBag();
            LoginSessions = new List<AccountProfile.Session>();
            Characters = new List<ICharacterInfo>();
        }

        public List<ICharacterInfo> Characters { get; set; }

        public DateTime LastLogin { get; set; }
        public DateTime UserSince { get; set; }
        public DateTime LastPasswordChange { get; set; }

        public Guid ID { get; set; }

        public string Email { get; set; }
        public string Username { get; set; }

        public bool IsLocked { get; set; }
        public bool IsOnline { get; set; }
        public bool IsApproved { get; set; }

        public List<ServiceLogEntry> ServiceNotes { get; set; }
        public List<string> Roles { get; set; }

        public PropertyBag AddedProperties { get; set; }

        public List<AccountProfile.Session> LoginSessions { get; set; }

        public DateTime CurrentLoginTime { get; set; }
    }
}
