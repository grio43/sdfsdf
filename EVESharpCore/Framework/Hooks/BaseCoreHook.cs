using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public abstract class BaseCoreHook : IDisposable
    {
        public abstract void Configure(DirectEve directEve);
        public abstract void Setup();
        public abstract void Teardown();

        public void Dispose() => Teardown();
    }
}
