using System;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void ClearWithinWeaponsRangeOnlyAction(Action action)
        {
            // Get lowest range
            int DistanceToClear;
            if (!int.TryParse(action.GetParameterValue("distance"), out DistanceToClear))
                DistanceToClear = (int)ESCache.Instance.Combat.MaxRange - 1000;

            if (DistanceToClear == 0 || DistanceToClear == -2147483648 || DistanceToClear == 2147483647)
                DistanceToClear = (int)Distances.OnGridWithMe;

            //
            // note this WILL clear sentries within the range given... it does NOT respect the KillSentries setting. 75% of the time this wont matter as sentries will be outside the range
            //

            if (ESCache.Instance.Combat.GetBestPrimaryWeaponTarget(DistanceToClear))
                _clearPocketTimeout = null;

            // Do we have a timeout?  No, set it to now + 5 seconds
            if (!_clearPocketTimeout.HasValue)
                _clearPocketTimeout = DateTime.UtcNow.AddSeconds(5);

            // Are we in timeout?
            if (DateTime.UtcNow < _clearPocketTimeout.Value)
                return;

            Log.WriteLine("is complete: no more targets in weapons range");
            Nextaction();

            // Reset timeout
            _clearPocketTimeout = null;
            return;
        }

        #endregion Methods
    }
}