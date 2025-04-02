using System;
using EVESharpCore.Cache;
using EVESharpCore.Logging;

namespace EVESharpCore.Traveller
{
    public class SolarSystemDestination : TravelerDestination
    {
        #region Constructors

        public SolarSystemDestination(long solarSystemId)
        {
            Log.WriteLine("Destination set to solar system id [" + solarSystemId + "]");
            SolarSystemId = solarSystemId;
        }

        #endregion Constructors

        #region Methods

        public override bool PerformFinalDestinationTask(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            // The destination is the solar system, not the station in the solar system.
            if (ESCache.Instance.InDockableLocation && !ESCache.Instance.InSpace)
            {
                if (_nextTravelerDestinationAction < DateTime.UtcNow)
                {
                    Undock();
                    return false;
                }

                // We are not there yet
                return false;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            // The task was to get to the solar system, we're there :)
            Log.WriteLine("Arrived in system");
            return true;
        }

        #endregion Methods
    }
}