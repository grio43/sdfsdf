extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using AmmoType = SC::SharedComponents.EVE.ClientSettings.AmmoType;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Methods

        public bool ReloadAll()
        {

            if (DebugConfig.DebugReloadAll)
                Log.WriteLine("Weapons (or stacks of weapons?): [" + ESCache.Instance.DirectEve.Weapons.Count() + "]");

            if (ESCache.Instance.DirectEve.Weapons.Any())
            {
                foreach (var weapon in ESCache.Instance.DirectEve.Weapons)
                {

                    if (!weapon.CanBeReloaded)
                        continue;

                    if (weapon.MaxCharges > 0 && weapon.CurrentCharges == weapon.MaxCharges)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("Weapon [" + weapon.ItemId + "] has [" + weapon.CurrentCharges + "] charges. MaxCharges is [" +
                                          weapon.MaxCharges +
                                          "]: checking next weapon");
                        continue;
                    }

                    if (weapon.IsEnergyWeapon)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("if (weapon.IsEnergyWeapon) continue (energy weapons do not really need to reload)");
                        continue;
                    }

                    if (weapon.IsReloadingAmmo)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("[" + weapon.TypeName + "][" + weapon.ItemId + "] is still reloading, moving on to next weapon");
                        continue;
                    }

                    if (weapon.IsDeactivating)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("[" + weapon.TypeName + "][" + weapon.ItemId + "] is still Deactivating, moving on to next weapon");
                        continue;
                    }

                    if (weapon.IsActive)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("[" + weapon.TypeName + "][" + weapon.ItemId + "] is Active, moving on to next weapon");
                        continue;
                    }

                    if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any())
                        if (!ReloadAmmo(weapon))
                            continue;

                }

                Log.WriteLine("Reloaded all weapons");
                return true;
            }

            return true;
        }

        private bool ReloadAmmo(DirectUIModule weapon, [CallerMemberName] string caller = "")
        {

            return weapon.IsEnergyWeapon ? ReloadEnergyWeaponAmmo(weapon, caller) : ReloadNormalAmmo(weapon, caller);
        }

        private bool ReloadEnergyWeaponAmmo(DirectUIModule weapon, string caller)
        {
            if (!weapon.CanBeReloaded)
                return true;

            IEnumerable<AmmoType> correctAmmo = Ammo.Where(a => a.DamageType == ESCache.Instance.MissionSettings.CurrentDamageType).ToList();

            IEnumerable<AmmoType> correctAmmoInCargo =
                correctAmmo.Where(a => ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId))
                    .ToList();

            correctAmmoInCargo =
                correctAmmoInCargo.Where(
                        a =>
                            ESCache.Instance.CurrentShipsCargo != null &&
                            ESCache.Instance.CurrentShipsCargo.Items.Any(e => e.TypeId == a.TypeId && e.Quantity >= ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges))
                    .ToList();

            if (!correctAmmoInCargo.Any())
            {
                if (ESCache.Instance.MissionSettings.CurrentDamageType.HasValue && ESCache.Instance.MissionSettings.AnyAmmoOfTypeLeft(ESCache.Instance.MissionSettings.CurrentDamageType.Value))
                {
                    Log.WriteLine($"No charges left in ships cargo, using the remaining charges in the launchers before swapping to the second best damage type.");
                    return true;
                }
                else
                {
                    Log.WriteLine("ReloadEnergyWeapon: not enough [" + ESCache.Instance.MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                                  ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges + "]");
                    ESCache.Instance.State.CurrentCombatState = CombatState.OutOfAmmo;
                    return false;
                }
            }

            if (weapon.Charge != null)
            {
                var areWeMissingAmmo = correctAmmoInCargo.Where(a => a.TypeId == weapon.Charge.TypeId);
                if (!areWeMissingAmmo.Any())
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We have ammo loaded that does not have a full reload available in the cargo.");
            }

            var ammo = correctAmmoInCargo.OrderBy(a => a.Range).FirstOrDefault();

            if (ammo == null)
            {
                if (DebugConfig.DebugReloadorChangeAmmo)
                    Log.WriteLine("ReloadEnergyWeaponAmmo: best possible ammo: [ ammo == null]");
                return false;
            }

            var charge = ESCache.Instance.CurrentShipsCargo.Items.OrderBy(e => e.Quantity).FirstOrDefault(e => e.TypeId == ammo.TypeId);

            if (charge == null)
            {
                if (DebugConfig.DebugReloadorChangeAmmo)
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We do not have any ammo left that can hit targets at that range!");
                return false;
            }

            if (DebugConfig.DebugReloadorChangeAmmo)
                Log.WriteLine("ReloadEnergyWeaponAmmo: charge: [" + charge.TypeName + "][" + charge.TypeId + "]");

            if (weapon.Charge != null && weapon.Charge.TypeId == ammo.TypeId)
            {
                if (DebugConfig.DebugReloadorChangeAmmo)
                    Log.WriteLine("ReloadEnergyWeaponAmmo: We have Enough Ammo of that type Loaded Already");
                return true;
            }

            if (weapon.IsReloadingAmmo)
                return true;

            return weapon.ChangeAmmo(charge);
        }

        private bool ReloadNormalAmmo(DirectUIModule weapon, string caller)
        {
            if (!weapon.CanBeReloaded)
                return true;

            List<AmmoType> correctAmmoToUse;
            List<AmmoType> correctAmmoInCargo;

            if (Ammo.Any(a => a.DamageType == ESCache.Instance.MissionSettings.CurrentDamageType))
            {
                correctAmmoToUse = Ammo.Where(a => a.DamageType == ESCache.Instance.MissionSettings.CurrentDamageType).ToList();

                correctAmmoInCargo =
                    correctAmmoToUse.Where(
                            a =>
                                ESCache.Instance.CurrentShipsCargo != null &&
                                ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == a.TypeId && i.Quantity >= ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges))
                        .ToList();

                if (!correctAmmoInCargo.Any())
                {
                    if (ESCache.Instance.MissionSettings.CurrentDamageType.HasValue && ESCache.Instance.MissionSettings.AnyAmmoOfTypeLeft(ESCache.Instance.MissionSettings.CurrentDamageType.Value))
                    {
                        Log.WriteLine($"No charges left in ships cargo, using the remaining charges in the launchers before swapping to the second best damage type.");
                        return true;
                    }
                    else
                    {
                        Log.WriteLine("Not enough [" + ESCache.Instance.MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                                      ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges +
                                      "] Note: CurrentDamageType [" + ESCache.Instance.MissionSettings.CurrentDamageType + "]");
                        ESCache.Instance.State.CurrentCombatState = CombatState.OutOfAmmo;
                        return false;
                    }
                }
            }
            else
            {
                if (ESCache.Instance.CurrentShipsCargo != null)
                {
                    var result =
                        Ammo.ToList()
                            .Where(a => ESCache.Instance.CurrentShipsCargo.Items.ToList().Any(c => c.TypeId == a.TypeId && c.Quantity >= ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges));

                    if (result.Any())
                        correctAmmoInCargo = result.ToList();
                    else
                        return false;
                }
                else
                {
                    Log.WriteLine("ReloadNormalAmmo: if Cache.Instance.CurrentShipsCargo == null");
                    ESCache.Instance.State.CurrentCombatState = CombatState.OutOfAmmo;
                    return false;
                }
            }

            var ammo = correctAmmoInCargo.FirstOrDefault();
            try
            {
                if (ammo != null)
                    ammo = correctAmmoInCargo.OrderBy(a => a.Range).FirstOrDefault();
            }
            catch (Exception exception)
            {
                Log.WriteLine("ReloadNormalAmmo: [" + weapon.ItemId + "] Unable to find the correct ammo: waiting [" + exception + "]");
                return false;
            }

            if (ammo == null)
            {
                if (DebugConfig.DebugReloadAll)
                    Log.WriteLine("[" + weapon.ItemId + "] We do not have any ammo left that can hit targets at that range!");
                return false;
            }

            if (weapon == null)
            {
                if (DebugConfig.DebugReloadAll) Log.WriteLine("weapon == null");
                return false;
            }

            if (weapon.CurrentCharges > weapon.MaxCharges)
            {
                Log.WriteLine($"if (weapon.CurrentCharges [{weapon.CurrentCharges}] > weapon.MaxCharges [{weapon.MaxCharges}])");
            }

            var minCharge = ESCache.Instance.EveAccount.CS.QMS.QS.PreventWeaponAmmoZero ? 1 : 0;
            if (!caller.Equals(nameof(ReloadAll)) && weapon.Charge != null && weapon.CurrentCharges > minCharge && weapon.Charge.TypeId == ammo.TypeId && weapon.CurrentCharges <= weapon.MaxCharges)
            {
                if (DebugConfig.DebugReloadAll)
                    Log.WriteLine("[" + weapon.ItemId + "] MaxRange - if we have 0 charges MaxRange will be 0");
                return true;
            }

            DirectItem charge = null;
            if (ESCache.Instance.CurrentShipsCargo != null)
            {
                if (ESCache.Instance.CurrentShipsCargo.Items.Any())
                {
                    charge = ESCache.Instance.CurrentShipsCargo.Items.FirstOrDefault(e => e.TypeId == ammo.TypeId && e.Quantity >= ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges);
                    if (charge == null)
                    {
                        if (DebugConfig.DebugReloadAll)
                            Log.WriteLine("We have no ammo in cargo?! This should have shown up as out of ammo");
                        return false;
                    }
                }
                else
                {
                    if (DebugConfig.DebugReloadAll)
                        Log.WriteLine("We have no items in cargo at all?! This should have shown up as out of ammo");
                    return false;
                }
            }
            else
            {
                if (DebugConfig.DebugReloadAll) Log.WriteLine("CurrentShipsCargo is null?!");
                return false;
            }

            if (weapon.IsReloadingAmmo)
            {
                if (DebugConfig.DebugReloadAll)
                    Log.WriteLine("We are already reloading, wait - weapon.IsReloadingAmmo [" + weapon.IsReloadingAmmo + "]");
                return true;
            }

            try
            {
                if (weapon.ChangeAmmo(charge))
                    return true;

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }

            return true;
        }

        #endregion Methods
    }
}