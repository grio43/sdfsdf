using EVESharpCore.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Hooks
{
    public class CreateProcessWHook : BaseCoreHook
    {
        private DirectEve _directEve;
        private DirectHook _hook;

        public event OnCreateProcessEventhandler OnCreateProcess;
        public delegate void OnCreateProcessEventhandler(string applicationName, string commandLine, string currentDirectory);

        public override void Setup()
        {
            _hook = _directEve.Hooking.CreateNewHook("kernel32.dll", "CreateProcessW", new CreateProcessDelegate(CreateProcessDetour));
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

        private bool CreateProcessDetour(
                 [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string LpApplicationName,
                 [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCommandLine,
                 [InAttribute()] IntPtr lpProcessAttributes,
                 [InAttribute()] IntPtr lpThreadAttributes,
                 [MarshalAsAttribute(UnmanagedType.Bool)] bool bInheritHandles,
                 uint dwCreationFlags,
                 [InAttribute()] IntPtr lpEnvironment,
                 [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCurrentDirectory,
                 [InAttribute()] ref STARTUPINFOW lpStartupInfo,
                 [InAttribute()] ref PROCESS_INFORMATION lpProcessInformation)
        {
            var msg = $"CreateProcW lpApplicationName {LpApplicationName} lpCommandLine {lpCommandLine} lpCurrentDirectory {lpCurrentDirectory}";
            //_directEve.Log(msg);

            OnCreateProcess?.Invoke(LpApplicationName, lpCommandLine, lpCurrentDirectory);

            /*
            var ret = CreateProcessW(LpApplicationName, lpCommandLine, lpProcessAttributes, lpThreadAttributes,
                bInheritHandles, dwCreationFlags, lpEnvironment, lpCurrentDirectory, ref lpStartupInfo, ref lpProcessInformation);*/
            return false;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct STARTUPINFOW
        {
            public uint cb;
            [MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string lpReserved;
            [MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string lpDesktop;
            [MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.LPWStr)]
            public string lpTitle;
            public uint dwX;
            public uint dwy;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public ushort wShowWindow;
            public ushort cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }


        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate bool CreateProcessDelegate(
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string LpApplicationName,
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCommandLine,
            [InAttribute()] IntPtr lpProcessAttributes,
            [InAttribute()] IntPtr lpThreadAttributes,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            [InAttribute()] IntPtr lpEnvironment,
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            [InAttribute()] ref STARTUPINFOW lpStartupInfo,
            [InAttribute()] ref PROCESS_INFORMATION lpProcessInformation);



        [DllImportAttribute("kernel32.dll", EntryPoint = "CreateProcessW")]
        [return: MarshalAsAttribute(UnmanagedType.Bool)]
        public static extern bool CreateProcessW(
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string LpApplicationName,
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCommandLine,
            [InAttribute()] IntPtr lpProcessAttributes,
            [InAttribute()] IntPtr lpThreadAttributes,
            [MarshalAsAttribute(UnmanagedType.Bool)] bool bInheritHandles,
            uint dwCreationFlags,
            [InAttribute()] IntPtr lpEnvironment,
            [InAttribute()][MarshalAsAttribute(UnmanagedType.LPWStr)] string lpCurrentDirectory,
            [InAttribute()] ref STARTUPINFOW lpStartupInfo,
            [InAttribute()] ref PROCESS_INFORMATION lpProcessInformation);


    }
}
