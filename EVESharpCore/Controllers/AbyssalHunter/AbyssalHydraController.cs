extern alias SC;
using EVESharpCore.Controllers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;
using ServiceStack.Text;

namespace EVESharpCore.Controllers.AbyssalHunter
{

    public enum HydraMessageTypes
    {
        RoundRobinAllowAction,
        BroadcastMasterState,
        BroadcastSlaveState,
        ComeToMe,
        ComeToNeighbourSystemSun,
        PreloadModules,
        OrderSlavesToGoIdle,
        BroadcastGankEntityId,
    }


    public class HydraMasterState
    {
        public string MasterName;
        public bool IsInSpace { get; set; }
        public bool IsInDockableLocation { get; set; }
        public int SolarSystemId { get; set; }
        public long DockableLocationId { get; set; }
        public bool IsWarping { get; set; }
        public long MasterCharacterId { get; set; }
    }

    public class AbyssalHydraController : BaseController, IOnFrameController
    {


        private Dictionary<string, HydraSlaveState> _slaveStates;

        // Will be the CovOps
        // Will invite the slaves
        // Will round robin actions
        // Hast a list of character names for the enslavement
        // If the master sees a character which is not in fleet, it will broadcast and invite to fleet

        public AbyssalHydraController()
        {
            Form = new AbyssalHydraControllerForm(this);
            _slaveStates = new Dictionary<string, HydraSlaveState>();
        }

        private List<string> SlavesNames
        {
            get
            {
                var slaves = ESCache.Instance.EveAccount.ClientSetting.AbyssalHunterMainSetting
                    ?.SlaveCharacterNames
                    ?.Split(',').Select(x => x.Trim()).ToList() ?? new List<string>();

                var scramDisruptorGroupId = 52;
                var webGroupId = 65;

                slaves = slaves.OrderByDescending(e =>
                        _slaveStates.ContainsKey(e) && _slaveStates[e].ModuleGroupIds.Contains(scramDisruptorGroupId))
                    .ThenByDescending(e =>
                        _slaveStates.ContainsKey(e) && _slaveStates[e].ModuleGroupIds.Contains(webGroupId)).ToList();
                return slaves;
            }
        }

        private int _currentRRSlaveIndex;


        public bool GankState { get; set; }
        public string GankTargetCharacterName { get; set; }

        public override void DoWork()
        {
            HandleSlavesInvites();
            SendMasterState();
        }

        private void SendMasterState()
        {
            if (DirectEve.Interval(1000))
            {
                var data = new HydraMasterState
                {
                    MasterName = Framework.Session.Character.Name,
                    IsInSpace = Framework.Session.IsInSpace,
                    IsInDockableLocation = Framework.Session.IsInDockableLocation,
                    SolarSystemId = Framework.Session.SolarSystemId ?? 0,
                    DockableLocationId = Framework.Session.StationId ?? 0,
                    IsWarping = Framework.ActiveShip?.Entity?.IsWarpingByMode ?? false,
                    MasterCharacterId = Framework.Session.CharacterId ?? 0,
                };

                foreach (var slave in SlavesNames)
                {
                    SendBroadcastMessage(slave, nameof(AbyssalHydraSlaveController), HydraMessageTypes.BroadcastMasterState.ToString(), data);
                }
            }
        }

        private void HandleSlavesInvites()
        {
            var backgroundWorker = ControllerManager.Instance.GetController<BackgroundWorkerController>();

            if (SlavesNames.Any())
                backgroundWorker.SetInviteMembers(SlavesNames);
        }

        public void ComeToMe()
        {
            SendBroadcastToSlaves(HydraMessageTypes.ComeToMe.ToString(), "");
            _currentRRSlaveIndex = 0;
        }


        public void PreloadModules()
        {
            SendBroadcastToSlaves(HydraMessageTypes.PreloadModules.ToString(), "");
            _currentRRSlaveIndex = 0;
        }

        public void IdleSlaves()
        {
            SendBroadcastToSlaves(HydraMessageTypes.OrderSlavesToGoIdle.ToString(), "");
            _currentRRSlaveIndex = 0;
        }

        private void SendBroadcastToSlaves<T>(string command, T parameter)
        {
            foreach (var slave in SlavesNames)
            {
                SendBroadcastMessage(slave, nameof(AbyssalHydraSlaveController), command, parameter);
            }
        }

        public void ComeToAdjacentSystemSun()
        {
            // Find a neighbour which doesn't route through our current system from Jita (30000142)
            var neighbours = Framework.SolarSystems[Framework.Session.SolarSystemId.Value].GetNeighbours(2).Where(e => e.GetSecurity() >= 0.45);
            var jita = Framework.SolarSystems[30000142];
            var excludeSystems = Framework.GetInsurgencyInfestedSystems();
            DirectSolarSystem chosenSolarSystem = null;
            var routeLength = int.MaxValue;
            foreach (var neighbour in neighbours)
            {
                var route = jita.CalculatePathTo(neighbour, excludeSystems.ToHashSet());
                if (route.Item1.Any(x => x.Id == Framework.Session.SolarSystemId.Value))
                    continue;

                if (route.Item1.Count < routeLength)
                {
                    routeLength = route.Item1.Count;
                    chosenSolarSystem = neighbour;
                }
            }

            if (chosenSolarSystem == null)
            {
                Log("No adjacent system found. Trying to take any neighbour.");
                chosenSolarSystem = neighbours.FirstOrDefault();

                if (chosenSolarSystem == null)
                {
                    Log("Warn: No valid neighbour found.");
                    return;
                }
            }

            SendBroadcastToSlaves(HydraMessageTypes.ComeToNeighbourSystemSun.ToString(), chosenSolarSystem.Id);
            _currentRRSlaveIndex = 0;
        }

        private void HandleRoundRobin()
        {
            var minRRMs = ESCache.Instance.EveAccount.ClientSetting.AbyssalHunterMainSetting.BroadcastDelayMinimum;
            var maxRRMs = ESCache.Instance.EveAccount.ClientSetting.AbyssalHunterMainSetting.BroadcastDelayMaximum;

            if (!DirectEve.Interval(minRRMs, maxRRMs))
                return;

            var slave = SlavesNames[_currentRRSlaveIndex % SlavesNames.Count];
            _currentRRSlaveIndex++;

            SendBroadcastMessage(slave, nameof(AbyssalHydraSlaveController),
                HydraMessageTypes.RoundRobinAllowAction.ToString(), "");
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }


        public override void ReceiveBroadcastMessage(BroadcastMessage bc)
        {
            if (bc.Command == HydraMessageTypes.BroadcastSlaveState.ToString())
            {
                var slaveState = bc.GetPayload<HydraSlaveState>();
                _slaveStates[slaveState.SlaveName] = slaveState;
            }
        }

        public void OnFrame()
        {
            if (GankState && DirectEve.Interval(80, 150))
            {
                if (string.IsNullOrEmpty(GankTargetCharacterName))
                {
                    var trace = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(
                        i => i.GroupId == (int)Group.AbyssalTrace);
                    if (trace != null)
                    {
                        var target = Framework.Entities.FirstOrDefault(e =>
                            e.IsPlayer && Framework.FleetMembers.All(f => f.CharacterId != e.OwnerId)
                            && e.DirectAbsolutePosition.GetDistance(trace.DirectEntity.DirectAbsolutePosition) > 4000
                            && e.DirectAbsolutePosition.GetDistance(trace.DirectEntity.DirectAbsolutePosition) < 6000);

                        if (target != null)
                        {
                            var name = Framework.GetOwner(target.OwnerId).Name;
                            if (!string.IsNullOrEmpty(name))
                            {
                                Log($"Found a gank target with name [{name}]. Setting the target.");
                                GankTargetCharacterName = name;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(GankTargetCharacterName))
                {
                    var target = Framework.Entities.FirstOrDefault(e =>
                        e.IsPlayer && Framework.GetOwner(e.OwnerId).Name == GankTargetCharacterName);
                    if (target != null)
                    {
                        if (DirectEve.Interval(2000))
                            SendBroadcastToSlaves(HydraMessageTypes.BroadcastGankEntityId.ToString(), target.Id);

                        if (!target.IsInvulnerable)
                        {
                            if (DirectEve.Interval(30000))
                                _currentRRSlaveIndex = 0;
                        }
                    }
                }
            }

            HandleRoundRobin();
        }
    }
}
