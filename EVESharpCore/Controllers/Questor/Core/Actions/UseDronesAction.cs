using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions.Base;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void UseDronesAction(Action action)
        {
            if (!bool.TryParse(action.GetParameterValue("use"), out var usedrones))
                usedrones = false;

            if (!usedrones)
            {
                Log.WriteLine("Disable launch of drones");
                ESCache.Instance.MissionSettings.PocketUseDrones = false;
            }
            else
            {
                Log.WriteLine("Enable launch of drones");
                ESCache.Instance.MissionSettings.PocketUseDrones = true;
            }

            Nextaction();
            return;
        }

        #endregion Methods
    }
}