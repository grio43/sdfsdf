using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void DoneAction()
        {
            // Tell the drones module to retract drones
            ESCache.Instance.Drones.IsMissionPocketDone = true;
            ESCache.Instance.MissionSettings.MissionUseDrones = null;

            if (ESCache.Instance.Drones.ActiveDrones.Any())
            {
                if (DebugConfig.DebugDoneAction)
                    Log.WriteLine("We still have drones out! Wait for them to return.");
                return;
            }

            foreach (var e in ESCache.Instance.EntitiesOnGrid.Where(e => ESCache.Instance.Statistics.BountyValues.TryGetValue(e.Id, out var val) && val > 0))
                ESCache.Instance.Statistics.BountyValues.Remove(e.Id);
            ESCache.Instance.State.CurrentActionControlState = ActionControlState.Done;
            if (DebugConfig.DebugDoneAction)
                Log.WriteLine("we are ready and have set [ _States.CurrentCombatMissionCtrlState = CombatMissionCtrlState.Done ]");
            return;
        }

        #endregion Methods
    }
}