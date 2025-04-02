using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void WaitUntilTargetedAction(Action action)
        {
            var targetedBy = ESCache.Instance.Combat.TargetedBy;
            if (targetedBy != null && targetedBy.Any())
            {
                Log.WriteLine("We have been targeted!");

                // We have been locked, go go go ;)
                _waiting = false;
                Nextaction();
                return;
            }

            // Default timeout is 30 seconds
            int timeout;
            if (!int.TryParse(action.GetParameterValue("timeout"), out timeout))
                timeout = 30;

            if (_waiting)
            {
                if (DateTime.UtcNow < _waitingSince.AddSeconds(timeout))
                    return;

                Log.WriteLine("Nothing targeted us within [ " + timeout + "sec]!");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                Nextaction();
                return;
            }

            Log.WriteLine("Nothing has us targeted yet: waiting up to [ " + timeout + "sec] starting now.");
            // Start waiting
            _waiting = true;
            _waitingSince = DateTime.UtcNow;
            return;
        }

        #endregion Methods
    }
}