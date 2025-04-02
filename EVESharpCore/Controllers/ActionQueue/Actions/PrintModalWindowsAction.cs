using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using System.Linq;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class PrintModalWindowsAction : Base.ActionQueueAction
    {
        #region Constructors

        public PrintModalWindowsAction()
        {
            Action = new Action(() =>
            {
                try
                {
                    if (ESCache.Instance.DirectEve.ModalWindows == null || !ESCache.Instance.DirectEve.ModalWindows.Any())
                    {
                        Log.WriteLine("PrintModalWindowsActionQueueAction: DirectEve.ModalWindows returned null or empty");
                        return;
                    }

                    Log.WriteLine("Checking Each window in DirectEve.ModalWindows");

                    var windowNum = 0;
                    foreach (var window in ESCache.Instance.DirectEve.ModalWindows)
                    {
                        windowNum++;
                        Log.WriteLine("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                        Log.WriteLine("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                        Log.WriteLine("[" + windowNum + "] Debug_Window.Type: [" + window.Guid + "]");
                        Log.WriteLine("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                        Log.WriteLine("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                        Log.WriteLine("--------------------------------------------------");
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: " + ex);
                    return;
                }
            });
        }

        #endregion Constructors
    }
}