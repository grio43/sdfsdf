using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void MoveToBackgroundAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            Log.WriteLine("MoveToBackgroundAction.");

            if (ESCache.Instance.NormalApproach)
                ESCache.Instance.NormalApproach = false;

            ESCache.Instance.NormalNavigation = false;

            if (!int.TryParse(action.GetParameterValue("distance"), out var DistanceToApproach))
                DistanceToApproach = (int)Distances.GateActivationRange;

            var target = action.GetParameterValue("target");
            var alternativeTarget = action.GetParameterValue("alternativetarget");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            if (string.IsNullOrEmpty(alternativeTarget))
                alternativeTarget = "Acceleration Gate";

            IEnumerable<EntityCache> targets = ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == target.ToLower()).ToList().ToList();

            if (!targets.Any())
            {
                Log.WriteLine("First target not found, using alternative target [" + alternativeTarget + "]");
                targets = ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == alternativeTarget.ToLower()).ToList().ToList();
            }

            if (!targets.Any())
            {
                // Unlike activate, no target just means next action
                Nextaction();
                return;
            }

            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault();



            if (!ESCache.Instance.DirectEve.ActiveShip.CanWeMove)
            {
                Nextaction();
                Log.WriteLine("We can't move, skipping move to background action.");
                return;
            }

            if (closest != null)
            {
                if (closest.IsApproachedOrKeptAtRangeByActiveShip)
                {
                    Nextaction();
                    Log.WriteLine($"Mode {ESCache.Instance.ActiveShip.Entity.Mode} FollowEnt {ESCache.Instance.ActiveShip.FollowingEntity.Name}");
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(3);
                    return;
                }
                // Move to the target
                if (closest.KeepAtRange(DistanceToApproach))
                {
                    Log.WriteLine("Approaching target [" + closest.Name + "][" + closest.DirectEntity.Id.ToString() + "][" + Math.Round(closest.Distance / 1000, 0) +
                                  "k away]");
                    _nextCombatMissionCtrlAction = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                return;
            }

            return;
        }

        #endregion Methods
    }
}