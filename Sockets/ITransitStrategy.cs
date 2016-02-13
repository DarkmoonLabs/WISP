using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    /// <summary>
    ///  Describes the interface for a packet sending/receiving algorithm.
    /// </summary>
    public interface ITransitStrategy
    {
        INetworkConnection OwningConnection { get; set; }
        int Send(Shared.Packet msg);
        int Send(byte[] data, Shared.PacketFlags flags);
        void OnPacketReceiptACK(PacketACK msg);
        void InitTCP();
        void InitUDP();
        void BeforeShutdown();
        void AfterShutdown();
        DateTime LastUDPACKReceived { get; set; }
        int NumAcksWaitingFor { get; }
        bool ListenForDataOnSocket();
        void ProcessSend();
        bool ProcessReceive();
        bool HasQueuedPackets { get; }
    }
}
