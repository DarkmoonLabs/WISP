using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public class CommandRestartMachine
    {
        public string Restart(string executor)
        {
            Log1.Logger("Server").Info(executor + "RESTARTING SERVER");
            Environment.Exit(0);
            return executor + " restarting server.";
        }
    }
}
