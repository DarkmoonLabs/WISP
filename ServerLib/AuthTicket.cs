using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class AuthTicket
    {
        public string AuthorizingServer { get; set; }
        public string TargetServer { get; set; }
        public DateTime AuthorizedOn { get; set; }
        public Guid AccountID { get; set; }
        public string AccountName { get; set; }
        public int CharacterID { get; set; }
    }
}
