using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void AggroOnlyAction(Action action)
        {
            if (ESCache.Instance.NormalApproach)
                ESCache.Instance.NormalApproach = false;

            // Get lowest range
            if (!int.TryParse(action.GetParameterValue("distance"), out var DistanceToClear))
                DistanceToClear = (int)Distances.OnGridWithMe;

            if (DistanceToClear != 0 && DistanceToClear != int.MinValue && DistanceToClear != int.MaxValue)
                DistanceToClear = (int)Distances.OnGridWithMe;

            //
            // the important bit is here... Adds target to the PrimaryWeapon or Drone Priority Target Lists so that they get killed (we basically wait for combat.cs to do that before proceeding)
            //
            if (ESCache.Instance.Combat.GetBestPrimaryWeaponTarget(DistanceToClear, ESCache.Instance.Combat.CombatTargets.Where(t => t.IsTargetedBy).ToList()))
                _clearPocketTimeout = null;

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
                return;

            Log.WriteLine("is complete: no more targets that are targeting us");
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        #endregion Methods
    }
}