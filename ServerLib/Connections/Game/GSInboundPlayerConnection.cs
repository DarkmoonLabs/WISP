using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Handles a single player connection to the game server
    /// </summary>
    public class GSInboundPlayerConnection : InboundPlayerConnection
    {
        public GSInboundPlayerConnection(Socket s, ServerBase server, bool isBlocking)
            : base(s, server, isBlocking)
        {
        }

        private static bool m_IsInitialized = false;
        protected override void OnInitialize()
        {
            base.OnInitialize();
            if (!m_IsInitialized)
            {
                m_IsInitialized = true;
                // Init code here
            }
        }

        /// <summary>
        /// Gets called when the player login/transfer request has been resolved - i.e. the player connected and gave us login credentials.  Return false to prevent login. Don't forget to call base.OnPlayerLoginResolved 
        /// if you override this method so that all necessary packet handlers can be registered.
        /// </summary>
        protected override bool OnPlayerLoginResolved(PacketLoginRequest login, bool result, ref string msg)
        {
            if (base.OnPlayerLoginResolved(login, result, ref msg))
            {
                if (ServerUser.CurrentCharacter != null)
                {
                    CharacterCache.CacheCharacter(ServerUser.CurrentCharacter, MyServer.ServerUserID, TimeSpan.FromMinutes(60));
                }
            }
            return result;
        }

    }
}
