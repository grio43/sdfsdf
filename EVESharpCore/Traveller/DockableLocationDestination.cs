using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Traveller
{
    public class DockableLocationDestination : TravelerDestination
    {
        #region Constructors

        public DockableLocationDestination(long dockableLocationId)
        {
            var dockableLocation = ESCache.Instance.DirectEve.Navigation.GetLocation(dockableLocationId);

            if (dockableLocation.IsStructureLocation)
            {
                Log.WriteLine("Destination set to [" + dockableLocation.Name + "]");
                DockableLocationId = dockableLocation.LocationId;
                StationName = dockableLocation.Name;
                SolarSystemId = dockableLocation.SolarSystemId.Value;
            }
            else
            {
                if (dockableLocation == null || !dockableLocation.ItemId.HasValue || !dockableLocation.SolarSystemId.HasValue)
                {
                    Log.WriteLine("Invalid id [" + DockableLocationId + "]");
                    SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                    DockableLocationId = -1;
                    StationName = "";
                    return;
                }
                else
                {
                    Log.WriteLine("Destination set to [" + dockableLocation.Name + "]");
                    DockableLocationId = dockableLocationId;
                    StationName = dockableLocation.Name;
                    SolarSystemId = dockableLocation.SolarSystemId.Value;
                }
            }
        }

        public DockableLocationDestination(long solarSystemId, long dockableLocationId)
        {
            Log.WriteLine("Destination set to [" + dockableLocationId + "]");
            SolarSystemId = solarSystemId;
            DockableLocationId = dockableLocationId;
        }

        #endregion Constructors

        #region Properties

        public long DockableLocationId { get; set; }

        public string StationName { get; set; }

        #endregion Properties

        #region Methods

        public override bool PerformFinalDestinationTask(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            var arrived = PerformFinalDestinationTask(DockableLocationId);
            return arrived;
        }

        internal static bool PerformFinalDestinationTask(long dockableLocationId)
        {
            if (ESCache.Instance.InDockableLocation && (ESCache.Instance.DirectEve.Session.StationId == dockableLocationId
                                              || ESCache.Instance.DirectEve.Session.Structureid == dockableLocationId))
            {
                Log.WriteLine("Arrived in station");
                return true;
            }

            if (ESCache.Instance.InDockableLocation)
            {
                if (DateTime.UtcNow > ESCache.Instance.Time.NextUndockAction) // We are in a station, but not the correct station!
                {
                    Undock();
                    return false;
                }
                return false; // We are not there yet
            }

            if (!ESCache.Instance.InSpace)
                return false;

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            var dockableLocations = ESCache.Instance.Stations.Concat(ESCache.Instance.Citadels);

            var dockableLocation = dockableLocations.Where(e => e.Id == dockableLocationId).ToList().FirstOrDefault();
            if (dockableLocation == null)
                return false;

            if (dockableLocation.Distance <= (int)Distances.DockingRange)
            {
                if (dockableLocation.Dock())
                {
                    Log.WriteLine("Dock at [" + dockableLocation.Name + "] which is [" + Math.Round(dockableLocation.Distance / 1000, 0) +
                                  "k away]");
                    return false; //we do not return true until we actually appear in the destination (station in this case)
                }
                return false;
            }

            if (dockableLocation.Distance < (int)Distances.WarptoDistance)
            {
                if (dockableLocation.MoveTo())
                    Log.WriteLine("MoveTo [" + dockableLocation.Name + "] which is [" + Math.Round(dockableLocation.Distance / 1000, 0) + "k away]");

                return false;
            }

            var dockableEntity = dockableLocation.DirectEntity;
            if (dockableEntity.IsValid)
            {
                if (ESCache.Instance.DirectEve.Bookmarks.Any(b => b.DistanceTo(dockableEntity) < 150000))
                {

                    var bm = ESCache.Instance.DirectEve.Bookmarks.FirstOrDefault(b => b.DistanceTo(dockableEntity) < 150000);
                    if (bm.WarpTo())
                    {
                        Log.WriteLine("Found a bookmark near the dockable location which seem to be a undock spot. Using that instead of the station. Warping.");
                        return false;
                    }
                }
            }

            if (dockableLocation.WarpTo())
            {
                Log.WriteLine("Warp to and dock at [" + dockableLocation.Name + "][" +
                              Math.Round(dockableLocation.Distance / 1000 / 149598000, 2) + " AU away]");
                return false;
            }

            return false;
        }

        #endregion Methods
    }
}