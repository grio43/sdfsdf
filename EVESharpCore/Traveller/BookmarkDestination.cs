extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Traveller
{
    public class BookmarkDestination : TravelerDestination
    {
        #region Constructors

        public BookmarkDestination(DirectBookmark bookmark)
        {
            if (bookmark == null)
            {
                Log.WriteLine("Invalid bookmark destination!");

                SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                BookmarkId = -1;
                return;
            }

            Log.WriteLine("Destination set to bookmark [" + bookmark.Title + "]");
            BookmarkId = bookmark.BookmarkId ?? -1;

            if (bookmark.TypeId == (int)TypeID.SolarSystem && bookmark.ItemId.HasValue)
            {
                Log.WriteLine("Bookmark is a solar system bookmark.");
                SolarSystemId = bookmark.ItemId.Value;
            }
            else
            {
                SolarSystemId = bookmark.LocationId ?? -1;
            }

            Log.WriteLine($"BookmarkId {BookmarkId} SolarSystemId {SolarSystemId}");
        }

        #endregion Constructors

        #region Properties

        public long BookmarkId { get; set; }

        #endregion Properties

        #region Methods

        public override bool PerformFinalDestinationTask(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            var bookmark = ESCache.Instance.DirectEve.Bookmarks.FirstOrDefault(b => b.BookmarkId == BookmarkId);
            var arrived = PerformFinalDestinationTask(bookmark, 150000, finalWarpDistance, randomFinalWarpdDistance);
            return arrived;
        }

        internal static bool PerformFinalDestinationTask(DirectBookmark bookmark, int warpDistance, double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            // The bookmark no longer exists, assume we are not there
            if (bookmark == null)
                return false;

            // Is this a station bookmark?
            if (bookmark.ItemId.HasValue && (bookmark.BookmarkType == BookmarkType.Station
                                             || bookmark.BookmarkType == BookmarkType.Citadel))
            {
                var arrived = DockableLocationDestination.PerformFinalDestinationTask(bookmark.ItemId.Value);
                if (arrived)
                    Log.WriteLine("Arrived at bookmark [" + bookmark.Title + "]");

                return arrived;
            }

            if (ESCache.Instance.InDockableLocation)
            {
                // We have arrived
                if (bookmark.ItemId.HasValue && (bookmark.ItemId == ESCache.Instance.DirectEve.Session.StationId
                                                 || bookmark.ItemId == ESCache.Instance.DirectEve.Session.Structureid))
                    return true;

                // We are in a station, but not the correct station!
                if (DateTime.UtcNow > ESCache.Instance.Time.NextUndockAction)
                {
                    Log.WriteLine($"Undock");
                    Undock();
                    return false;
                }

                return false;
            }

            if (ESCache.Instance.InWarp)
            {
                Log.WriteLine("In warp to bookmark.");
                return false;
            }

            if (!ESCache.Instance.InSpace)
                return false;

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            _undockAttempts = 0;

            // This bookmark has no x / y / z, assume we are there.
            if (bookmark.X == -1 || bookmark.Y == -1 || bookmark.Z == -1 || bookmark.X == null || bookmark.Y == null || bookmark.Z == null)
            {
                Log.WriteLine("Arrived at the bookmark [" + bookmark.Title + "][No XYZ]");
                return true;
            }

            var distance = ESCache.Instance.DirectEve.Me.DistanceFromMe(bookmark.X ?? 0, bookmark.Y ?? 0, bookmark.Z ?? 0);
            if (distance < warpDistance)
            {
                Log.WriteLine("Arrived at the bookmark [" + bookmark.Title + "]");
                return true;
            }

            if (_nextTravelerDestinationAction > DateTime.UtcNow)
                return false;

            if (Math.Round(distance / 1000) < (int)Distances.MaxPocketsDistanceKm && ESCache.Instance.AccelerationGates.Count() != 0)
            {
                Log.WriteLine("Warp to bookmark in same pocket requested but acceleration gate found delaying.");
                return true;
            }

            var nameOfBookmark = "Encounter";
            if (ESCache.Instance.MissionSettings.MissionWarpAtDistanceRange != 0 && bookmark.Title.Contains(nameOfBookmark))
            {
                if (bookmark.WarpTo(ESCache.Instance.MissionSettings.MissionWarpAtDistanceRange * 1000))
                {
                    Log.WriteLine("Warping to bookmark [" + bookmark.Title + "][" + " At " +
                                  ESCache.Instance.MissionSettings.MissionWarpAtDistanceRange + " km]");
                    return false;
                }
            }
            else
            {

                var range = 0d;
                List<float> warpRanges = new List<float>() {
                            //10_000,
                            20_000,
                            30_000,
                            50_000,
                            70_000,
                            100_000,
                        };

                var randomRange = ListExtensions.Random(warpRanges);
                if (randomRange < 0 || randomRange > 100_000)
                    randomRange = 0;

                if (finalWarpDistance != 0)
                    range = finalWarpDistance;

                if (randomFinalWarpdDistance)
                    range = randomRange;

                if (bookmark.WarpTo(range))
                {
                    Log.WriteLine("Warping to bookmark [" + bookmark.Title + "][" +
                                  Math.Round(distance / 1000 / 149598000, 2) + $" AU away] at Range [{range}]");
                    return false;
                }
            }

            return false;
        }

        #endregion Methods
    }
}