using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;

namespace Shared
{
    /// <summary>
    /// Represent a connection initiated by the central server to a sub-server node.
    /// </summary>
    public class CSOutboundConnection : OutboundServerConnection
    {
        public CSOutboundConnection(string name, ServerBase server, string reportedIP, bool isBlocking)
            : base(name, server, reportedIP, isBlocking)
        {
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();            
        }

        protected override void OnRemoteCharacterDisconnected(INetworkConnection con, int characterId, string transferTarget)
        {
            // there is a remote possibility that the player can reconnect to THIS server (as part of a transfer)
            // BEFORE the content server's disconnection message arives (which calls this method).  In such a case,
            // the character would become uncached (i.e. the player connecting causes the character to be re-cached
            // and then when this disconnection packet arrives, it would uncache the character.  
            
            base.OnRemoteCharacterDisconnected(con, characterId, transferTarget);
            // uncache, if appropriate
            if (!CharacterCache.IsCharacterConnectionAlive(characterId))
            {
                if (transferTarget != null && transferTarget.Length > 0)
                {
                    // just transferring, so don't immediately expire the cache.  let it expire if the player never makes it on again.
                    // if the player logs back in on another server, we will have OnCharacterHandoffComplete fire, which will reset the 
                    // cache timer
                    CharacterCache.UpdateCacheTime(characterId, TimeSpan.FromSeconds(60));
                }
                else
                {
                    CharacterCache.UncacheCharacter(characterId);
                }
            }
        }

        protected override void OnCharacterHandoffComplete(INetworkConnection con, ServerCharacterInfo character, Guid owner)
        {
            base.OnCharacterHandoffComplete(con, character, owner);
            OutboundServerConnection ocon = con as OutboundServerConnection;

            // we uncache a character when they disconnect (even as part of a transfer, just in case they never reconnect).
            // now that they have reconnected, recache them.  
            CharacterCache.CacheCharacter(character, ocon.ServerUserID, TimeSpan.MaxValue);
        }


    }
}
