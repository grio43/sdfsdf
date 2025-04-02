extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.SharedMemory;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    public class DirectSharedMemory
    {
        private Random _rnd;
        private DirectEve _directEve;

        public DirectSharedMemory(DirectEve directEve)
        {
            _directEve = directEve;
            _rnd = new Random();
        }
    }
}
