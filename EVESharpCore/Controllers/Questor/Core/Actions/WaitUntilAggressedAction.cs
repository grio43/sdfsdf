using System;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void WaitUntilAggressedAction(Action action)
        {
            // Default timeout is 60 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 60;

            int WaitUntilShieldsAreThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilShieldsAreThisLow"), out WaitUntilShieldsAreThisLow))
                ESCache.Instance.MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilShieldsAreThisLow;

            int WaitUntilArmorIsThisLow;
            if (int.TryParse(action.GetParameterValue("WaitUntilArmorIsThisLow"), out WaitUntilArmorIsThisLow))
                ESCache.Instance.MissionSettings.MissionActivateRepairModulesAtThisPerc = WaitUntilArmorIsThisLow;

            if (_waiting)
            {
                if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds < timeout)
                    return;

                Log.WriteLine("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
            return;
        }

        #endregion Methods
    }
}