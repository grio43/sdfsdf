using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Methods

        public bool GetBestPrimaryWeaponTarget(double distance, List<EntityCache> _potentialTargets = null)
        {
            
            var currentTarget = CurrentWeaponTarget;
            if (currentTarget != null && currentTarget.IsReadyToShoot)
            {
                #region Is our current target any other primary weapon priority target?
                if (PrimaryWeaponPriorityEntities.Any(pt => pt.IsReadyToShoot
                                                            && pt.Distance < MaxRange
                                                            && pt.IsCurrentTarget
                                                            && !currentTarget.IsHigherPriorityPresent))
                {
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("CurrentTarget [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" +
                                      currentTarget.DirectEntity.Id.ToString() +
                                      "] GroupID [" + currentTarget.GroupId + "]");
                    PreferredPrimaryWeaponTarget = currentTarget;
                    ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target any other primary weapon priority target?

                #region Is our current target already in armor? keep shooting the same target if so...

                if (DebugConfig.DebugGetBestTarget)
                    Log.WriteLine("Checking Low Health");
                if (currentTarget.IsEntityIShouldKeepShooting)
                {
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("CurrentTarget [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" +
                                      currentTarget.DirectEntity.Id.ToString() +
                                      " GroupID [" + currentTarget.GroupId + "]] has less than 60% armor, keep killing this target");
                    PreferredPrimaryWeaponTarget = currentTarget;
                    ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target already in armor? keep shooting the same target if so...

                #region If none of the above matches, does our current target meet the conditions of being hittable and in range

                if (!currentTarget.IsHigherPriorityPresent)
                {
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("Does the currentTarget exist? can it be hit?");
                    if (currentTarget.IsReadyToShoot
                        && (!currentTarget.IsNPCFrigate || !ESCache.Instance.Drones.UseDrones && !currentTarget.IsTooCloseTooFastTooSmallToHit)
                        && currentTarget.Distance < MaxRange)
                    {
                        if (DebugConfig.DebugGetBestTarget)
                            Log.WriteLine("if  the currentTarget exists and the target is the right size then continue shooting it;");
                        if (DebugConfig.DebugGetBestTarget)
                            Log.WriteLine("currentTarget is [" + currentTarget.Name + "][" + Math.Round(currentTarget.Distance / 1000, 2) + "k][" +
                                          currentTarget.DirectEntity.Id.ToString() +
                                          "] GroupID [" + currentTarget.GroupId + "]");

                        PreferredPrimaryWeaponTarget = currentTarget;
                        ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                        return true;
                    }
                }

                #endregion If none of the above matches, does our current target meet the conditions of being hittable and in range
            }

            if (CheckForECMPriorityTargetsInOrder(currentTarget, distance)) return true;

            #region Get the closest primary weapon priority target

            if (DebugConfig.DebugGetBestTarget)
                Log.WriteLine("Checking Closest PrimaryWeaponPriorityTarget");
            EntityCache primaryWeaponPriorityTarget = null;
            try
            {
                if (PrimaryWeaponPriorityEntities != null && PrimaryWeaponPriorityEntities.Any())
                    primaryWeaponPriorityTarget = PrimaryWeaponPriorityEntities.Where(p => p.Distance < MaxRange
                                                                                           && !p.IsIgnored
                                                                                           && p.IsReadyToShoot
                                                                                           &&
                                                                                           (!p.IsNPCFrigate && !p.IsFrigate ||
                                                                                            !ESCache.Instance.Drones.UseDrones && !p.IsTooCloseTooFastTooSmallToHit))
                        .OrderByDescending(t => t.IsVunlnerableAgainstCurrentDamageType && t.IsBattleship)
                        .ThenByDescending(t => t.IsVunlnerableAgainstCurrentDamageType)
                        .ThenByDescending(pt => pt.IsCurrentTarget)
                        .ThenByDescending(pt => pt.IsInOptimalRange)
                        .ThenByDescending(pt => pt.IsEwarTarget)
                        .ThenBy(pt => pt.WeaponPriority)
                        .ThenByDescending(pt => pt.TargetValue)
                        .ThenBy(pt => pt.Distance)
                        .FirstOrDefault();
            }
            catch (NullReferenceException)
            {
            }

            if (primaryWeaponPriorityTarget != null)
            {
                if (DebugConfig.DebugGetBestTarget)
                    Log.WriteLine("primaryWeaponPriorityTarget is [" + primaryWeaponPriorityTarget.Name + "][" +
                                  Math.Round(primaryWeaponPriorityTarget.Distance / 1000, 2) +
                                  "k][" + primaryWeaponPriorityTarget.DirectEntity.Id.ToString() + "] GroupID [" + primaryWeaponPriorityTarget.GroupId + "]");
                PreferredPrimaryWeaponTarget = primaryWeaponPriorityTarget;
                ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion Get the closest primary weapon priority target

            #region did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            if (DebugConfig.DebugGetBestTarget)
                Log.WriteLine("Checking Calling Target");
            if (_potentialTargets != null && _potentialTargets.Any())
            {
                EntityCache callingTarget = null;
                try
                {
                    callingTarget = _potentialTargets.OrderBy(t => t.Distance).FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

                if (callingTarget != null && (callingTarget.IsReadyToShoot || callingTarget.IsLargeCollidable)
                )
                {
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("if (callingTarget != null && !callingTarget.IsIgnored)");
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("callingTarget is [" + callingTarget.Name + "][" + Math.Round(callingTarget.Distance / 1000, 2) + "k][" +
                                      callingTarget.DirectEntity.Id.ToString() +
                                      "] GroupID [" + callingTarget.GroupId + "]");
                    AddPrimaryWeaponPriorityTarget(callingTarget, WeaponPriority.KillTarget, "GetBestTarget: callingTarget");
                    PreferredPrimaryWeaponTarget = callingTarget;
                    ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }
            }

            #endregion did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            #region Get the closest High Value Target

            if (DebugConfig.DebugGetBestTarget)
                Log.WriteLine("Checking Closest High Value");
            EntityCache highValueTarget = null;

            if (PotentialCombatTargets.Any())
            {
                if (DebugConfig.DebugGetBestTarget)
                    Log.WriteLine("get closest: if (potentialCombatTargets.Any())");

                highValueTarget = PotentialCombatTargets.Where(t => t.IsHighValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => t.IsVunlnerableAgainstCurrentDamageType && t.IsBattleship)
                    .ThenByDescending(t => t.IsVunlnerableAgainstCurrentDamageType)
                    .ThenByDescending(t => !t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenByDescending(t => !t.IsTooCloseTooFastTooSmallToHit)
                    .ThenByDescending(t => t.IsInOptimalRange)
                    .ThenByDescending(pt => pt.TargetValue)
                    .ThenByDescending(t => !t.IsCruiser)
                    .ThenBy(t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct))
                    .ThenBy(t => t.Distance)
                    .FirstOrDefault();
            }

            #endregion Get the closest High Value Target

            #region Get the closest low value target that is not moving too fast for us to hit

            if (DebugConfig.DebugGetBestTarget)
                Log.WriteLine("Checking closest Low Value");
            EntityCache lowValueTarget = null;
            if (PotentialCombatTargets.Any())
                lowValueTarget = PotentialCombatTargets.Where(t => t.IsLowValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => t.IsVunlnerableAgainstCurrentDamageType)
                    .ThenByDescending(t => t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenByDescending(t => t.IsTooCloseTooFastTooSmallToHit)
                    .ThenBy(pt => pt.TargetValue)
                    .ThenBy(t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct))
                    .ThenBy(t => t.Distance)
                    .FirstOrDefault();

            #endregion Get the closest low value target that is not moving too fast for us to hit

            #region High Value - aggrod, or no low value aggrod

            if (highValueTarget != null)
                if (highValueTarget.IsTargetedBy
                    || ESCache.Instance.Drones.UseDrones || lowValueTarget == null || lowValueTarget != null
                    && !lowValueTarget.IsTargetedBy)
                {
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("Checking Use High Value");
                    if (DebugConfig.DebugGetBestTarget)
                        Log.WriteLine("highValueTarget is [" + highValueTarget.Name + "][" + Math.Round(highValueTarget.Distance / 1000, 2) + "k][" +
                                      highValueTarget.DirectEntity.Id.ToString() + "] GroupID [" + highValueTarget.GroupId + "]");
                    PreferredPrimaryWeaponTarget = highValueTarget;
                    ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

            #endregion High Value - aggrod, or no low value aggrod

            #region If we do not have a high value target but we do have a low value target

            if (lowValueTarget != null)
            {
                if (DebugConfig.DebugGetBestTarget)
                    Log.WriteLine("Checking use Low Value");
                if (DebugConfig.DebugGetBestTarget)
                    Log.WriteLine("lowValueTarget is [" + lowValueTarget.Name + "][" + Math.Round(lowValueTarget.Distance / 1000, 2) + "k][" +
                                  lowValueTarget.DirectEntity.Id.ToString() +
                                  "] GroupID [" + lowValueTarget.GroupId + "]");
                PreferredPrimaryWeaponTarget = lowValueTarget;
                ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion If we do not have a high value target but we do have a low value target

            if (DebugConfig.DebugGetBestTarget) Log.WriteLine("Could not determine a suitable target");

            #region If we did not find anything at all (wtf!?!?)

            if (DebugConfig.DebugGetBestTarget)
            {
                if (ESCache.Instance.Targets.Any())
                {
                    Log.WriteLine(".");
                    Log.WriteLine("*** ALL LOCKED/LOCKING TARGETS LISTED BELOW");
                    var LockedTargetNumber = 0;
                    foreach (var __target in ESCache.Instance.Targets)
                    {
                        LockedTargetNumber++;
                        Log.WriteLine("*** Target: [" + LockedTargetNumber + "][" + __target.Name + "][" + Math.Round(__target.Distance / 1000, 2) +
                                      "k][" +
                                      __target.DirectEntity.Id.ToString() + "][isTarget: " + __target.IsTarget + "][isTargeting: " + __target.IsTargeting + "] GroupID [" +
                                      __target.GroupId +
                                      "]");
                    }
                    Log.WriteLine("*** ALL LOCKED/LOCKING TARGETS LISTED ABOVE");
                    Log.WriteLine(".");
                }

                if (PotentialCombatTargets.Any(t => !t.IsTarget && !t.IsTargeting))
                {
                    if (ActionControl.IgnoreTargets.Any())
                    {
                        var IgnoreCount = ActionControl.IgnoreTargets.Count;
                        Log.WriteLine("Ignore List has [" + IgnoreCount + "] Entities in it.");
                    }

                    Log.WriteLine(
                        "***** ALL [" + PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED BELOW (not yet targeted or targeting)");
                    var potentialCombatTargetNumber = 0;
                    foreach (var potentialCombatTarget in PotentialCombatTargets)
                    {
                        potentialCombatTargetNumber++;
                        Log.WriteLine("***** Unlocked [" + potentialCombatTargetNumber + "]: [" + potentialCombatTarget.Name + "][" +
                                      Math.Round(potentialCombatTarget.Distance / 1000, 2) + "k][" + potentialCombatTarget.DirectEntity.Id.ToString() + "][isTarget: " +
                                      potentialCombatTarget.IsTarget + "] GroupID [" + potentialCombatTarget.GroupId + "]");
                    }
                    Log.WriteLine(
                        "***** ALL [" + PotentialCombatTargets.Count() + "] potentialCombatTargets LISTED ABOVE (not yet targeted or targeting)");
                    Log.WriteLine(".");
                }
            }

            #endregion If we did not find anything at all (wtf!?!?)

            return false;
        }

        #endregion Methods
    }
}