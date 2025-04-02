using System;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Logging;

namespace EVESharpCore.Traveller
{
    public abstract class TravelerDestination
    {
        #region Fields

        internal static DateTime _nextTravelerDestinationAction;
        internal static int _undockAttempts;

        #endregion Fields

        #region Properties

        public long SolarSystemId { get; protected set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     This function returns true if we are at the final destination and false if the task is not yet complete
        /// </summary>
        /// <returns></returns>
        public abstract bool PerformFinalDestinationTask(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false);

        internal static void Undock()
        {
            if (ESCache.Instance.InDockableLocation && !ESCache.Instance.InSpace)
            {
                if (_undockAttempts + ESCache.Instance.RandomNumber(0, 4) > 10)
                //If we are having to retry at all there is likely something very wrong. Make it non-obvious if we do have to restart by restarting at diff intervals.
                {
                    var msg = "This is not the destination station, we have tried to undock [" + _undockAttempts +
                              "] times - and it is evidentially not working (lag?) - restarting Questor (and EVE)";
                    ESCache.Instance.ExitEve(msg);
                    return;
                }

                if (DateTime.UtcNow > ESCache.Instance.Time.NextUndockAction)
                {
                    Log.WriteLine("Exiting station");
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                    ESCache.Instance.Time.LastUndockAction = DateTime.UtcNow;
                    _undockAttempts++;
                    return;
                }

                if (DebugConfig.DebugTraveler)
                    Log.WriteLine("LastInSpace is more than 45 sec old (we are docked), but NextUndockAction is still in the future [" +
                                  ESCache.Instance.Time.NextUndockAction.Subtract(DateTime.UtcNow).TotalSeconds + "seconds]");

                // We are not UnDocked yet
                return;
            }
        }

        #endregion Methods
    }
}