using System;
using System.Collections.Generic;
using System.Text;

namespace Shared
{
    /// <summary>
    /// An object that can Receive Telegrams
    /// </summary>
    public interface IMessagable
    {
        void HandleTelegram(Telegram t);
    }
}
