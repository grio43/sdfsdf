using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class WndEventLogTriggerAction : Base.ActionQueueAction
    {

        private int counter = 0;
        private Random _random = new Random();

        // switching a tab on the journal causes grpc events // wnd events
        public WndEventLogTriggerAction()
        {
            this.Action = () =>
            {
                var jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();
                if (jw == null)
                {

                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                    return;
                }

                var val = counter % 3;

                if (val == 0)
                {
                    if (jw.SelectedMainTab != MainTab.AgentMissions)
                    {
                        Log.WriteLine("Switching main tab to MainTab.AgentMissions.");
                        jw.SwitchMaintab(MainTab.AgentMissions);
                    }
                }
                else if (val == 1)
                {
                    if (jw.SelectedMainTab != MainTab.Research)
                    {
                        Log.WriteLine("Switching main tab to MainTab.Research.");
                        jw.SwitchMaintab(MainTab.Research);
                    }

                }
                else
                {
                    if (jw.SelectedMainTab != MainTab.IncursionLPs)
                    {
                        Log.WriteLine("Switching main tab to MainTab.IncursionLPs.");
                        jw.SwitchMaintab(MainTab.IncursionLPs);
                    }

                }

                counter++;
                QueueAction(_random.Next(1000, 2500));

            };
        }
    }
}
