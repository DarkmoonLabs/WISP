using System;
using System.Net.Sockets;
namespace Shared
{
    /// <summary>
    /// Interface that all UDP listeners must implement. Both ClientConenction and ServerConnection already have UDP listeners implemented, so there should be no
    /// reason to roll your own.
    /// </summary>
    public interface IUDPListener
    {
        bool Listening { get; set; }
        bool StartListening(System.Net.Sockets.AddressFamily family, int port, int maxSimultaneousListens, INetworkConnection owner);
        bool StartListening(System.Net.Sockets.AddressFamily family, int port, int maxSimultaneousListens, Func<System.Net.IPEndPoint, INetworkConnection> getConMethod);
        void StopListening();
        int Port { get; set; }
        Socket Socket { get; set; }
    }
}
