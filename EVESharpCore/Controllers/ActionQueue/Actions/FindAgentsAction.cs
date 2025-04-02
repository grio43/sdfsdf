extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class FindAgentsAction : Base.ActionQueueAction
    {
        //<faction logo = ""500001"" name=""Caldari State"" damagetype=""Kinetic"" />
        //<faction logo = ""500002"" name=""Minmatar Republic"" damagetype=""Explosive"" />
        //<faction logo = ""500003"" name=""Amarr Empire"" damagetype=""EM"" />
        //<faction logo = ""500004"" name=""Gallente Federation"" damagetype=""Kinetic"" />

        #region Constructors

        public FindAgentsAction()
        {
            Action = new Action(() =>
            {
                var factions = new List<int>() { 500001, 500002, 500003, 500004 };
                foreach (var factionId in factions)
                {
                    Log.WriteLine($"FactionId: {factionId}");
                    long divisionId = 24;
                    bool highsecIsles = false;
                    int noLowsecWithinXJumps = 2;
                    int jumpsAvgRadius = 3;

                    var agents = ESCache.Instance.DirectEve.GetAllAgents().Select(a => a.Value).Select(a => ESCache.Instance.DirectEve.GetAgentById(a)).Where(a => a.FactionId == factionId && a.Level == 4 && a.DivisionId == divisionId);

                    var list = agents.Where(k => !k.System.GetNeighbours(noLowsecWithinXJumps).Any(s => s.GetSecurity() < 0.45) // no lowsec system within 2 jumps
                                                 && k.System.GetSecurity() <= 0.64 // max 0.6 sec
                                                 && k.System.GetSecurity() >= 0.45
                                                 && (highsecIsles || (!highsecIsles && k.System.CalculatePathTo(ESCache.Instance.DirectEve.SolarSystems[30000142],null, false, false).Item1.Any()))) // include exclude highsec isles
                        .Select(n =>
                            new
                            {
                                origin = n,
                                avgRadius = n.System.GetNeighbours(jumpsAvgRadius).Where(s => s.GetSecurity() >= 0.45).Select(o => o.Radius).Sum() / n.System.GetNeighbours(jumpsAvgRadius).Count(s => s.GetSecurity() >= 0.45)
                            }) // avg radius of all systems reachable with max 3 jumps except lowsec systems
                        .ToList();

                    int i = 0;
                    foreach (var agent in list.OrderBy(k => k.avgRadius))
                    {
                        i++;
                        Log.WriteLine($"[{i}] Name [{agent.origin.Name}] Sec [{Math.Round(agent.origin.System.GetSecurity(), 1)}] AvgRadiusNeighbours [{Math.Round(agent.avgRadius, 1)}] Radius [{Math.Round(agent.origin.System.Radius, 1)}] System [{agent.origin.System.Name}] Region [{agent.origin.System.Constellation.Region.Name}]");
                    }

                    Log.WriteLine($"--------------------------------------------------");
                }
            });
        }

        #endregion Constructors
    }
}