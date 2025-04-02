using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {

        #region Properties

        private long? _currentTargetId;

        #endregion Properties

        #region Methods

        private void ActivateSensorDampeners(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring SensorDamps Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var sensorDampeners = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.SensorDampener).ToList();

            foreach (var sensorDampener in sensorDampeners)
            {
                if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(sensorDampener.ItemId))
                    if (ESCache.Instance.Time.LastActivatedTimeStamp[sensorDampener.ItemId].AddMilliseconds(ESCache.Instance.Time.PainterDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (sensorDampener.IsActive)
                {
                    if (sensorDampener.TargetId != target.Id)
                    {
                        if (sensorDampener.Click()) return;
                        return;
                    }

                    continue;
                }

                if (sensorDampener.IsDeactivating)
                    continue;

                if (CanActivate(sensorDampener, target, false))
                {
                    if (sensorDampener.Activate(target.Id))
                    {
                        Log.WriteLine("Activating [" + sensorDampener.TypeName + "][" + sensorDampener.ItemId + "] on [" + target.Name + "][" + target.DirectEntity.Id.ToString() +
                                      "][" +
                                      Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }
                    continue;
                }
            }
        }

        private void ActivateStasisWeb(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring StasisWeb Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var webs = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.StasisWeb).ToList();

            foreach (var web in webs)
            {
                if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(web.ItemId))
                    if (ESCache.Instance.Time.LastActivatedTimeStamp[web.ItemId].AddMilliseconds(ESCache.Instance.Time.WebDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (web.IsActive)
                {
                    if (web.TargetId != target.Id)
                    {
                        if (web.Click()) return;

                        return;
                    }

                    continue;
                }

                if (web.IsDeactivating)
                    continue;

                if (target.Distance >= web.OptimalRange)
                    continue;

                if (CanActivate(web, target, false))
                {
                    if (web.Activate(target.Id))
                    {
                        Log.WriteLine("Activating [" + web.TypeName + "][" + web.ItemId + "] on [" + target.Name + "][" + target.DirectEntity.Id.ToString() + "]");
                        return;
                    }

                    continue;
                }
            }
        }

        private void ActivateTargetPainters(EntityCache target)
        {
            if (target.IsEwarImmune)
            {
                if (DebugConfig.DebugKillTargets)
                    Log.WriteLine("Ignoring TargetPainter Activation on [" + target.Name + "]IsEwarImmune[" + target.IsEwarImmune + "]");
                return;
            }

            var targetPainters = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TargetPainter && m.IsOnline).ToList();
            foreach (var painter in targetPainters)
            {
                if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(painter.ItemId))
                    if (ESCache.Instance.Time.LastActivatedTimeStamp[painter.ItemId].AddMilliseconds(ESCache.Instance.Time.PainterDelay_milliseconds) > DateTime.UtcNow)
                        continue;

                if (painter.IsActive)
                {
                    if (painter.TargetId != target.Id)
                    {
                        if (painter.Click()) return;
                    }
                    continue;
                }

                if (painter.IsDeactivating)
                {
                    if (DebugConfig.DebugKillTargets)
                        Log.WriteLine("if (painter.IsDeactivating)");
                    continue;
                }

                if (CanActivate(painter, target, false))
                {
                    if (painter.Activate(target.Id))
                    {
                        Log.WriteLine("Activating [" + painter.TypeName + "][" + painter.ItemId + "] on [" + target.Name + "][" + target.DirectEntity.Id.ToString() + "][" +
                                      Math.Round(target.Distance / 1000, 0) + "k away]");
                        return;
                    }
                    else
                    {
                        if (DebugConfig.DebugKillTargets)
                            Log.WriteLine("!painter.Activate(target.Id)");
                    }

                    continue;
                }
                else
                {
                    if (DebugConfig.DebugKillTargets)
                        Log.WriteLine("!CanActivate(painter, target, false)");
                }
            }
        }

        private void ActivateWeapons(EntityCache target)
        {
            if (ESCache.Instance.InSpace && ESCache.Instance.InWarp)
            {
                if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
                    RemovePrimaryWeaponPriorityTargets(PrimaryWeaponPriorityEntities.ToList());

                if (ESCache.Instance.Drones.UseDrones && ESCache.Instance.Drones.DronePriorityEntities != null && ESCache.Instance.Drones.DronePriorityEntities.Any())
                    ESCache.Instance.Drones.RemoveDronePriorityTargets(ESCache.Instance.Drones.DronePriorityEntities.ToList());

                return;
            }

            if (!ESCache.Instance.DirectEve.Weapons.Any())
            {
                return;
            }

            if (DebugConfig.DebugActivateWeapons)
                Log.WriteLine("ActivateWeapons: deactivate: after navigate into range...");

            if (DebugConfig.DebugActivateWeapons)
                Log.WriteLine("ActivateWeapons: deactivate: Do we need to deactivate any weapons?");

            if (ESCache.Instance.DirectEve.Weapons.Any())
            {
                foreach (var weapon in ESCache.Instance.DirectEve.Weapons)
                {
                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: deactivate: for each weapon [" + weapon.ItemId + "] in weapons");

                    if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                        if (ESCache.Instance.Time.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(ESCache.Instance.Time.WeaponDelay_milliseconds) > DateTime.UtcNow)
                            continue;

                    if (!weapon.IsActive)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + weapon.ItemId +
                                          "] is not active: no need to do anything");
                        continue;
                    }

                    if (weapon.IsReloadingAmmo)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is reloading ammo: waiting");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is deactivating: waiting");
                        continue;
                    }

                    if (weapon.Charge == null)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: no ammo loaded? [" + weapon.TypeName + "][" + weapon.ItemId +
                                          "] reload will happen elsewhere");
                        continue;
                    }

                    var ammo = Ammo.FirstOrDefault(a => a.TypeId == weapon.Charge.TypeId);

                    if (ammo == null)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: deactivate: ammo == null [" + weapon.TypeName + "][" + weapon.ItemId +
                                          "] someone manually loaded ammo?");
                        continue;
                    }



                    if (!ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                        if (target.Distance <= ammo.Range)
                        {
                            if (ESCache.Instance.EveAccount.CS.QMS.QS.PreventWeaponAmmoZero)
                            {
                                var minCharge = ESCache.Instance.EveAccount.CS.QMS.QS.PreventWeaponAmmoZero ? 1 : 0;
                                if (weapon.Charge.Quantity > minCharge)
                                {
                                    if (DebugConfig.DebugActivateWeapons)
                                        Log.WriteLine("ActivateWeapons: deactivate: target is in range && ammo charges >= 1: do nothing");
                                    continue;
                                }
                            }
                            else
                            {
                                if (DebugConfig.DebugActivateWeapons)
                                    Log.WriteLine("ActivateWeapons: deactivate: target is in range: do nothing, wait until it is dead");
                                continue;
                            }
                        }


                    if (DebugConfig.DebugActivateWeapons)
                        Log.WriteLine("ActivateWeapons: deactivate: target is out of range, stop firing");
                    if (weapon.Click()) return;
                    return;
                }

                var weaponsActivatedThisTick = 0;
                var weaponsToActivateThisTick = ESCache.Instance.RandomNumber(3, 5);

                if (DebugConfig.DebugActivateWeapons)
                    Log.WriteLine("ActivateWeapons: activate: Do we need to activate any weapons?");
                foreach (var weapon in ESCache.Instance.DirectEve.Weapons)
                {
                    if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(weapon.ItemId))
                        if (ESCache.Instance.Time.LastActivatedTimeStamp[weapon.ItemId].AddMilliseconds(ESCache.Instance.Time.WeaponDelay_milliseconds) > DateTime.UtcNow)
                            continue;

                    if (weapon.IsReloadingAmmo)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is reloading, waiting.");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is deactivating, waiting.");
                        continue;
                    }

                    if (!target.IsTarget)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is [" + target.Name +
                                          "] is not locked, waiting.");
                        continue;
                    }

                    if (weapon.IsActive)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + "] is active already");
                        if (weapon.TargetId != target.Id && target.IsTarget)
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId +
                                              "] is shooting at the wrong target: deactivating");
                            if (weapon.Click()) return;

                            return;
                        }
                        continue;
                    }

                    if (!ReloadAmmo(weapon))
                        continue;

                    var minCharge = ESCache.Instance.EveAccount.CS.QMS.QS.PreventWeaponAmmoZero ? 1 : 0;

                    if (weapon.CurrentCharges <= minCharge)
                    {
                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + $"] charge qty == {minCharge}.");
                        continue;
                    }

                    if (ESCache.Instance.Modules.Any(m => m.GroupId == (int)Group.TargetPainter && m.IsOnline))
                    {
                        if (!target.IsEwarImmune && (target.IsNPCFrigate || target.IsNPCCruiser)
                            && !ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TargetPainter
                                                                   && m.IsOnline).All(m => m.TargetId == target.Id))
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine("ActivateWeapons: Waiting for all target painters to be on the target.");
                            continue;
                        }
                    }

                    if (CanActivate(weapon, target, true))
                    {
                        if (weaponsActivatedThisTick > weaponsToActivateThisTick)
                        {
                            if (DebugConfig.DebugActivateWeapons)
                                Log.WriteLine(
                                    "ActivateWeapons: if we have already activated x number of weapons return, which will wait until the next ProcessState");
                            return;
                        }

                        if (DebugConfig.DebugActivateWeapons)
                            Log.WriteLine("ActivateWeapons: Activate: [" + weapon.TypeName + "][" + weapon.ItemId + "] has the correct ammo: activate");
                        if (weapon.Activate(target.Id))
                        {
                            weaponsActivatedThisTick++;
                            Log.WriteLine("Activating weapon[" + weapon.ItemId + "] on [" + target.Name + "][" + target.DirectEntity.Id.ToString() + "][" +
                                          Math.Round(target.Distance / 1000, 0) + "k] away");
                            continue;
                        }

                        continue;
                    }
                }
            }
            else
            {
                Log.WriteLine("ActivateWeapons: you have no weapons");
                foreach (var __module in ESCache.Instance.Modules.Where(e => e.IsOnline && e.IsActivatable))
                {
                    Log.WriteLine("[" + __module.ItemId + "] Module TypeID [ " + __module.TypeId + " ] ModuleGroupID [ " + __module.GroupId +
                                  " ] EveCentral Link [ http://eve-central.com/home/quicklook.html?typeid=" + __module.TypeId + " ]");
                }
            }
        }

        private bool CanActivate(DirectUIModule uiModule, EntityCache entity, bool isWeapon)
        {
            if (!uiModule.IsOnline)
                return false;

            if (uiModule.IsActive || !uiModule.IsActivatable)
                return false;

            if (isWeapon && !entity.IsTarget)
            {
                Log.WriteLine("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) +
                              "] which is currently not locked!");
                return false;
            }

            if (isWeapon && entity.Distance > MaxRange)
            {
                Log.WriteLine("We attempted to shoot [" + entity.Name + "][" + Math.Round(entity.Distance / 1000, 2) +
                              "] which is out of weapons range!");
                return false;
            }

            if (uiModule.IsReloadingAmmo)
                return false;

            if (uiModule.IsDeactivating)
                return false;

            return true;
        }

        #endregion Methods
    }
}