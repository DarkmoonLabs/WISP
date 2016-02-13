using System;
namespace Shared
{
    /// <summary>
    /// Interface that all networking connections, inbound and outbound, must implemenent. There is probably no reason to implement your own networking connection.  Instead,
    /// derive from ServerConnection or ClientConnection objects which already implement this interface.
    /// </summary>
    public interface INetworkConnection
    {
        void AssembleInboundPacket(byte[] buffer, int bytesReceived, SockState state);
        void AssembleInboundPacket(System.Net.Sockets.SocketAsyncEventArgs args, SockState state);
        byte[] ConnectionKey { get; set; }
        Packet CreatePacket(int type, int subType, bool encrypt, bool compress);
        PacketReply CreateStandardReply(Packet request, ReplyType replyCode, string msg);
        void DeserializePacket(SockState state);
        void Dispose();
        int GetHashCode();
        bool IsAlive { get; }
        bool IsConnected { get; }
        void KillConnection(string msg);
        void KillConnection(string msg, bool allowPendingPacketsToSend);
        System.Net.Sockets.Socket MyTCPSocket { get; set; }
        System.Net.Sockets.Socket MyUDPSocket { get; set; }
        void OnAfterPacketProcessed(Packet p);
        bool OnBeforePacketProcessed(Packet p);
        void PacketSent();
        bool ProcessIncomingPacketsImmediately { get; set; }
        int ProcessNetworking();
        void ReceivedBytes(int count);    
        System.Net.EndPoint RemoteEndPoint { get; }
        byte[] RemoteRsaKey { get; set; }
        int Send(Packet p);
        int Send(byte[] data, PacketFlags flags);
        bool SendFileStream(System.IO.FileInfo fi, string arg);
        bool SendFileStream(System.IO.FileInfo fi, string arg, int subType);
        bool SendGenericMessage(int msgType);
        bool SendGenericMessage(int msgType, PropertyBag parms, bool encrypt);
        bool SendGenericMessage(int msgType, bool encrypt);
        bool SendGenericMessage(int msgType, string text, PropertyBag parms, bool encrypt);
        bool SendGenericMessage(int msgType, string text, bool encrypt);
        bool SendPacketAck(Packet packet);
        bool SendPacketAck(PacketGenericMessage packet);
        void SentBytes(byte[] bytes);
        void SentBytes(int count);
        byte[] SerializePacket(Packet msg);
        int ServiceID { get; set; }
        void SetLastUDPACKReceived(DateTime when);
        bool ShuttingDown { get; }
        event SocketKilledDelegate SocketKilled;
        System.Net.IPEndPoint UDPSendTarget { get; set; }
        Guid UID { get; }
        bool CanSendUDP { get; set; }
        bool BlockingMode { get; set; }
        void RegisterPacketHandler(int packetType, Action<INetworkConnection, Packet> handlerMethod);
        void RegisterPacketHandler(int packetType, int packetSubType, Action<INetworkConnection, Packet> handlerMethod);
        void RegisterStandardPacketReplyHandler(int repliedToPacketType, Action<INetworkConnection, Packet> handlerMethod);
        void RegisterStandardPacketReplyHandler(int repliedToPacketType, int repliedToPacketSubType, Action<INetworkConnection, Packet> handlerMethod);
        
        void UnregisterPacketHandler(int packetType, int packetSubType, Action<INetworkConnection, Packet> handlerMethod);
        void UnregisterStandardPacketHandler(int repliedToPacketType, Action<INetworkConnection, Packet> handlerMethod);
        void UnregisterStandardPacketHandler(int repliedToPacketType, int repliedToPacketSubType, Action<INetworkConnection, Packet> handlerMethod);

        Action<INetworkConnection, Packet> GetPacketHandlerDelegate(int packetType, int packetSubType);
        Action<INetworkConnection, Packet> GetStandardPacketReplyHandlerDelegate(int repliedToPacketType);
        Action<INetworkConnection, Packet> GetStandardPacketReplyHandlerDelegate(int repliedToPacketType, int repliedToPacketSubType);

    }
}
