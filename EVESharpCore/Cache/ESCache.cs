extern alias SC;

using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py.D3DDetour;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EVESharpCore.Controllers.Questor;
using EVESharpCore.Controllers.Questor.Core.Activities;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Combat;
using EVESharpCore.Controllers.Questor.Core.Settings;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Controllers.Questor.Core.Stats;
using EVESharpCore.Controllers.Questor.Core.Storylines;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Traveller;

namespace EVESharpCore.Cache
{
    public partial class ESCache
    {
        #region Fields

        public static int CacheInstances;
        public static bool LootAlreadyUnloaded;
        public static DateTime NextSlotActivate = DateTime.UtcNow;
        public List<long> AgentBlacklist;
        public bool doneUsingRepairWindow;
        public HashSet<long> ListofContainersToLoot = new HashSet<long>();
        public HashSet<string> ListofMissionCompletionItemsToLoot = new HashSet<string>();
        public DirectLocation MissionSolarSystem;
        public bool NormalApproach = true;
        public bool NormalNavigation = true;
        public string OrbitEntityNamed;
        public bool QuestorJustStarted = true;
        private DirectAgent _agent;
        private EveAccount _eveAccount = null;
        private EveSetting _eveSetting = null;
        private DateTime _lastAnyWorthyTargetLeftWithinRange;

        #endregion Fields

        #region Constructors

        private ESCache()
        {
            LootedContainers = new HashSet<long>();
            InitInstances();
        }

        #endregion Constructors

        #region Properties

        public static ESCache Instance { get; } = new ESCache();
        public static Storyline Storyline { get; set; }
        public DirectActiveShip ActiveShip => Instance.DirectEve.ActiveShip;

        public DirectAgent Agent
        {
            get
            {
                if (_agent == null)
                    _agent = Instance.DirectEve.GetAgentByName(Instance.EveAccount.CS.QMS.AgentName);
                return _agent;
            }
            set => _agent = value;
        }

        // instances
        public AgentInteraction AgentInteraction { get; private set; }

        public bool ForceDumpLoop { get; set; }

        public Arm Arm { get; private set; }
        public string CharName { get; set; }
        public Combat Combat { get; private set; }
        public string CurrentPocketAction { get; set; }
        public DirectContainer CurrentShipsCargo => Instance.DirectEve.GetShipsCargo();
        public DirectEve DirectEve { get; set; }
        public Drones Drones { get; private set; }

        public bool SellError { get; set; }

        public EveAccount EveAccount
        {
            get
            {
                if (_eveAccount == null)
                {
                    try
                    {
                        _eveAccount = WCFClient.Instance.GetPipeProxy.GetEveAccount(CharName);
                        CancellationTokenSource eveAccountTokenSource = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            while (!eveAccountTokenSource.Token.IsCancellationRequested)
                            {
                                eveAccountTokenSource.Token.WaitHandle.WaitOne(2000);
                                try
                                {
                                    var r = WCFClient.Instance.GetPipeProxy.GetEveAccount(CharName);
                                    if (r != null)
                                        _eveAccount = r;
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine(e.ToString());
                                }
                            }
                        }, eveAccountTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.ToString());
                    }
                }
                return _eveAccount;
            }
        }

        public EveSetting EveSetting
        {
            get
            {
                if (_eveSetting == null)
                {
                    try
                    {
                        _eveSetting = WCFClient.Instance.GetPipeProxy.GetEVESettings();
                        CancellationTokenSource eveSettingTokenSource = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            while (!eveSettingTokenSource.Token.IsCancellationRequested)
                            {
                                eveSettingTokenSource.Token.WaitHandle.WaitOne(10000);
                                try
                                {
                                    var r = WCFClient.Instance.GetPipeProxy.GetEVESettings();
                                    if (r != null)
                                        _eveSetting = r;
                                }
                                catch (Exception e)
                                {
                                    Log.WriteLine(e.ToString());
                                }
                            }
                        }, eveSettingTokenSource.Token);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine(e.ToString());
                    }
                }
                return _eveSetting;
            }
        }

        public DirectContainer FittedModules => Instance.DirectEve.GetShipsModules();

        public bool InMission
        {
            get
            {
                if (!InDockableLocation)
                {
                    var station = Instance.Stations.OrderBy(s => s.Distance).FirstOrDefault();
                    var stargate = Instance.Stargates.OrderBy(s => s.Distance).FirstOrDefault();
                    var accelgate = Instance.AccelerationGates.OrderBy(s => s.Distance).FirstOrDefault();

                    if (station != null && station.Distance < 1000000)
                        return false;

                    if (stargate != null && stargate.Distance < 1000000)
                        return false;

                    if (accelgate != null && accelgate.Distance < 1000000)
                        return true;

                    if (Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                        return true;

                    if (ControllerManager.Instance.TryGetController<QuestorController>(out var qc))
                    {
                        if (Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.Storyline &&
                           Instance.State.CurrentStorylineState == StorylineState.ExecuteMission && qc.CombatMissionsBehaviorInstance.Storyline.StorylineHandler is GenericCombatStoryline &&
                           (qc.CombatMissionsBehaviorInstance.Storyline.StorylineHandler as GenericCombatStoryline).State == GenericCombatStorylineState.ExecuteMission)
                            return true;
                    }
                }
                return false;
            }
        }

        public HashSet<long> LootedContainers { get; private set; }
        public MissionSettings MissionSettings { get; private set; }
        public IEnumerable<DirectUIModule> Modules => Instance.DirectEve.Modules;
        public double MyWalletBalance { get; set; }
        public NavigateOnGrid NavigateOnGrid { get; private set; }
        public bool PauseAfterNextDock { get; set; }
        public State State { get; private set; }
        public Statistics Statistics { get; private set; }
        public Time Time { get; private set; }
        public Traveler Traveler { get; private set; }
        public UnloadLoot UnloadLoot { get; private set; }
        // end instances

        public double Wealth { get; set; }

        public double WealthatStartofPocket { get; set; }

        // TODO: read from game
        public int WeaponRange => (int)Instance.Combat.Ammo.Average(a => a.Range);

        public void InitInstances()
        {
            AgentInteraction = new AgentInteraction();
            Arm = new Arm();
            UnloadLoot = new UnloadLoot();
            NavigateOnGrid = new NavigateOnGrid();
            Combat = new Combat();
            Drones = new Drones();
            MissionSettings = new MissionSettings();
            State = new State();
            Traveler = new Traveler();
            Statistics = new Statistics();
            Time = new Time();
        }

        #endregion Properties

        #region Methods

        public static bool LoadDirectEVEInstance()
        {
            if (Instance.DirectEve == null)
                Instance.DirectEve = new DirectEve(new StandaloneFramework());

            return Instance.DirectEve != null;
        }

        public DirectItem CheckCargoForItem(int typeIdToFind) =>
            Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == typeIdToFind && i.Quantity >= 1);

        public void ClearPerPocketCache()
        {
            try
            {
                Instance.MissionSettings.ClearPocketSpecificSettings();
                Instance.Drones.LastTargetIDDronesEngaged = null;
                ListofContainersToLoot.Clear();
                ListofMissionCompletionItemsToLoot.Clear();
                LootedContainers.Clear();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public bool ExitEve(string Reason)
        {
            Log.WriteLine($"Closing E# with reason [{Reason}].");
            Log.RemoteWriteLine(Reason);
            Util.TaskKill(Process.GetCurrentProcess().Id);
            return false;
        }

        public void DisableThisInstance()
        {
            var msg = string.Format("Set [{0}] disabled.", Instance.EveAccount.CharacterName);
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValueBlocking(Instance.CharName, nameof(EveAccount.IsActive), false);
        }

        public bool GroupWeapons(bool ignoreWarp = false)
        {
            if (Instance.InSpace && (Instance.InWarp && !ignoreWarp) &&
                Instance.Time.LastGroupWeapons.AddSeconds(Instance.Time.Rnd.Next(15, 30)) < DateTime.UtcNow)
            {
                Instance.Time.LastGroupWeapons = DateTime.UtcNow;
                if (Instance.DirectEve.Weapons.Any() && Instance.DirectEve.Weapons.Count() > 1 &&
                    Instance.DirectEve.Weapons.All(w => w.IsOnline && !w.IsInLimboState && !w.IsActive))
                    if (Instance.ActiveShip != null && Instance.ActiveShip.Entity != null)
                        if (Instance.ActiveShip.CanGroupAll())
                        {
                            Instance.ActiveShip.GroupAllWeapons();
                            Log.WriteLine("Grouped weapons.");
                            return true;
                        }
            }

            return false;
        }

        public void InvalidateCache()
        {
            try
            {
                Instance.Arm.InvalidateCache();
                Instance.Drones.InvalidateCache();
                Instance.Combat.InvalidateCache();
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public bool IsAgentMissionFinished(bool ignoreObjectivesComplete = false)
        {
            var disableBlitz = MissionSettings.DisableBlitz ?? false;

            if (disableBlitz)
                return false;

            var objectivesComplete = Agent.ObjectivesComplete || ignoreObjectivesComplete;
            if (objectivesComplete)
            {
                var weaponMaxRange = Instance.Combat.MaxRange;
                var tractorsMaxRange = Instance.Modules.FirstOrDefault(e => e.GroupId == (int)Group.TractorBeam)?.OptimalRange ?? 0;
                var anyTractors = tractorsMaxRange != 0;
                var anyWorthyTargetLeftWithinRange = Instance.EntitiesOnGrid.Any(e => e.BracketType == BracketType.NPC_Battleship && e.Distance < weaponMaxRange);
                if (anyWorthyTargetLeftWithinRange)
                {
                    _lastAnyWorthyTargetLeftWithinRange = DateTime.UtcNow;
                }

                var anyWorthyTargetInRangeSeenInTheLast4Seconds = _lastAnyWorthyTargetLeftWithinRange.AddSeconds(4) > DateTime.UtcNow;
                var anyLargeWreckLeftWithinRange = anyTractors && Instance.EntitiesOnGrid.Any(e => e.IsLargeWreck && e.Distance < tractorsMaxRange);

                //Log.WriteLine($"weaponMaxRange {weaponMaxRange} tractorsMaxRange {tractorsMaxRange} anyTractors {anyTractors} anyWorthyTargetLeftWithinRange {anyWorthyTargetLeftWithinRange} anyWorthyTargetInRangeSeenInTheLast4Seconds {anyWorthyTargetInRangeSeenInTheLast4Seconds}");

                objectivesComplete = objectivesComplete
                                     && (Instance.MissionSettings.DelayBlitzIfBattleshipOnGrid == false
                                         || Instance.MissionSettings.DelayBlitzIfBattleshipOnGrid == null
                                         || (Instance.MissionSettings.DelayBlitzIfBattleshipOnGrid == true
                                             && !anyWorthyTargetLeftWithinRange
                                             && !anyWorthyTargetInRangeSeenInTheLast4Seconds))

                                     && (Instance.MissionSettings.DelayBlitzIfLargeWreckOnGrid == false
                                         || Instance.MissionSettings.DelayBlitzIfLargeWreckOnGrid == null
                                         || (Instance.MissionSettings.DelayBlitzIfLargeWreckOnGrid == true
                                             && !anyLargeWreckLeftWithinRange));
            }

            return objectivesComplete;
        }

        public bool IsMissionItem(DirectItem item) => Instance.MissionSettings.MissionItems.Contains((item.TypeName ?? string.Empty).ToLower())
                                                      || ListofMissionCompletionItemsToLoot.Contains((item.TypeName ?? string.Empty)
                                                          .ToLower()) || item.IsCommonMissionItem;

        public bool LocalSafe(double s)
        {
            var local = DirectEve.ChatWindows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));
            if (local == null)
            {
                Log.WriteLine($"local == null?");
                return true;
            }
            return local.Members.All(m => DirectEve.Standings.GetMinStanding(m) > s);
        }

        public int RandomNumber(int min, int max)
        {
            var random = new Random();
            return random.Next(min, max);
        }

        public void SetInfoAttribute(string info)
        {
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(Instance.CharName, nameof(EveAccount.Info), info);
        }

        #endregion Methods
    }
}