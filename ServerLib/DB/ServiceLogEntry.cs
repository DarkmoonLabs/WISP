using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ServerLib
{
    /// <summary>
    /// A log entry in the database.  Might pertain to characters, or account users, etc - depending on context.
    /// </summary>
    public class ServiceLogEntry
    {
        public ServiceLogEntry(Guid account, string entryType, string entryBy, string note, DateTime timeStampUTC, int characterId)
        {
            EntryBy = entryBy;
            Note = note;
            TimeStampUTC = timeStampUTC;
            EntryType = entryType;
            Account = account;
            CharacterId = characterId;
        }

        public ServiceLogEntry(Guid account, string entryType, string entryBy, string note, DateTime timeStampUTC) : this(account, entryType, entryBy, note, timeStampUTC, -1)
        {
        }

        public ServiceLogEntry()
        {
            // TODO: Complete member initialization
        }

        public string EntryBy { get; set; }
        public string Note { get; set; }
        public DateTime TimeStampUTC { get; set; }
        public string EntryType { get; set; }
        public Guid Account { get; set; }
        public int CharacterId { get; set; }
    }
}
