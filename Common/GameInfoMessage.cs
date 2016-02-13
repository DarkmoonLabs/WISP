using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum GameInfoMessageCategory : int
    {
        System,
        TurnUpdate,
        CommandResult
    }

    public class GameInfoMessage
    {
        public int StringID { get; set; }
        public GameInfoMessageCategory Category { get; set; }
        public int Sender { get; set; }
        public string Parms { get; set; }
        public DateTime TimeStampUTC { get; set; }
        public string[] ParmsArray
        {
            get
            {
                return Parms.Split(new char[] { ',' });
            }
        }
    }
}
