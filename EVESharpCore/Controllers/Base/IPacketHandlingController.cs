extern alias SC;

using SC::SharedComponents.EveMarshal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Controllers.Base
{
    internal interface IPacketHandlingController
    {
        void HandleRecvPacket(byte[] packetBytes);
        void HandleSendPacket(byte[] packetBytes);
    }
}
