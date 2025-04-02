// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Activities
{
    public class UnloadLoot
    {
        #region Fields

        private DateTime _lastUnloadAction = DateTime.MinValue;
        private bool LootIsBeingMoved;

        #endregion Fields

        #region Properties

        private long CurrentLootValue { get; set; }

        #endregion Properties

        #region Methods

        public long CurrentLootValueInCurrentShipInventory()
        {
            return LootValueOfItems(LootItemsInCurrentShipInventory());
        }

        public long CurrentLootValueInItemHangar()
        {
            return LootValueOfItems(LootItemsInItemHangar());
        }

        public bool IsLootItem(DirectItem i)
        {
            // need to add recently added items, like new scripts etc
            return (ESCache.Instance.Combat.Ammo.All(a => a.TypeId != i.TypeId)
                    && ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript != i.TypeId
                    && ESCache.Instance.EveAccount.CS.QMS.QS.Factionfittings.All(a => a.DronetypeId != i.TypeId)
                    && ESCache.Instance.EveAccount.CS.QMS.QS.DroneTypeId != i.TypeId
                    && i.Volume != 0
                    && i.TypeId != (int)CategoryID.Skill
                    && i.TypeId != (int)TypeID.AngelDiamondTag
                    && i.TypeId != (int)TypeID.GuristasDiamondTag
                    && i.TypeId != (int)TypeID.ImperialNavyGatePermit
                    && i.GroupId != (int)Group.AccelerationGateKeys
                    && i.GroupId != (int)Group.Livestock
                    && i.GroupId != (int)Group.MiscSpecialMissionItems
                    && i.GroupId != 448 // containers
                    && i.GroupId != (int)Group.Commodities
                    && i.TypeId != (int)TypeID.AncillaryShieldBoosterScript
                    && i.TypeId != (int)TypeID.CapacitorInjectorScript
                    && i.TypeId != (int)TypeID.FocusedWarpDisruptionScript
                    && i.TypeId != (int)TypeID.OptimalRangeDisruptionScript
                    && i.TypeId != (int)TypeID.OptimalRangeScript
                    && i.TypeId != (int)TypeID.ScanResolutionDampeningScript
                    && i.TypeId != (int)TypeID.ScanResolutionScript
                    && i.TypeId != (int)TypeID.TargetingRangeDampeningScript
                    && i.TypeId != (int)TypeID.TargetingRangeScript
                    && i.TypeId != (int)TypeID.TrackingSpeedDisruptionScript
                    && i.TypeId != (int)TypeID.TrackingSpeedScript
                    && i.GroupId != (int)Group.CapacitorGroupCharge)
                   // explicit loot items below
                   || i.TypeId == 17895 // High - Tech Manufacturing Tools 
                   || i.TypeId == 17795 // Amarr MIY -1 Nexus Chip
                   || i.TypeId == 15331  // Metal scraps
                   || i.TypeId == 30497; // Reinforced Metal scraps
        }

        public List<DirectItem> LootItemsInCurrentShipInventory()
        {
            return ESCache.Instance.CurrentShipsCargo.Items.Where(IsLootItem).ToList();
        }

        public List<DirectItem> LootItemsInItemHangar()
        {
            return ESCache.Instance.DirectEve.GetItemHangar().Items.Where(IsLootItem).ToList();
        }

        public List<DirectItem> NonLootItemsInItemHangar()
        {
            return ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => !IsLootItem(i)).ToList();
        }

        public long LootValueOfItems(List<DirectItem> items)
        {
            long lootValue = 0;
            foreach (var item in items)
                lootValue += (long)item.AveragePrice() * Math.Max(item.Quantity, 1);
            return lootValue;
        }

        public void ProcessState()
        {
            if (!ESCache.Instance.InDockableLocation) return;

            switch (ESCache.Instance.State.CurrentUnloadLootState)
            {
                case UnloadLootState.Idle:
                    break;

                case UnloadLootState.Done:
                    break;

                case UnloadLootState.Begin:
                    LootIsBeingMoved = false;
                    _lastUnloadAction = DateTime.UtcNow.AddMinutes(-1);
                    ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.StackItemHangar;
                    CurrentLootValue = 0;
                    break;

                case UnloadLootState.StackItemHangar:
                    if (!StackItemHangar()) return;
                    break;

                case UnloadLootState.MoveLoot:
                    if (!MoveLoot()) return;
                    break;
            }
        }

        private bool MoveLoot()
        {
            if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(1000))
                return false;

            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log.WriteLine(" if (QCache.Instance.ItemHangar == null)");
                return false;
            }

            var lootToMove = ESCache.Instance.CurrentShipsCargo.Items.ToList();

            if (lootToMove.Any() && ESCache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower())
            {
                foreach (var ammo in ESCache.Instance.Combat.Ammo)
                {
                    var amount = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ammo.TypeId && !i.IsSingleton).Sum(i => i.Stacksize);
                    if (amount <= ammo.Quantity * 2) // we forgot ammo in weapons
                    {
                        lootToMove.RemoveAll(e => e.TypeId == ammo.TypeId);
                    }
                }
            }

            if (lootToMove.Any() && !LootIsBeingMoved)
            {
                CurrentLootValue = CurrentLootValueInCurrentShipInventory();
                if (ESCache.Instance.DirectEve.GetItemHangar().Add(lootToMove))
                {
                    Log.WriteLine("Moving [" + lootToMove.Count() + "] items from CargoHold to Loothangar");
                    LootIsBeingMoved = true;
                    _lastUnloadAction = DateTime.UtcNow;
                }
                return false;
            }
            else
            {
                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                    return false;

                Log.WriteLine("Loot was worth an estimated [" + CurrentLootValue.ToString("#,##0") + "] isk in buy-orders");
                LootIsBeingMoved = false;
                ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.Done;
                return true;
            }
        }

        private bool StackItemHangar()
        {
            try
            {
                if (DateTime.UtcNow < _lastUnloadAction.AddMilliseconds(ESCache.Instance.RandomNumber(2000, 3000)))
                    return false;

                try
                {
                    Log.WriteLine("Stacking item hangar.");
                    if (!(ESCache.Instance.DirectEve.GetItemHangar() != null
                          && ESCache.Instance.DirectEve.GetItemHangar().StackAll())) return false;
                    ESCache.Instance.State.CurrentUnloadLootState = UnloadLootState.MoveLoot;
                    return true;
                }
                catch (NullReferenceException)
                {
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return false;
        }

        #endregion Methods
    }
}