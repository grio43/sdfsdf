extern alias SC;

using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    //example: new MonitorPyObjectAction(() => QCache.Instance.ActiveShip.Entity.Ball, new List<string>(){ "__dict__", "__iroot__", "modelLoadSignal" }).Initialize().QueueAction();
    public class MonitorPyObjectAction : ActionQueueAction
    {
        #region Constructors

        public MonitorPyObjectAction(Func<PyObject> getPyObj, List<string> excludeLines)
        {
            ExecuteEveryFrame = true;
            var previousValue = string.Empty;
            Action = new Action(() =>
            {
                try
                {
                    var obj = getPyObj();
                    if (obj.IsValid)
                    {
                        var currentValue = obj.LogObject();
                        if (!previousValue.Equals(currentValue))
                        {
                            var previousLines = previousValue.Split('\n');
                            var currentLines = currentValue.Split('\n');
                            var diff = currentLines.Except(previousLines).ToList();
                            diff.RemoveAll(k => excludeLines.Any(e => k.Contains(e)));
                            previousValue = currentValue;
                            if (diff.Any())
                            {
                                Log.WriteLine($"-------------------");
                                foreach (var l in diff) Log.WriteLine(l);
                                Log.WriteLine($"-------------------");
                            }
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Obj is not valid.");
                    }

                    if (obj != null && obj.IsValid)
                        QueueAction();
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