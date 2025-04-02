extern alias SC;

using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public class PythonCallHook : BaseCoreHook
    {
        private const string MemManDllName = "MemMan.dll";
        private DirectEve _directEve;

        public override void Configure(DirectEve directEve)
        {
            _directEve = directEve;
        }

        private static CallbackDelegate dele;

        public override void Setup()
        {
            Util.LoadLibrary(MemManDllName);
            dele = new CallbackDelegate(OnDamageMessagesCallBack);
            var callbackPtr = Marshal.GetFunctionPointerForDelegate(dele);
            RegisterCallback("eve.client.script.environment.godma", "OnDamageMessages", callbackPtr);
        }

        public override void Teardown()
        {
            UnregisterCallback("eve.client.script.environment.godma", "OnDamageMessages");
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void CallbackDelegate(IntPtr self, IntPtr args, IntPtr kvargs);


        public void OnDamageMessagesCallBack(IntPtr self, IntPtr args, IntPtr kvargs)
        {
            try
            {
                //using (var pySharp = new PySharp(false))
                //{
                //    var resultObj = new PyObject(pySharp, args, false);
                //    var k = resultObj.ToList().FirstOrDefault().ToList().FirstOrDefault().ToList()[1].ToDictionary<string>();
                //    foreach (var item in k)
                //    {
                //        item.Value.GetValue(out var obj, out var type);
                //        _directEve.Log($"key {item.Key} value {obj}");
                //    }
                //    //_directEve.Log($"{k.LogObject()}");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        //[DllImport(MemManDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "#1")]
        [DllImport(MemManDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int RegisterCallback(string module, string function, IntPtr ptr);

        [DllImport(MemManDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UnregisterCallback(string module, string function);

        [DllImport(MemManDllName, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int UnregisterAllCallbacks();

    }
}
