using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public class CoreHookManager
    {
        private DirectEve _directEve;

        private Dictionary<Type, BaseCoreHook> _coreHooks = new Dictionary<Type, BaseCoreHook>();

        public CoreHookManager(DirectEve directEve)
        {
            _directEve = directEve;
        }

        public void RegisterDefaults()
        {
            //Register<CreateProcessWHook>();
            //Register<PythonCallHook>();
            Register<PacketRecvHook>();
            Register<PacketSendHook>();
        }

        public TCoreHook Register<TCoreHook>()
            where TCoreHook : BaseCoreHook, new()
        {
            if (_coreHooks.TryGetValue(typeof(TCoreHook), out var alreadyRegistered))
            {
                // Already registered??
                return alreadyRegistered as TCoreHook;
            }

            _directEve.Log($"Registering CoreHook {typeof(TCoreHook).Name}");
            Console.WriteLine($"Registering CoreHook {typeof(TCoreHook).Name}");
            var hook = new TCoreHook();
            
            hook.Configure(_directEve);
            hook.Setup();

            _coreHooks[typeof(TCoreHook)] = hook;
            return hook;
        }

        public void CleanupAll()
        {
            foreach (var entry in _coreHooks.ToList())
            {
                _coreHooks.Remove(entry.Key);
                entry.Value.Dispose();
            }
        }

        public TCoreHook Query<TCoreHook>()
            where TCoreHook : BaseCoreHook
        {
            _coreHooks.TryGetValue(typeof(TCoreHook), out var hook);
            return hook as TCoreHook;
        }
    }
}
