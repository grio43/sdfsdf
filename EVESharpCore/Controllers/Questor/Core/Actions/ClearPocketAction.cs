using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void ClearPocketAction(Action action)
        {
            if (!ESCache.Instance.NormalApproach)
                ESCache.Instance.NormalApproach = true;

            // Get lowest range
            if (!int.TryParse(action.GetParameterValue("distance"), out var DistanceToClear))
                DistanceToClear = (int)ESCache.Instance.Combat.MaxRange;

            if (DistanceToClear != 0 && DistanceToClear != int.MinValue && DistanceToClear != int.MaxValue)
                DistanceToClear = (int)Distances.OnGridWithMe;

            //panic handles adding any priority targets and combat will prefer to kill any priority targets

            //If the closest target is out side of our max range, combat cant target, which means GetBest cant return true, so we are going to try and use potentialCombatTargets instead
            if (ESCache.Instance.Combat.PotentialCombatTargets.Any())
            {
                //we may be too far out of range of the closest target to get combat to kick in, lets move us into range here
                EntityCache ClosestPotentialCombatTarget = null;

                if (DebugConfig.DebugClearPocket)
                    Log.WriteLine("Cache.Instance.__GetBestWeaponTargets(DistanceToClear);");

                if (ESCache.Instance.Combat.GetBestPrimaryWeaponTarget(DistanceToClear))
                    _clearPocketTimeout = null;
                //
                // grab the preferredPrimaryWeaponsTarget if its defined and exists on grid as our navigation point
                //
                if (ESCache.Instance.Combat.PreferredPrimaryWeaponTargetID != null && ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null)
                    if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Distance < (double)Distances.OnGridWithMe)
                    {
                        if (DebugConfig.DebugClearPocket)
                            Log.WriteLine("ClosestPotentialCombatTarget = Combat.PreferredPrimaryWeaponTarget [" +
                                          ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Name + "]");
                        ClosestPotentialCombatTarget = ESCache.Instance.Combat.PreferredPrimaryWeaponTarget;
                    }

                //
                // retry to use PreferredPrimaryWeaponTarget
                //
                if (ClosestPotentialCombatTarget == null && ESCache.Instance.Combat.PreferredPrimaryWeaponTargetID != null &&
                    ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null)
                    if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Distance < (double)Distances.OnGridWithMe)
                    {
                        if (DebugConfig.DebugClearPocket)
                            Log.WriteLine("ClosestPotentialCombatTarget = Combat.PreferredPrimaryWeaponTarget [" +
                                          ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Name + "]");
                        ClosestPotentialCombatTarget = ESCache.Instance.Combat.PreferredPrimaryWeaponTarget;
                    }

                if (ClosestPotentialCombatTarget == null) //otherwise just grab something close (excluding sentries)
                {
                    if (ESCache.Instance.Combat.PotentialCombatTargets.Any())
                        if (ESCache.Instance.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).FirstOrDefault() != null)
                        {
                            var closestPCT = ESCache.Instance.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).FirstOrDefault();
                            if (closestPCT != null)
                                if (DebugConfig.DebugClearPocket)
                                    Log.WriteLine(
                                        "ClosestPotentialCombatTarget = Combat.PotentialCombatTargets.OrderBy(t => t.Nearest5kDistance).FirstOrDefault(); [" +
                                        closestPCT.Name + "]");
                        }

                    ClosestPotentialCombatTarget = ESCache.Instance.Combat.PotentialCombatTargets.OrderBy(t => t.Distance).FirstOrDefault();
                }

                if (ClosestPotentialCombatTarget != null &&
                    (ClosestPotentialCombatTarget.Distance > ESCache.Instance.Combat.MaxRange || !ClosestPotentialCombatTarget.IsInOptimalRange))
                    if (!ClosestPotentialCombatTarget.IsOrbitedByActiveShip && !ClosestPotentialCombatTarget.IsApproachedOrKeptAtRangeByActiveShip)
                        ESCache.Instance.NavigateOnGrid.NavigateIntoRange(ClosestPotentialCombatTarget, "combatMissionControl", true);

                _clearPocketTimeout = null;
            }

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue) _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value) return;

            // We have cleared the Pocket, perform the next action \o/ - reset the timers that we had set for actions...
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        #endregion Methods
    }
}