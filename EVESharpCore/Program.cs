extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py.D3DDetour;
using SC::SharedComponents.Utility;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;

namespace EVESharpCore
{
    public static class Program
    {
        #region Fields

        private static ThreadExceptionEventHandler tHandler = new ThreadExceptionEventHandler(Application_ThreadException);

        #endregion Fields

        #region Properties

        public static EVESharpCoreForm EveSharpCoreFormInstance { get; private set; }

        public static bool IsShuttingDown { get; set; }

        #endregion Properties

        #region Methods

        public static void Main(string[] args)
        {
            try
            {
                
                AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
                Application.ThreadException += tHandler;

                Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

                ESCache.Instance.CharName = args[0];
                WCFClient.Instance.pipeName = args[1];
                Log.WriteLine("Starting E# Core.");

                System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.Batch;

                try
                {
                    WCFClient.Instance.GetPipeProxy.Ping();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

                if (ESCache.Instance.EveAccount == null || args.Length != 2)
                {
                    Util.TaskKill(Process.GetCurrentProcess().Id);
                    return;
                }

                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.AccountName) || string.IsNullOrEmpty(ESCache.Instance.EveAccount.CharacterName))
                    return;

                //ESCache.LoadDirectEVEInstance();

                EveSharpCoreFormInstance = new EVESharpCoreForm();
                Log.WriteLine("Launching EVESharpCoreForm");

                Application.Run(EveSharpCoreFormInstance);
                Console.WriteLine("Exiting.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception [" + ex + "]");
            }

            Console.WriteLine("EVESharpCore is terminating.");
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
            Application.ThreadException -= tHandler;
        }

        private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            HandleException(e.Exception);
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException((Exception)e.ExceptionObject);
        }

        private static void HandleException(Exception e)
        {
            Console.WriteLine(e);
            Debug.WriteLine(e);
        }

        #endregion Methods
    }
}