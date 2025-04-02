extern alias SC;

using SC::EasyHook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework
{
    public class DirectHooking : IDisposable
    {
        private DirectEve _directEve;

        private List<DirectHook> _activeHooks = new List<DirectHook>();
        private object _lock = new object();

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr PyDelegate(IntPtr self, IntPtr args);

        public DirectHooking(DirectEve d)
        {
            _directEve = d;
        }

        public bool RemoveHook(DirectHook directHook)
        {
            lock (_lock)
            {
                try
                {
                    directHook.Hook?.Dispose();
                    return _activeHooks.Remove(directHook);
                }
                catch (Exception ex)
                {
                    _directEve.Log($"Unable to Unhook at {directHook.Hook.HookBypassAddress}, {ex}");
                }
                return false;
            }
        }

        public DirectHook CreateNewHook<TDelegate>(string module, string functionName, TDelegate detour)
            where TDelegate : Delegate
        {
            try
            {
                var modulePtr = NativeAPI.GetModuleHandle(module);
                var address = LocalHook.GetProcAddress(module, functionName);
                return CreateNewHook(address, detour);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"------------- Unable to Hook at  module {module} functionName {functionName}, {ex}");
                _directEve.Log($"-------------- Unable to Hook at  module {module} functionName {functionName}, {ex}");
                return null;
            }
        }

        public DirectHook CreateNewHook<TDelegate>(IntPtr funcAddr, TDelegate detour)
            where TDelegate : Delegate
        {
            lock (_lock)
            {
                try
                {
                    var hook = LocalHook.Create(
                        funcAddr,
                        detour,
                        this);

                    var origFunc = Marshal.GetDelegateForFunctionPointer<TDelegate>(funcAddr);
                    hook.ThreadACL.SetExclusiveACL(new Int32[] { });

                    return new DirectHook(hook, origFunc);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"------------- Unable to Hook at {funcAddr}, {ex}");
                    _directEve.Log($"------------- Unable to Hook at {funcAddr}, {ex}");
                    return null;
                }
            }
        }

        public bool RemoveAll()
        {
            var hooks = _activeHooks.AsEnumerable().Reverse().ToList();
            var allUnhooked = true;
            foreach (var hook in hooks)
            {
                if (!RemoveHook(hook))
                {
                    allUnhooked = false;
                }
            }
            return allUnhooked;
        }

        public void Dispose()
        {
            RemoveAll();
        }
    }

    public class DirectHook
    {
        public LocalHook Hook { get; private set; }
        public Delegate OrigionalTrampoline { get; private set; }

        public DirectHook(
            LocalHook hook,
            Delegate _origionalTrampoline)
        {
            this.Hook = hook;
            this.OrigionalTrampoline = _origionalTrampoline;
        }
    }
}
