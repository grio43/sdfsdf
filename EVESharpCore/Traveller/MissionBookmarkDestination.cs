using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Traveller
{
    public class MissionBookmarkDestination : TravelerDestination
    {
        #region Constructors

        public MissionBookmarkDestination(DirectAgentMissionBookmark bookmark)
        {
            if (bookmark == null)
            {
                Log.WriteLine("Invalid bookmark destination!");

                SolarSystemId = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;
                AgentId = -1;
                Title = string.Empty;
                return;
            }

            Log.WriteLine("Destination set to mission bookmark [" + bookmark.Title + "]");
            AgentId = bookmark.AgentId ?? -1;
            Title = bookmark.Title;
            SolarSystemId = bookmark.SolarSystemId ?? -1;
        }

        #endregion Constructors

        #region Properties

        public long AgentId { get; set; }

        public string Title { get; set; }

        #endregion Properties

        #region Methods

        public override bool PerformFinalDestinationTask(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            var arrived = BookmarkDestination.PerformFinalDestinationTask(GetMissionBookmark(AgentId, Title), (int)Distances.MissionWarpLimit);
            return arrived; // Mission bookmarks have a 1.000.000 distance warp-to limit (changed it to 150.000.000 as there are some bugged missions around)
        }

        private static DirectAgentMissionBookmark GetMissionBookmark(long agentId, string title)
        {
            var mission = ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == agentId).FirstOrDefault();
            if (mission == null)
                return null;

            return mission.Bookmarks.FirstOrDefault(b => b.Title.ToLower() == title.ToLower());
        }

        #endregion Methods
    }
}