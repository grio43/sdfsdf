using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void KillAction(Action action)
        {
            if (ESCache.Instance.NormalApproach) ESCache.Instance.NormalApproach = false;

            if (!bool.TryParse(action.GetParameterValue("ignoreattackers"), out var ignoreAttackers))
                ignoreAttackers = false;

            if (!bool.TryParse(action.GetParameterValue("breakonattackers"), out var breakOnAttackers))
                breakOnAttackers = false;

            if (!bool.TryParse(action.GetParameterValue("notclosest"), out var notTheClosest))
                notTheClosest = false;

            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out var numberToIgnore))
                numberToIgnore = 0;

            if (!int.TryParse(action.GetParameterValue("attackUntilBelowShieldPercentage"), out var attackUntilBelowShieldPercentage))
                attackUntilBelowShieldPercentage = 0;

            if (!int.TryParse(action.GetParameterValue("attackUntilBelowArmorPercentage"), out var attackUntilBelowArmorPercentage))
                attackUntilBelowArmorPercentage = 0;

            if (!int.TryParse(action.GetParameterValue("attackUntilBelowHullPercentage"), out var attackUntilBelowHullPercentage))
                attackUntilBelowHullPercentage = 0;

            var targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Log.WriteLine("No targets defined in kill action!");
                Nextaction();
                return;
            }

            if (DebugConfig.DebugKillAction)
            {
                var targetNameCount = 0;
                foreach (var targetName in targetNames)
                {
                    targetNameCount++;
                    Log.WriteLine("targetNames [" + targetNameCount + "][" + targetName + "]");
                }
            }

            var killTargets = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderBy(t => t.Distance).ToList();

            if (notTheClosest)
                killTargets = ESCache.Instance.EntitiesOnGrid.Where(e => targetNames.Contains(e.Name)).OrderByDescending(t => t.Distance).ToList();

            if (!killTargets.Any() || killTargets.Count() <= numberToIgnore)
            {
                Log.WriteLine("All targets killed " +
                              targetNames.Aggregate((current, next) => current + "[" + next + "] NumToIgnore [" + numberToIgnore + "]"));

                // We killed it/them !?!?!? :)
                IgnoreTargets.RemoveWhere(targetNames.Contains);
                if (ignoreAttackers)
                    foreach (var target in ESCache.Instance.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                        if (target.IsTargetedBy && target.IsAttacking)
                        {
                            Log.WriteLine("UN-Ignoring [" + target.Name + "][" + target.DirectEntity.Id.ToString() + "][" + Math.Round(target.Distance / 1000, 0) +
                                          "k away] due to ignoreAttackers parameter (and kill action being complete)");
                            IgnoreTargets.Remove(target.Name.Trim());
                        }

                Nextaction();
                return;
            }

            if (ignoreAttackers)
                foreach (var target in ESCache.Instance.Combat.PotentialCombatTargets.Where(e => !targetNames.Contains(e.Name)))
                    if (target.IsTargetedBy && target.IsAttacking)
                    {
                        Log.WriteLine("Ignoring [" + target.Name + "][" + target.DirectEntity.Id.ToString() + "][" + Math.Round(target.Distance / 1000, 0) +
                                      "k away] due to ignoreAttackers parameter");
                        IgnoreTargets.Add(target.Name.Trim());
                    }

            if (breakOnAttackers &&
                ESCache.Instance.Combat.TargetedBy.Count(
                    t => (!t.IsSentry || t.IsSentry && false || t.IsSentry && t.IsEwarTarget) && !t.IsIgnored) >
                killTargets.Count(e => e.IsTargetedBy))
            {
                //
                // We are being attacked, break the kill order
                // which involves removing the named targets as PrimaryWeaponPriorityTargets, PreferredPrimaryWeaponTarget, DronePriorityTargets, and PreferredDroneTarget
                //
                Log.WriteLine("Breaking off kill order, new spawn has arrived!");
                targetNames.ForEach(t => IgnoreTargets.Add(t));

                if (killTargets.Any())
                {
                    ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(killTargets.ToList());

                    if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null && killTargets.Any(i => i.Name == ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Name))
                    {
                        var PreferredPrimaryWeaponTargetsToRemove = killTargets.Where(i => i.Name == ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Name).ToList();
                        ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(PreferredPrimaryWeaponTargetsToRemove);
                        if (ESCache.Instance.Drones.UseDrones)
                            ESCache.Instance.Drones.RemoveDronePriorityTargets(PreferredPrimaryWeaponTargetsToRemove);
                    }

                    if (ESCache.Instance.Combat.PreferredPrimaryWeaponTargetID != null)
                        foreach (var killTarget in killTargets.Where(e => e.Id == ESCache.Instance.Combat.PreferredPrimaryWeaponTargetID))
                        {
                            if (ESCache.Instance.Combat.PreferredPrimaryWeaponTargetID == null) continue;
                            Log.WriteLine("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                          ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.DirectEntity.Id.ToString() + "]");
                            ESCache.Instance.Combat.PreferredPrimaryWeaponTarget = null;
                        }

                    if (ESCache.Instance.Drones.PreferredDroneTargetID != null)
                        foreach (var killTarget in killTargets.Where(e => e.Id == ESCache.Instance.Drones.PreferredDroneTargetID))
                        {
                            if (ESCache.Instance.Drones.PreferredDroneTargetID == null) continue;
                            Log.WriteLine("Breaking Kill Order in: [" + killTarget.Name + "][" + Math.Round(killTarget.Distance / 1000, 0) + "k][" +
                                          ESCache.Instance.Drones.PreferredDroneTarget.DirectEntity.Id.ToString() + "]");
                            ESCache.Instance.Drones.PreferredDroneTarget = null;
                        }
                }

                foreach (var KillTargetEntity in ESCache.Instance.Targets.Where(e => targetNames.Contains(e.Name) && (e.IsTarget || e.IsTargeting)))
                {
                    if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null)
                        if (KillTargetEntity.Id == ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Id)
                            continue;

                    Log.WriteLine("Unlocking [" + KillTargetEntity.Name + "][" + KillTargetEntity.DirectEntity.Id.ToString() + "][" +
                                  Math.Round(KillTargetEntity.Distance / 1000, 0) +
                                  "k away] due to kill order being put on hold");
                    KillTargetEntity.UnlockTarget();
                }
            }
            else //Do not break aggression on attackers (attack normally)
            {
                //
                // check to see if we have priority targets (ECM, warp scramblers, etc, and let combat process those first)
                //
                EntityCache primaryWeaponPriorityTarget = null;
                if (ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Any())
                    try
                    {
                        primaryWeaponPriorityTarget = ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Where(p => p.Distance < ESCache.Instance.Combat.MaxRange
                                                                                                             && p.IsReadyToShoot
                                                                                                             && p.Distance < (double)Distances.OnGridWithMe
                                                                                                             &&
                                                                                                             (!p.IsNPCFrigate && !p.IsFrigate ||
                                                                                                              !ESCache.Instance.Drones.UseDrones &&
                                                                                                              !p.IsTooCloseTooFastTooSmallToHit))
                            .OrderByDescending(pt => pt.IsTargetedBy)
                            .ThenByDescending(pt => pt.IsInOptimalRange)
                            .ThenByDescending(pt => pt.IsEwarTarget)
                            .ThenBy(pt => pt.WeaponPriority)
                            .ThenBy(pt => pt.Distance)
                            .FirstOrDefault();
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception [" + ex + "]");
                    }

                if (primaryWeaponPriorityTarget != null && primaryWeaponPriorityTarget.Distance < (double)Distances.OnGridWithMe)
                {
                    if (DebugConfig.DebugKillAction)
                        if (ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any())
                        {
                            var icount = 0;
                            foreach (var primaryWeaponPriorityEntity in ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Where(i => i.Distance < (double)Distances.OnGridWithMe))
                            {
                                icount++;
                                if (DebugConfig.DebugKillAction)
                                    Log.WriteLine("[" + icount + "] PrimaryWeaponPriorityTarget Named [" + primaryWeaponPriorityEntity.Name + "][" +
                                                  primaryWeaponPriorityEntity.DirectEntity.Id.ToString() + "][" +
                                                  Math.Round(primaryWeaponPriorityEntity.Distance / 1000, 0) + "k away]");
                                continue;
                            }
                        }

                    //
                    // GetBestTarget below will choose to assign PriorityTargets over preferred targets, so we might as well wait... (and not approach the wrong target)
                    //
                }
                else
                {
                    //
                    // then proceed to kill the target
                    //
                    IgnoreTargets.RemoveWhere(targetNames.Contains);

                    if (killTargets.FirstOrDefault() != null) //if it is not null is HAS to be OnGridWithMe as all killTargets are verified OnGridWithMe
                    {
                        if (attackUntilBelowShieldPercentage > 0 && killTargets.FirstOrDefault().ShieldPct * 100 < attackUntilBelowShieldPercentage)
                        {
                            Log.WriteLine("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" +
                                          Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                          "k] Armor % is [" + killTargets.FirstOrDefault().ShieldPct * 100 +
                                          "] which is less then attackUntilBelowShieldPercentage [" +
                                          attackUntilBelowShieldPercentage + "] Kill Action Complete, Next Action.");
                            ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            ESCache.Instance.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (attackUntilBelowArmorPercentage > 0 && killTargets.FirstOrDefault().ArmorPct * 100 < attackUntilBelowArmorPercentage)
                        {
                            Log.WriteLine("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" +
                                          Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                          "k] Armor % is [" + killTargets.FirstOrDefault().ArmorPct * 100 +
                                          "] which is less then attackUntilBelowArmorPercentage [" +
                                          attackUntilBelowArmorPercentage + "] Kill Action Complete, Next Action.");
                            ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            ESCache.Instance.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (attackUntilBelowHullPercentage > 0 && killTargets.FirstOrDefault().ArmorPct * 100 < attackUntilBelowHullPercentage)
                        {
                            Log.WriteLine("Kill target [" + killTargets.FirstOrDefault().Name + "] at [" +
                                          Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) +
                                          "k] Armor % is [" + killTargets.FirstOrDefault().StructurePct * 100 +
                                          "] which is less then attackUntilBelowHullPercentage [" +
                                          attackUntilBelowHullPercentage + "] Kill Action Complete, Next Action.");
                            ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(killTargets);
                            ESCache.Instance.Combat.PreferredPrimaryWeaponTarget = null;
                            Nextaction();
                            return;
                        }

                        if (DebugConfig.DebugKillAction)
                            Log.WriteLine(" proceeding to kill [" + killTargets.FirstOrDefault().Name + "] at [" +
                                          Math.Round(killTargets.FirstOrDefault().Distance / 1000, 2) + "k] (this is spammy, but useful debug info)");

                        ESCache.Instance.Combat.AddPrimaryWeaponPriorityTarget(killTargets.FirstOrDefault(), WeaponPriority.KillTarget,
                            "CombatMissionCtrl.Kill[" + PocketNumber + "]." + _pocketActions[_currentAction]);
                        ESCache.Instance.Combat.PreferredPrimaryWeaponTarget = killTargets.FirstOrDefault();

                        if (DebugConfig.DebugKillAction)
                        {
                            if (DebugConfig.DebugKillAction)
                                Log.WriteLine("Combat.PreferredPrimaryWeaponTarget =[ " + ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Name + " ][" +
                                              ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.DirectEntity.Id.ToString() + "]");

                            if (ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any())
                            {
                                if (DebugConfig.DebugKillAction)
                                    Log.WriteLine("PrimaryWeaponPriorityTargets Below (if any)");
                                var icount = 0;
                                foreach (var PT in ESCache.Instance.Combat.PrimaryWeaponPriorityEntities)
                                {
                                    icount++;
                                    if (DebugConfig.DebugKillAction)
                                        Log.WriteLine("PriorityTarget [" + icount + "] [ " + PT.Name + " ][" + PT.DirectEntity.Id.ToString() + "] IsOnGridWithMe [" +
                                                      (PT.Distance < (double)Distances.OnGridWithMe) +
                                                      "]");
                                }

                                if (DebugConfig.DebugKillAction)
                                    Log.WriteLine("PrimaryWeaponPriorityTargets Above (if any)");
                            }
                        }

                        EntityCache NavigateTowardThisTarget = null;
                        if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null)
                            NavigateTowardThisTarget = ESCache.Instance.Combat.PreferredPrimaryWeaponTarget;
                        if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null)
                            NavigateTowardThisTarget = killTargets.FirstOrDefault();
                        //we may need to get closer so combat will take over
                        if (NavigateTowardThisTarget.Distance > ESCache.Instance.Combat.MaxRange || !NavigateTowardThisTarget.IsInOptimalRange)
                        {
                            if (DebugConfig.DebugKillAction)
                                Log.WriteLine("if (Combat.PreferredPrimaryWeaponTarget.Distance > Combat.MaxRange)");
                            //if (!Cache.Instance.IsApproachingOrOrbiting(Combat.PreferredPrimaryWeaponTarget.Id))
                            //{
                            //    if (Logging.DebugKillAction) Logging.Log("CombatMissionCtrl[" + PocketNumber + "]." + _pocketActions[_currentAction], "if (!Cache.Instance.IsApproachingOrOrbiting(Combat.PreferredPrimaryWeaponTarget.Id))", Logging.Debug);
                            ESCache.Instance.NavigateOnGrid.NavigateIntoRange(NavigateTowardThisTarget, "combatMissionControl", true);
                            //}
                        }
                    }
                }

                if (ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != killTargets.FirstOrDefault())
                    ESCache.Instance.Combat.GetBestPrimaryWeaponTarget(ESCache.Instance.Combat.MaxRange);
            }

            // Don't use NextAction here, only if target is killed (checked further up)
            return;
        }

        #endregion Methods
    }
}