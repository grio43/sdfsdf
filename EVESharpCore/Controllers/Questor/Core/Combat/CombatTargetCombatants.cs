extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Methods

        private void TargetCombatants()
        {
            if (ESCache.Instance.InSpace && ESCache.Instance.InWarp
                || ESCache.Instance.InDockableLocation
                || DateTime.UtcNow < ESCache.Instance.Time.NextTargetAction
            )
                return;

            #region ECM Jamming checks

            if (ESCache.Instance.MaxLockedTargets == 0)
            {
                if (!_isJammed)
                    Log.WriteLine("We are jammed, trying to lock the jammers.");

                _isJammed = true;

                var jammers = ESCache.Instance.Combat.TargetedBy.Where(t => t.IsJammingMe || t.IsTryingToJamMe || t.DirectEntity.IsJammingEntity).OrderBy(e => e.IsJammingMe).ToList();

                foreach (var jammer in jammers)
                {
                    if (!jammer.IsTargeting && !jammer.IsTarget && DirectEve.Interval(3500, 4500))
                    {
                        Log.WriteLine($"Targeting jammer [{jammer.Id}].");
                        jammer.LockTarget("");
                    }
                }

                return;
            }

            if (_isJammed)
            {
                ESCache.Instance.Drones.LastTargetIDDronesEngaged = null;
                Log.WriteLine("We are no longer jammed, reTargeting");
            }


            if (ESCache.Instance.MaxLockedTargets < 3)
            {
                Log.WriteLine("Warning: ESCache.Instance.MaxLockedTargets < 3!");
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Warning: ESCache.Instance.MaxLockedTargets < 3"));
            }

            _isJammed = false;

            #endregion ECM Jamming checks

            #region Current active targets/targeting

            highValueTargetsTargeted = ESCache.Instance.EntitiesOnGrid.Where(t => (t.IsTarget || t.IsTargeting) && t.IsHighValueTarget).ToList();

            lowValueTargetsTargeted = ESCache.Instance.EntitiesOnGrid.Where(t => (t.IsTarget || t.IsTargeting) && t.IsLowValueTarget).ToList();

            var targetsTargeted = highValueTargetsTargeted.Count() + lowValueTargetsTargeted.Count();

            #endregion Current active targets/targeting

            #region Remove any target that is out of range (lower of Weapon Range or targeting range, definitely matters if damped)

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Remove any target that is out of range");
            if (ESCache.Instance.Targets.Any() && ESCache.Instance.Targets.Count() > 1 && ESCache.Instance.Time.NextUnlockTargetOutOfRange < DateTime.UtcNow)
            {
                ESCache.Instance.Time.NextUnlockTargetOutOfRange = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.Rnd.Next(5, 8));
                if (!UnlockLowValueTarget("[lowValue]OutOfRange or Ignored", true)) return;
                if (!UnlockHighValueTarget("[highValue]OutOfRange or Ignored", true)) return;
            }

            #endregion Remove any target that is out of range (lower of Weapon Range or targeting range, definitely matters if damped)

            var lockedTargets = ESCache.Instance.EntitiesOnGrid.Where(t => t.IsTarget || t.IsTargeting);
            var nextPotentialTarget = PotentialCombatTargets.Where(t => !t.IsTarget && !t.IsTargeting).OrderBy(t => t.Distance).FirstOrDefault();

            #region focus scramblers/neuts

            var scramblers = ESCache.Instance.Combat.TargetedBy.Where(t => t.IsWarpScramblingOrDisruptingMe || t.DirectEntity.IsWarpDisruptingEntity ||  t.DirectEntity.IsWarpScramblingEntity).ToList();
            var neuts = ESCache.Instance.Combat.TargetedBy.Where(t => t.IsNeutralizingMe || t.DirectEntity.IsNeutingEntity).ToList();
            var neutsAndScrambler = scramblers.Concat(neuts);

            var untargetedScramblersOrNeuts = neutsAndScrambler.Where(t => !t.IsTarget && !t.IsTargeting);
            if (untargetedScramblersOrNeuts.Any())
            {
                Log.WriteLine("Not targeted neuts/scrams/disrupts detected: ");
                Log.WriteLine("--------------------------------");
                foreach (var t in untargetedScramblersOrNeuts)
                {
                    Log.WriteLine($"Name {t.Name} Id {t.Id} Scram {t.IsWarpScramblingOrDisruptingMe || t.DirectEntity.IsWarpDisruptingEntity || t.DirectEntity.IsWarpScramblingEntity} Neut {t.IsNeutralizingMe || t.DirectEntity.IsNeutingEntity}");
                }
                Log.WriteLine("--------------------------------");
                var nextScramOrNeut = untargetedScramblersOrNeuts.FirstOrDefault();
                if (lockedTargets.Count() < ESCache.Instance.MaxLockedTargets)
                {
                    Log.WriteLine($"Locking [{nextScramOrNeut.Name}]");
                    nextScramOrNeut.LockTarget("Scrams/Neuts");
                }
                else
                {
                    var unlockTarget = lockedTargets.FirstOrDefault(t => !t.IsWarpScramblingOrDisruptingMe && !t.IsNeutralizingMe);
                    if (unlockTarget != null && unlockTarget.IsTarget)
                    {
                        Log.WriteLine($"Unlocking a random target [{nextScramOrNeut.Name}] to make room for scramblers/neuts.");
                        unlockTarget.UnlockTarget();
                    }
                }
                return;
            }

            #endregion scramblers/neuts

            if (nextPotentialTarget != null &&
                nextPotentialTarget.Distance <= MaxRange &&
                lockedTargets.Any() &&
                lockedTargets.All(t => t.Distance > MaxRange) &&
                !lockedTargets.Contains(nextPotentialTarget) &&
                lockedTargets.Count() < ESCache.Instance.MaxLockedTargets)
            {
                Log.WriteLine("!DEBUG!: Targeting nextPotentialTarget priority target [" + nextPotentialTarget.Name + "][" +
                              nextPotentialTarget.DirectEntity.Id.ToString() + "][" + Math.Round(nextPotentialTarget.Distance / 1000, 0) + "k away]");

                if (nextPotentialTarget.LockTarget("TargetCombatants.nextPotentialTarget"))
                {
                    ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                    if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                        ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                    return;
                }
            }

            #region Priority Target Handling

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Priority Target Handling");
            if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("We have [" + PrimaryWeaponPriorityEntities.Count() + "] PWPT. We have [" +
                                  ESCache.Instance.TotalTargetsandTargeting.Count() +
                                  "] TargetsAndTargeting. We have [" + PrimaryWeaponPriorityEntities.Count(i => i.IsTarget) +
                                  "] PWPT that are already targeted");
                var PrimaryWeaponsPriorityTargetUnTargeted = PrimaryWeaponPriorityEntities.Count() -
                                                             ESCache.Instance.TotalTargetsandTargeting.Count(t => PrimaryWeaponPriorityEntities.Contains(t));

                if (PrimaryWeaponsPriorityTargetUnTargeted > 0)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("if (PrimaryWeaponsPriorityTargetUnTargeted > 0)");
                    if (!UnlockHighValueTarget("PrimaryWeaponPriorityTargets")) return;

                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("if (!UnlockHighValueTarget(Combat.TargetCombatants, PrimaryWeaponPriorityTargets return;");

                    IEnumerable<EntityCache> _primaryWeaponPriorityEntities = PrimaryWeaponPriorityEntities.Where(
                            t => t.IsTargetWeCanShootButHaveNotYetTargeted)
                        .OrderByDescending(c => c.IsCurrentTarget)
                        .ThenByDescending(c => c.IsInOptimalRange)
                        .ThenBy(c => c.Distance);

                    if (_primaryWeaponPriorityEntities.Any())
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: [" + _primaryWeaponPriorityEntities.Count() + "] primaryWeaponPriority targets");

                        foreach (var primaryWeaponPriorityEntity in _primaryWeaponPriorityEntities)
                        {
                            if (highValueTargetsTargeted.Count() >= MaxHighValueTargets)
                            {
                                if (DebugConfig.DebugTargetCombatants)
                                    Log.WriteLine("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() +
                                                  "] >= maxHighValueTargets [" +
                                                  MaxHighValueTargets + "]");
                                break;
                            }

                            if (primaryWeaponPriorityEntity.Distance < MaxRange
                                && primaryWeaponPriorityEntity.IsReadyToTarget)
                            {
                                if (ESCache.Instance.TotalTargetsandTargeting.Count() < ESCache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                    if (primaryWeaponPriorityEntity.LockTarget("TargetCombatants.PrimaryWeaponPriorityEntity"))
                                    {
                                        Log.WriteLine("Targeting primary weapon priority target [" + primaryWeaponPriorityEntity.Name + "][" +
                                                      primaryWeaponPriorityEntity.DirectEntity.Id.ToString() + "][" +
                                                      Math.Round(primaryWeaponPriorityEntity.Distance / 1000, 0) + "k away]");
                                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                                        if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                                            ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                                            ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                                        return;
                                    }

                                if (ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (lowValueTargetsTargeted.Any())
                                    {
                                        UnlockLowValueTarget("PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    if (highValueTargetsTargeted.Any())
                                    {
                                        UnlockHighValueTarget("PriorityTarget Needs to be targeted");
                                        return;
                                    }
                                }
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: 0 primaryWeaponPriority targets");
                    }
                }
            }

            #endregion Priority Target Handling

            #region Drone Priority Target Handling

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Drone Priority Target Handling");
            if (ESCache.Instance.Drones.DronePriorityTargets.Any())
            {
                var dronesPriorityTargetUnTargeted = ESCache.Instance.Drones.DronePriorityEntities.Count() -
                                                     ESCache.Instance.TotalTargetsandTargeting.Count(t => ESCache.Instance.Drones.DronePriorityEntities.Contains(t));

                if (dronesPriorityTargetUnTargeted > 0)
                {
                    if (!UnlockLowValueTarget("DronePriorityTargets")) return;

                    IEnumerable<EntityCache> _dronePriorityTargets = ESCache.Instance.Drones.DronePriorityEntities.Where(t => t.IsTargetWeCanShootButHaveNotYetTargeted)
                        .OrderByDescending(c => c.IsInDroneRange)
                        .ThenByDescending(c => c.IsCurrentTarget)
                        .ThenBy(c => c.Distance);

                    if (_dronePriorityTargets.Any())
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: [" + _dronePriorityTargets.Count() + "] dronePriority targets");

                        foreach (var dronePriorityEntity in _dronePriorityTargets)
                        {
                            if (lowValueTargetsTargeted.Count() >= MaxLowValueTargets)
                            {
                                if (DebugConfig.DebugTargetCombatants)
                                    Log.WriteLine("DebugTargetCombatants: __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() +
                                                  "] >= maxLowValueTargets [" +
                                                  MaxLowValueTargets + "]");
                                break;
                            }

                            if (dronePriorityEntity.Distance < ESCache.Instance.Drones.MaxDroneRange
                                && dronePriorityEntity.IsReadyToTarget
                                && dronePriorityEntity.Distance < MaxRange
                                && !dronePriorityEntity.IsIgnored)
                            {
                                if (ESCache.Instance.TotalTargetsandTargeting.Count() < ESCache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                    if (dronePriorityEntity.LockTarget("TargetCombatants.PrimaryWeaponPriorityEntity"))
                                    {
                                        Log.WriteLine("Targeting primary weapon priority target [" + dronePriorityEntity.Name + "][" +
                                                      dronePriorityEntity.DirectEntity.Id.ToString() + "][" +
                                                      Math.Round(dronePriorityEntity.Distance / 1000, 0) + "k away]");
                                        return;
                                    }

                                if (ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.TargetingSlotsNotBeingUsedBySalvager)
                                {
                                    if (lowValueTargetsTargeted.Any())
                                    {
                                        UnlockLowValueTarget("PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    if (highValueTargetsTargeted.Any())
                                    {
                                        UnlockHighValueTarget("PriorityTarget Needs to be targeted");
                                        return;
                                    }

                                    ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);
                                }
                            }

                            continue;
                        }
                    }
                    else
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: 0 primaryWeaponPriority targets");
                    }
                }
            }

            #endregion Drone Priority Target Handling

            #region Preferred Primary Weapon target handling

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Preferred Primary Weapon target handling");
            if (PreferredPrimaryWeaponTarget != null)
            {
                if (PreferredPrimaryWeaponTarget.IsIgnored)
                    Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget.IsIgnored) Combat.PreferredPrimaryWeaponTarget = null;");

                if (PreferredPrimaryWeaponTarget != null)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget != null)");
                    if (ESCache.Instance.EntitiesOnGrid.Any(e => e.Id == PreferredPrimaryWeaponTarget.Id))
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("if (Cache.Instance.Entities.Any(i => i.Id == Combat.PreferredPrimaryWeaponTarget.Id))");

                        if (PreferredPrimaryWeaponTarget.IsReadyToTarget)
                        {
                            if (DebugConfig.DebugTargetCombatants)
                                Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget.IsReadyToTarget)");
                            if (PreferredPrimaryWeaponTarget.Distance <= MaxRange)
                            {
                                if (DebugConfig.DebugTargetCombatants)
                                    Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget.Distance <= Combat.MaxRange)");
                                if (highValueTargetsTargeted.Count() >= MaxHighValueTargets && MaxHighValueTargets > 1)
                                {
                                    if (DebugConfig.DebugTargetCombatants)
                                        Log.WriteLine("DebugTargetCombatants: we have enough targets targeted [" +
                                                      ESCache.Instance.TotalTargetsandTargeting.Count() + "]");
                                    if (!UnlockLowValueTarget("PreferredPrimaryWeaponTarget")
                                        || !UnlockHighValueTarget("PreferredPrimaryWeaponTarget"))
                                        return;

                                    return;
                                }

                                if (PreferredPrimaryWeaponTarget.LockTarget("TargetCombatants.PreferredPrimaryWeaponTarget"))
                                {
                                    Log.WriteLine("Targeting preferred primary weapon target [" + PreferredPrimaryWeaponTarget.Name + "][" +
                                                  PreferredPrimaryWeaponTarget.DirectEntity.Id.ToString() + "][" +
                                                  Math.Round(PreferredPrimaryWeaponTarget.Distance / 1000, 0) + "k away]");
                                    ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                                    if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                                        ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                                    return;
                                }
                            }

                            return;
                        }
                    }
                }
            }

            #endregion Preferred Primary Weapon target handling

            #region Preferred Drone target handling

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Preferred Drone target handling");
            if (ESCache.Instance.Drones.PreferredDroneTarget != null)
            {
                if (ESCache.Instance.Drones.PreferredDroneTarget.IsIgnored)
                    ESCache.Instance.Drones.PreferredDroneTarget = null;

                if (ESCache.Instance.Drones.PreferredDroneTarget != null
                    && ESCache.Instance.EntitiesOnGrid.Any(I => I.Id == ESCache.Instance.Drones.PreferredDroneTarget.Id)
                    && ESCache.Instance.Drones.UseDrones
                    && ESCache.Instance.Drones.PreferredDroneTarget.IsReadyToTarget
                    && ESCache.Instance.Drones.PreferredDroneTarget.Distance < ESCache.Instance.WeaponRange
                    && ESCache.Instance.Drones.PreferredDroneTarget.Distance <= ESCache.Instance.Drones.MaxDroneRange)
                {
                    if (lowValueTargetsTargeted.Count() >= MaxLowValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: we have enough targets targeted [" + ESCache.Instance.TotalTargetsandTargeting.Count() +
                                          "]");
                        if (!UnlockLowValueTarget("PreferredPrimaryWeaponTarget")
                            || !UnlockHighValueTarget("PreferredPrimaryWeaponTarget"))
                            return;

                        return;
                    }

                    if (ESCache.Instance.Drones.PreferredDroneTarget.LockTarget("TargetCombatants.PreferredDroneTarget"))
                    {
                        Log.WriteLine("Targeting preferred drone target [" + ESCache.Instance.Drones.PreferredDroneTarget.Name + "][" +
                                      ESCache.Instance.Drones.PreferredDroneTarget.DirectEntity.Id.ToString() + "][" +
                                      Math.Round(ESCache.Instance.Drones.PreferredDroneTarget.Distance / 1000, 0) + "k away]");
                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                        if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                            ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                            ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                        return;
                    }
                }
            }

            #endregion Preferred Drone target handling

            #region Do we have enough targets?

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Do we have enough targets? Locked [" + ESCache.Instance.Targets.Count() + "] Locking [" +
                              ESCache.Instance.Targeting.Count() + "] Total [" + ESCache.Instance.TotalTargetsandTargeting.Count() + "] Slots Total [" +
                              ESCache.Instance.MaxLockedTargets + "]");
            var highValueSlotsreservedForPriorityTargets = 1;
            var lowValueSlotsreservedForPriorityTargets = 1;

            if (ESCache.Instance.MaxLockedTargets <= 4)
            {
                highValueSlotsreservedForPriorityTargets = 0;
                lowValueSlotsreservedForPriorityTargets = 0;
            }

            if (MaxHighValueTargets <= 2)
                highValueSlotsreservedForPriorityTargets = 0;

            if (MaxLowValueTargets <= 2)
                lowValueSlotsreservedForPriorityTargets = 0;

            if (highValueTargetsTargeted.Count() >= MaxHighValueTargets - highValueSlotsreservedForPriorityTargets
                && lowValueTargetsTargeted.Count() >= MaxLowValueTargets - lowValueSlotsreservedForPriorityTargets)
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: we have enough targets targeted [" + ESCache.Instance.TotalTargetsandTargeting.Count() +
                                  "] __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] __lowValueTargetsTargeted [" +
                                  lowValueTargetsTargeted.Count() +
                                  "] maxHighValueTargets [" + MaxHighValueTargets + "] maxLowValueTargets [" + MaxLowValueTargets + "]");
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] maxHighValueTargets [" +
                                  MaxHighValueTargets + "] highValueSlotsreservedForPriorityTargets [" + highValueSlotsreservedForPriorityTargets + "]");
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() + "] maxLowValueTargets [" +
                                  MaxLowValueTargets +
                                  "] lowValueSlotsreservedForPriorityTargets [" + lowValueSlotsreservedForPriorityTargets + "]");

                return;
            }

            if (ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: we have enough targets targeted... [" + ESCache.Instance.TotalTargetsandTargeting.Count() +
                                  "] __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() + "] __lowValueTargetsTargeted [" +
                                  lowValueTargetsTargeted.Count() +
                                  "] maxHighValueTargets [" + MaxHighValueTargets + "] maxLowValueTargets [" + MaxLowValueTargets + "]");
                return;
            }

            #endregion Do we have enough targets?

            #region Aggro Handling

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: Aggro Handling");
            TargetingMe = TargetedBy.Where(t => t.Distance < (double)Distances.OnGridWithMe
                                                && t.CategoryId != (int)CategoryID.Asteroid
                                                && t.IsTargetingMeAndNotYetTargeted
                                                && (!t.IsSentry || t.IsSentry && t.IsEwarTarget)
                                                && t.Distance < MaxRange)
                .ToList();

            var highValueTargetingMe = TargetingMe.Where(t => t.IsHighValueTarget)
                .OrderByDescending(t => t.IsVunlnerableAgainstCurrentDamageType && t.IsBattleship)
                .ThenByDescending(t => t.IsVunlnerableAgainstCurrentDamageType && t.IsBattlecruiser)
                .ThenByDescending(t => t.IsVunlnerableAgainstCurrentDamageType)
                .ThenBy(t => t.Distance)
                .ToList();

            var lockedTargetsThatHaveHighValue = ESCache.Instance.Targets.Count(t => t.IsHighValueTarget);

            var lowValueTargetingMe = TargetingMe.Where(t => t.IsLowValueTarget)
                .OrderByDescending(t => t.Distance)
                .ToList();

            var lockedTargetsThatHaveLowValue = ESCache.Instance.Targets.Count(t => t.IsLowValueTarget);

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("TargetingMe [" + TargetingMe.Count() + "] lowValueTargetingMe [" + lowValueTargetingMe.Count() + "] targeted [" +
                              lockedTargetsThatHaveLowValue + "] :::  highValueTargetingMe [" + highValueTargetingMe.Count() + "] targeted [" +
                              lockedTargetsThatHaveHighValue + "] LargeCollidables [" + ESCache.Instance.EntitiesOnGrid.Count(e => e.IsLargeCollidable) + "]");

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: foreach (EntityCache entity in highValueTargetingMe)");

            if (highValueTargetingMe.Any())
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: [" + highValueTargetingMe.Count() + "] highValueTargetingMe targets");

                var highValueTargetsTargetedThisCycle = 1;
                foreach (var highValueTargetingMeEntity in highValueTargetingMe.Where(t => t.IsReadyToTarget && t.Distance < MaxRange))
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: [" + highValueTargetsTargetedThisCycle + "][" + highValueTargetingMeEntity.Name + "][" +
                                      Math.Round(highValueTargetingMeEntity.Distance / 1000, 2) + "k][groupID" + highValueTargetingMeEntity.GroupId +
                                      "]");
                    if (highValueTargetsTargeted.Count() >= MaxHighValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: __highValueTargetsTargeted.Count() [" + highValueTargetsTargeted.Count() +
                                          "] maxHighValueTargets [" +
                                          MaxHighValueTargets + "], done for this iteration");
                        break;
                    }

                    if (highValueTargetsTargetedThisCycle >= 4)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + highValueTargetsTargetedThisCycle +
                                          "], done for this iteration");
                        break;
                    }

                    if (highValueTargetsTargeted.Count() < MaxHighValueTargets && lowValueTargetsTargeted.Count() > MaxLowValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: __highValueTargetsTargeted [" + highValueTargetsTargeted.Count() +
                                          "] < maxHighValueTargets [" +
                                          MaxHighValueTargets + "] && __lowValueTargetsTargeted [" + lowValueTargetsTargeted.Count() +
                                          "] > maxLowValueTargets [" +
                                          MaxLowValueTargets + "], try to unlock a low value target, and return.");
                        UnlockLowValueTarget("HighValueTarget");
                        return;
                    }

                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine((highValueTargetingMeEntity.Distance < MaxRange).ToString()
                                      + highValueTargetingMeEntity.IsReadyToTarget.ToString()
                                      + highValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail.ToString()
                                      + (!highValueTargetingMeEntity.IsIgnored).ToString());

                    if (highValueTargetingMeEntity != null
                        && highValueTargetingMeEntity.Distance < MaxRange
                        && highValueTargetingMeEntity.IsReadyToTarget
                        && highValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail
                        && !highValueTargetingMeEntity.IsIgnored
                        && highValueTargetingMeEntity.LockTarget("TargetCombatants.HighValueTargetingMeEntity"))
                    {
                        highValueTargetsTargetedThisCycle++;
                        Log.WriteLine("Targeting high value target [" + highValueTargetingMeEntity.Name + "][" + highValueTargetingMeEntity.DirectEntity.Id.ToString() +
                                      "][" +
                                      Math.Round(highValueTargetingMeEntity.Distance / 1000, 0) + "k away] highValueTargets.Count [" +
                                      highValueTargetsTargeted.Count() +
                                      "]");
                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                        if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                            ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                            ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                        if (highValueTargetsTargetedThisCycle > 2)
                        {
                            if (DebugConfig.DebugTargetCombatants)
                                Log.WriteLine("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + highValueTargetsTargetedThisCycle +
                                              "] > 3, return");
                            return;
                        }
                    }

                    continue;
                }

                if (highValueTargetsTargetedThisCycle > 1)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: HighValueTargetsTargetedThisCycle [" + highValueTargetsTargetedThisCycle + "] > 1, return");
                    return;
                }
            }
            else
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: 0 highValueTargetingMe targets");
            }

            if (DebugConfig.DebugTargetCombatants)
                Log.WriteLine("DebugTargetCombatants: foreach (EntityCache entity in lowValueTargetingMe)");

            if (lowValueTargetingMe.Any())
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: [" + lowValueTargetingMe.Count() + "] lowValueTargetingMe targets");

                var LowValueTargetsTargetedThisCycle = 1;
                foreach (
                    var lowValueTargetingMeEntity in
                    lowValueTargetingMe.Where(t => !t.IsTarget && !t.IsTargeting && t.Distance < MaxRange)
                        .OrderByDescending(i => i.IsLastTargetDronesWereShooting)
                        .ThenBy(i => i.IsCurrentTarget))
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: lowValueTargetingMe [" + LowValueTargetsTargetedThisCycle + "][" +
                                      lowValueTargetingMeEntity.Name + "][" +
                                      Math.Round(lowValueTargetingMeEntity.Distance / 1000, 2) + "k] groupID [ " + lowValueTargetingMeEntity.GroupId +
                                      "]");

                    if (lowValueTargetsTargeted.Count() >= MaxLowValueTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: __lowValueTargetsTargeted.Count() [" + lowValueTargetsTargeted.Count() +
                                          "] maxLowValueTargets [" +
                                          MaxLowValueTargets + "], done for this iteration");
                        break;
                    }

                    if (LowValueTargetsTargetedThisCycle >= 3)
                    {
                        if (DebugConfig.DebugTargetCombatants)
                            Log.WriteLine("DebugTargetCombatants: LowValueTargetsTargetedThisCycle [" + LowValueTargetsTargetedThisCycle +
                                          "], done for this iteration");
                        break;
                    }

                    if (lowValueTargetsTargeted.Count() < MaxLowValueTargets && highValueTargetsTargeted.Count() > MaxHighValueTargets)
                    {
                        UnlockLowValueTarget("HighValueTarget");
                        return;
                    }

                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine((lowValueTargetingMeEntity.Distance < MaxRange).ToString()
                                      + lowValueTargetingMeEntity.IsReadyToTarget.ToString()
                                      + lowValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail.ToString()
                                      + (!lowValueTargetingMeEntity.IsIgnored).ToString());

                    if (lowValueTargetingMeEntity != null
                        && lowValueTargetingMeEntity.Distance < ESCache.Instance.WeaponRange
                        && lowValueTargetingMeEntity.IsReadyToTarget
                        && lowValueTargetingMeEntity.IsInOptimalRangeOrNothingElseAvail
                        && lowValueTargetingMeEntity.Distance < MaxRange
                        && !lowValueTargetingMeEntity.IsIgnored
                        && lowValueTargetingMeEntity.LockTarget("TargetCombatants.LowValueTargetingMeEntity"))
                    {
                        LowValueTargetsTargetedThisCycle++;
                        Log.WriteLine("Targeting low  value target [" + lowValueTargetingMeEntity.Name + "][" + lowValueTargetingMeEntity.DirectEntity.Id.ToString() +
                                      "][" +
                                      Math.Round(lowValueTargetingMeEntity.Distance / 1000, 0) + "k away] lowValueTargets.Count [" +
                                      lowValueTargetsTargeted.Count() + "]");
                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                        if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                            ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                            ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);
                        if (LowValueTargetsTargetedThisCycle > 2)
                        {
                            if (DebugConfig.DebugTargetCombatants)
                                Log.WriteLine("DebugTargetCombatants: LowValueTargetsTargetedThisCycle [" + LowValueTargetsTargetedThisCycle +
                                              "] > 2, return");
                            return;
                        }
                    }

                    continue;
                }

                if (LowValueTargetsTargetedThisCycle > 1)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log.WriteLine("DebugTargetCombatants: if (LowValueTargetsTargetedThisCycle > 1)");
                    return;
                }
            }
            else
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: 0 lowValueTargetingMe targets");
            }

            #endregion Aggro Handling

            #region All else fails grab an unlocked target that is not yet targeting me

            NotYetTargetingMeAndNotYetTargeted = PotentialCombatTargets.Where(e => e.IsNotYetTargetingMeAndNotYetTargeted)
                .OrderByDescending(t => t.IsVunlnerableAgainstCurrentDamageType)
                .OrderBy(t => t.Distance)
                .ToList();

            if (NotYetTargetingMeAndNotYetTargeted.Any())
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: [" + NotYetTargetingMeAndNotYetTargeted.Count() +
                                  "] IsNotYetTargetingMeAndNotYetTargeted targets");

                foreach (var TargetThisNotYetAggressiveNPC in NotYetTargetingMeAndNotYetTargeted)
                    if (TargetThisNotYetAggressiveNPC != null
                        && TargetThisNotYetAggressiveNPC.IsReadyToTarget
                        && TargetThisNotYetAggressiveNPC.IsInOptimalRangeOrNothingElseAvail
                        && TargetThisNotYetAggressiveNPC.Distance < MaxRange
                        && !TargetThisNotYetAggressiveNPC.IsIgnored
                        && TargetThisNotYetAggressiveNPC.LockTarget("TargetCombatants.TargetThisNotYetAggressiveNPC"))
                    {
                        Log.WriteLine("Targeting non-aggressed NPC target [" + TargetThisNotYetAggressiveNPC.Name + "][GroupID: " +
                                      TargetThisNotYetAggressiveNPC.GroupId +
                                      "][TypeID: " + TargetThisNotYetAggressiveNPC.TypeId + "][" + TargetThisNotYetAggressiveNPC.DirectEntity.Id.ToString() + "][" +
                                      Math.Round(TargetThisNotYetAggressiveNPC.Distance / 1000, 0) + "k away]");
                        ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.TargetDelay_milliseconds);
                        if (ESCache.Instance.TotalTargetsandTargeting.Any() &&
                            ESCache.Instance.TotalTargetsandTargeting.Count() >= ESCache.Instance.MaxLockedTargets)
                            ESCache.Instance.Time.NextTargetAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TargetsAreFullDelay_seconds);

                        return;
                    }
            }
            else
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log.WriteLine("DebugTargetCombatants: 0 IsNotYetTargetingMeAndNotYetTargeted targets");
            }

            return;

            #endregion All else fails grab an unlocked target that is not yet targeting me
        }

        #endregion Methods
    }
}