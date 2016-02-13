using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Shared;

namespace LobbyBeholder
{
    public class Commands
    {
        public void ExampleCommand(string executor)
        {
            Log1.Logger("Server").Info("Example command executed by " + executor);
        }

    }
}
