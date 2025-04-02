using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Methods

        public void InvalidateCache()
        {
            try
            {
                _aggressed = null;
                _combatTargets = null;
                _maxrange = null;
                _potentialCombatTargets = null;
                _primaryWeaponPriorityTargetsPerFrameCaching = null;
                _targetedBy = null;

                _primaryWeaponPriorityEntities = null;
                _preferredPrimaryWeaponTarget = null;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public void ProcessState()
        {
            try
            {
                if (ESCache.Instance.InWarp)
                    return;

                if (!ESCache.Instance.InSpace
                    || ESCache.Instance.InDockableLocation
                    || ESCache.Instance.ActiveShip.Entity.IsCloaked)
                {
                    ESCache.Instance.State.CurrentCombatState = CombatState.Idle;
                    if (DebugConfig.DebugCombat) Log.WriteLine("We are in station or cloaked. Idle state.");
                    return;
                }

                switch (ESCache.Instance.State.CurrentCombatState)
                {
                    case CombatState.TargetCombatants:
                        ESCache.Instance.State.CurrentCombatState = CombatState.KillTargets;
                        TargetCombatants();
                        break;

                    case CombatState.KillTargets:
                        if (ESCache.Instance.InWarp) return;
                        ESCache.Instance.State.CurrentCombatState = CombatState.TargetCombatants;

                        if (DebugConfig.DebugPreferredPrimaryWeaponTarget || DebugConfig.DebugKillTargets)
                            if (ESCache.Instance.Targets.Any())
                                if (PreferredPrimaryWeaponTarget != null)
                                    Log.WriteLine("PreferredPrimaryWeaponTarget [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                                  Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k][" +
                                                  PreferredPrimaryWeaponTarget.DirectEntity.Id.ToString() + "]");
                                else
                                    Log.WriteLine("PreferredPrimaryWeaponTarget [ null ]");

                        EntityCache killTarget = null;

                        if (ESCache.Instance.Targets.Any(i => !i.IsContainer && !i.IsBadIdea))
                            killTarget = ESCache.Instance.Targets.Where(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxRange && i.IsTarget)
                                .OrderByDescending(k => k.IsWarpScramblingOrDisruptingMe)
                                .OrderByDescending(k => k.IsNeutralizingMe)
                                .OrderByDescending(k => k.IsPreferredPrimaryWeaponTarget)
                                .ThenByDescending(i => i.IsInOptimalRange)
                                .ThenByDescending(i => i.IsCorrectSizeForMyWeapons)
                                .ThenBy(i => i.Distance)
                                .FirstOrDefault();

                        if (killTarget != null)
                        {
                            if (!ESCache.Instance.InMission || ESCache.Instance.NavigateOnGrid.SpeedTank)
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log.WriteLine("Navigate Toward the Closest Preferred PWPT");
                                ESCache.Instance.NavigateOnGrid.NavigateIntoRange(killTarget, "Combat", ESCache.Instance.NormalNavigation);
                            }

                            if (killTarget.IsReadyToShoot)
                            {
                                if (_currentTargetId != killTarget.Id)
                                    _currentTargetId = killTarget.Id;

                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("Activating TPs.");
                                ActivateTargetPainters(killTarget);
                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("Activating Webs.");
                                ActivateStasisWeb(killTarget);
                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("Activating SensorDampeners.");
                                ActivateSensorDampeners(killTarget);
                                if (DebugConfig.DebugKillTargets)
                                    Log.WriteLine("Activating Weapons.");
                                ActivateWeapons(killTarget);
                            }
                            return;
                        }

                        if (DebugConfig.DebugKillTargets)
                            Log.WriteLine("We do not have a killTarget targeted, waiting");

                        if (PrimaryWeaponPriorityTargets.Any() ||
                            PotentialCombatTargets.Any() && ESCache.Instance.Targets.Any() 
                                                         && (!ESCache.Instance.InMission || ESCache.Instance.NavigateOnGrid.SpeedTank))
                        {
                            GetBestPrimaryWeaponTarget(MaxRange);
                        }

                        break;

                    case CombatState.OutOfAmmo:
                        if (ESCache.Instance.InDockableLocation)
                        {
                            Log.WriteLine("Out of ammo. Pausing questor if in station.");
                            ControllerManager.Instance.SetPause(true);
                        }

                        break;

                    case CombatState.Idle:

                        if (ESCache.Instance.InSpace && !ESCache.Instance.ActiveShip.Entity.IsCloaked &&
                            ESCache.Instance.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower())
                        {
                            ESCache.Instance.State.CurrentCombatState = CombatState.TargetCombatants;
                            if (DebugConfig.DebugCombat)
                                Log.WriteLine("We are in space and ActiveShip is null or Cloaked or we arent in the combatship or we are in warp");
                            return;
                        }
                        break;

                    default:

                        Log.WriteLine("CurrentCombatState was not set thus ended up at default");
                        ESCache.Instance.State.CurrentCombatState = CombatState.TargetCombatants;
                        break;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}