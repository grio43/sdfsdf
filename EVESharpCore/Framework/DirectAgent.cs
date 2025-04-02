// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Extensions;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgent : DirectObject
    {
        #region Fields

        private static Dictionary<string, long> AgentLookupDict = new Dictionary<string, long>();

        #endregion Fields

        #region Constructors

        internal DirectAgent(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public static readonly Dictionary<int, double> AGENT_LEVEL_REQUIRED_STANDING = new Dictionary<int, double>()
        {
            {1,-11.0},
            {2,1.0},
            {3,3.0},
            {4,5.0},
            {5,7.0},
        };

        public long AgentId { get; private set; }
        public long AgentTypeId { get; private set; }
        public long BloodlineId { get; private set; }

        public bool IsLocatorAgent { get; private set; }

        public bool CanAccessAgent
        {
            get
            {
                if (AGENT_LEVEL_REQUIRED_STANDING.TryGetValue(Level, out var s))
                {
                    var min = MinEffectiveStanding;
                    var max = MaxEffectiveStanding;

                    if (min < -1.99)
                        return false;

                    return max > s;
                }
                return false;
            }
        }

        public long CorpId { get; private set; }
        public long DivisionId { get; private set; }
        public double EffectiveAgentStanding => DirectEve.Standings.EffectiveStanding(AgentId, DirectEve.Session.CharacterId ?? -1) ?? 0;
        public double EffectiveCorpStanding => DirectEve.Standings.EffectiveStanding(CorpId, DirectEve.Session.CharacterId ?? -1) ?? 0;
        public double EffectiveFactionStanding => DirectEve.Standings.EffectiveStanding(FactionId, DirectEve.Session.CharacterId ?? -1) ?? 0;
        public long FactionId { get; private set; }
        public bool Gender { get; private set; }
        public bool IsValid { get; private set; }
        public int Level { get; private set; }

        public int? LoyaltyPoints
        {
            get
            {
                var debug = ESCache.Instance.EveAccount.CS.QMS.QuestorDebugSetting.DebugAgentInteractionReplyToAgent;
                int? ret = null;
                var wallet = DirectEve.Windows.OfType<DirectWalletWindow>().FirstOrDefault();
                if (wallet == null)
                {
                    DirectEve.Log("Opening Wallet.");
                    DirectEve.ExecuteCommand(DirectCmd.OpenWallet);
                    return ret;
                }

                var lpSvc = DirectEve.GetLocalSvc("loyaltyPointsWalletSvc");
                if (lpSvc.IsValid)
                {
                    var mappings = lpSvc.Call("GetAllCharacterLPBalancesExcludingEvermarks").ToList();

                    if (debug)
                    {
                        DirectEve.Log($"Agent CorpId [{this.CorpId}] --- LP mappings found (amount) [{mappings.Count}]");
                    }

                    foreach (var mapping in mappings)
                    {
                        if (debug)
                        {
                            DirectEve.Log($"Corp Id [{(int)mapping.GetItemAt(0)}] LPs [{(int)mapping.GetItemAt(1)}]");
                        }

                        if ((int)mapping.GetItemAt(0) != CorpId)
                            continue;

                        return (int)mapping.GetItemAt(1);
                    }
                    return ret;
                }                
                return ret;
            }
        }

        public double MaxEffectiveStanding => Math.Max(Math.Max(EffectiveAgentStanding, EffectiveCorpStanding), EffectiveFactionStanding);
        public double MinEffectiveStanding => Math.Min(Math.Min(EffectiveAgentStanding, EffectiveCorpStanding), EffectiveFactionStanding);
        public DirectAgentMission Mission => DirectEve.AgentMissions.Where(m => m.AgentId == AgentId).FirstOrDefault();

        public string Name
        {
            get
            {
                var owner = DirectOwner.GetOwner(DirectEve, AgentId);
                if (owner == null)
                    return string.Empty;

                return owner.Name;
            }
        }

        public bool ObjectivesComplete
        {
            get
            {
                var objectivesComplete = GetAgentMissionInfo().ToLower().Contains("FetchObjectAcquiredDungeonDone".ToLower())
                                         || GetAgentMissionInfo().ToLower().Contains("AllObjectivesComplete".ToLower());

                return objectivesComplete;
            }
        }

        public long SolarSystemId { get; private set; }

        public long StationId { get; private set; }

        public DirectSolarSystem System => DirectEve.SolarSystems[(int)SolarSystemId];

        public DirectAgentWindow Window
        {
            get { return DirectEve.Windows.OfType<DirectAgentWindow>().FirstOrDefault(w => w.AgentId == AgentId); }
        }

        //private PyObject PyAgentId { get; set; }

        public DirectAgentMissionBookmark GetMissionBookmark(string startsWith) =>
                                                                            this?.Mission?.Bookmarks.FirstOrDefault(b => b.Title.ToLower().StartsWith(startsWith.ToLower()));

        #endregion Properties

        #region Methods

        public static Dictionary<string, long> GetAllAgents(DirectEve directEve)
        {
            var ret = new Dictionary<string, long>();

            var agentsById = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").ToDictionary<long>();
            foreach (var agent in agentsById)
            {
                var owner = DirectOwner.GetOwner(directEve, agent.Key);
                if (ret.ContainsKey(owner.Name))
                    continue;
                ret.AddOrUpdate(owner.Name, agent.Key);
            }

            return ret;
        }

        public static bool IsAgentsByIdDictionaryPopulated(DirectEve directEve)
        {
            return directEve.GetLocalSvc("agents").Attribute("allAgents").IsValid;
        }

        public static void PopulateAgentsByIdDictionary(DirectEve directEve)
        {
            if (!IsAgentsByIdDictionaryPopulated(directEve))
                directEve.ThreadedLocalSvcCall("agents", "GetAgentsByID");
        }

        public String GetAgentMissionInfo()
        {
            //TravelTo                                  LocationID
            //MissionFetch                              TypeID
            //MissionFetchContainer                     TypeID and ContainerID
            //MissionFetchMine                          TypeID and Quantity
            //MissionFetchMineTrigger                   TypeID
            //MissionFetchTarget                        TypeID and TargetTypeID
            //AllObjectivesComplete                     AgentID
            //TransportItemsPresent                     TypeID and StationID
            //TransportItemsMissing                     TypeID
            //FetchObjectAcquiredDungeonDone            TypeID, AgentID, and StationID
            //GoToGate                                  ItemID
            //KillTrigger                               TypeID, ItemID, and EventTypeName
            //DestroyLCSAndAll                          TypeID and ItemID
            //Destroy                                   TypeID and ItemID
            //Attack                                    TypeID and ItemID
            //Approach                                  TypeID and ItemID
            //Hack                                      TypeID and ItemID
            //Salvage                                   TypeID and ItemID
            //DestroyAll                                None

            var ret = String.Empty;

            if (!IsValid) return ret;

            var obj = DirectEve.GetLocalSvc("missionObjectivesTracker").Attribute("currentAgentMissionInfo");

            if (obj == null || !obj.IsValid) return ret;

            var dict = obj.ToDictionary<long>();

            if (dict.ContainsKey(AgentId))
                ret = dict[AgentId].ToUnicodeString();

            return ret ?? string.Empty;
        }

        public bool InteractWith()
        {
            if (!DirectEve.Interval(1200, 2000))
                return false;

            DirectEve.DWM.ActivateWindow(typeof(DirectLobbyWindow), true, true);

            return DirectEve.ThreadedLocalSvcCall("agents", "OnInteractWith", AgentId);
        }

        internal static DirectAgent GetAgentById(DirectEve directEve, long id)
        {
            var pyAgent = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").DictionaryItem(id);

            var agent = new DirectAgent(directEve);
            agent.IsValid = pyAgent.IsValid;
            //agent.PyAgentId = pyAgent.Item(0);
            agent.AgentId = (long)pyAgent.GetItemAt(0);
            agent.AgentTypeId = (long)pyAgent.GetItemAt(1);
            agent.DivisionId = (long)pyAgent.GetItemAt(2);
            agent.Level = (int)pyAgent.GetItemAt(3);
            agent.StationId = (long)pyAgent.GetItemAt(4);
            agent.BloodlineId = (long)pyAgent.GetItemAt(5);
            agent.CorpId = (long)pyAgent.GetItemAt(6);
            //agent.Gender = (bool)pyAgent.GetItemAt(7);
            //agent.IsLocatorAgent = (bool)pyAgent.GetItemAt(8);
            agent.FactionId = (long)pyAgent.GetItemAt(9);
            agent.SolarSystemId = (long)pyAgent.GetItemAt(10);
            return agent;
        }

        internal static DirectAgent GetAgentByName(DirectEve directEve, string name)
        {
            if (AgentLookupDict.Count == 0)
            {
                var agentsById = directEve.GetLocalSvc("agents").Attribute("allAgentsByID").Attribute("items").ToDictionary<long>();
                foreach (var agent in agentsById)
                {
                    var owner = DirectOwner.GetOwner(directEve, agent.Key);
                    AgentLookupDict[owner.Name.ToLower()] = agent.Key;
                }
            }

            if (AgentLookupDict.TryGetValue(name.ToLower(), out var id))
            {
                return GetAgentById(directEve, id);
            }
            else
            {
                //directEve.Log("AgentLookupDict error. Agent not found?");
                return null;
            }
        }

        #endregion Methods
    }
}