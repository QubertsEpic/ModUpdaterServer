using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal enum TransferTypes : int
    {
        Invalid = 0,
        Ping = 1,
        RecieveModList = 2,
        SendModList = 3,
        GetSpecificMods = 4,
        SendSpecificMods = 5,
    }
}
