extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.Abyssal.AbyssalGuard;
using EVESharpCore.Controllers.AbyssalHunter;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor
{
    /// <summary>
    ///     Description of DefenseController.
    /// </summary>
    public class DefenseController : BaseController
    {
        #region Fields

        private readonly Dictionary<long, DateTime> NextScriptReload = new Dictionary<long, DateTime>();
        private DateTime _nextOverloadAttempt = DateTime.UtcNow;
        private int _sensorBoosterScriptAttempts;
        private int _sensorDampenerScriptAttempts;
        private int _trackingComputerScriptAttempts;
        private int _trackingDisruptorScriptAttempts;
        private int _trackingLinkScriptAttempts;

        #endregion Fields

        #region Constructors

        public DefenseController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            if (ESCache.Instance.DirectEve.Me.IsInAbyssalSpace())
                return;

            if (DebugConfig.DebugDefense)
                Logging.Log.WriteLine("DebugDefense: Defense ProcessState");

            if (ESCache.Instance.InDockableLocation)
            {
                if (DebugConfig.DebugDefense)
                    Logging.Log.WriteLine("DebugDefense: We are in a station.");
                _trackingLinkScriptAttempts = 0;
                _sensorBoosterScriptAttempts = 0;
                _sensorDampenerScriptAttempts = 0;
                _trackingComputerScriptAttempts = 0;
                _trackingDisruptorScriptAttempts = 0;
                _nextOverloadAttempt = DateTime.UtcNow;
                return;
            }

            if (ESCache.Instance.Stations != null && ESCache.Instance.Stations.Any())
            {
                if (ESCache.Instance.Time.LastOfflineModuleCheck.AddSeconds(25) < DateTime.UtcNow && ESCache.Instance.InSpace &&
                    ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity != null &&
                    ESCache.Instance.ActiveShip.GroupId != (int)Group.Capsule && ESCache.Instance.Modules.Count(m => !m.IsOnline) > 0)
                {
                    ESCache.Instance.Time.LastOfflineModuleCheck = DateTime.UtcNow;

                    foreach (var mod in ESCache.Instance.Modules.Where(m => !m.IsOnline))
                        Logging.Log.WriteLine("Offline module: " + mod.TypeName);

                    Logging.Log.WriteLine("Offline modules found, going back to base and trying to fit again.");
                    ESCache.Instance.MissionSettings.CurrentFittingName = String.Empty;
                    ESCache.Instance.MissionSettings.OfflineModulesFound = true;
                    ESCache.Instance.State.CurrentQuestorState = QuestorState.Start;
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.Traveler.Destination = null;
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    return;
                }
            }

            if (!ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugDefense) Logging.Log.WriteLine("we are not in space (yet?)");
                return;
            }

            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity == null)
            {
                Logging.Log.WriteLine("no ship entity");
                return;
            }

            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
            {
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.CAPSULE, "Character is in a capsule."));

                if (DirectEve.Interval(15000, 25000))
                    Logging.Log.WriteLine("We are in a pod, no defense required...");
                return;
            }

            if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
            {
                if (DebugConfig.DebugDefense) Logging.Log.WriteLine("we are cloaked... no defense needed.");
                return;
            }

            if (ESCache.Instance.ActiveShip.CapacitorPercentage < 10 && !ESCache.Instance.Combat.TargetedBy.Any() &&
                ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.ShieldBoosters ||
                                                   i.GroupId == (int)Group.AncillaryShieldBooster ||
                                                   i.GroupId == (int)Group.CapacitorInjector ||
                                                   i.GroupId == (int)Group.ArmorRepairer)
                    .All(x => !x.IsActive))
            {
                if (DebugConfig.DebugDefense)
                    Logging.Log.WriteLine("Cap is SO low that we should not care about hardeners/boosters as we are not being targeted anyhow)");
                return;
            }

            ActivateRepairModules();
            if (DebugConfig.DebugDefense) Logging.Log.WriteLine("Starting ActivateOnce();");
            ActivateOnce();

            if (ESCache.Instance.InWarp)
            {
                _trackingLinkScriptAttempts = 0;
                _sensorBoosterScriptAttempts = 0;
                _sensorDampenerScriptAttempts = 0;
                _trackingComputerScriptAttempts = 0;
                _trackingDisruptorScriptAttempts = 0;
                return;
            }

            if (ControllerManager.Instance.TryGetController<QuestorController>(out _))
            {
                if (DebugConfig.DebugDefense || DebugConfig.DebugSpeedMod)
                    Logging.Log.WriteLine("Starting ActivateSpeedMod();");
                ActivateSpeedMod();

            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            if (cm.TryGetController<AbyssalController>(out _)) // do not run it while an abyss controller is running
            {
                return false;
            }

            if (cm.TryGetController<AbyssalGuardController>(out _)) 
            {
                return false;
            }

            if (cm.TryGetController<AbyssalHydraController>(out _)) 
            {
                return false;
            }

            if (cm.TryGetController<AbyssalHydraSlaveController>(out _))
            {
                return false;
            }

            return true;
        }

        private void ActivateOnce()
        {
            if (DateTime.UtcNow < ESCache.Instance.Time.NextActivateModules)
                return;

            foreach (var ActivateOncePerSessionModulewScript in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.TrackingDisruptor ||
                                                                                                   i.GroupId == (int)Group.TrackingComputer ||
                                                                                                   i.GroupId == (int)Group.TrackingLink ||
                                                                                                   i.GroupId == (int)Group.SensorBooster ||
                                                                                                   i.GroupId == (int)Group.SensorDampener ||
                                                                                                   i.GroupId == (int)Group.CapacitorInjector ||
                                                                                                   i.GroupId == (int)Group.AncillaryShieldBooster))
            {
                if (!ActivateOncePerSessionModulewScript.IsActivatable)
                    continue;

                if (ActivateOncePerSessionModulewScript.CurrentCharges < ActivateOncePerSessionModulewScript.MaxCharges)
                {
                    if (DebugConfig.DebugLoadScripts)
                        Logging.Log.WriteLine("Found Activatable Module with no charge[typeID:" + ActivateOncePerSessionModulewScript.TypeId + "]");
                    DirectItem scriptToLoad;

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.TrackingDisruptor && _trackingDisruptorScriptAttempts < 5)
                    {
                        _trackingDisruptorScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("TrackingDisruptor Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingDisruptorScript);

                        if (scriptToLoad != null)
                        {
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Activating TrackingDisruptor");
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);

                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.TrackingComputer && _trackingComputerScriptAttempts < 5)
                    {
                        _trackingComputerScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("TrackingComputer Found");
                        var TrackingComputerScript = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingComputerScript);

                        var EntityTrackingDisruptingMe = ESCache.Instance.Combat.TargetedBy.FirstOrDefault(t => t.IsTrackingDisruptingMe);
                        if (EntityTrackingDisruptingMe != null || TrackingComputerScript == null)
                            TrackingComputerScript = ESCache.Instance.CheckCargoForItem((int)TypeID.OptimalRangeScript);

                        scriptToLoad = TrackingComputerScript;
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Script Found for TrackingComputer");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Activate TrackingComputer");
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.TrackingLink && _trackingLinkScriptAttempts < 5)
                    {
                        _trackingLinkScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("TrackingLink Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingLinkScript);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Script Found for TrackingLink");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.SensorBooster && _sensorBoosterScriptAttempts < 5)
                    {
                        _sensorBoosterScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("SensorBooster Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.SensorBoosterScript);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Script Found for SensorBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.SensorDampener && _sensorDampenerScriptAttempts < 5)
                    {
                        _sensorDampenerScriptAttempts++;
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("SensorDampener Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.SensorDampenerScript);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("Script Found for SensorDampener");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(2);
                                    return;
                                }

                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.AncillaryShieldBooster)
                    {
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("ancillaryShieldBooster Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.AncillaryShieldBoosterScript);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts)
                                Logging.Log.WriteLine("CapBoosterCharges Found for ancillaryShieldBooster");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(500);
                                    return;
                                }

                            var inCombat = ESCache.Instance.Combat.TargetedBy.Any();
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline ||
                                inCombat && ActivateOncePerSessionModulewScript.CurrentCharges > 0)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                            return;
                        }
                        continue;
                    }

                    if (ActivateOncePerSessionModulewScript.GroupId == (int)Group.CapacitorInjector)
                    {
                        if (DebugConfig.DebugLoadScripts) Logging.Log.WriteLine("capacitorInjector Found");
                        scriptToLoad = ESCache.Instance.CheckCargoForItem(ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript);
                        if (scriptToLoad != null)
                        {
                            if (DebugConfig.DebugLoadScripts)
                                Logging.Log.WriteLine("CapBoosterCharges Found for capacitorInjector");
                            if (ActivateOncePerSessionModulewScript.IsActive)
                                if (ActivateOncePerSessionModulewScript.Click())
                                {
                                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(500);
                                    return;
                                }

                            var inCombat = ESCache.Instance.Combat.TargetedBy.Any();
                            if (ActivateOncePerSessionModulewScript.IsActive || ActivateOncePerSessionModulewScript.IsDeactivating ||
                                ActivateOncePerSessionModulewScript.IsReloadingAmmo || ActivateOncePerSessionModulewScript.IsInLimboState ||
                                !ActivateOncePerSessionModulewScript.IsOnline ||
                                inCombat && ActivateOncePerSessionModulewScript.CurrentCharges > 0)
                            {
                                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                continue;
                            }

                            if (!LoadthisScript(scriptToLoad, ActivateOncePerSessionModulewScript))
                            {
                                continue;
                            }
                        }
                        else if (ActivateOncePerSessionModulewScript.CurrentCharges == 0)
                        {
                            Logging.Log.WriteLine("ReloadCapBooster: ran out of cap booster with typeid: [ " + ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript + " ]");
                            ESCache.Instance.State.CurrentCombatState = CombatState.OutOfAmmo;
                            continue;
                        }
                        continue;
                    }
                }

                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                continue;
            }

            foreach (var ActivateOncePerSessionModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.ShieldHardeners ||
                                                                                            i.GroupId == (int)Group.ArmorHardeners ||
                                                                                            i.GroupId == (int)Group.ArmorResistanceShiftHardener ||
                                                                                            i.GroupId == (int)Group.SensorBooster ||
                                                                                            i.GroupId == (int)Group.TrackingComputer ||
                                                                                            i.GroupId == (int)Group.MissuleGuidanceComputer ||
                                                                                            i.GroupId == (int)Group.ECCM))

                                                                                         
            {
                if (!ActivateOncePerSessionModule.IsActivatable)
                    continue;

                if (DebugConfig.DebugDefense)
                    Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName + "] TypeID [" + ActivateOncePerSessionModule.TypeId +
                                  "] GroupId [" +
                                  ActivateOncePerSessionModule.GroupId + "] Activatable [" + ActivateOncePerSessionModule.IsActivatable + "] Found");

                if (ActivateOncePerSessionModule.IsActive)
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName + "] is already active");
                    continue;
                }

                if (ActivateOncePerSessionModule.IsInLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] is in LimboState (likely being activated or decativated already)");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 45)
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 45 UNITS of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 3% of cap: do not make it worse by turning on the hardeners");
                    continue;
                }

                if (ESCache.Instance.ActiveShip.Capacitor < 400 && !ESCache.Instance.Combat.TargetedBy.Any() &&
                    ESCache.Instance.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower())
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName +
                                      "] You have less then 400 units total cap and nothing is targeting you yet, no need for hardeners yet.");
                    continue;
                }

                if (ActivateOncePerSessionModule.Click())
                {
                    ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + ActivateOncePerSessionModule.ItemId + "][" + ActivateOncePerSessionModule.TypeName + "] activated");
                    return;
                }
            }
        }

        private void ActivateRepairModules()
        {

            if (ESCache.Instance.Modules.Any(e => e.GroupId == 60 && e.EffectId == 7012 && e.IsActivatable)) // assault damage control effect
            {
                if (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking))
                {
                    var module =
                        ESCache.Instance.Modules.FirstOrDefault(e => e.GroupId == 60 && e.EffectId == 7012 && e.IsActivatable);

                    if (module.IsInLimboState)
                        return;

                    if (module.IsDeactivating)
                        return;

                    if (module.IsActive)
                        return;

                    Log($"We are being attacked by a player, activating assault damage control.");

                    if (DirectEve.Interval(5000, 6000))
                        module.Click();
                }
            }

            if (DateTime.UtcNow < ESCache.Instance.Time.NextRepModuleAction)
            {
                if (DebugConfig.DebugDefense)
                    Logging.Log.WriteLine("if (DateTime.UtcNow < Time.Instance.NextRepModuleAction [" +
                                  ESCache.Instance.Time.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds +
                                  " Sec from now])");
                return;
            }

            foreach (var repairModule in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.ShieldBoosters ||
                                                                            i.GroupId == (int)Group.AncillaryShieldBooster ||
                                                                            i.GroupId == (int)Group.CapacitorInjector ||
                                                                            i.GroupId == (int)Group.ArmorRepairer)
                .Where(x => x.IsOnline))
            {
                if (repairModule.IsInLimboState)
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("[" + repairModule.ItemId + "][" + repairModule.TypeName + "] is IsInLimboState, continue");
                    continue;
                }

                double perc;
                double cap;
                cap = ESCache.Instance.ActiveShip.CapacitorPercentage;

                if (repairModule.GroupId == (int)Group.ShieldBoosters ||
                    repairModule.GroupId == (int)Group.AncillaryShieldBooster ||
                    repairModule.GroupId == (int)Group.CapacitorInjector)
                    perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                else if (repairModule.GroupId == (int)Group.ArmorRepairer)
                    perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                else
                    continue;

                var inCombat = ESCache.Instance.EntitiesOnGrid.Any(i => i.IsTargetedBy) || ESCache.Instance.Combat.PotentialCombatTargets.Any();
                if (!repairModule.IsActive && inCombat && cap < ESCache.Instance.EveAccount.CS.QMS.QS.Injectcapperc && repairModule.GroupId == (int)Group.CapacitorInjector &&
                    repairModule.CurrentCharges > 0)
                    if (repairModule.Click())
                    {
                        Logging.Log.WriteLine("Cap: [" + Math.Round(cap, 0) + "%] Capacitor Booster: [" + repairModule.ItemId + "] activated");
                        return;
                    }

                if (!repairModule.IsActive &&
                    (inCombat && perc < ESCache.Instance.EveAccount.CS.QMS.QS.ActivateRepairModules ||
                     !inCombat && perc < ESCache.Instance.EveAccount.CS.QMS.QS.DeactivateRepairModules && cap > ESCache.Instance.EveAccount.CS.QMS.QS.SafeCapacitorPct))
                {
                    if (ESCache.Instance.ActiveShip.ShieldPercentage < ESCache.Instance.Statistics.LowestShieldPercentageThisPocket)
                    {
                        ESCache.Instance.Statistics.LowestShieldPercentageThisPocket = ESCache.Instance.ActiveShip.ShieldPercentage;
                        ESCache.Instance.Statistics.LowestShieldPercentageThisMission = ESCache.Instance.ActiveShip.ShieldPercentage;
                    }

                    if (ESCache.Instance.ActiveShip.ArmorPercentage < ESCache.Instance.Statistics.LowestArmorPercentageThisPocket)
                    {
                        ESCache.Instance.Statistics.LowestArmorPercentageThisPocket = ESCache.Instance.ActiveShip.ArmorPercentage;
                        ESCache.Instance.Statistics.LowestArmorPercentageThisMission = ESCache.Instance.ActiveShip.ArmorPercentage;
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.Statistics.LowestCapacitorPercentageThisPocket)
                    {
                        ESCache.Instance.Statistics.LowestCapacitorPercentageThisPocket = ESCache.Instance.ActiveShip.CapacitorPercentage;
                        ESCache.Instance.Statistics.LowestCapacitorPercentageThisMission = ESCache.Instance.ActiveShip.CapacitorPercentage;
                    }

                    if (ESCache.Instance.UnlootedContainers != null && ESCache.Instance.Statistics.WrecksThisPocket != ESCache.Instance.UnlootedContainers.Count())
                        ESCache.Instance.Statistics.WrecksThisPocket = ESCache.Instance.UnlootedContainers.Count();

                    if (repairModule.GroupId == (int)Group.AncillaryShieldBooster)
                        if (repairModule.CurrentCharges > 0)
                            if (repairModule.Click())
                            {
                                Logging.Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Ancillary Shield Booster: [" + repairModule.ItemId + "] activated");
                                ESCache.Instance.Time.StartedBoosting = DateTime.UtcNow;
                                ESCache.Instance.Time.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                                return;
                            }

                    if (ESCache.Instance.ActiveShip.Capacitor == 0 || ESCache.Instance.ActiveShip.Capacitor < 25)
                    {
                        if (DebugConfig.DebugDefense)
                            Logging.Log.WriteLine("if (Cache.Instance.ActiveShip.Capacitor [" + ESCache.Instance.ActiveShip.Capacitor + "] < 25)");
                        continue;
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage == 0 || ESCache.Instance.ActiveShip.CapacitorPercentage < 3)
                    {
                        if (DebugConfig.DebugDefense)
                            Logging.Log.WriteLine("if (Cache.Instance.ActiveShip.CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] < 3)");
                        continue;
                    }

                    if (repairModule.GroupId == (int)Group.ShieldBoosters || repairModule.GroupId == (int)Group.ArmorRepairer)
                    {
                        if (DebugConfig.DebugDefense)
                            Logging.Log.WriteLine("Perc: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" +
                                          repairModule.ItemId +
                                          "][" + repairModule.TypeName + "]");
                        if (repairModule.Click())
                        {
                            ESCache.Instance.Time.StartedBoosting = DateTime.UtcNow;
                            ESCache.Instance.Time.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);

                            if (ESCache.Instance.ActiveShip.ArmorPercentage * 100 < 100)
                                ESCache.Instance.Arm.NeedRepair = true;

                            if (repairModule.GroupId == (int)Group.ShieldBoosters || repairModule.GroupId == (int)Group.AncillaryShieldBooster)
                            {
                                perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                                Logging.Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" +
                                              repairModule.ItemId + "] activated");
                            }
                            else if (repairModule.GroupId == (int)Group.ArmorRepairer)
                            {
                                perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                                Logging.Log.WriteLine("Tank % [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: [" +
                                              repairModule.ItemId + "] activated");
                            }

                            return;
                        }
                    }
                }

                if (repairModule.IsActive && (perc >= ESCache.Instance.EveAccount.CS.QMS.QS.DeactivateRepairModules || repairModule.GroupId == (int)Group.CapacitorInjector))
                {
                    if (DebugConfig.DebugDefense)
                        Logging.Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Attempting to Click to Deactivate [" +
                                      repairModule.ItemId +
                                      "][" + repairModule.TypeName + "]");
                    if (repairModule.Click())
                    {

                        try
                        {
                            ESCache.Instance.Time.NextRepModuleAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.DefenceDelay_milliseconds);
                            ESCache.Instance.Statistics.RepairCycleTimeThisPocket = ESCache.Instance.Statistics.RepairCycleTimeThisPocket +
                                                                   (int)DateTime.UtcNow.Subtract(ESCache.Instance.Time.StartedBoosting).TotalSeconds;
                            ESCache.Instance.Statistics.RepairCycleTimeThisMission = ESCache.Instance.Statistics.RepairCycleTimeThisMission +
                                                                    (int)DateTime.UtcNow.Subtract(ESCache.Instance.Time.StartedBoosting).TotalSeconds;
                        }
                        catch (Exception)
                        {

                        }

                        if (repairModule.GroupId == (int)Group.ShieldBoosters || repairModule.GroupId == (int)Group.CapacitorInjector)
                        {
                            perc = ESCache.Instance.ActiveShip.ShieldPercentage;
                            Logging.Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Shield Booster: [" + repairModule.ItemId +
                                          "] deactivated [" +
                                          Math.Round(ESCache.Instance.Time.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                          "] sec reactivation delay");
                        }
                        else if (repairModule.GroupId == (int)Group.ArmorRepairer)
                        {
                            perc = ESCache.Instance.ActiveShip.ArmorPercentage;
                            Logging.Log.WriteLine("Tank %: [" + Math.Round(perc, 0) + "%] Cap: [" + Math.Round(cap, 0) + "%] Armor Repairer: [" + repairModule.ItemId +
                                          "] deactivated [" +
                                          Math.Round(ESCache.Instance.Time.NextRepModuleAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                          "] sec reactivation delay");
                        }

                        return;
                    }
                }

                continue;
            }
        }

        private void ActivateSpeedMod()
        {
            foreach (var SpeedMod in ESCache.Instance.Modules.Where(i => i.GroupId == (int)Group.Afterburner))
            {
                if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(SpeedMod.ItemId))
                {
                    if (DebugConfig.DebugSpeedMod)
                        Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName + "] was last activated [" +
                                      Math.Round(DateTime.UtcNow.Subtract(ESCache.Instance.Time.LastActivatedTimeStamp[SpeedMod.ItemId]).TotalSeconds, 0) +
                                      "] sec ago");
                    if (ESCache.Instance.Time.LastActivatedTimeStamp[SpeedMod.ItemId].AddMilliseconds(ESCache.Instance.Time.AfterburnerDelay_milliseconds) > DateTime.UtcNow)
                        continue;
                }

                if (SpeedMod.IsInLimboState)
                {
                    if (DebugConfig.DebugSpeedMod)
                        Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName + "] isActive [" + SpeedMod.IsActive + "]");
                    continue;
                }

                if (DebugConfig.DebugSpeedMod)
                    Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName + "] isActive [" + SpeedMod.IsActive + "]");

                if (SpeedMod.IsActive)
                {
                    var deactivate = false;

                    if (!ESCache.Instance.DirectEve.ActiveShip.Entity.IsApproachingOrKeptAtRange && !ESCache.Instance.DirectEve.ActiveShip.Entity.IsOrbiting)
                    {
                        deactivate = true;
                        if (DebugConfig.DebugSpeedMod)
                            Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName +
                                                  "] We are not approaching or orbiting anything: Deactivate [" +
                                                  deactivate + "]");
                    }
                    else if (!ESCache.Instance.Combat.PotentialCombatTargets.Any() && DateTime.UtcNow > ESCache.Instance.Statistics.StartedPocket.AddMinutes(10) &&
                             ESCache.Instance.ActiveShip.GivenName == ESCache.Instance.EveAccount.CS.QMS.CombatShipName)
                    {
                        deactivate = true;
                        if (DebugConfig.DebugSpeedMod)
                            Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName +
                                          "] Nothing on grid is attacking and it has been more than 60 seconds since we landed in this pocket. Deactivate [" +
                                          deactivate + "]");
                    }
                    else if (!ESCache.Instance.NavigateOnGrid.SpeedTank)
                    {
                        if ((ESCache.Instance.ActiveShip.Entity.IsOrbiting || ESCache.Instance.ActiveShip.Entity.IsApproachingOrKeptAtRange) && ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.FollowingEntity.Distance < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleDistance)
                            {
                                deactivate = true;
                                if (DebugConfig.DebugSpeedMod)
                                    Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName + "] We are approaching... and [" +
                                                  Math.Round(ESCache.Instance.FollowingEntity.Distance / 1000, 0) + "] is within [" +
                                                  Math.Round((double)ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleDistance / 1000, 0) + "] Deactivate [" + deactivate + "]");
                            }
                    }
                    else if (ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleCapacitor)
                    {
                        deactivate = true;
                        if (DebugConfig.DebugSpeedMod)
                            Logging.Log.WriteLine("[" + SpeedMod.ItemId + "][" + SpeedMod.TypeName + "] Capacitor is at [" +
                                          ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] which is below MinimumPropulsionModuleCapacitor [" + ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleCapacitor + "] Deactivate [" +
                                          deactivate +
                                          "]");
                    }

                    if (deactivate)
                    {
                        if (ESCache.Instance.NavigateOnGrid.SpeedTank) return;

                        if (SpeedMod.Click())
                        {
                            if (DebugConfig.DebugSpeedMod)
                                Logging.Log.WriteLine("[" + SpeedMod.ItemId + "] [" + SpeedMod.TypeName + "] Deactivated");
                            return;
                        }
                    }
                }

                if (!SpeedMod.IsActive && !SpeedMod.IsInLimboState)
                {
                    var activate = false;

                    if ((ESCache.Instance.ActiveShip.Entity.IsOrbiting || ESCache.Instance.ActiveShip.Entity.IsApproachingOrKeptAtRange) && ESCache.Instance.FollowingEntity != null)
                    {
                        if (ESCache.Instance.FollowingEntity.Distance > ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleDistance)
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Logging.Log.WriteLine("[" + SpeedMod.ItemId + "] SpeedTank is [" + ESCache.Instance.NavigateOnGrid.SpeedTank +
                                              "] We are approaching or orbiting and [" +
                                              Math.Round(ESCache.Instance.FollowingEntity.Distance / 1000, 0) +
                                              "k] is within MinimumPropulsionModuleDistance [" +
                                              Math.Round((double)ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleDistance / 1000, 2) + "] Activate [" + activate + "]");
                        }

                        if (ESCache.Instance.NavigateOnGrid.SpeedTank)
                        {
                            activate = true;
                            if (DebugConfig.DebugSpeedMod)
                                Logging.Log.WriteLine("[" + SpeedMod.ItemId + "] We are approaching or orbiting: Activate [" + activate + "]");
                        }
                    }

                    if (ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleCapacitor)
                    {
                        activate = false;
                        if (DebugConfig.DebugSpeedMod)
                            Logging.Log.WriteLine("[" + SpeedMod.ItemId + "] CapacitorPercentage is [" + ESCache.Instance.ActiveShip.CapacitorPercentage +
                                          "] which is less than MinimumPropulsionModuleCapacitor [" + ESCache.Instance.EveAccount.CS.QMS.QS.MinimumPropulsionModuleCapacitor + "] Activate [" +
                                          activate + "]");
                    }

                    if (ESCache.Instance.DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.warpScramblerMWD))
                    {
                        activate = false;
                        if (DebugConfig.DebugSpeedMod)
                            Logging.Log.WriteLine("[" + SpeedMod.ItemId + "] We are scrammed: Activate [" + activate + "]");
                    }

                    if (activate)
                        if (SpeedMod.Click())
                            return;
                }

                continue;
            }
        }

        private bool LoadthisScript(DirectItem scriptToLoad, DirectUIModule uiModule)
        {
            if (scriptToLoad != null)
            {
                if (uiModule.IsReloadingAmmo || uiModule.IsActive || uiModule.IsDeactivating || uiModule.IsInLimboState ||
                    !uiModule.IsOnline)
                    return false;

                if (uiModule.Charge != null && uiModule.Charge.TypeId == scriptToLoad.TypeId && uiModule.CurrentCharges == uiModule.MaxCharges)
                {
                    Logging.Log.WriteLine("module is already loaded with the script we wanted");
                    NextScriptReload[uiModule.ItemId] = DateTime.UtcNow.AddSeconds(15);
                    return false;
                }

                if (NextScriptReload.ContainsKey(uiModule.ItemId) && DateTime.UtcNow < NextScriptReload[uiModule.ItemId].AddSeconds(15))
                {
                    Logging.Log.WriteLine("module was reloaded recently... skipping");
                    return false;
                }
                NextScriptReload[uiModule.ItemId] = DateTime.UtcNow.AddSeconds(15);

                if (DateTime.UtcNow.Subtract(ESCache.Instance.Time.LastLoggingAction).TotalSeconds > 10)
                    ESCache.Instance.Time.LastLoggingAction = DateTime.UtcNow;

                if (uiModule.ChangeAmmo(scriptToLoad))
                {
                    Logging.Log.WriteLine("Changing [" + uiModule.TypeId + "] with [" + scriptToLoad.TypeName + "][TypeID: " + scriptToLoad.TypeId + "]");
                    return true;
                }

                return false;
            }
            Logging.Log.WriteLine("script to load was NULL!");
            return false;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}