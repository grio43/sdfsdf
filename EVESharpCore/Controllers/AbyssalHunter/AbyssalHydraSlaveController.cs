extern alias SC;
using EVESharpCore.Controllers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms.VisualStyles;
using EVESharpCore.Framework;
using SC::SharedComponents.IPC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Traveller;
using static EVESharpCore.Controllers.AbyssalHunter.AbyssalHunterController;
using static EVESharpCore.Controllers.Abyssal.AbyssalGuard.AbyssalGuardController;

namespace EVESharpCore.Controllers.AbyssalHunter
{


    public class HydraSlaveState
    {
        public string SlaveName;
        public long CharacterId;
        public int SolarSystemId { get; set; }
        public long DockableLocationId { get; set; }
        public int ShipTypeId { get; set; }
        public int[] ModuleTypeIds { get; set; }
        public int[] ModuleGroupIds { get; set; }
    }


    public class AbyssalHydraSlaveController : BaseController, IOnFrameController
    {

        enum HydraSlaveStates
        {
            None,
            GoingToNeighbourSystemSun,
            GoingToMaster,
            PreloadModules,
            Gank,
            Error,
        }

        private bool _allowNextFrameAction;
        private HydraMasterState _hydraMasterState;
        private HydraSlaveStates _state;
        private int _neighbourSolarSystemId;

        public override void DoWork()
        {
            // Use DoWorkRR instead

            if (_hydraMasterState != null && !string.IsNullOrEmpty(_hydraMasterState.MasterName))
            {

                if (!DirectEve.Interval(3000))
                    return;

                var p = new HydraSlaveState()
                {
                    CharacterId = Framework.Session.CharacterId.Value,
                    SolarSystemId = Framework.Session?.SolarSystemId ?? 0,
                    DockableLocationId = Framework.Session?.StationId ?? 0,
                    ShipTypeId = Framework.ActiveShip.TypeId,
                    SlaveName = Framework.Session.Character.Name,
                    ModuleTypeIds = Framework.Modules.Select(e => e.TypeId).ToArray(),
                    ModuleGroupIds = Framework.Modules.Select(e => e.GroupId).ToArray()
                };

                SendBroadcastMessage(_hydraMasterState.MasterName, nameof(AbyssalHydraController),
                    HydraMessageTypes.BroadcastSlaveState.ToString(), p);
            }

        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage bc)
        {
            if (bc.Command == HydraMessageTypes.BroadcastGankEntityId.ToString())
            {
                _broadcastTarget = bc.GetPayload<long>();
                Log($"Received broadcast gank entity id [{_broadcastTarget}]");
            }

            if (bc.Command == HydraMessageTypes.BroadcastMasterState.ToString())
            {
                _hydraMasterState = bc.GetPayload<HydraMasterState>();
                if (DirectEve.Interval(10000))
                {
                    Log($"Master state: {_hydraMasterState.MasterName} - IsInSpace: {_hydraMasterState.IsInSpace} - IsInDockableLocation: {_hydraMasterState.IsInDockableLocation} - SolarSystemId: {_hydraMasterState.SolarSystemId} - DockableLocationId: {_hydraMasterState.DockableLocationId}");
                }
            }

            if (bc.Command == HydraMessageTypes.ComeToMe.ToString())
            {
                _state = HydraSlaveStates.GoingToMaster;
            }

            if (bc.Command == HydraMessageTypes.ComeToNeighbourSystemSun.ToString())
            {
                _neighbourSolarSystemId = bc.GetPayload<int>();
                Log($"Set _neighbourSolarSystemId to [{_neighbourSolarSystemId}]");
                _state = HydraSlaveStates.GoingToNeighbourSystemSun;
            }

            if (bc.Command == HydraMessageTypes.RoundRobinAllowAction.ToString())
            {
                _allowNextFrameAction = true;
            }

            if (bc.Command == HydraMessageTypes.PreloadModules.ToString())
            {
                _state = HydraSlaveStates.PreloadModules;
            }

            if (bc.Command == HydraMessageTypes.OrderSlavesToGoIdle.ToString())
            {
                _state = HydraSlaveStates.None;
            }
        }

        private void GoingToNeighbourSystemSun()
        {
            if (_neighbourSolarSystemId == 0)
                return;

            if (Framework.Session.SolarSystemId != _neighbourSolarSystemId &&
                _neighbourSolarSystemId != 0)
            {
                if (ESCache.Instance.Traveler.Destination == null ||
                    ESCache.Instance.Traveler.Destination.SolarSystemId != _neighbourSolarSystemId)
                {
                    Log($"Set destination to SolarSystem Id {_neighbourSolarSystemId}");
                    ESCache.Instance.Traveler.Destination = new SolarSystemDestination(_neighbourSolarSystemId);
                }

                try
                {
                    if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        ESCache.Instance.Traveler.ProcessState();
                        return;
                    }
                    else
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        ESCache.Instance.Traveler.Destination = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    _state = HydraSlaveStates.Error;
                    return;
                }

                return;
            }

            if (_hydraMasterState.IsInSpace && !Framework.Session.IsInSpace)
            {
                Log("Docked, undocking");
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                return;
            }

            // Warp to the sun if we are not close to the sun
            if (Framework.Session.IsInSpace)
            {
                long _starDist = 500000000;
                var star = ESCache.Instance.Star;
                if (ESCache.Instance.Star.Distance <= _starDist)
                {
                    Log($"We are at the star. Changing the state to idle.");
                    _state = HydraSlaveStates.None;
                    return;
                }
                else
                {
                    if (Framework.Me.CanIWarp() && DirectEve.Interval(3000, 5000))
                    {
                        Log($"Warping to the star.");
                        star.WarpTo();
                        return;
                    }
                }
            }
            _state = HydraSlaveStates.None;
        }

        private void DoWorkRR()
        {

            if (DirectEve.Interval(4000))
                Log($"State [{_state}]");

            switch (_state)
            {
                case HydraSlaveStates.None:
                    _broadcastTarget = null;
                    break;
                case HydraSlaveStates.GoingToNeighbourSystemSun:
                    GoingToNeighbourSystemSun();
                    break;
                case HydraSlaveStates.GoingToMaster:
                    GoingToMaster();
                    break;
                case HydraSlaveStates.PreloadModules:
                    PreloadModules();
                    break;
                case HydraSlaveStates.Gank:
                    Gank();
                    break;
                case HydraSlaveStates.Error:
                    break;
            }
        }

        private long? _broadcastTarget = null;

        private void Gank()
        {

            if (_broadcastTarget == null)
            {
                var bcs = Framework.GetTargetBroadcasts();
                if (bcs.Count == 0)
                {
                    return;
                }

                if (!bcs.TryGetValue(_hydraMasterState.MasterCharacterId, out var bc))
                {
                    return;
                }

                var targetId = bc.FirstOrDefault();

                if (targetId == 0)
                    return;

                _broadcastTarget = targetId;
            }

            if (Framework.ActiveShip.Entity.IsWarpingByMode)
                return;

            if (_broadcastTarget.Value == Framework.ActiveShip.Entity.Id)
                return;

            var bcEnt = Framework.GetEntityById(_broadcastTarget.Value);

            if (bcEnt == null)
                return;

            if (bcEnt.IsInvulnerable)
            {
                if (!bcEnt.IsApproachedOrKeptAtRangeByActiveShip)
                {
                    if (DirectEve.Interval(1500, 3500))
                    {
                        Log($"Approaching target with Id [{_broadcastTarget.Value}]");
                        bcEnt.Approach();
                        return;
                    }
                }
                return;
            }

            // bc ent is now targetable, we will activate guns and other offensive modules automatically

            if (bcEnt.IsTargeting)
                return;

            if (!bcEnt.IsTarget)
            {
                Log($"Locking target with Id [{_broadcastTarget.Value}]");
                bcEnt.LockTarget();
                return;
            }

            if (!bcEnt.IsApproachedOrKeptAtRangeByActiveShip)
            {
                if (DirectEve.Interval(1500, 3500))
                {
                    Log($"Approaching target with Id [{_broadcastTarget.Value}]");
                    bcEnt.Approach();
                    return;
                }
            }
            _state = HydraSlaveStates.None;
        }

        private void PreloadModules()
        {
            // unlock all targets if there is any lock
            // ensure highslots are grouped
            // ensure safety is red
            // activate offensive modules
            // overload all modules (except prop)
            // turn on prop mod if any available

            // if there is any target locked, unlock all
            var lockedTargets = ESCache.Instance.Targets;
            var targetingTargets = ESCache.Instance.Targeting;

            if (targetingTargets.Any())
                return;

            foreach (var target in lockedTargets)
            {
                Log($"Unlocking target [{target.Id}] Name [{target.Name}]");
                target.UnlockTarget();
                return;
            }

            // ensure highslots are grouped
            if (ESCache.Instance.GroupWeapons(true))
            {
                Log($"Grouped weapons.");
                return;
            }

            // ensure safety is red
            if (Framework.Me.GetSafety() != DirectMe.SafetyLevel.ShipSafetyLevelNone)
            {
                Log($"Setting RED safety.");
                Framework.Me.SetSafety(DirectMe.SafetyLevel.ShipSafetyLevelNone);
                return;
            }

            // activate offensive modules
            var offensiveModules = Framework.Modules.Where(e => (e.IsEffectOffensive ?? false) && e.IsOnline && e.IsActivatable).ToList();

            foreach (var module in offensiveModules)
            {
                if (module.IsActive)
                    continue;

                if (module.WaitingForActiveTarget)
                    continue;

                if (!module.IsOverloaded && !module.IsOverloadLimboState)
                {
                    Log($"Toggling overload for module [{module.ItemId}]");
                    module.ToggleOverload();
                    return;
                }

                module.Click();
                return;
            }

            var nonOffensiveAndActivatableModules = Framework.Modules.Where(e => !(e.IsEffectOffensive ?? false) && e.IsOnline && e.IsActivatable && e.EffectName != "miningLaser").ToList();

            if (Framework.ActiveShip.Entity.IsWarpingByMode)
            {
                if (DirectEve.Interval(3000))
                    Log($"IsWarpingByMode");
                return;
            }

            foreach (var module in nonOffensiveAndActivatableModules)
            {
                if (module.IsActive)
                    continue;

                Log($"Clicking module with Id [{module.ItemId}]");
                module.Click();
                return;
            }

            _broadcastTarget = null;
            _state = HydraSlaveStates.Gank;
            Log("Finished preload, transitioning to gank.");
        }

        private void GoingToMaster()
        {

            if (_hydraMasterState == null || _hydraMasterState.SolarSystemId == 0)
                return;


            if (Framework.Session.SolarSystemId != _hydraMasterState.SolarSystemId &&
                _hydraMasterState.SolarSystemId != 0)
            {
                if (ESCache.Instance.Traveler.Destination == null ||
                    ESCache.Instance.Traveler.Destination.SolarSystemId != _hydraMasterState.SolarSystemId)
                {
                    Log($"Set destination to SolarSystem Id {_hydraMasterState.SolarSystemId}");
                    ESCache.Instance.Traveler.Destination = new SolarSystemDestination(_hydraMasterState.SolarSystemId);
                }

                try
                {
                    if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        ESCache.Instance.Traveler.ProcessState();
                        return;
                    }
                    else
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        ESCache.Instance.Traveler.Destination = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    _state = HydraSlaveStates.Error;
                    return;
                }

                return;
            }

            if (Framework.Session.StationId != _hydraMasterState.DockableLocationId &&
                _hydraMasterState.DockableLocationId != 0)
            {

                bool isDockableDest = ESCache.Instance.Traveler.Destination is DockableLocationDestination;
                if (ESCache.Instance.Traveler.Destination == null || !isDockableDest || ((DockableLocationDestination)ESCache.Instance.Traveler.Destination).DockableLocationId != _hydraMasterState.DockableLocationId)
                {
                    Log($"Set destination to DockableLocationId Id {_hydraMasterState.DockableLocationId}");
                    ESCache.Instance.Traveler.Destination = new DockableLocationDestination(_hydraMasterState.DockableLocationId);
                }

                try
                {
                    if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        ESCache.Instance.Traveler.ProcessState();
                        return;
                    }
                    else
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        ESCache.Instance.Traveler.Destination = null;
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log(ex.ToString());
                    _state = HydraSlaveStates.Error;
                    return;
                }

                return;
            }

            if (_hydraMasterState.IsInSpace && !Framework.Session.IsInSpace)
            {
                Log("Docked, undocking");
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                return;
            }

            // If we are not docked warp to the master
            if (Framework.Session.IsInSpace && _hydraMasterState.IsInSpace)
            {
                var master = Framework.FleetMembers.FirstOrDefault(x => x.Name == _hydraMasterState.MasterName);
                if (master != null && !_hydraMasterState.IsWarping)
                {
                    if (Framework.Me.CanIWarp() && master.Entity == null)
                    {
                        if (DirectEve.Interval(2500, 4500))
                        {
                            Log($"Warping to master [{_hydraMasterState.MasterName}]");
                            master.WarpToMember();
                            return;
                        }
                    }
                    if (Framework.Me.CanIWarp() && master.Entity.Distance > 150_000)
                    {
                        if (DirectEve.Interval(2500, 4500))
                        {
                            Log($"Warping to master [{_hydraMasterState.MasterName}]");
                            master.WarpToMember();
                            return;
                        }
                    }
                }
            }

            // If we are close to the master go to idle
            _state = HydraSlaveStates.None;
        }

        public void OnFrame()
        {
            if (!_allowNextFrameAction)
                return;

            try
            {
                DoWorkRR();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                _allowNextFrameAction = false;
            }
        }
    }
}