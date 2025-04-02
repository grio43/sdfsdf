using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    public class Storyline
    {
        #region Fields

        private static DateTime LastOfferRemove = DateTime.MinValue;
        private readonly Dictionary<string, IStoryline> _storylines;

        private DateTime _nextAction = DateTime.UtcNow;
        private DateTime _nextStoryLineAttempt = DateTime.UtcNow;
        private bool _setDestinationStation;
        private IStoryline _storyline;

        private int moveCnt = 0;

        #endregion Fields

        #region Constructors

        public Storyline()
        {
            //_combat = new Combat();
            //_agentInteraction = new AgentInteraction();

            ESCache.Instance.AgentBlacklist = new List<long>();

            _storylines = new Dictionary<string, IStoryline>
            {
                // Examples
                // note: All storylines must be entered in lowercase or use ".ToLower()"
                //
                //{"StorylineCombatNameHere".ToLower(), new GenericCombatStoryline()},
                //{"StorylineCourierNameHere".ToLower(), new GenericCourier()},
                //
                // COURIER/DELIVERY - ALL FACTIONS - ALL LEVELS
                //
                {"Materials For War Preparation".ToLower(), new MaterialsForWarPreparation()},
                {"Transaction Data Delivery".ToLower(), new TransactionDataDelivery()},
                {"A Special Delivery".ToLower(), new GenericCourier()}, // Needs 40k m3 cargo (i.e. Iteron Mark V, T2 CHO rigs) for lvl4

                //
                // COURIER/DELIVERY - ALL FACTIONS - LEVEL 1
                //
                {"The Essence of Speed".ToLower(), new GenericCourier()},
                {"A Watchful Eye".ToLower(), new GenericCourier()},

                //
                // COURIER/DELIVERY - ALL FACTIONS - LEVEL 2
                //

                //
                // COURIER/DELIVERY - ALL FACTIONS - LEVEL 3
                //
                {"Kidnappers Strike - The Interrogation (2 of 10)".ToLower(), new GenericCourier()}, //lvl3
                {"Kidnappers Strike - Possible Leads (4 of 10)".ToLower(), new GenericCourier()}, //lvl3
                {"Kidnappers Strike - The Flu Outbreak (6 of 10)".ToLower(), new GenericCourier()}, //lvl3

                //
                // COURIER/DELIVERY - ALL FACTIONS - LEVEL 4
                //

                //
                // COURIER/DELIVERY - AMARR - LEVEL 1
                //

                {"Of Fangs and Claws".ToLower(), new GenericCourier()}, //amarr lvl1
                {"Divinie Intervention".ToLower(), new GenericCourier()}, //amarr lvl1

                //
                // COURIER/DELIVERY - AMARR - LEVEL 2
                //

                //
                // COURIER/DELIVERY - AMARR - LEVEL 3
                //

                //
                // COURIER/DELIVERY - AMARR - LEVEL 4
                //
                {"Opiate of the Masses".ToLower(), new GenericCourier()}, //lvl4
                //{"Send the Marines".ToLower(), new GenericCourier()}, //lvl4
                {"Send the Marines!".ToLower(), new GenericCourier()}, //lvl4
                {"The Governors Ball".ToLower(), new GenericCourier()}, //lvl4
                {"The State of the Empire".ToLower(), new GenericCourier()}, //lvl4
                {"Unmasking the Traitor".ToLower(), new GenericCourier()}, //lvl4
                //
                // COURIER/DELIVERY - CALDARI - LEVEL 1
                //
                {"A Fathers Love".ToLower(), new GenericCourier()}, //lvl1 note: 300m3 needed
                //{"A Greener World".ToLower(), new GenericCourier()}, //lvl1
                //{"Eradication".ToLower(), new GenericCourier()}, //lvl1
                //{"Evacuation".ToLower(), new GenericCourier()}, //lvl1
                //{"On the Run".ToLower(), new GenericCourier()}, //lvl1

                //
                // COURIER/DELIVERY - CALDARI - LEVEL 2
                //

                //
                // COURIER/DELIVERY - CALDARI - LEVEL 3
                //

                //
                // COURIER/DELIVERY - CALDARI - LEVEL 4
                //
                {"A Desperate Rescue".ToLower(), new GenericCourier()}, //lvl4
                {"Black Ops Crisis".ToLower(), new GenericCourier()}, //lvl4
                {"Fire and Ice".ToLower(), new GenericCourier()}, //lvl4
                {"Hunting Black Dog".ToLower(), new GenericCourier()}, //lvl4
                {"Operation Doorstop".ToLower(), new GenericCourier()}, //lvl4
                //
                // COURIER/DELIVERY - GALLENTE - LEVEL 1
                //
                //{"A Little Work On The Side".ToLower(), new GenericCourier()}, //lvl1
                //{"Ancient Treasures".ToLower(), new GenericCourier()}, //lvl1
                //{"Pieces of the Past".ToLower(), new GenericCourier()}, //lvl1
                //{"The Latest Style".ToLower(), new GenericCourier()}, //lvl1
                //{"Wartime Advances".ToLower(), new GenericCourier()}, //lvl1

                //
                // COURIER/DELIVERY - GALLENTE - LEVEL 2
                //

                //
                // COURIER/DELIVERY - GALLENTE - LEVEL 3
                //

                //
                // COURIER/DELIVERY - GALLENTE - LEVEL 4
                //
                //{"A Fathers Love".ToLower(), new GenericCourier()}, //lvl4
                {"A Fine Wine".ToLower(), new GenericCourier()}, //lvl4
                //{"A Greener World".ToLower(), new GenericCourier()}, //lvl4
                {"Amphibian Error".ToLower(), new GenericCourier()}, //lvl4
                //{"Eradication".ToLower(), new GenericCourier()}, //lvl4
                //{"Evacuation".ToLower(), new GenericCourier()}, //lvl4
                //{"On the Run".ToLower(), new GenericCourier()}, //lvl4
                {"Shifting Rocks".ToLower(), new GenericCourier()}, //lvl4
                {"The Creeping Cold".ToLower(), new GenericCourier()}, //lvl4
                {"The Natural Way".ToLower(), new GenericCourier()}, //lvl4

                //
                // COURIER/DELIVERY - MINMATAR - LEVEL 1
                //
                {"Culture Clash".ToLower(), new GenericCourier()}, //lvl1
                //
                // COURIER/DELIVERY - MINMATAR - LEVEL 2
                //
                {"A Different Drone".ToLower(), new GenericCourier()}, //lvl2
                //
                // COURIER/DELIVERY - MINMATAR - LEVEL 3
                //

                //
                // COURIER/DELIVERY - MINMATAR - LEVEL 4
                //
                {"A Cargo With Attitude".ToLower(), new GenericCourier()}, //lvl4
                {"A Load of Scrap".ToLower(), new GenericCourier()}, //lvl4
                {"Brand New Harvesters".ToLower(), new GenericCourier()}, //lvl4
                {"Heart of the Rogue Drone".ToLower(), new GenericCourier()}, //lvl4
                {"Their Secret Defense".ToLower(), new GenericCourier()}, //lvl4
                {"Very Important Pirates".ToLower(), new GenericCourier()}, //lvl1 and 4

                //
                // COMBAT - ALL FACTIONS - ALL LEVELS
                //
                {"Soothe the Salvage Beast".ToLower(), new GenericCombatStoryline()}, //lvl3 and lvl4

                //
                // COMBAT - ALL FACTIONS - LEVEL 1
                //

                //
                // COMBAT - ALL FACTIONS - LEVEL 2
                //

                //
                // COMBAT - ALL FACTIONS - LEVEL 3
                //
                {"Kidnappers Strike - Ambush in the Dark (1 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Kidnapping (3 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Incriminating Evidence (5 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Secret Meeting (7 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Defend the Civilian Convoy (8 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - Retrieve the Prisoners (9 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3
                {"Kidnappers Strike - The Final Battle (10 of 10)".ToLower(), new GenericCombatStoryline()}, //lvl3

                //
                // COMBAT - ALL FACTIONS - LEVEL 4
                //
                {"Covering Your Tracks".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Evolution".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Patient Zero".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Record Cleaning".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Shipyard Theft".ToLower(), new GenericCombatStoryline()}, //lvl4

                //
                // COMBAT - AMARR - LEVEL 1
                //

                {"A Case of Kidnapping".ToLower(), new GenericCombatStoryline()}, //amarr lvl1

                //
                // COMBAT - AMARR - LEVEL 2
                //
                { "Whispers in the Dark - First Contact (1 of 4)".ToLower(), new GenericCombatStoryline()}, //vs sansha lvl2
                {"Whispers in the Dark - Lay and Pray (2 of 4)".ToLower(), new GenericCombatStoryline()}, //vs sansha lvl2
                {"Whispers in the Dark - The Outpost (4 of 4)".ToLower(), new GenericCombatStoryline()}, //vs sansha lvl2
                //
                // COMBAT - AMARR - LEVEL 3
                //

                //
                // COMBAT - AMARR - LEVEL 4
                //
                {"Blood Farm".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Dissidents".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Extract the Renegade".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Gate to Nowhere".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Jealous Rivals".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"Racetrack Ruckus".ToLower(), new GenericCombatStoryline()}, //amarr lvl4
                {"The Mouthy Merc".ToLower(), new GenericCombatStoryline()}, //amarr lvl4

                //
                // COMBAT - CALDARI - LEVEL 1
                //
                {"An End To EavesDropping".ToLower(), new GenericCombatStoryline()}, //lvl1

                //
                // COMBAT - CALDARI - LEVEL 2
                //

                //
                // COMBAT - CALDARI - LEVEL 3
                //

                //
                // COMBAT - CALDARI - LEVEL 4
                //
                {"Crowd Control".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"Forgotten Outpost".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                //{"Illegal Mining".ToLower(), new GenericCombatStoryline()}, //caldari lvl4 note: Extremely high DPS after shooting structures!
                {"Innocents in the Crossfire".ToLower(), new GenericCombatStoryline()}, //caldari lvl4
                {"Stem the Flow".ToLower(), new GenericCombatStoryline()}, //caldari lvl4

                //
                // COMBAT - GALLENTE - LEVEL 1
                //

                //
                // COMBAT - GALLENTE - LEVEL 2
                //

                //
                // COMBAT - GALLENTE - LEVEL 3
                //
                {"A Force to Be Reckoned With".ToLower(), new GenericCombatStoryline()}, //gallente lvl4

                //
                // COMBAT - GALLENTE - LEVEL 4
                //
                {"Federal Confidence".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Hidden Hope".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                //{"Missing Persons Report", new GenericCombatStoryline()},
                //{"Inspired".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Prison Transfer".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Serpentis Ship Builders".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"The Serpent and the Slaves".ToLower(), new GenericCombatStoryline()}, //gallente lvl4
                {"Tomb of the Unknown Soldiers".ToLower(), new GenericCombatStoryline()}, //gallente lvl4

                //
                // COMBAT - MINMATAR - LEVEL 1
                //

                //
                // COMBAT - MINMATAR - LEVEL 2
                //

                //
                // COMBAT - MINMATAR - LEVEL 3
                //
                //
                // COMBAT - MINMATAR - LEVEL 4
                //
                {"Amarrian Excavators".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Diplomatic Incident".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Matriarch".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Nine Tenths of the Wormhole".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Postmodern Primitives".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"Quota Season".ToLower(), new GenericCombatStoryline()}, //lvl4
                {"The Blood of Angry Men".ToLower(), new GenericCombatStoryline()}, //lvl4

                // TEMP
                {"Enemies Abound (2 of 5)".ToLower(), new GenericCourier()},
            };
        }

        #endregion Constructors

        #region Properties

        public IStoryline StorylineHandler => _storyline;

        public DirectAgentMission StorylineMission
        {
            get
            {
                try
                {
                    Log.WriteLine("Currently have  [" + CompatibleStorylineMissions.Count() + "] storyline missions questor knows how to do and are not blacklisted.");
                    return CompatibleStorylineMissions.FirstOrDefault();
                }
                catch (Exception exception)
                {
                    Log.WriteLine("StorylineMission - Exception: [" + exception + "]");
                    return null;
                }
            }
        }

        private IEnumerable<DirectAgentMission> CompatibleStorylineMissions
        {
            get
            {
                IEnumerable<DirectAgentMission> missions = ESCache.Instance.DirectEve.AgentMissions.ToList();

                missions = missions.Where(m => !ESCache.Instance.AgentBlacklist.Contains(m.AgentId)).ToList();

                missions = missions.Where(m => (m.Type.ToLower().Contains("Storyline".ToLower()) &&
                                               !m.Name.Contains("Cash Flow for Capsuleers"))
                                               || Log.FilterPath(m.Name).ToLower().Contains("Enemies Abound (2 of 5)".ToLower())).ToList();

                missions = missions.Where(m => _storylines.ContainsKey(Log.FilterPath(m.Name).ToLower())).ToList();
                missions = missions.Where(m => ESCache.Instance.MissionSettings.MissionBlacklist.All(b => b.Name.ToLower()
                                                                                         != Log.FilterPath(m.Name).ToLower()))
                                                                                         .ToList();
                return missions;
            }
        }

        #endregion Properties

        #region Methods

        public bool HasStoryline()
        {
            // Do we have a registered storyline?
            return StorylineMission != null;
        }

        public void ProcessState()
        {
            switch (ESCache.Instance.State.CurrentStorylineState)
            {
                case StorylineState.Idle:
                    IdleState();
                    break;

                case StorylineState.Arm:

                    //Logging.Log("Storyline: Arm");
                    ESCache.Instance.State.CurrentStorylineState = _storyline.Arm(this);
                    break;

                case StorylineState.GotoAgent:

                    //Logging.Log("Storyline: GotoAgent");
                    GotoAgent(StorylineState.PreAcceptMission);
                    break;

                case StorylineState.PreAcceptMission:

                    //Logging.Log("Storyline: PreAcceptMission-!!");
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    ESCache.Instance.State.CurrentStorylineState = _storyline.PreAcceptMission(this);
                    break;

                case StorylineState.DeclineMission:
                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                    {
                        Log.WriteLine("Start conversation [Decline Mission]");

                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                        ESCache.Instance.AgentInteraction.Purpose = AgentInteractionPurpose.DeclineMission;
                    }

                    ESCache.Instance.AgentInteraction.ProcessState();

                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    break;

                case StorylineState.AcceptMission:

                    //Logging.Log("Storyline: AcceptMission!!-");
                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                    {
                        Log.WriteLine("Start conversation [Start Mission]");

                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                        ESCache.Instance.AgentInteraction.Purpose = AgentInteractionPurpose.StartMission;
                    }

                    ESCache.Instance.AgentInteraction.ProcessState();

                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
                    {
                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;

                        // If there is no mission anymore then we're done (we declined it)
                        ESCache.Instance.State.CurrentStorylineState = StorylineMission == null ? StorylineState.Done : StorylineState.ExecuteMission;
                    }
                    break;

                case StorylineState.ExecuteMission:
                    ESCache.Instance.State.CurrentStorylineState = _storyline.ExecuteMission(this);
                    break;

                case StorylineState.ReturnToAgent:
                    GotoAgent(StorylineState.CompleteMission);
                    break;

                case StorylineState.CompleteMission:

                    Log.WriteLine($"[Storyline] CurrentAgentInteractionState {ESCache.Instance.State.CurrentAgentInteractionState}");

                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Idle)
                    {
                        Log.WriteLine("Start Conversation [Complete Mission]");

                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                        ESCache.Instance.AgentInteraction.Purpose = AgentInteractionPurpose.CompleteMission;
                    }

                    ESCache.Instance.AgentInteraction.ProcessState();

                    if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
                    {
                        ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                        ESCache.Instance.State.CurrentStorylineState = StorylineState.BringSpoilsOfWar;
                    }
                    break;

                case StorylineState.BringSpoilsOfWar:
                    if (!BringSpoilsOfWar()) return;
                    break;

                case StorylineState.BlacklistAgent:

                    if (!ESCache.Instance.AgentInteraction.OpenJournalWindow())
                        return;

                    var jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                    if (jw.SelectedMainTab != MainTab.AgentMissions)
                    {
                        Log.WriteLine("Journal window agent mission tab is not selected. Switching the tab.");
                        jw.SwitchMaintab(MainTab.AgentMissions);
                        return;
                    }

                    var currentStorylines =
                        ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == ESCache.Instance.Agent.AgentId)
                            .Where(m => m.Type.Contains("Storyline") && m.State == (int)MissionState.Offered)
                            .ToList();

                    // remove the storyline offer here and set the default agent
                    if (currentStorylines.Any())
                    {
                        var mission = currentStorylines.FirstOrDefault();
                        if (mission != null)
                        {
                            Log.WriteLine("Removing storyline mission [" + Log.FilterPath(mission.Name) + "] because it's against a blacklisted faction.");
                            mission.RemoveOffer();
                        }
                        else
                        {
                            // just blacklist the agent then if the mission has already accepted...
                            ESCache.Instance.AgentBlacklist.Add(ESCache.Instance.Agent.AgentId);
                            Log.WriteLine(
                                "BlacklistAgent: The agent that provided us with this storyline mission has been added to the session blacklist AgentId[" +
                                ESCache.Instance.Agent.AgentId + "]");
                        }
                    }

                    Reset();
                    //_States.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                    break;

                case StorylineState.Done:
                    break;
            }
        }

        public bool RemoveNotCompatibleStorylineMissions()
        {
            if (!ESCache.Instance.AgentInteraction.OpenJournalWindow())
                return false;

            var offeredStorylines =
                ESCache.Instance.DirectEve.AgentMissions.Where(m => !ESCache.Instance.AgentBlacklist.Contains(m.AgentId))
                    .Where(m => m.Type.ToLower().Contains("Storyline".ToLower()) && m.State == (int)MissionState.Offered)
                    .ToList();
            var notCompatibleStorylines = offeredStorylines.Except(CompatibleStorylineMissions);

            if (notCompatibleStorylines.Any(m => m.State == (int)MissionState.Offered))
            {
                var mission = notCompatibleStorylines.FirstOrDefault(m => m.State == (int)MissionState.Offered);
                if (mission != null && ESCache.Instance.EveAccount.CS.QMS.QS.RemoveNotCompatibleStorylines)
                {
                    if (LastOfferRemove.AddMinutes(10) < DateTime.UtcNow)
                    {
                        var jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                        if (jw.SelectedMainTab != MainTab.AgentMissions)
                        {
                            Log.WriteLine("Journal window agent mission tab is not selected. Switching the tab.");
                            jw.SwitchMaintab(MainTab.AgentMissions);
                            return false;
                        }

                        Log.WriteLine("Removing storyline mission offer [" + Log.FilterPath(mission.Name) + "] to make room for new storylines.");
                        mission.RemoveOffer();
                        LastOfferRemove = DateTime.UtcNow;
                    }
                }
            }

            return true;
        }

        public void Reset()
        {
            try
            {
                //Logging.Log("Storyline", "Storyline.Reset", Logging.White);
                ESCache.Instance.State.CurrentStorylineState = StorylineState.Idle;
                _storyline = null;
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                ESCache.Instance.Traveler.Destination = null;
            }
            catch (Exception exception)
            {
                Log.WriteLine("IterateShipTargetValues - Exception: [" + exception + "]");
                return;
            }
        }

        private bool BringSpoilsOfWar()
        {
            if (_nextAction > DateTime.UtcNow) return false;

            // Open the item hangar (should still be open)
            if (ESCache.Instance.DirectEve.GetItemHangar() == null) return false;

            // Do we have anything here we want to bring home, like implants or ?
            //if (to.Items.Any(i => i.GroupId == (int)Group.MiscSpecialMissionItems || i.GroupId == (int)Group.Livestock))

            if (!ESCache.Instance.DirectEve.GetItemHangar().Items.Any(i => i.GroupId >= 738 && i.GroupId <= 750) || moveCnt > 10)
            {
                ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                moveCnt = 0;
                return true;
            }

            // Yes, open the ships cargo
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                if (DebugConfig.DebugUnloadLoot) Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            // If we are not moving items
            if (ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
            {
                // Move all the implants to the cargo bay
                foreach (var item in ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.GroupId >= 738
                                                                                 && i.GroupId <= 750))
                {
                    if (ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity - item.Volume * item.Quantity < 0)
                    {
                        Log.WriteLine("We are full, not moving anything else");
                        ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                        return true;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Add(item, item.Quantity))
                    {
                        Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to cargo");
                        moveCnt++;
                        _nextAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.RandomNumber(1000, 3000));
                    }
                    return false;
                }
                _nextAction = DateTime.UtcNow.AddSeconds(5);
                return false;
            }

            return false;
        }

        private void GotoAgent(StorylineState nextState)
        {
            if (_nextAction > DateTime.UtcNow)
                return;

            var storylineagent = ESCache.Instance.DirectEve.GetAgentById(ESCache.Instance.Agent.AgentId);
            if (storylineagent == null)
            {
                ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                return;
            }

            var baseDestination = ESCache.Instance.Traveler.Destination as DockableLocationDestination;
            if (baseDestination == null || baseDestination.DockableLocationId != storylineagent.StationId)
            {
                ESCache.Instance.Traveler.Destination = new DockableLocationDestination(storylineagent.SolarSystemId, storylineagent.StationId);
                return;
            }

            if (storylineagent.SolarSystemId != ESCache.Instance.DirectEve.Session.SolarSystemId)
            {
                // if we haven't already done so, set Eve's autopilot
                if (!_setDestinationStation)
                {
                    if (!ESCache.Instance.Traveler.SetStationDestination(storylineagent.StationId))
                    {
                        Log.WriteLine("GotoAgent: Unable to find route to storyline agent. Skipping.");
                        ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                        return;
                    }
                    _setDestinationStation = true;
                    _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(2, 4));
                    return;
                }
            }

            ESCache.Instance.Traveler.ProcessState();
            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
            {
                ESCache.Instance.State.CurrentStorylineState = nextState;
                Log.WriteLine($"[Storyline] CurrentAgentInteractionState {ESCache.Instance.State.CurrentAgentInteractionState}");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ESCache.Instance.Traveler.Destination = null;
                _setDestinationStation = false;
            }

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
            {
                Log.WriteLine("Traveller state = Error. Blacklisting this agent.");
                ESCache.Instance.State.CurrentStorylineState = StorylineState.BlacklistAgent;
                return;
            }
        }

        private void IdleState()
        {
            try
            {
                var currentStorylineMission = StorylineMission;
                if (currentStorylineMission == null)
                {
                    _nextStoryLineAttempt = DateTime.UtcNow.AddMinutes(15);
                    ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                    return;
                }

                var storylineagent = ESCache.Instance.Agent;
                if (storylineagent == null)
                {
                    Log.WriteLine("Storyline agent == null.");
                    ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                    return;
                }

                Log.WriteLine("Going to do [" + Log.FilterPath(currentStorylineMission.Name) + "] for agent [" + storylineagent.Name + "] AgentID[" +
                              storylineagent.AgentId + "]");
                ESCache.Instance.State.CurrentStorylineState = StorylineState.Arm;
                _storyline = _storylines[Log.FilterPath(currentStorylineMission.Name.ToLower())];
            }
            catch (Exception exception)
            {
                Log.WriteLine("IterateShipTargetValues - Exception: [" + exception + "]");
                return;
            }
        }

        #endregion Methods
    }
}