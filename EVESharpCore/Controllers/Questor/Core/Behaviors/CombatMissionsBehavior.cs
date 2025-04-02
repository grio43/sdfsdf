extern alias SC;
using System;
using System.Linq;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Controllers.Questor.Core.Storylines;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor.Core.Behaviors
{
    public class CombatMissionsBehavior
    {
        #region Fields

        private readonly ActionControl _actionControl;
        private readonly Storyline _storyline;

        private bool _previousInMission;

        #endregion Fields

        #region Constructors

        public CombatMissionsBehavior()
        {
            _actionControl = new ActionControl();
            _storyline = new Storyline();
            ESCache.Storyline = _storyline;

            ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
            ESCache.Instance.State.CurrentArmState = ArmState.Idle;
            ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.Idle;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.AtDestination;
        }

        #endregion Constructors

        #region Properties

        public bool MovedToNextPocket { get; set; }
        public Storyline Storyline => _storyline;

        #endregion Properties

        #region Methods

        public bool ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState _CMBStateToSet)
        {
            try
            {
                Log.WriteLine("New state [" + _CMBStateToSet.ToString() + "]");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
            finally
            {
                if (ESCache.Instance.State.CurrentCombatMissionBehaviorState != _CMBStateToSet)
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = _CMBStateToSet;
            }

            return true;
        }

        public void ProcessState()
        {
            if (!CMBEveryPulse()) return;

            switch (ESCache.Instance.State.CurrentCombatMissionBehaviorState)
            {
                case CombatMissionsBehaviorState.Idle:
                    IdleCMBState();
                    break;

                case CombatMissionsBehaviorState.Cleanup:
                    CleanupCMBState();
                    break;

                case CombatMissionsBehaviorState.Start:
                    StartCMBState();
                    break;

                case CombatMissionsBehaviorState.Switch:
                    SwitchCMBState();
                    break;

                case CombatMissionsBehaviorState.Arm:
                    ArmCMBState();
                    break;

                case CombatMissionsBehaviorState.GotoMission:
                    GotoMissionCMBState();
                    break;

                case CombatMissionsBehaviorState.ExecuteMission:
                    ExecuteMissionCMBState();
                    break;

                case CombatMissionsBehaviorState.GotoBase:
                    GotoBaseCMBState();
                    break;

                case CombatMissionsBehaviorState.CompleteMission:
                    CompleteMissionCMBState();
                    break;

                case CombatMissionsBehaviorState.QuitMission:
                    QuitMissionCMBState();
                    break;

                case CombatMissionsBehaviorState.Statistics:
                    StatisticsCMBState();
                    break;

                case CombatMissionsBehaviorState.UnloadLoot:
                    UnloadLootCMBState();
                    break;

                case CombatMissionsBehaviorState.PrepareStorylineSwitchAgents:

                    DirectAgent agent = null;
                    if (_storyline.StorylineMission != null)
                        if (_storyline.StorylineMission.AgentId != 0)
                            agent = ESCache.Instance.DirectEve.GetAgentById(_storyline.StorylineMission.AgentId);

                    if (agent != null)
                    {
                        ESCache.Instance.Agent = agent;
                        ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                    }
                    else
                    {
                        Log.WriteLine("Storyline agent  error.");
                        ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    }

                    break;

                case CombatMissionsBehaviorState.PrepareStorylineGotoBase:
                    PrepareStorylineGotoBaseCMBState();
                    break;

                case CombatMissionsBehaviorState.Storyline:
                    _storyline.ProcessState();
                    if (ESCache.Instance.State.CurrentStorylineState == StorylineState.Done)
                    {
                        Log.WriteLine("We have completed the storyline, returning to base");
                        ESCache.Instance.State.CurrentStorylineState = StorylineState.Idle;
                        ESCache.Instance.Agent = null; // reset agent, regular agent will be used
                        _storyline.Reset();

                        ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.StorylineReturnToBase;
                        break;
                    }

                    break;

                case CombatMissionsBehaviorState.StorylineReturnToBase:
                    StorylineReturnToBaseCMBState();
                    break;

                case CombatMissionsBehaviorState.GotoNearestStation:
                    GotoNearestStationCMBState();
                    break;
            }
        }

        private void ArmCMBState()
        {

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                return;

            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
                return;

            if (!ESCache.Instance.InDockableLocation) return;


            if (ESCache.Instance.State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");

                // buy ammo
                if (ESCache.Instance.EveAccount.CS.QMS.BuyAmmo && BuyAmmoController.ShouldBuyAmmo())
                {
                    ControllerManager.Instance.RemoveController(typeof(QuestorController));
                    ControllerManager.Instance.AddController(new BuyAmmoController((() =>
                    {
                        ControllerManager.Instance.AddController(typeof(QuestorController)); // done action
                    })));
                    return;
                }
                // dump loot
                var anyBlockadeRunnder = ESCache.Instance.DirectEve.GetShipHangar().Items.Any(i => i.IsSingleton
                                                                                                     && i.GroupId == (int)Group.BlockadeRunner
                                                                                                     && i.GivenName != null);
                if (ESCache.Instance.EveAccount.CS.QMS.DumpLoot && DumpLootController.ShouldDumpLoot && anyBlockadeRunnder)
                {
                    ControllerManager.Instance.RemoveController(typeof(QuestorController));
                    ControllerManager.Instance.AddController(new DumpLootController((() =>
                    {
                        ControllerManager.Instance.AddController(typeof(QuestorController)); // done action
                    })));
                    return;
                }

                ESCache.Instance.Arm.ChangeArmState(ArmState.Begin);
            }

            ESCache.Instance.Arm.ProcessState();

            if (ESCache.Instance.State.CurrentArmState == ArmState.NotEnoughAmmo)
            {
                Log.WriteLine("Armstate.NotEnoughAmmo");
                ESCache.Instance.Arm.ChangeArmState(ArmState.Idle);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                return;
            }

            if (ESCache.Instance.State.CurrentArmState == ArmState.NotEnoughDrones)
            {
                Log.WriteLine("Armstate.NotEnoughDrones");
                ESCache.Instance.Arm.ChangeArmState(ArmState.Idle);
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                return;
            }

            if (ESCache.Instance.State.CurrentArmState == ArmState.Done)
            {
                ESCache.Instance.Arm.ChangeArmState(ArmState.Idle);
                ESCache.Instance.State.CurrentDroneState = DroneState.WaitingForTargets;
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                return;
            }

            return;
        }

        private void CleanupCMBState()
        {
            _storyline.Reset();
            ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Start;
            return;
        }

        private bool CMBEveryPulse()
        {

            if (ESCache.Instance.ActiveShip.ArmorPercentage < 100 || ESCache.Instance.ActiveShip.StructurePercentage < 100)
                ESCache.Instance.Arm.NeedRepair = true;

            var isAtWar = ESCache.Instance.DirectEve.Me.IsAtWar;
            var isLocalsafe = ESCache.Instance.LocalSafe(ESCache.Instance.EveAccount.CS.QMS.QS.LocalBadStandingLevelToConsiderBad);

            if (ESCache.Instance.State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoNearestStation &&
                ESCache.Instance.State.CurrentCombatMissionBehaviorState != CombatMissionsBehaviorState.GotoBase
                && ESCache.Instance.InSpace
                && (!isLocalsafe || isAtWar))
            {
                Log.WriteLine("Local is not safe or we are at war.");
                EntityCache station = null;
                if (ESCache.Instance.Stations != null && ESCache.Instance.Stations.Any())
                    station = ESCache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();

                if (station != null)
                {
                    Log.WriteLine("Station found. Going to nearest station");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoNearestStation;
                }
                else
                {
                    Log.WriteLine("Station not found. Going back to base");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                }
            }

            if (ESCache.Instance.InMission)
            {
                var info = $"{ESCache.Instance.MissionSettings.MissionName} ({Math.Round(DateTime.UtcNow.Subtract(ESCache.Instance.Statistics.StartedMission).TotalMinutes, 0)})";
                Task.Run(() =>
                {
                    try
                    {
                        ESCache.Instance.SetInfoAttribute(info);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }

                });
            }

            if (ESCache.Instance.InDockableLocation && isAtWar)
            {
                Log.WriteLine("We are docked and a war was detected. Disabling this instance.");
                ESCache.Instance.DisableThisInstance();
                ControllerManager.Instance.SetPause(true);
                return false;
            }

            return true;
        }

        private void CompleteMissionCMBState()
        {
            if (!ESCache.Instance.InDockableLocation) return;

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

                if (ESCache.Instance.Statistics.LastMissionCompletionError.AddSeconds(10) < DateTime.UtcNow)
                {
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Statistics);
                    return;
                }

                Log.WriteLine("Skipping statistics: We have not yet completed a mission");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
            }
        }


        private static Random _rnd = new Random();
        private DateTime _doNotDeactivateBastionUntil;

        private void ExecuteMissionCMBState()
        {
            if (!ESCache.Instance.InSpace)
                return;

            _actionControl.ProcessState();
            ESCache.Instance.Combat.ProcessState();
            ESCache.Instance.Drones.ProcessState();

            var inMission = ESCache.Instance.InMission;
            if ((_previousInMission != inMission || MovedToNextPocket) && ESCache.Instance.EntitiesOnGrid.Any(e => e.BracketType == BracketType.NPC_Frigate
                                                                                                                  || e.BracketType == BracketType.NPC_Cruiser
                                                                                                                  || e.BracketType == BracketType.NPC_Battleship
                                                                                                                  || e.BracketType == BracketType.NPC_Destroyer
                                                                                                                  ))
            {
                if (!_previousInMission && inMission || MovedToNextPocket)
                {
                    Log.WriteLine($"NPCs found on grid and InMission has been changed. Reloading.");
                    Log.WriteLine($"_previousInMission {_previousInMission} inMission {inMission} MovedToNextPocket {MovedToNextPocket}");
                    ESCache.Instance.Combat.ReloadAll();
                    MovedToNextPocket = false;
                }

                _previousInMission = inMission;
            }

            var maxRange = ESCache.Instance.Combat.MaxRange;
            var npcCount = ESCache.Instance.EntitiesOnGrid.Count(e => e.IsNPCByBracketType && e.Distance < maxRange);
            var warpGatesOnGrind = ESCache.Instance.EntitiesOnGrid.Where(e => e.BracketType == BracketType.Warp_Gate);
            var warpGatesInRange = ESCache.Instance.EntitiesOnGrid.Where(e => e.BracketType == BracketType.Warp_Gate && e.Distance < 3000);


            var bastion = ESCache.Instance.DirectEve.Modules.FirstOrDefault(m => m.TypeId == 33400);

            if (bastion != null)
            {
                if (bastion.IsActive)
                {
                    // deactivate
                    if (!bastion.IsInLimboState && DirectEve.Interval(900, 1400) && npcCount < 3 && _doNotDeactivateBastionUntil < DateTime.UtcNow)
                    {
                        Logging.Log.WriteLine($"Deactivating bastion module.");
                        bastion.Click();
                    }
                }

                if (inMission && npcCount >= 3 && ((warpGatesOnGrind.Any() && warpGatesInRange.Any()) || !warpGatesOnGrind.Any())
                    && (ActionControl.CurrentAction == null || ActionControl.CurrentAction.State != ActionState.Activate))
                {
                    // activate
                    if (!bastion.IsActive)
                    {
                        if (!bastion.IsInLimboState && DirectEve.Interval(900, 1400))
                        {
                            Logging.Log.WriteLine($"Activating bastion module.");
                            _doNotDeactivateBastionUntil = DateTime.UtcNow.AddMilliseconds(_rnd.Next(4500, 25000));
                            bastion.Click();
                        }
                    }
                }
            }

            if (ESCache.Instance.IsAgentMissionFinished() && !ESCache.Instance.Drones.ActiveDrones.Any())
            {
                Log.WriteLine("Mission objectives are complete, setting state to done.");
                foreach (var e in ESCache.Instance.EntitiesOnGrid.Where(e => ESCache.Instance.Statistics.BountyValues.TryGetValue(e.Id, out var val) && val > 0))
                    ESCache.Instance.Statistics.BountyValues.Remove(e.Id);
                ESCache.Instance.State.CurrentActionControlState = ActionControlState.Done;
            }

            if (ESCache.Instance.State.CurrentCombatState == CombatState.OutOfAmmo)
            {
                Log.WriteLine("Out of Ammo! - Not enough [" + ESCache.Instance.MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                              ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges +
                              "]");
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                ESCache.Instance.LootedContainers.Clear();
            }

            if (ESCache.Instance.State.CurrentActionControlState == ActionControlState.Done)
            {
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                ESCache.Instance.LootedContainers.Clear();
            }

            if (ESCache.Instance.State.CurrentActionControlState == ActionControlState.Error)
            {
                Log.WriteLine("Error");
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                ESCache.Instance.LootedContainers.Clear();
            }
        }

        private void GotoBaseCMBState()
        {
            ESCache.Instance.Drones.IsMissionPocketDone = true;

            if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: Traveler.TravelHome()");

            ESCache.Instance.Traveler.TravelHome();

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InDockableLocation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("GotoBase: We are at destination");

                if (ESCache.Instance.EveAccount.CS.QMS.BuyPlex && BuyPlexController.ShouldBuyPlex)
                {

                    ControllerManager.Instance.RemoveController(typeof(QuestorController));
                    ControllerManager.Instance.AddController(new BuyPlexController((() =>
                    {
                        ControllerManager.Instance.AddController(typeof(QuestorController)); // what to do after the plex has been bought
                    })));
                    return;
                }

                if (ESCache.Instance.State.CurrentActionControlState == ActionControlState.Error)
                {
                    ESCache.Instance.Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Error);
                    return;
                }

                if (ESCache.Instance.State.CurrentCombatState != CombatState.OutOfAmmo && ESCache.Instance.MissionSettings.Mission != null &&
                    ESCache.Instance.MissionSettings.Mission.State == (int)MissionState.Accepted)
                {
                    ESCache.Instance.Traveler.Destination = null;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.CompleteMission);
                    return;
                }

                ESCache.Instance.Traveler.Destination = null;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
            }
        }

        private void GotoMissionCMBState()
        {
            ESCache.Instance.Statistics.MissionLoggingCompleted = false;
            ESCache.Instance.Drones.IsMissionPocketDone = false;
            ESCache.LootAlreadyUnloaded = false;

            if (!(ESCache.Instance.Traveler.Destination is MissionBookmarkDestination missionDestination) || missionDestination.AgentId != ESCache.Instance.Agent.AgentId)
            {
                var nameOfBookmark = "Encounter";
                var bm = ESCache.Instance.Agent.GetMissionBookmark(nameOfBookmark);
                if (bm != null)
                {
                    Log.WriteLine("Setting Destination to 1st bookmark from AgentID: " + ESCache.Instance.Agent.AgentId + " with [" + nameOfBookmark +
                                  "] in the title");
                    ESCache.Instance.Traveler.Destination =
                        new MissionBookmarkDestination(bm);
                    if (ESCache.Instance.DirectEve.Navigation.GetLocation(ESCache.Instance.Traveler.Destination.SolarSystemId) != null)
                    {
                        ESCache.Instance.MissionSolarSystem = ESCache.Instance.DirectEve.Navigation.GetLocation(ESCache.Instance.Traveler.Destination.SolarSystemId);
                        Log.WriteLine("MissionSolarSystem is [" + ESCache.Instance.MissionSolarSystem.Name + "]");
                    }
                }
                else
                {
                    Log.WriteLine("We have no mission bookmark available for our current agent.");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                }
            }


            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
            {
                if (DirectEve.Interval(30000))
                {
                    Log.WriteLine(
                        "Traveller state = Error. This usually means that the destination system is invaded or there is no secure route to the system.");

                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive due invasion."));
                    // TODO: maybe add functionality to drop the mission and get a new one (take care of standing)
                }
                return;
            }

            ESCache.Instance.Traveler.ProcessState();

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
            {
                ESCache.Instance.State.CurrentActionControlState = ActionControlState.Start;
                ESCache.Instance.Traveler.Destination = null;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.ExecuteMission);
                return;
            }


        }

        private void GotoNearestStationCMBState()
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InSpace && ESCache.Instance.InWarp) return;
            EntityCache station = null;
            if (ESCache.Instance.Stations != null && ESCache.Instance.Stations.Any())
                station = ESCache.Instance.Stations.OrderBy(x => x.Distance).FirstOrDefault();

            if (station != null)
            {
                if (station.Distance <= (int)Distances.DockingRange)
                {
                    if (station.Dock())
                    {
                        Log.WriteLine("[" + station.Name + "] which is [" + Math.Round(station.Distance / 1000, 0) + "k away]");
                        return;
                    }

                    return;
                }
                else
                {
                    if (station.Distance < (int)Distances.WarptoDistance)
                        station.MoveTo();
                    else
                        ESCache.Instance.NavigateOnGrid.NavigateToTarget(station, "panic", false, 0);
                }

                return;
            }
            ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
        }

        private void IdleCMBState()
        {
            if (ESCache.Instance.InSpace)
            {
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                return;
            }

            ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
            ESCache.Instance.State.CurrentArmState = ArmState.Idle;
            ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
            ESCache.Instance.State.CurrentStorylineState = StorylineState.Idle;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.AtDestination;
            ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.Idle;
            ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Cleanup;
        }

        private void PrepareStorylineGotoBaseCMBState()
        {

            if (DebugConfig.DebugGotobase) Log.WriteLine("PrepareStorylineGotoBase: Traveler.TravelHome()");

            ESCache.Instance.Traveler.TravelHome();

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InDockableLocation)
            {
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                return;
            }

            return;
        }

        private void StartCMBState()
        {
            if (!_storyline.RemoveNotCompatibleStorylineMissions())
                return;

            if (ESCache.LootAlreadyUnloaded == false)
            {
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Switch;
                return;
            }

            if (ESCache.Instance.InSpace)
            {
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
                return;
            }

            ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;
            Log.WriteLine($"CurrentAgentInteractionState [{ESCache.Instance.State.CurrentAgentInteractionState}]");
            if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Idle)
            {
                ESCache.Instance.Wealth = ESCache.Instance.DirectEve.Me.Wealth;
                ESCache.Instance.Statistics.WrecksThisMission = 0;
                if (ESCache.Instance.EveAccount.CS.QMS.QS.EnableStorylines && _storyline.HasStoryline())
                {
                    Log.WriteLine("Storyline detected, doing storyline.");
                    _storyline.Reset();
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.PrepareStorylineSwitchAgents);
                    return;
                }

                Log.WriteLine("Start conversation [Start Mission]");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
                ESCache.Instance.AgentInteraction.Purpose = AgentInteractionPurpose.StartMission;
            }

            ESCache.Instance.AgentInteraction.ProcessState();

            if (ESCache.Instance.AgentInteraction.Purpose == AgentInteractionPurpose.CompleteMission)
            {
                if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
                {
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                    ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                }
                return;
            }

            if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                ESCache.Instance.Statistics.LoyaltyPointsTotal = ESCache.Instance.Agent.LoyaltyPoints ?? 0;
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm);
            }
        }

        private void QuitMissionCMBState()
        {
            ESCache.Instance.AgentInteraction.ProcessState();
            if (ESCache.Instance.State.CurrentAgentInteractionState == AgentInteractionState.Done)
            {
                ESCache.Instance.Statistics.LoyaltyPointsTotal = ESCache.Instance.Agent.LoyaltyPoints ?? 0;
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.Arm);
                return;
            }
        }

        private void StatisticsCMBState()
        {
            if (ESCache.Instance.Drones.UseDrones)
            {
                var drone = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.MissionSettings.CurrentDroneTypeId);
                if (drone != null && drone.Volume != 0)
                {
                    if (ESCache.Instance.Drones.DroneBay == null) return;
                    ESCache.Instance.Statistics.LostDrones = (int)Math.Floor((ESCache.Instance.Drones.DroneBay.Capacity - ESCache.Instance.Drones.DroneBay.UsedCapacity) / drone.Volume);
                }
                else
                {
                    Log.WriteLine("Could not find the drone TypeID specified in the character settings xml; this should not happen!");
                }
            }

            if (!ESCache.Instance.Statistics.AmmoConsumptionStatistics()) return;
            ESCache.Instance.Statistics.FinishedMission = DateTime.UtcNow;

            if (!ESCache.Instance.Statistics.MissionLoggingCompleted)
            {
                ESCache.Instance.Statistics.WriteMissionStatistics(ESCache.Instance.Agent.AgentId);
                return;
            }

            ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
        }

        private void StorylineReturnToBaseCMBState()
        {

            if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: TravelToStorylineBase");

            ESCache.Instance.Traveler.TravelHome();

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination && ESCache.Instance.InDockableLocation)
            {
                if (DebugConfig.DebugGotobase) Log.WriteLine("StorylineReturnToBase: We are at destination");
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Switch;
            }
        }

        private void SwitchCMBState()
        {
            if (!ESCache.Instance.InDockableLocation)
            {
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                return;
            }

            if (ESCache.Instance.DirectEve.Session.StationId != null && ESCache.Instance.Agent != null &&
                ESCache.Instance.DirectEve.Session.StationId != ESCache.Instance.Agent.StationId)
            {
                Log.WriteLine("We're not in the right station, going home.");
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                return;
            }

            if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Items == null || ESCache.Instance.DirectEve.GetItemHangar() == null ||
                ESCache.Instance.DirectEve.GetItemHangar().Items == null)
                return;

            if (ESCache.Instance.EveAccount.CS.QMS.BuyPlex && BuyPlexController.ShouldBuyPlex)
            {

                ControllerManager.Instance.RemoveController(typeof(QuestorController));
                ControllerManager.Instance.AddController(new BuyPlexController((() =>
                {
                    ControllerManager.Instance.AddController(typeof(QuestorController)); // what to do after the plex has been bought
                })));
                return;
            }

            if (ESCache.Instance.DirectEve.ActiveShip != null && ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any() &&
                ESCache.Instance.DirectEve.ActiveShip.GivenName.ToLower() != ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower())
            {
                Log.WriteLine("if(Cache.Instance.CurrentShipsCargo.Items.Any())");
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.UnloadLoot);
                return;
            }

            if (ESCache.Instance.State.CurrentArmState == ArmState.Idle)
            {
                Log.WriteLine("Begin");
                ESCache.Instance.State.CurrentArmState = ArmState.ActivateCombatShip;
                ESCache.Instance.Arm.SwitchShipsOnly = true;
            }

            if (DebugConfig.DebugArm) Log.WriteLine("CombatMissionBehavior.Switch is Entering Arm.Processstate");
            ESCache.Instance.Arm.ProcessState();

            if (ESCache.Instance.State.CurrentArmState == ArmState.Done)
            {
                Log.WriteLine("Done");
                ESCache.Instance.State.CurrentArmState = ArmState.Idle;
                ESCache.Instance.Arm.SwitchShipsOnly = false;
                ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.GotoBase);
            }
        }

        private void UnloadLootCMBState()
        {

            if (!ESCache.Instance.InDockableLocation)
                return;

            if (ESCache.Instance.State.CurrentUnloadLootState == UnloadLootState.Idle)
            {
                Log.WriteLine("UnloadLoot: Begin");
                ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.Begin;
            }

            ESCache.Instance.UnloadLoot.ProcessState();

            if (ESCache.Instance.State.CurrentUnloadLootState == UnloadLootState.Done)
            {
                ESCache.LootAlreadyUnloaded = true;
                ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.Idle;

                if (ESCache.Instance.State.CurrentCombatState == CombatState.OutOfAmmo)
                {
                    Log.WriteLine("_States.CurrentCombatState == CombatState.OutOfAmmo");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                    return;
                }

                if (ESCache.Instance.MissionSettings.Mission != null && ESCache.Instance.MissionSettings.Mission.State != (int)MissionState.Offered)
                {
                    Log.WriteLine("We are on mission");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                    return;
                }

                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Idle;
                ESCache.Instance.State.CurrentQuestorState = QuestorState.Start;
            }
        }
        #endregion Methods
    }
}