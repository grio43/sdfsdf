extern alias SC;
using EVESharpCore.Controllers.Debug;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using EVESharpCore.Logging;
using SC::SharedComponents.Utility;
using System.Net;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.Py;
using SC::SharedComponents.IPC;
using System.Runtime.InteropServices;
using EVESharpCore.Controllers.Questor;
using SC::EasyHook;
using EVESharpCore.Controllers.Pinata;
using System.Linq;
using System.Threading.Tasks;

namespace EVESharpCore.Controllers
{
    public partial class DebugControllerForm : Form
    {
        #region Fields

        private DebugController _debugController;

        #endregion Fields

        #region Constructors

        public DebugControllerForm(DebugController debugController)
        {
            this._debugController = debugController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button1_Click(object sender, EventArgs e)
        {
            new DebugEntities().Show();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            new DebugSkills().Show();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new DebugScan().Show();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            new DebugUIModules().Show();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            new DebugChannels().Show();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            new DebugMap().Show();
        }

        #endregion Methods

        private void button7_Click(object sender, EventArgs e)
        {
            if (ControllerManager.Instance.TryGetController<PanicController>(out var panic))
            {
                panic.SimulatePanic = !panic.SimulatePanic;
                Log.WriteLine($"Simulate panic is now [{panic.SimulatePanic}]");
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            if (ControllerManager.Instance.TryGetController<PanicController>(out var panic))
            {
                panic.SimulateInvasion = !panic.SimulateInvasion;
                Log.WriteLine($"Simulate invasion is now [{panic.SimulateInvasion}]");
            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            Log.WriteLine($"WMI generated machine Id: [{FingerPrint.Value()}]");
        }

        private void button10_Click(object sender, EventArgs e)
        {
            DirectActiveShip.SimulateLowShields();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            new DebugMarketPlex().Show();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            ActionQueueAction action = new ActionQueueAction(() =>
            {
                Log.WriteLine("Enable full logging.");
                ESCache.Instance.DirectEve.enableFullLogging();
            });
            action.Initialize().QueueAction();
        }

        private void button13_Click(object sender, EventArgs e)
        {

            new MonitorPyObjectAction(() => ESCache.Instance.DirectEve.PySharp.Import("__builtin__").Attribute("eve").Attribute("session"), new List<string>() { "__dict__", "__iroot__", "modelLoadSignal" }).Initialize().QueueAction();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            ESCache.Instance.ForceDumpLoop = !ESCache.Instance.ForceDumpLoop;
            Log.WriteLine($"ForceDumpLoot [{ESCache.Instance.ForceDumpLoop}]");
        }

        private void button15_Click(object sender, EventArgs e)
        {
            new DebugModules().Show();
        }

        private void button16_Click(object sender, EventArgs e)
        {
            new DebugInvType().Show();
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SYSTEM_INFO
        {
            public ushort wProcessorArchitecture;
            public ushort wReserved;
            public uint dwPageSize;
            public IntPtr lpMinimumApplicationAddress;
            public IntPtr lpMaximumApplicationAddress;
            public UIntPtr dwActiveProcessorMask;
            public uint dwNumberOfProcessors;
            public uint dwProcessorType;
            public uint dwAllocationGranularity;
            public ushort wProcessorLevel;
            public ushort wProcessorRevision;
        };

        [DllImport("kernel32.dll")]
        internal static extern void GetNativeSystemInfo(ref SYSTEM_INFO lpSystemInfo);

        private void button17_Click(object sender, EventArgs e)
        {
            //SYSTEM_INFO sysInfo = new SYSTEM_INFO();
            //GetNativeSystemInfo(ref sysInfo);
            //Log.WriteLine($"dwNumberOfProcessors: [{sysInfo.dwNumberOfProcessors}]");
            //WriteToConsoleA("Hello from WriteConsoleA!\n");
            //PinataController.SendDiscordWebHookMessage("Test [XD] . [:)]");
            var acq = (ActionQueueController)ControllerManager.Instance.GetController<ActionQueueController>();
            acq.EnqueueNewAction(new ActionQueueAction(() =>
            {
                try
                {
                    ESCache.Instance.DirectEve.SendFakeInputToPreventIdle();
                }
                catch (Exception exception)
                {
                    System.Diagnostics.Debug.WriteLine(exception.ToString());
                }

            }));
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        // Constants for standard handles
        private const int STD_OUTPUT_HANDLE = -11;

        public static void WriteToConsoleA(string message)
        {
            // Get the handle to the console output
            IntPtr consoleHandle = GetStdHandle(STD_OUTPUT_HANDLE);
            if (consoleHandle == IntPtr.Zero)
            {
                Console.WriteLine("Failed to get console handle.");
                return;
            }

            // Write the message using WriteConsoleA
            uint charsWritten;
            bool result = WriteConsoleA(consoleHandle, message, (uint)message.Length, out charsWritten, IntPtr.Zero);

            if (!result)
            {
                Console.WriteLine("Failed to write to console. Error: " + Marshal.GetLastWin32Error());
            }
            else
            {
                Console.WriteLine($"Successfully wrote {charsWritten} characters to the console.");
            }
        }


        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern bool WriteConsoleA(
            IntPtr hConsoleOutput,
            string lpBuffer,
            uint nNumberOfCharsToWrite,
            out uint lpNumberOfCharsWritten,
            IntPtr lpReserved);

        private void button18_Click(object sender, EventArgs e)
        {
            new DebugBoosters().Show();
        }

        private void button19_Click(object sender, EventArgs e)
        {
            new DebugMutaplasmid().Show();
        }

        private void button20_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.ResponsiveMode = !ControllerManager.Instance.ResponsiveMode;
        }

        private void button21_Click(object sender, EventArgs e)
        {
            WCFClient.Instance.GetPipeProxy.SendBroadcastMessage(new BroadcastMessage(ESCache.Instance.CharName, "*", nameof(BackgroundWorkerController), "Hello", "World"));
        }

        private void button22_Click(object sender, EventArgs e)
        {
            new DebugBookmarks().Show();
        }

        private void button23_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                ESCache.Instance.DirectEve.SceneManager.EnableZoomHack();

            }).Initialize().QueueAction();
        }

        private void button24_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                ESCache.Instance.DirectEve.SceneManager.RemoveAllDrawnObjects();

            }).Initialize().QueueAction();
        }

        private WindowRecorderSession _windowRecorderSession = null;

        private void button25_Click(object sender, EventArgs e)
        {
            if (_windowRecorderSession == null)
            {
                _windowRecorderSession = ESCache.Instance.DirectEve.StartWindowRecording("Debug");
                if (_windowRecorderSession == null)
                {
                    _recordButton.Text = "Record (Failed)";
                    return;
                }
                _recordButton.Text = "Record (Recording)";
            }
            else
            {
                _windowRecorderSession.Stop();
                _windowRecorderSession = null;
                _recordButton.Text = "Record (Stopped)";
            }
        }

        private void button25_Click_1(object sender, EventArgs e)
        {
            new DebugCollider().Show();
        }

        private void button26_Click(object sender, EventArgs e)
        {
            new DebugEveDevTools().Show();
        }

        private void button27_Click(object sender, EventArgs e)
        {
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    //__builtin__.sm.services[invCache].inventories
                    var d = ESCache.Instance.DirectEve.GetLocalSvc("invCache").Attribute("inventories").ToDictionary();

                    Logging.Log.WriteLine($"Found [{d.Count}] inventories");
                    foreach (var kv in d)
                    {
                        //Logging.Log.WriteLine($"ID [{kv.Key.GetItemAt(0).ToLong()}] VALUE [{kv.Value.LogObject()}]");
                        Logging.Log.WriteLine($"InvID [{kv.Key.GetItemAt(0).ToLong()}]");
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button28_Click(object sender, EventArgs e)
        {
            new DebugInventory().Show();
        }

        private void button29_Click(object sender, EventArgs e)
        {
            SC::SharedComponents.Utility.MemoryOptimizer.OptimizeMemory();
        }
    }
}