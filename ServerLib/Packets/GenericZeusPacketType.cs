using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shared
{
    public enum GenericZeusPacketType
    {
        RequestConfigListing = 1,
        SaveConfigListing = 2,
        RequestUserOverview = 3,
        RequestSearchUsers = 4,
        RequestCreateNewUser = 5,
        RequestCommandOverview = 6,
        ExecuteCommand = 7,
        RequestUserDetail = 8,
        AddServiceNote = 9,
        RequestCharacterDetail = 10,
        RequestLogOverview = 11,
        RequestLogs = 12,
        RequestPerfMonStart = 13,
        RequestPerfMonCounterOverview = 14,
        RequestPerfMonCounterData = 15,
        RequestServiceOverview = 16,
        RequestChangeServiceState = 17        
    }
}
