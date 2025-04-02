extern alias SC;
using EVESharpCore.Cache;
using SC::SharedComponents.SharedMemory;
using SC::SharedComponents.EveMarshal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public class PacketRecvHook : BaseCoreHook
    {
        private DirectEve _directEve;
        private DirectHook _hook;

        public static event OnPacketRecvHandler OnPacketRecv;
        public delegate void OnPacketRecvHandler(byte[] packetBytes);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate void RecvHookDele(IntPtr byteArrayPtr, int length);

        [DllImport("MemMan.dll")]
        public static extern void RecvPacket(IntPtr byteArrayPtr, int length);

        public override void Setup()
        {
            var recvFuncPtr = new SharedArray<IntPtr>(Process.GetCurrentProcess().Id + nameof(UsedSharedMemoryNames.RecvFuncPointer));
            Debug.WriteLine("---- RecvFuncPtr: " + recvFuncPtr[0].ToString("X8"));
            _hook = _directEve.Hooking.CreateNewHook(recvFuncPtr[0], new RecvHookDele(RecvPacketDetour));
        }

        public override void Teardown()
        {
            _directEve.Hooking.RemoveHook(_hook);
            _hook = null;
        }

        public override void Configure(DirectEve directEve)
        {
            _directEve = directEve;
        }

        private void RecvPacketDetour(IntPtr byteArrayPtr, int length)
        {
            if (OnPacketRecv == null)
                return;

            if (OnPacketRecv.GetInvocationList().Length == 0)
                return;

            var _byteArray = new byte[length];
            Marshal.Copy(byteArrayPtr, _byteArray, 0, length);

            try
            {
                //var unmarshal = new Unmarshal();
                //var unmarshObj = unmarshal.Process(_byteArray, null);
                OnPacketRecv?.Invoke(_byteArray);
            }
            catch (Exception ex)
            {
                try
                {
                    OnPacketRecv?.Invoke(_byteArray);
                    Debug.WriteLine(ex);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                
            }
        }
    }
}
