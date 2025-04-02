extern alias SC;

using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class MonitorEntityAction : ActionQueueAction
    {
        #region Constructors

        public MonitorEntityAction(Func<DirectEntity> getEntity)
        {
            ExecuteEveryFrame = true;
            double prevStructurePerc = 0d;
            Action = new Action(() =>
            {
                try
                {
                    var obj = getEntity();
                    if (obj.IsValid)
                    {
                        var ent = getEntity();
                        if (prevStructurePerc != ent.StructurePct)
                        {
                            prevStructurePerc = ent.StructurePct;
                            Log.WriteLine($"New structure value {ent.StructurePct}");
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