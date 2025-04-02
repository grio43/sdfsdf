/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers.Questor
{
    /// <summary>
    ///     Description of SalvageController.
    /// </summary>
    public class SalvageController : BaseController
    {
        #region Fields

        public List<int> IgnoreUntilLoot = new List<int>() { 24765 };
        public bool MissionLoot;
        private SalvageState _state;
        private Dictionary<long, DateTime> OpenedContainers = new Dictionary<long, DateTime>();
        private Dictionary<long, DateTime> _activatedTractors = new Dictionary<long, DateTime>();
        private DateTime _nextCargoStack;

        #endregion Fields

        #region Constructors

        public SalvageController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            _tractorTimes = new Dictionary<long, DateTime>();
        }

        #endregion Constructors

        #region Properties

        public bool OpenWrecks { get; set; }
        private Dictionary<long, DateTime> _tractorTimes;

        #endregion Properties

        #region Methods

        public void ActivateSalvagers()
        {
            if (ESCache.Instance.Time.NextSalvageAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugSalvage)
                    Logging.Log.WriteLine("Debug: Cache.Instance.NextSalvageAction is still in the future, waiting");
                return;
            }

            var salvagers = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.Salvager).ToList();

            if (salvagers.Count == 0)
            {
                if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("Debug: if (salvagers.Count == 0)");
                return;
            }

            if (ESCache.Instance.InMission && ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.TractorBeamMinimumCapacitor)
            {
                if (DebugConfig.DebugSalvage)
                    Logging.Log.WriteLine("Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                  ESCache.Instance.EveAccount.CS.QMS.QS.SalvagerMinimumCapacitor +
                                  "%] SalvagerMinimumCapacitor");
                return;
            }

            var wrecks =
                ESCache.Instance.Targets.Where(t => t.GroupId == (int)Group.Wreck && t.Distance < salvagers.Min(s => s.OptimalRange))
                    .ToList();

            if (wrecks.Count == 0)
            {
                if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("Debug: if (wrecks.Count == 0)");
                return;
            }

            //
            // Activate
            //
            var salvagersProcessedThisTick = 0;
            var WreckNumber = 0;
            foreach (var wreck in wrecks.OrderByDescending(i => i.IsLootTarget).ThenByDescending(i => i.IsLargeWreck)
                .ThenByDescending(i => i.IsMediumWreck)
                .ThenByDescending(i => i.IsSmallWreck))
            {
                WreckNumber++;

                foreach (var salvager in salvagers)
                {
                    if (salvager.IsActive)
                    {
                        if (DebugConfig.DebugSalvage)
                            Logging.Log.WriteLine("[" + WreckNumber + "][::" + salvager.ItemId + "] _ Salvager is: IsActive [" + salvager.IsActive + "]. Continue");
                        continue;
                    }

                    if (salvager.IsInLimboState)
                    {
                        if (DebugConfig.DebugSalvage)
                            Logging.Log.WriteLine("[" + WreckNumber + "][::" + salvager.ItemId + "] __ Salvager is: IsInLimboState [" + salvager.IsInLimboState +
                                          "] IsDeactivating [" +
                                          salvager.IsDeactivating + "] IsActivatable [" + salvager.IsActivatable + "] IsOnline [" + salvager.IsOnline +
                                          "] TargetId [" + salvager.TargetId + "]. Continue");
                        continue;
                    }

                    //
                    // this tractor has already been activated at least once
                    //
                    if (ESCache.Instance.Time.LastActivatedTimeStamp != null && ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(salvager.ItemId))
                        if (ESCache.Instance.Time.LastActivatedTimeStamp[salvager.ItemId].AddSeconds(5) > DateTime.UtcNow)
                            continue;

                    //
                    // if we have more wrecks on the field then we have salvagers that have not yet been activated
                    //

                    var notActiveSalvAmount = salvagers.Count(i => !i.IsActive);
                    if (wrecks.Count > notActiveSalvAmount)
                    {
                        if (DebugConfig.DebugSalvage)
                            Logging.Log.WriteLine("We have [" + wrecks.Count + "] wrecks  and [" + salvagers.Count(i => !i.IsActive) +
                                          "] available salvagers of [" +
                                          salvagers.Count + "] total");
                        //
                        // skip activating any more salvagers on this wreck that already has at least 1 salvager on it.
                        //
                        if (salvagers.Any(i => i.IsActive && i.TargetId == wreck.Id))
                        {
                            if (DebugConfig.DebugSalvage)
                                Logging.Log.WriteLine("Not assigning another salvager to wreck [" + wreck.Name + "][" + wreck.DirectEntity.Id.ToString() + "]at[" +
                                              Math.Round(wreck.Distance / 1000, 0) + "k] as it already has at least 1 salvager active");
                            //
                            // Break out of the Foreach salvager in salvagers and continue to the next wreck
                            //
                            break;
                        }
                    }

                    Logging.Log.WriteLine("Activating salvager [" + salvager.ItemId + "] on [" + wreck.Name + "][ID: " + wreck.DirectEntity.Id.ToString() + "] we have [" +
                                  wrecks.Count +
                                  "] wrecks targeted in salvager range");
                    if (salvager.Activate(wreck.Id))
                    {
                        salvagersProcessedThisTick++;
                        ESCache.Instance.Time.NextSalvageAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.SalvageDelayBetweenActions_milliseconds);
                        if (salvagersProcessedThisTick < ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle)
                        {
                            if (DebugConfig.DebugSalvage)
                                Logging.Log.WriteLine("Debug: if (salvagersProcessedThisTick < Settings.Instance.NumberOfModulesToActivateInCycle)");
                            continue;
                        }

                        //
                        // return, no more processing this tick
                        //
                        return;
                    }

                    //
                    // move on to the next salvager
                    //
                    continue;
                }

                //
                // move on to the next wreck
                //
                continue;
            }
        }

        public override void DoWork()
        {
            // Nothing to salvage in stations
            if (ESCache.Instance.InDockableLocation || !ESCache.Instance.InSpace)
            {
                _state = SalvageState.Idle;
                return;
            }

            // What? No ship entity?
            if (ESCache.Instance.ActiveShip.Entity == null)
            {
                _state = SalvageState.Idle;
                return;
            }

            // When in warp there's nothing we can do, so ignore everything
            if (ESCache.Instance.InSpace && ESCache.Instance.InWarp)
            {
                _state = SalvageState.Idle;
                return;
            }

            // There is no salving when cloaked -
            // why not? seems like we might be able to ninja-salvage with a covert-ops hauler with some additional coding (someday?)
            if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
            {
                _state = SalvageState.Idle;
                return;
            }

            switch (_state)
            {
                case SalvageState.TargetWrecks:
                    if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("SalvageState.TargetWrecks:");
                    TargetWrecks();

                    // Next state
                    _state = SalvageState.LootWrecks;
                    break;

                case SalvageState.LootWrecks:
                    if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("SalvageState.LootWrecks:");
                    LootWrecks();

                    _state = SalvageState.SalvageWrecks;
                    break;

                case SalvageState.SalvageWrecks:
                    if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("SalvageState.SalvageWrecks:");
                    ActivateTractorBeams();
                    ActivateSalvagers();

                    _state = SalvageState.TargetWrecks;
                    if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.CanBeStacked
                        && _nextCargoStack < DateTime.UtcNow)
                    {
                        _nextCargoStack = DateTime.UtcNow.AddSeconds(new Random().Next(50, 90));
                        _state = SalvageState.StackItems;
                    }
                    break;

                case SalvageState.StackItems:
                    if (!(ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.StackAll())) return;
                    Logging.Log.WriteLine("Done stacking");
                    _state = SalvageState.TargetWrecks;
                    break;

                case SalvageState.Idle:
                    if (DebugConfig.DebugSalvage) Logging.Log.WriteLine("SalvageState.Idle:");
                    if (ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.Entity != null && !ESCache.Instance.ActiveShip.Entity.IsCloaked &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower() && !ESCache.Instance.InWarp && ESCache.Instance.InMission)
                    {
                        _state = SalvageState.TargetWrecks;
                        return;
                    }
                    break;

                default:

                    // Unknown state, goto first state
                    _state = SalvageState.TargetWrecks;
                    break;
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        /// <summary>
        ///     Target wrecks within range
        /// </summary>
        public void TargetWrecks()
        {
            // We are jammed, we do not need to log (Combat does this already)
            if (ESCache.Instance.MaxLockedTargets == 0 || ESCache.Instance.Targets.Any() && ESCache.Instance.Targets.Count() >= ESCache.Instance.MaxLockedTargets)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Logging.Log.WriteLine(
                        "Debug: if (Cache.Instance.MaxLockedTargets == 0) || Cache.Instance.Targets.Any() && Cache.Instance.Targets.Count() >= Cache.Instance.MaxLockedTargets");
                return;
            }

            var tractorBeams = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TractorBeam).ToList();

            var targets = new List<EntityCache>();
            targets.AddRange(ESCache.Instance.Targets);
            targets.AddRange(ESCache.Instance.Targeting);

            var hasSalvagers = ESCache.Instance.Modules.Any(m => m.GroupId == (int)Group.Salvager);
            var wreckTargets =
                targets.Where(t => (t.GroupId == (int)Group.Wreck || t.GroupId == (int)Group.CargoContainer) && t.CategoryId == (int)CategoryID.Celestial)
                    .ToList();

            //
            // UnTarget Wrecks/Containers, etc as they get in range
            //
            foreach (var wreck in wreckTargets.OrderByDescending(i => i.IsLootTarget))
            {
                if (!hasSalvagers && (wreck.IsWreckEmpty || ESCache.Instance.LootedContainers.Contains(wreck.Id))
                ) //this  only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                {
                    Logging.Log.WriteLine("Wreck: [" + wreck.Name + "][" + Math.Round(wreck.Distance / 1000, 0) + "k][ID: " + wreck.DirectEntity.Id.ToString() +
                                  "] wreck is empty, unlocking container.");

                    if (!ESCache.Instance.LootedContainers.Contains(wreck.Id)) //
                        ESCache.Instance.LootedContainers.Add(wreck.Id);
                    wreck.UnlockTarget();
                    continue;
                }

                if (hasSalvagers && wreck.GroupId != (int)Group.CargoContainer)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (hasSalvagers && wreck.GroupId != (int)Group.CargoContainer))");
                    continue;
                }

                // Unlock if within loot range
                if (wreck.Distance < (int)Distances.SafeScoopRange)
                {
                    Logging.Log.WriteLine("Cargo Container [" + wreck.Name + "][" + Math.Round(wreck.Distance / 1000, 0) + "k][ID: " + wreck.DirectEntity.Id.ToString() +
                                  "] within loot range, unlocking container.");
                    wreck.UnlockTarget();
                    continue;
                }
            }

            if (MissionLoot)
            {
                if (wreckTargets.Count >= ESCache.Instance.MaxLockedTargets)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (wreckTargets.Count >= Cache.Instance.MaxLockedTargets)");
                    return;
                }
            }
            else if (wreckTargets.Count >= ESCache.Instance.EveAccount.CS.QMS.QS.MaximumWreckTargets && ESCache.Instance.Combat.TargetedBy.Any() ||
                     ESCache.Instance.Targets.Count() >= ESCache.Instance.MaxLockedTargets)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Logging.Log.WriteLine("Debug: else if (wreckTargets.Count >= MaximumWreckTargets)");
                return;
            }

            double tractorBeamRange = 0;
            if (tractorBeams.Count > 0)
                tractorBeamRange = (double)tractorBeams.Min(t => t.OptimalRange);

            if (!OpenWrecks)
            {
                if (DebugConfig.DebugTargetWrecks)
                    Logging.Log.WriteLine("Debug: OpenWrecks is false, we do not need to target any wrecks.");
                return;
            }

            //
            // TargetWrecks/Container, etc If needed
            //
            var wrecksProcessedThisTick = 0;
            var AttemptToTargetThese = ESCache.Instance.UnlootedContainers
                .OrderByDescending(i => i.IsLootTarget)
                .ThenByDescending(i => i.IsLargeWreck)
                .ThenByDescending(i => i.IsMediumWreck)
                .ThenByDescending(i => i.IsSmallWreck);

            if (DebugConfig.DebugTargetWrecks)
            {
                foreach (var e in AttemptToTargetThese)
                {
                    Logging.Log.WriteLine($"[Targetwrecks] Typename {e.TypeName} TypeId {e.TypeId} IsLargeWreck {e.IsLargeWreck} IsMediumWreck {e.IsMediumWreck} IsSmallWreck {e.IsSmallWreck}");
                }
            }

            foreach (var wreck in AttemptToTargetThese)
            {
                // Its already a target, ignore it
                if (wreck.IsTarget || wreck.IsTargeting)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (wreck.IsTarget || wreck.IsTargeting)");
                    continue;
                }

                if (wreck.Distance > tractorBeamRange)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (wreck.Distance > tractorBeamRange)");
                    continue;
                }

                if (!wreck.HaveLootRights)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (!wreck.HaveLootRights)");
                    continue;
                }

                // No need to tractor a non-wreck within loot range
                if (wreck.GroupId != (int)Group.Wreck && wreck.Distance < (int)Distances.SafeScoopRange)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (wreck.GroupId != (int)Group.Wreck && wreck.Distance < (int)Distance.SafeScoopRange)");
                    continue;
                }

                if (IgnoreUntilLoot.Any(a => a == wreck.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(wreck.Id))
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: IgnoreUntilLoot.Any(a => a == wreck.TypeId) && !QCache.Instance.ListofContainersToLoot.Contains(wreck.Id)");
                    continue;
                }

                if (wreck.GroupId != (int)Group.Wreck && wreck.GroupId != (int)Group.CargoContainer)
                {
                    if (DebugConfig.DebugTargetWrecks)
                        Logging.Log.WriteLine("Debug: if (wreck.GroupId != (int)Group.Wreck && wreck.GroupId != (int)Group.CargoContainer)");
                    continue;
                }

                if (!hasSalvagers)
                {
                    // Ignore already looted wreck
                    if (ESCache.Instance.LootedContainers.Contains(wreck.Id))
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Logging.Log.WriteLine("Debug: Ignoring Already Looted Entity ID [" + wreck.Id + "]");
                        continue;
                    }

                    // Ignore empty wrecks
                    if (wreck.IsWreckEmpty) //this only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                    {
                        ESCache.Instance.LootedContainers.Add(wreck.Id);
                        if (DebugConfig.DebugTargetWrecks)
                            Logging.Log.WriteLine("Debug: Ignoring Empty Entity ID [" + wreck.Id + "]");
                        continue;
                    }

                    // Ignore wrecks already in loot range
                    if (wreck.Distance < (int)Distances.SafeScoopRange)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Logging.Log.WriteLine("Debug: Ignoring Entity that is already in loot range ID [" + wreck.Id + "]");
                        continue;
                    }
                }

                if (wreck.LockTarget("Salvage"))
                {
                    Logging.Log.WriteLine("Locking [" + wreck.Name + "][" + Math.Round(wreck.Distance / 1000, 0) + "k][ID: " + wreck.DirectEntity.Id.ToString() + "][" +
                                  Math.Round(wreck.Distance / 1000, 0) + "k away]");
                    wreckTargets.Add(wreck);
                    wrecksProcessedThisTick++;
                    if (DebugConfig.DebugSalvage)
                        Logging.Log.WriteLine("wrecksProcessedThisTick [" + wrecksProcessedThisTick + "]");

                    if (MissionLoot)
                    {
                        if (wreckTargets.Count >= ESCache.Instance.MaxLockedTargets)
                        {
                            if (DebugConfig.DebugTargetWrecks)
                                Logging.Log.WriteLine(" wreckTargets.Count [" + wreckTargets.Count + "] >= Cache.Instance.MaxLockedTargets) [" +
                                              ESCache.Instance.MaxLockedTargets +
                                              "]");
                            return;
                        }
                    }
                    else
                    {
                        if (wreckTargets.Count >= ESCache.Instance.EveAccount.CS.QMS.QS.MaximumWreckTargets)
                        {
                            if (DebugConfig.DebugTargetWrecks)
                                Logging.Log.WriteLine(" wreckTargets.Count [" + wreckTargets.Count + "] >= MaximumWreckTargets [" + ESCache.Instance.EveAccount.CS.QMS.QS.MaximumWreckTargets + "]");
                            return;
                        }
                    }

                    if (wrecksProcessedThisTick < ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Logging.Log.WriteLine("if (wrecksProcessedThisTick [" + wrecksProcessedThisTick +
                                          "] < Settings.Instance.NumberOfModulesToActivateInCycle [" +
                                          ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle + "])");
                        continue;
                    }
                }

                return;
            }
        }

        private void ActivateTractorBeams()
        {
            if (ESCache.Instance.Time.NextTractorBeamAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugTractorBeams)
                    Logging.Log.WriteLine("Debug: Cache.Instance.NextTractorBeamAction is still in the future, waiting");
                return;
            }

            var tractorBeams = ESCache.Instance.Modules.Where(m => m.GroupId == (int)Group.TractorBeam).ToList();
            if (!tractorBeams.Any())
                return;

            if (ESCache.Instance.InMission && ESCache.Instance.InSpace && ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.TractorBeamMinimumCapacitor)
            {
                if (DebugConfig.DebugTractorBeams)
                    Logging.Log.WriteLine("Capacitor [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] below [" +
                                  ESCache.Instance.EveAccount.CS.QMS.QS.TractorBeamMinimumCapacitor +
                                  "%] TractorBeamMinimumCapacitor");
                return;
            }

            var tractorBeamRange = tractorBeams.Min(t => t.OptimalRange);
            var wrecks =
                ESCache.Instance.Targets.Where(t => (t.GroupId == (int)Group.Wreck || t.GroupId == (int)Group.CargoContainer) && t.Distance < tractorBeamRange)
                    .ToList();

            var tractorsProcessedThisTick = 0;

            //
            // Deactivate tractorbeams
            //
            foreach (var tractorBeam in tractorBeams)
            {
                if (tractorBeam.IsActive && ESCache.Instance.MyShipEntity.Velocity > 40)
                {
                    if (DebugConfig.DebugTractorBeams)
                        Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Tractorbeam is: IsActive [" + tractorBeam.IsActive + "]");
                    continue;
                }

                if (tractorBeam.IsInLimboState)
                {
                    if (DebugConfig.DebugTractorBeams)
                        Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Tractorbeam is: IsInLimboState [" + tractorBeam.IsInLimboState + "] IsDeactivating [" +
                                      tractorBeam.IsDeactivating + "] IsActivatable [" + tractorBeam.IsActivatable + "] IsOnline [" +
                                      tractorBeam.IsOnline +
                                      "]");
                    continue;
                }

                var wreck = wrecks.FirstOrDefault(w => w.Id == tractorBeam.TargetId);

                var currentWreckUnlooted = false;

                if (DebugConfig.DebugTractorBeams)
                    Logging.Log.WriteLine("MyShip.Velocity [" + Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "]");
                if (ESCache.Instance.MyShipEntity.Velocity > 300)
                {
                    if (DebugConfig.DebugTractorBeams)
                        Logging.Log.WriteLine("if (Cache.Instance.MyShip.Velocity > 300)");
                    if (ESCache.Instance.UnlootedContainers.Any(unlootedcontainer => tractorBeam.TargetId == unlootedcontainer.Id))
                    {
                        currentWreckUnlooted = true;
                        if (DebugConfig.DebugTractorBeams)
                            Logging.Log.WriteLine("if (tractorBeam.TargetId == unlootedcontainer.Id)");
                    }
                }

                // If the wreck no longer exists, or its within loot range then disable the tractor beam
                // If the wreck no longer exist, beam should be deactivated automatically. Without our interaction.
                // Only deactivate while NOT speed tanking
                if (tractorBeam.IsActive && !ESCache.Instance.NavigateOnGrid.SpeedTank)
                {
                    if (wreck == null ||
                        wreck.Distance <= (int)Distances.SafeScoopRange && !currentWreckUnlooted && ESCache.Instance.MyShipEntity.Velocity < 300)
                    {
                        if (DebugConfig.DebugTractorBeams)
                            if (wreck != null)
                                Logging.Log.WriteLine(
                                    "[" + tractorBeam.ItemId + "] Tractorbeam: IsActive [" + tractorBeam.IsActive + "] and the wreck [" + wreck.Name ??
                                    "null" + "] is in SafeScoopRange [" + Math.Round(wreck.Distance / 1000, 0) + "]");
                            else
                                Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Tractorbeam: IsActive [" + tractorBeam.IsActive + "] on what? wreck was null!");

                        //tractorBeams.Remove(tractorBeam);
                        if (tractorBeam.Click())
                        {
                            Logging.Log.WriteLine("Deactivated [" + tractorBeam.ItemId + "] tractor beam.");
                            tractorsProcessedThisTick++;
                            ESCache.Instance.Time.NextTractorBeamAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.SalvageDelayBetweenActions_milliseconds);
                            if (tractorsProcessedThisTick < ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle)
                            {
                                if (DebugConfig.DebugTractorBeams)
                                    Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Tractorbeam: Process Next Tractorbeam");
                                continue;
                            }

                            if (DebugConfig.DebugTractorBeams)
                                Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Tractorbeam: We have processed [" +
                                              ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle +
                                              "] tractors this tick, return");
                            return;
                        }

                        continue;
                    }

                    continue;
                }
            }

            if (tractorsProcessedThisTick > 0)
                return;

            //
            // Activate tractorbeams
            //
            var WreckNumber = 0;
            var list = wrecks.OrderByDescending(i => i.IsLootTarget).ThenByDescending(i => i.IsLargeWreck)
                .ThenByDescending(i => i.IsMediumWreck)
                .ThenByDescending(i => i.IsSmallWreck);

            if (DebugConfig.DebugTargetWrecks)
            {
                foreach (var e in list)
                {
                    Logging.Log.WriteLine($"[ActivateTractorbeams] Typename {e.TypeName} TypeId {e.TypeId} IsLargeWreck {e.IsLargeWreck} IsMediumWreck {e.IsMediumWreck} IsSmallWreck {e.IsSmallWreck}");
                }
            }

            foreach (var wreck in list)
            {
                WreckNumber++;
                // This velocity check solves some bugs where velocity showed up as 150000000m/s
                if ((int)wreck.Velocity != 0) //if the wreck is already moving assume we should not tractor it.
                {
                    if (DebugConfig.DebugTractorBeams)
                        Logging.Log.WriteLine("[" + WreckNumber + "] Wreck [" + wreck.Name + "][" + wreck.DirectEntity.Id.ToString() +
                                      "] is already moving: do not tractor a wreck that is moving");
                    continue;
                }

                // Is this wreck within range?
                if (wreck.Distance < (int)Distances.SafeScoopRange)
                    continue;

                if (!tractorBeams.Any()) return;

                if (tractorBeams.All(t => t.IsActive))
                    break;

                foreach (var tractorBeam in tractorBeams.Where(t => !t.IsActive))
                {

                    if (tractorBeam.IsInLimboState)
                    {
                        if (DebugConfig.DebugTractorBeams)
                            Logging.Log.WriteLine("[" + WreckNumber + "][::" + tractorBeam.ItemId +
                                                  "] __ Tractorbeam is: IsInLimboState [" + tractorBeam.IsInLimboState +
                                                  "] IsDeactivating [" + tractorBeam.IsDeactivating +
                                                  "] IsActivatable [" + tractorBeam.IsActivatable +
                                                  "] IsOnline [" +
                                                  tractorBeam.IsOnline + "] TargetId [" + tractorBeam.TargetId +
                                                  "]. Continue");
                        continue;
                    }

                    //
                    // this tractor has already been activated at least once
                    //
                    if (ESCache.Instance.Time.LastActivatedTimeStamp.ContainsKey(tractorBeam.ItemId))
                        if (ESCache.Instance.Time.LastActivatedTimeStamp[tractorBeam.ItemId].AddSeconds(2) > DateTime.UtcNow)
                            continue;

                    if (tractorBeams.Any(i => i.TargetId == wreck.Id && i.IsActive))
                        continue;

                    if (_activatedTractors.ContainsKey(wreck.Id) && _activatedTractors[wreck.Id].AddSeconds(4) > DateTime.UtcNow)
                    {
                        // continue if any other tractor was activated on this wreck in the past X seconds
                        continue;
                    }

                    foreach (var trac in tractorBeams)
                    {
                        Logging.Log.WriteLine($"TracId {trac.ItemId} TargetId {trac.TargetId} Active {trac.IsActive}");
                    }

                    Logging.Log.WriteLine("[" + tractorBeam.ItemId + "] Activating tractorbeam [" + tractorBeam.ItemId + "] on [" + wreck.Name +
                                          "][" +
                                          Math.Round(wreck.Distance / 1000, 0) + "k][" + wreck.DirectEntity.Id.ToString() + "] IsWreckEmpty [" + wreck.IsWreckEmpty + "]");

                    if (!_tractorTimes.ContainsKey(wreck.Id))
                    {
                        _tractorTimes.Add(wreck.Id, DateTime.UtcNow.AddMilliseconds(new Random().Next(600, 650)));
                        continue;
                    }
                    else
                    {
                        if (_tractorTimes[wreck.Id] > DateTime.UtcNow)
                            continue;
                    }

                    _tractorTimes.Remove(wreck.Id);

                    wreck.MakeActiveTarget(false);
                    if (tractorBeam.Click())
                    {
                        _activatedTractors[wreck.Id] = DateTime.UtcNow;
                        tractorsProcessedThisTick++;
                        ESCache.Instance.Time.NextTractorBeamAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.SalvageDelayBetweenActions_milliseconds);
                        break; //we do not need any more tractors on this wreck
                    }

                    continue;
                }

                if (tractorsProcessedThisTick > ESCache.Instance.EveAccount.CS.QMS.QS.NumberOfModulesToActivateInCycle)
                    return;

                //
                // move on to the next wreck
                //
                continue;
            }

            return;
        }

        /// <summary>
        ///     Loot any wrecks & cargo containers close by
        /// </summary>
        private void LootWrecks()
        {
            try
            {
                if (ESCache.Instance.Time.NextLootAction > DateTime.UtcNow)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Logging.Log.WriteLine("Debug: Cache.Instance.NextLootAction is still in the future, waiting");
                    return;
                }

                //
                // when full return to base and unloadloot
                //
                if (ESCache.Instance.EveAccount.CS.QMS.QS.UnloadLootAtStation && ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Capacity > 150 &&
                    ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity < 50)
                {
                    if (ESCache.Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("(mission) We are full, heading back to base to dump loot ");
                        //						_States.CurrentCombatHelperBehaviorState = States.CombatHelperBehaviorState.GotoBase;
                        //						_States.CurrentCombatMissionBehaviorState = States.CombatMissionsBehaviorState.GotoBase;
                        return;
                    }

                    Logging.Log.WriteLine("We are full: we are using a behavior that does not have a supported place to auto dump loot: error!");
                    return;
                }

                // Open a container in range
                var containersProcessedThisTick = 0;

                if (DebugConfig.DebugLootWrecks)
                {
                    var containersInRangeCount = 0;
                    if (ESCache.Instance.Containers.Any(i => i.Distance < (double)Distances.ScoopRange))
                        containersInRangeCount = ESCache.Instance.Containers.Count(i => i.Distance < (double)Distances.ScoopRange);

                    var containersOutOfRange = ESCache.Instance.Containers.Where(e => e.Distance >= (int)Distances.SafeScoopRange).ToList();
                    var containersOutOfRangeCount = 0;
                    if (containersOutOfRange.Any())
                        containersOutOfRangeCount = containersOutOfRange.Count;

                    Logging.Log.WriteLine("Debug: containersInRange count [" + containersInRangeCount + "]");
                    Logging.Log.WriteLine("Debug: containersOutOfRange count [" + containersOutOfRangeCount + "]");
                }

                if (ESCache.Instance.CurrentShipsCargo == null)
                {
                    if (DebugConfig.DebugLootWrecks)
                        Logging.Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                    return;
                }

                var shipsCargo = new List<DirectItem>();
                double freeCargoCapacity;
                if (ESCache.Instance.CurrentShipsCargo.Items.Any())
                {
                    shipsCargo = ESCache.Instance.CurrentShipsCargo.Items.ToList();
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                }
                else
                {
                    freeCargoCapacity = ESCache.Instance.CurrentShipsCargo.Capacity;
                    if (DebugConfig.DebugLootWrecks)
                        Logging.Log.WriteLine("if (!Cache.Instance.CurrentShipsCargo.Items.Any()) - really? 0 items in cargo?");
                }

                if (DebugConfig.DebugLootWrecks)
                    Logging.Log.WriteLine("FreeCargoCapacity [" + freeCargoCapacity + "]");

                foreach (
                    var containerEntity in
                    ESCache.Instance.Containers.Where(e => e.Distance <= (int)Distances.SafeScoopRange)
                        .OrderByDescending(i => i.IsLootTarget)
                        .ThenByDescending(i => i.IsLargeWreck)
                        .ThenByDescending(i => i.IsMediumWreck)
                        .ThenByDescending(i => i.IsSmallWreck))
                {
                    containersProcessedThisTick++;

                    // Empty wreck, ignore
                    if (containerEntity.IsWreckEmpty) //this only returns true if it is a wreck, not for cargo containers, spawn containers, etc.
                    {
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                        if (DebugConfig.DebugLootWrecks) Logging.Log.WriteLine("Ignoring Empty Wreck");
                        continue;
                    }

                    if (IgnoreUntilLoot.Any(a => a == containerEntity.TypeId) && !ESCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id))
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Logging.Log.WriteLine("Debug: IgnoreUntilLoot.Any(a => a == containerEntity.TypeId) && !QCache.Instance.ListofContainersToLoot.Contains(containerEntity.Id)");
                        continue;
                    }

                    // We looted this container
                    if (ESCache.Instance.LootedContainers.Contains(containerEntity.Id))
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("We have already looted [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Ignore open request within 10 seconds
                    if (OpenedContainers.ContainsKey(containerEntity.Id) && DateTime.UtcNow.Subtract(OpenedContainers[containerEntity.Id]).TotalSeconds < 10)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("We attempted to open [" + containerEntity.Id + "] less than 10 sec ago, ignoring");
                        continue;
                    }

                    // Don't even try to open a wreck if you are speed tanking and you are not processing a loot action
                    if (ESCache.Instance.NavigateOnGrid.SpeedTank && !ESCache.Instance.EveAccount.CS.QMS.QS.LootWhileSpeedTanking && OpenWrecks == false)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("SpeedTank is true and OpenWrecks is false [" + containerEntity.Id + "]");
                        continue;
                    }

                    // Don't even try to open a wreck if you are specified LootEverything as false and you are not processing a loot action
                    //      this is currently commented out as it would keep Golems and other non-speed tanked ships from looting the field as they cleared
                    //      missions, but NOT stick around after killing things to clear it ALL. Looteverything==false does NOT mean loot nothing
                    //if (Settings.Instance.LootEverything == false && Cache.Instance.OpenWrecks == false)
                    //    continue;

                    // Open the container
                    var cont = ESCache.Instance.DirectEve.GetContainer(containerEntity.Id);
                    if (cont == null)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("if (Cache.Instance.ContainerInSpace == null)");
                        continue;
                    }

                    if (cont.Window == null)
                    {
                        if (containerEntity.OpenCargo())
                        {
                            if (DebugConfig.DebugLootWrecks)
                                Logging.Log.WriteLine("if (containerEntity.OpenCargo())");
                            ESCache.Instance.Time.NextLootAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.LootingDelay_milliseconds);
                        }

                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("if (Cache.Instance.ContainerInSpace.Window == null)");

                        return;
                    }

                    if (!cont.Window.IsReady)
                    {
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is not ready");
                        return;
                    }

                    if (cont.Window.IsReady)
                    {

                        if (!ESCache.Instance.DirectEve.DWM.ActivateWindow(typeof(DirectContainerWindow)))
                            return;

                        Logging.Log.WriteLine("Opened container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                      containerEntity.DirectEntity.Id.ToString() + "]");
                        if (DebugConfig.DebugLootWrecks)
                            Logging.Log.WriteLine("LootWrecks: Cache.Instance.ContainerInSpace.Window is ready");
                        OpenedContainers[containerEntity.Id] = DateTime.UtcNow;
                        ESCache.Instance.Time.NextLootAction = DateTime.UtcNow.AddMilliseconds(ESCache.Instance.Time.LootingDelay_milliseconds);

                        // List its items
                        IEnumerable<DirectItem> items = cont.Items.ToList();
                        if (DebugConfig.DebugLootWrecks && items.Any())
                            Logging.Log.WriteLine("Found [" + items.Count() + "] items in [" + containerEntity.Name + "][" +
                                          Math.Round(containerEntity.Distance / 1000, 0) + "k][" +
                                          containerEntity.DirectEntity.Id.ToString() + "]");

                        // Build a list of items to loot
                        var lootItems = new List<DirectItem>();

                        if (items.Any())
                            foreach (var item in items.OrderByDescending(i => i.IsContraband).ThenByDescending(i => i.IskPerM3))
                            {
                                if (freeCargoCapacity < 1000)
                                    //this should allow BSs to not pickup large low value items but haulers and noctis' to scoop everything
                                    if (item.GroupId == (int)Group.CapacitorGroupCharge)
                                        continue;

                                // We pick up loot depending on isk per m3
                                var _isMissionItem = ESCache.Instance.MissionSettings.MissionItems.Contains((item.TypeName ?? string.Empty).ToLower());

                                // Never pick up contraband (unless its the mission item)
                                if (item.IsContraband) //is the mission item EVER contraband?!
                                {
                                    if (DebugConfig.DebugLootWrecks)
                                        Logging.Log.WriteLine("[" + item.TypeName + "] is not the mission item and is considered Contraband: ignore it");
                                    ESCache.Instance.LootedContainers.Add(containerEntity.Id);
                                    continue;
                                }

                                try
                                {
                                    // We are at our max, either make room or skip the item
                                    if (freeCargoCapacity - item.TotalVolume <= (ESCache.Instance.IsMissionItem(item) ? 0 : ESCache.Instance.EveAccount.CS.QMS.QS.ReserveCargoCapacity))
                                    {
                                        Logging.Log.WriteLine("We Need More m3: FreeCargoCapacity [" + freeCargoCapacity + "] - [" + item.TypeName + "][" +
                                                      item.TotalVolume +
                                                      "total][" + item.Volume + "each]");

                                        // Make a list of items which are worth less
                                        List<DirectItem> worthLess = null;
                                        if (_isMissionItem)
                                            worthLess = shipsCargo;
                                        else if (item.IskPerM3.HasValue)
                                            worthLess = shipsCargo.Where(sc => sc.IskPerM3.HasValue && sc.IskPerM3 < item.IskPerM3).ToList();
                                        else
                                            worthLess = shipsCargo.Where(sc => !ESCache.Instance.IsMissionItem(sc) && sc.IskPerM3.HasValue).ToList();

                                        if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                                        {
                                            // Remove mission item from this list
                                            worthLess.RemoveAll(wl => ESCache.Instance.MissionSettings.MissionItems.Contains((wl.TypeName ?? string.Empty).ToLower()));
                                            if (!string.IsNullOrEmpty(ESCache.Instance.MissionSettings.MoveMissionItems))
                                                worthLess.RemoveAll(wl => (wl.TypeName ?? string.Empty).ToLower() == ESCache.Instance.MissionSettings.MoveMissionItems.ToLower());

                                            // Consider dropping ammo if it concerns the mission item!
                                            if (!_isMissionItem)
                                                worthLess.RemoveAll(wl => ESCache.Instance.Combat.Ammo.Any(a => a.TypeId == wl.TypeId));
                                        }

                                        // Nothing is worth less then the current item
                                        if (!worthLess.Any())
                                        {
                                            if (DebugConfig.DebugLootWrecks)
                                                Logging.Log.WriteLine("[" + item.TypeName + "] ::: if (!worthLess.Any()) continue ");
                                            continue;
                                        }

                                        // Not enough space even if we dumped the crap
                                        if (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume) < item.TotalVolume)
                                        {
                                            if (ESCache.Instance.IsMissionItem(item))
                                                Logging.Log.WriteLine("Not enough space for [" + item.TypeName + "] Need [" + item.TotalVolume +
                                                              "] maximum available [" +
                                                              (freeCargoCapacity + worthLess.Sum(wl => wl.TotalVolume)) + "]");
                                            continue;
                                        }

                                        // Start clearing out items that are worth less
                                        var moveTheseItems = new List<DirectItem>();
                                        foreach (
                                            var wl in
                                            worthLess.OrderBy(wl => wl.IskPerM3.HasValue ? wl.IskPerM3.Value : double.MaxValue)
                                                .ThenByDescending(wl => wl.TotalVolume))
                                        {
                                            // Mark this item as moved
                                            moveTheseItems.Add(wl);

                                            // Subtract (now) free volume
                                            freeCargoCapacity += wl.TotalVolume;

                                            // We freed up enough space?
                                            if (freeCargoCapacity - item.TotalVolume >= ESCache.Instance.EveAccount.CS.QMS.QS.ReserveCargoCapacity)
                                                break;
                                        }

                                        if (moveTheseItems.Count > 0)
                                        {
                                            Logging.Log.WriteLine("We are full, not enough room for the mission item. Heading back to base to dump loot.");
                                            //GotoBase and dump loot in the hopes that we can grab what we need on the next run
                                            if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                                                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                                            return;
                                        }

                                        return;
                                    }

                                    // Update free space
                                    freeCargoCapacity -= item.TotalVolume;
                                    lootItems.Add(item);
                                    //if (Logging.DebugLootWrecks) Logging.Log("Salvage.LootWrecks", "We just added 1 more item to lootItems for a total of [" + lootItems.Count() + "] items we will loot from [" + containerID + "]", Logging.Teal);
                                }
                                catch (Exception exception)
                                {
                                    Logging.Log.WriteLine("We Need More m3: Exception [" + exception + "]");
                                }
                            }

                        // Mark container as looted
                        ESCache.Instance.LootedContainers.Add(containerEntity.Id);

                        // Loot actual items
                        if (lootItems.Count != 0)
                        {
                            Logging.Log.WriteLine("Looting container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) +
                                          "k][ID: " +
                                          containerEntity.DirectEntity.Id.ToString() + "], [" + lootItems.Count + "] valuable items");
                            if (DebugConfig.DebugLootWrecks)
                            {
                                var icount = 0;
                                if (lootItems != null && lootItems.Any())
                                    foreach (var lootItem in lootItems)
                                    {
                                        icount++;
                                        Logging.Log.WriteLine("[" + icount + "]LootItems Contains: [" + lootItem.TypeName + "] Quantity[" + lootItem.Quantity +
                                                      "k] isContraband [" + "] groupID [" + lootItem.GroupId + "] typeID [" + lootItem.TypeId +
                                                      "] isCommonMissionItem [" + "]");
                                        if (lootItem.GroupId == (int)Group.Drugs ||
                                            lootItem.GroupId == (int)Group.ToxicWaste ||
                                            lootItem.TypeId == (int)TypeID.Small_Arms ||
                                            lootItem.TypeId == (int)TypeID.Ectoplasm)
                                        {
                                            lootItems.Remove(lootItem);
                                            Logging.Log.WriteLine("[" + icount + "] Removed this from LootItems before looting [" + lootItem.TypeName +
                                                          "] Quantity[" +
                                                          lootItem.Quantity + "k] isContraband [" + "] groupID [" +
                                                          lootItem.GroupId +
                                                          "] typeID [" + lootItem.TypeId + "]" +
                                                          "]");
                                        }
                                    }
                            }

                            ESCache.Instance.CurrentShipsCargo.Add(lootItems);
                        }
                        else
                        {
                            Logging.Log.WriteLine("Container [" + containerEntity.Name + "][" + Math.Round(containerEntity.Distance / 1000, 0) + "k][ID: " +
                                          containerEntity.DirectEntity.Id.ToString() + "] contained no valuable items");
                        }

                        return;
                    }

                    if (DebugConfig.DebugLootWrecks)
                        Logging.Log.WriteLine("Reached End of LootWrecks Routine w/o finding a wreck to loot");

                    return;
                }
            }
            catch (Exception exception)
            {
                Logging.Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}