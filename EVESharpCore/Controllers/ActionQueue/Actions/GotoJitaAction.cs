using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class GotoJitaAction : Base.ActionQueueAction
    {
        #region Constructors

        public GotoJitaAction()
        {
            InitializeAction = new Action(() =>
            {
                ESCache.Instance.Traveler.Destination = null;
                ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            });

            Action = new Action(() =>
            {
                try
                {
                    if (ESCache.Instance.Traveler.Destination == null)
                    {
                        Log.WriteLine("Setting destination to Jita.");
                        ESCache.Instance.Traveler.Destination = new DockableLocationDestination(60003760);
                        ESCache.Instance.Traveler.SetStationDestination(60003760);
                    }

                    if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        try
                        {
                            Log.WriteLine("Traveller process state.");
                            ESCache.Instance.Traveler.ProcessState();
                        }
                        catch (Exception exception)
                        {
                            Log.WriteLine(exception.ToString());
                        }

                        Log.WriteLine("Requeue action.");
                        QueueAction();
                        return;
                    }

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Log.WriteLine("Traveller at dest.");
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        ESCache.Instance.Traveler.Destination = null;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });
        }

        #endregion Constructors
    }
}