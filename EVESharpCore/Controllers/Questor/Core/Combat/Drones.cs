using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public class Drones
    {
        #region Fields

        public long? PreferredDroneTargetID;
        public bool WarpScrambled;
        private IEnumerable<EntityCache> _activeDrones;
        private double _activeDronesArmorPercentageOnLastPulse;
        private double _activeDronesArmorTotalOnLastPulse;
        private double _activeDronesShieldPercentageOnLastPulse;
        private double _activeDronesShieldTotalOnLastPulse;
        private double _activeDronesStructurePercentageOnLastPulse;
        private double _activeDronesStructureTotalOnLastPulse;
        private IEnumerable<EntityCache> _dronePriorityEntities;
        private List<PriorityTarget> _dronePriorityTargets;
        private int _lastDroneCount;
        private DateTime _lastLaunch;
        private DateTime _lastRecall;
        private DateTime _lastRecallCommand;

        private DateTime _launchTimeout;
        private int _launchTries;
        private double? _maxDroneRange;
        private DateTime _nextDroneAction = DateTime.UtcNow;
        private DateTime _nextWarpScrambledWarning = DateTime.MinValue;
        private EntityCache _preferredDroneTarget;
        private int _recallCount;
        private DateTime LastDroneFightCmd = DateTime.MinValue;

        #endregion Fields

        #region Properties

        public IEnumerable<EntityCache> ActiveDrones
        {
            get
            {
                if (_activeDrones == null)
                {
                    if (ESCache.Instance.DirectEve.ActiveDrones.Any())
                    {
                        _activeDrones = ESCache.Instance.DirectEve.ActiveDrones.Select(d => new EntityCache(d)).ToList();
                        return _activeDrones;
                    }

                    return new List<EntityCache>();
                }

                return _activeDrones;
            }
        }

        public bool AddDampenersToDronePriorityTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddDampenersToDronePriorityTargetList;
        public bool AddECMsToDroneTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddECMsToDroneTargetList;
        public bool AddNeutralizersToDronePriorityTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddNeutralizersToDronePriorityTargetList;
        public bool AddTargetPaintersToDronePriorityTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddTargetPaintersToDronePriorityTargetList;

        public bool AddTrackingDisruptorsToDronePriorityTargetList =>
            ESCache.Instance.EveAccount.CS.QMS.QS.AddTrackingDisruptorsToDronePriorityTargetList;

        public bool AddWarpScramblersToDronePriorityTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddWarpScramblersToDronePriorityTargetList;
        public bool AddWebifiersToDronePriorityTargetList => ESCache.Instance.EveAccount.CS.QMS.QS.AddWebifiersToDronePriorityTargetList;
        public int BuyAmmoDroneAmmount => ESCache.Instance.EveAccount.CS.QMS.BuyAmmoDroneAmount;
        public DirectContainer DroneBay => ESCache.Instance.DirectEve.GetShipsDroneBay();
        public int DroneControlRange => ESCache.Instance.EveAccount.CS.QMS.QS.DroneControlRange;
        public int DroneMinimumArmorPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneMinimumArmorPct;
        public int DroneMinimumCapacitorPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneMinimumCapacitorPct;
        public int DroneMinimumShieldPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneMinimumShieldPct;

        public IEnumerable<EntityCache> DronePriorityEntities
        {
            get
            {
                try
                {
                    if (_dronePriorityEntities == null)
                    {
                        if (DronePriorityTargets != null && DronePriorityTargets.Any())
                        {
                            _dronePriorityEntities =
                                DronePriorityTargets.OrderByDescending(pt => pt.DronePriority).ThenBy(pt => pt.Entity.Distance).Select(pt => pt.Entity);
                            return _dronePriorityEntities;
                        }

                        _dronePriorityEntities = new List<EntityCache>();
                        return _dronePriorityEntities;
                    }

                    return _dronePriorityEntities;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public List<PriorityTarget> DronePriorityTargets
        {
            get
            {
                try
                {
                    if (_dronePriorityTargets != null && _dronePriorityTargets.Any())
                    {
                        foreach (var dronePriorityTarget in _dronePriorityTargets)
                            if (ESCache.Instance.EntitiesOnGrid.All(i => i.Id != dronePriorityTarget.EntityID))
                            {
                                _dronePriorityTargets.Remove(dronePriorityTarget);
                                break;
                            }

                        return _dronePriorityTargets;
                    }

                    _dronePriorityTargets = new List<PriorityTarget>();
                    return _dronePriorityTargets;
                }
                catch (NullReferenceException)
                {
                    return null;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return null;
                }
            }
        }

        public int DroneRecallArmorPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneRecallArmorPct;
        public int DroneRecallCapacitorPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneRecallCapacitorPct;
        public int DroneRecallShieldPct => ESCache.Instance.EveAccount.CS.QMS.QS.DroneRecallShieldPct;
        public bool DronesKillHighValueTargets => ESCache.Instance.EveAccount.CS.QMS.QS.DronesKillHighValueTargets;
        public bool IsMissionPocketDone { get; set; }
        public long? LastTargetIDDronesEngaged { get; set; }
        public int LongRangeDroneRecallArmorPct => ESCache.Instance.EveAccount.CS.QMS.QS.LongRangeDroneRecallArmorPct;

        public int LongRangeDroneRecallCapacitorPct => ESCache.Instance.EveAccount.CS.QMS.QS.LongRangeDroneRecallCapacitorPct;

        public int LongRangeDroneRecallShieldPct => ESCache.Instance.EveAccount.CS.QMS.QS.LongRangeDroneRecallShieldPct;

        public double MaxDroneRange
        {
            get
            {
                if (_maxDroneRange == null)
                {
                    _maxDroneRange = Math.Min(DroneControlRange, ESCache.Instance.ActiveShip.MaxTargetRange);
                    return (double)_maxDroneRange;
                }

                return (double)_maxDroneRange;
            }
        }

        public EntityCache PreferredDroneTarget
        {
            get
            {
                if (_preferredDroneTarget == null)
                {
                    if (PreferredDroneTargetID != null)
                    {
                        if (ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == PreferredDroneTargetID))
                        {
                            _preferredDroneTarget = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => i.Id == PreferredDroneTargetID);
                            return _preferredDroneTarget;
                        }

                        return null;
                    }

                    return null;
                }

                return _preferredDroneTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredDroneTarget != null)
                    {
                        _preferredDroneTarget = null;
                        PreferredDroneTargetID = null;
                        Log.WriteLine("[ null ]");
                        return;
                    }
                }
                else
                {
                    if (_preferredDroneTarget != null && _preferredDroneTarget.Id != value.Id)
                    {
                        _preferredDroneTarget = value;
                        PreferredDroneTargetID = value.Id;
                        if (DebugConfig.DebugGetBestTarget)
                            Log.WriteLine(value + " [" + value.DirectEntity.Id.ToString() + "]");
                        return;
                    }
                }
            }
        }

        public bool UseDrones
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.ActiveShip.HasDroneBay)
                        return false;

                    if (ESCache.Instance.MissionSettings.PocketUseDrones != null)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("We are using PocketDrones setting [" + ESCache.Instance.MissionSettings.PocketUseDrones + "]");
                        return (bool)ESCache.Instance.MissionSettings.PocketUseDrones;
                    }

                    if (ESCache.Instance.MissionSettings.MissionUseDrones != null)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine("We are using MissionDrones setting [" + ESCache.Instance.MissionSettings.PocketUseDrones + "]");
                        return (bool)ESCache.Instance.MissionSettings.MissionUseDrones;
                    }

                    return ESCache.Instance.EveAccount.CS.QMS.QS.UseDrones;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        #endregion Properties

        #region Methods

        public void AddDronePriorityTarget(EntityCache ewarEntity, DronePriority priority, string module, bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (AddEwarTypeToPriorityTargetList && UseDrones)
                {
                    if (ewarEntity.IsIgnored || DronePriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                    {
                        if (DebugConfig.DebugAddDronePriorityTarget)
                            Log.WriteLine("if ((target.IsIgnored) || DronePriorityTargets.Any(p => p.Id == target.Id))");
                        return;
                    }

                    if (DronePriorityTargets.All(i => i.EntityID != ewarEntity.Id))
                    {
                        var DronePriorityTargetCount = 0;
                        if (DronePriorityTargets.Any())
                            DronePriorityTargetCount = DronePriorityTargets.Count();
                        Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + " m/s] Distance [" +
                                      Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.DirectEntity.Id.ToString() + "] as a drone priority target [" +
                                      priority.ToString() +
                                      "] we have [" + DronePriorityTargetCount + "] other DronePriorityTargets");
                        _dronePriorityTargets.Add(new PriorityTarget { Name = ewarEntity.Name, EntityID = ewarEntity.Id, DronePriority = priority });
                    }

                    return;
                }

                if (DebugConfig.DebugAddDronePriorityTarget)
                    Log.WriteLine("UseDrones is [" + UseDrones.ToString() + "] AddWarpScramblersToDronePriorityTargetList is [" +
                                  AddWarpScramblersToDronePriorityTargetList + "] [" + ewarEntity.Name +
                                  "] was not added as a Drone PriorityTarget (why did we even try?)");
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public EntityCache FindDronePriorityTarget(EntityCache currentTarget, DronePriority priorityType, bool AddECMTypeToDronePriorityTargetList,
            double Distance, bool FindAUnTargetedEntity = true)
        {
            if (AddECMTypeToDronePriorityTargetList)
            {
                try
                {
                    EntityCache target = null;
                    try
                    {
                        if (DronePriorityEntities.Any(pt => pt.DronePriority == priorityType))
                            target =
                                DronePriorityEntities.Where(
                                        pt =>
                                            (FindAUnTargetedEntity || pt.IsReadyToShoot) && currentTarget != null && pt.Id == currentTarget.Id &&
                                            pt.Distance < Distance && pt.IsActiveDroneEwarType == priorityType
                                            ||
                                            (FindAUnTargetedEntity || pt.IsReadyToShoot) && pt.Distance < Distance && pt.IsActiveDroneEwarType == priorityType)
                                    .OrderByDescending(pt => pt.IsNPCFrigate)
                                    .ThenByDescending(pt => pt.IsLastTargetDronesWereShooting)
                                    .ThenByDescending(pt => pt.IsInDroneRange)
                                    .ThenBy(pt => pt.IsEntityIShouldKeepShootingWithDrones)
                                    .ThenBy(pt => pt.ShieldPct + pt.ArmorPct + pt.StructurePct)
                                    .ThenBy(pt => pt.Distance)
                                    .FirstOrDefault();
                    }
                    catch (NullReferenceException)
                    {
                    }

                    if (target != null)
                    {
                        if (!FindAUnTargetedEntity)
                        {
                            PreferredDroneTarget = target;
                            ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                            return target;
                        }

                        return target;
                    }

                    return null;
                }
                catch (NullReferenceException)
                {
                }

                return null;
            }

            return null;
        }

        public bool GetBestDroneTarget(double distance, bool highValueFirst, string callingroutine, List<EntityCache> _potentialTargets = null)
        {
            if (!UseDrones)
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("!Cache.Instance.UseDrones - drones are disabled currently");
                return true;
            }

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Attempting to get Best Drone Target");

            if (DateTime.UtcNow < ESCache.Instance.Time.NextGetBestDroneTarget)
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("Cant GetBest yet....Too Soon!");
                return false;
            }

            ESCache.Instance.Time.NextGetBestDroneTarget = DateTime.UtcNow.AddMilliseconds(2000);
            EntityCache currentDroneTarget = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(i => (i.IsWarpScramblingOrDisruptingMe
                                                                                                  && i.IsLastTargetDronesWereShooting)
                                                                                                 || i.IsWarpScramblingOrDisruptingMe
                                                                                                 || i.IsLastTargetDronesWereShooting);

            if (DateTime.UtcNow < ESCache.Instance.Time.LastPreferredDroneTargetDateTime.AddSeconds(6) && PreferredDroneTarget != null &&
                ESCache.Instance.EntitiesOnGrid.Any(t => t.Id == PreferredDroneTarget.Id))
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("We have a PreferredDroneTarget [" + PreferredDroneTarget.Name +
                                  "] that was chosen less than 6 sec ago, and is still alive.");
                return true;
            }

            if (currentDroneTarget != null)
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("Checking Low Health");
                if (currentDroneTarget.IsEntityIShouldKeepShootingWithDrones)
                {
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("currentDroneTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) +
                                      "k][" +
                                      currentDroneTarget.DirectEntity.Id.ToString() + " GroupID [" + currentDroneTarget.GroupId +
                                      "]] has less than 80% shields, keep killing this target");
                    PreferredDroneTarget = currentDroneTarget;
                    ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }
            }

            if (currentDroneTarget != null && currentDroneTarget.IsReadyToShoot && (currentDroneTarget.IsLowValueTarget))
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("We have a currentTarget [" + currentDroneTarget.Name + "][" + currentDroneTarget.DirectEntity.Id.ToString() + "][" +
                                  Math.Round(currentDroneTarget.Distance / 1000, 2) + "k], testing conditions");

                #region Is our current target any other drone priority target?

                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("Checking Priority");
                if (DronePriorityEntities.Any(pt => pt.IsReadyToShoot
                                                    && pt.Distance < MaxDroneRange
                                                    && pt.Id == currentDroneTarget.Id
                                                    && !currentDroneTarget.IsHigherPriorityPresent))
                {
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("CurrentTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) + "k][" +
                                      currentDroneTarget.DirectEntity.Id.ToString() + "] GroupID [" + currentDroneTarget.GroupId + "]");
                    PreferredDroneTarget = currentDroneTarget;
                    ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target any other drone priority target?

                #region Is our current target already in armor? keep shooting the same target if so...

                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("Checking Low Health");
                if (currentDroneTarget.IsEntityIShouldKeepShootingWithDrones)
                {
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("currentDroneTarget [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) +
                                      "k][" +
                                      currentDroneTarget.DirectEntity.Id.ToString() + " GroupID [" + currentDroneTarget.GroupId +
                                      "]] has less than 80% shields, keep killing this target");
                    PreferredDroneTarget = currentDroneTarget;
                    ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                #endregion Is our current target already in armor? keep shooting the same target if so...

                #region If none of the above matches, does our current target meet the conditions of being hittable and in range

                if (!currentDroneTarget.IsHigherPriorityPresent)
                {
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("Does the currentTarget exist? Can it be hit?");
                    if (currentDroneTarget.IsReadyToShoot && currentDroneTarget.Distance < MaxDroneRange)
                    {
                        if (DebugConfig.DebugGetBestDroneTarget)
                            Log.WriteLine("if  the currentDroneTarget exists and the target is the right size then continue shooting it;");
                        if (DebugConfig.DebugGetBestDroneTarget)
                            Log.WriteLine("currentDroneTarget is [" + currentDroneTarget.Name + "][" + Math.Round(currentDroneTarget.Distance / 1000, 2) +
                                          "k][" +
                                          currentDroneTarget.DirectEntity.Id.ToString() + "] GroupID [" + currentDroneTarget.GroupId + "]");

                        PreferredDroneTarget = currentDroneTarget;
                        ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                        return true;
                    }
                }

                #endregion If none of the above matches, does our current target meet the conditions of being hittable and in range
            }

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.WarpScrambler, AddWarpScramblersToDronePriorityTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.Webbing, AddECMsToDroneTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.KillTarget, AddTrackingDisruptorsToDronePriorityTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.KillTarget, AddNeutralizersToDronePriorityTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.KillTarget, AddTargetPaintersToDronePriorityTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.KillTarget, AddDampenersToDronePriorityTargetList, distance) != null)
                return true;

            if (FindDronePriorityTarget(currentDroneTarget, DronePriority.KillTarget, AddWebifiersToDronePriorityTargetList, distance) != null)
                return true;

            #region Get the closest drone priority target

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Checking Closest DronePriorityTarget");
            EntityCache dronePriorityTarget = null;
            try
            {
                dronePriorityTarget = DronePriorityEntities.Where(p => p.Distance < MaxDroneRange
                                                                       && !p.IsIgnored
                                                                       && p.IsReadyToShoot)
                    .OrderBy(pt => pt.DronePriority)
                    .ThenByDescending(pt => pt.IsEwarTarget)
                    .ThenByDescending(pt => pt.IsTargetedBy)
                    .ThenBy(pt => pt.Distance)
                    .FirstOrDefault();
            }
            catch (NullReferenceException)
            {
            }

            if (dronePriorityTarget != null)
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("dronePriorityTarget is [" + dronePriorityTarget.Name + "][" + Math.Round(dronePriorityTarget.Distance / 1000, 2) +
                                  "k][" +
                                  dronePriorityTarget.DirectEntity.Id.ToString() + "] GroupID [" + dronePriorityTarget.GroupId + "]");
                PreferredDroneTarget = dronePriorityTarget;
                ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion Get the closest drone priority target

            #region did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Checking Calling Target");
            if (_potentialTargets != null && _potentialTargets.Any())
            {
                EntityCache callingDroneTarget = null;
                try
                {
                    callingDroneTarget = _potentialTargets.OrderBy(t => t.Distance).FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

                if (callingDroneTarget != null && callingDroneTarget.IsReadyToShoot)
                {
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("if (callingDroneTarget != null && !callingDroneTarget.IsIgnored)");
                    if (DebugConfig.DebugGetBestDroneTarget)
                        Log.WriteLine("callingDroneTarget is [" + callingDroneTarget.Name + "][" + Math.Round(callingDroneTarget.Distance / 1000, 2) +
                                      "k][" +
                                      callingDroneTarget.DirectEntity.Id.ToString() + "] GroupID [" + callingDroneTarget.GroupId + "]");
                    AddDronePriorityTarget(callingDroneTarget, DronePriority.KillTarget, " GetBestDroneTarget: callingDroneTarget");
                    PreferredDroneTarget = callingDroneTarget;
                    ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                    return true;
                }
            }

            #endregion did our calling routine (CombatMissionCtrl?) pass us targets to shoot?

            #region Get the closest Low Value Target

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Checking Closest Low Value");
            EntityCache lowValueTarget = null;

            if (ESCache.Instance.Combat.PotentialCombatTargets.Any())
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("get closest: if (potentialCombatTargets.Any())");

                lowValueTarget = ESCache.Instance.Combat.PotentialCombatTargets.Where(t => t.IsLowValueTarget && t.IsReadyToShoot)
                    .OrderBy(t => t.IsEwarTarget)
                    .ThenByDescending(t => t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenBy(t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct))
                    .ThenBy(t => t.Distance)
                    .FirstOrDefault();
            }

            #endregion Get the closest Low Value Target

            #region Get the closest high value target

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Checking closest Low Value");
            EntityCache highValueTarget = null;
            if (ESCache.Instance.Combat.PotentialCombatTargets.Any())
                highValueTarget = ESCache.Instance.Combat.PotentialCombatTargets.Where(t => t.IsHighValueTarget && t.IsReadyToShoot)
                    .OrderByDescending(t => !t.IsNPCFrigate)
                    .ThenByDescending(t => t.IsTargetedBy)
                    .ThenBy(t => (int)(t.ShieldPct + t.ArmorPct + t.StructurePct))
                    .ThenBy(t => t.Distance)
                    .FirstOrDefault();

            #endregion Get the closest high value target

            #region prefer to grab a lowvaluetarget, if none avail use a high value target

            if (lowValueTarget != null || highValueTarget != null)
            {
                if (DebugConfig.DebugGetBestDroneTarget)
                    Log.WriteLine("Checking use High Value");
                if (DebugConfig.DebugGetBestDroneTarget)
                    if (highValueTarget != null)
                        Log.WriteLine("highValueTarget is [" + highValueTarget.Name + "][" + Math.Round(highValueTarget.Distance / 1000, 2) + "k][" +
                                      highValueTarget.DirectEntity.Id.ToString() + "] GroupID [" + highValueTarget.GroupId + "]");
                    else
                        Log.WriteLine("highValueTarget is [ null ]");
                PreferredDroneTarget = lowValueTarget ?? highValueTarget ?? null;
                ESCache.Instance.Time.LastPreferredDroneTargetDateTime = DateTime.UtcNow;
                return true;
            }

            #endregion prefer to grab a lowvaluetarget, if none avail use a high value target

            if (DebugConfig.DebugGetBestDroneTarget)
                Log.WriteLine("Could not determine a suitable Drone target");

            #region If we did not find anything at all (wtf!?!?)

            if (DebugConfig.DebugGetBestDroneTarget)
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

                if (ESCache.Instance.Combat.PotentialCombatTargets.Any(t => !t.IsTarget && !t.IsTargeting))
                {
                    if (ActionControl.IgnoreTargets.Any())
                    {
                        var IgnoreCount = ActionControl.IgnoreTargets.Count;
                        Log.WriteLine("Ignore List has [" + IgnoreCount + "] Entities in it.");
                    }

                    Log.WriteLine("***** ALL [" + ESCache.Instance.Combat.PotentialCombatTargets.Count() +
                                  "] potentialCombatTargets LISTED BELOW (not yet targeted or targeting)");
                    var potentialCombatTargetNumber = 0;
                    foreach (var potentialCombatTarget in ESCache.Instance.Combat.PotentialCombatTargets)
                    {
                        potentialCombatTargetNumber++;
                        Log.WriteLine("***** Unlocked [" + potentialCombatTargetNumber + "]: [" + potentialCombatTarget.Name + "][" +
                                      Math.Round(potentialCombatTarget.Distance / 1000, 2) + "k][" + potentialCombatTarget.DirectEntity.Id.ToString() + "][isTarget: " +
                                      potentialCombatTarget.IsTarget + "] GroupID [" + potentialCombatTarget.GroupId + "]");
                    }

                    Log.WriteLine("***** ALL [" + ESCache.Instance.Combat.PotentialCombatTargets.Count() +
                                  "] potentialCombatTargets LISTED ABOVE (not yet targeted or targeting)");
                    Log.WriteLine(".");
                }
            }

            #endregion If we did not find anything at all (wtf!?!?)

            ESCache.Instance.Time.NextGetBestDroneTarget = DateTime.UtcNow;
            return false;
        }

        public void InvalidateCache()
        {
            try
            {
                _activeDrones = null;
                _dronePriorityEntities = null;
                _maxDroneRange = null;
                _preferredDroneTarget = null;
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
                if (!OnEveryDroneProcessState()) return;

                switch (ESCache.Instance.State.CurrentDroneState)
                {
                    case DroneState.WaitingForTargets:
                        if (!WaitingForTargetsDroneState()) return;
                        break;

                    case DroneState.Launch:
                        if (!LaunchDronesState()) return;
                        break;

                    case DroneState.Launching:
                        if (!LaunchingDronesState()) return;
                        break;

                    case DroneState.OutOfDrones:
                        if (!OutOfDronesDronesState()) return;
                        break;

                    case DroneState.Fighting:
                        if (!FightingDronesState()) return;
                        break;

                    case DroneState.Recalling:
                        if (!RecallingDronesState()) return;
                        break;

                    case DroneState.Idle:
                        if (!IdleDroneState()) return;
                        break;
                }

                _activeDronesShieldTotalOnLastPulse = GetActiveDroneShieldTotal();
                _activeDronesArmorTotalOnLastPulse = GetActiveDroneArmorTotal();
                _activeDronesStructureTotalOnLastPulse = GetActiveDroneStructureTotal();
                _activeDronesShieldPercentageOnLastPulse = GetActiveDroneShieldPercentage();
                _activeDronesArmorPercentageOnLastPulse = GetActiveDroneArmorPercentage();
                _activeDronesStructurePercentageOnLastPulse = GetActiveDroneStructurePercentage();
                _lastDroneCount = ActiveDrones.Count();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        public bool RemoveDronePriorityTargets(List<EntityCache> targets)
        {
            try
            {
                targets = targets.ToList();

                if (targets.Any() && _dronePriorityTargets != null && _dronePriorityTargets.Any() &&
                    _dronePriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _dronePriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private void EngageTarget()
        {
            try
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("Entering EngageTarget()");

                if (DebugConfig.DebugDrones)
                    Log.WriteLine("MaxDroneRange [" + MaxDroneRange + "] lowValueTargetTargeted [" + ESCache.Instance.Combat.lowValueTargetsTargeted.Count() +
                                  "] LVTT InDroneRange [" +
                                  ESCache.Instance.Combat.lowValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "] highValueTargetTargeted [" +
                                  ESCache.Instance.Combat.highValueTargetsTargeted.Count() + "] HVTT InDroneRange [" +
                                  ESCache.Instance.Combat.highValueTargetsTargeted.Count(i => i.Distance < MaxDroneRange) + "]");
                if (PreferredDroneTarget == null || !PreferredDroneTarget.IsFrigate)
                    GetBestDroneTarget(MaxDroneRange, !DronesKillHighValueTargets, "Drones");

                var droneTarget = PreferredDroneTarget;

                if (droneTarget == null)
                {
                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("PreferredDroneTarget is null, picking a target using a simple rule set...");
                    if (ESCache.Instance.Targets.Any(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxDroneRange))
                    {
                        droneTarget =
                            ESCache.Instance.Targets.Where(i => !i.IsContainer && !i.IsBadIdea && i.Distance < MaxDroneRange)
                                .OrderByDescending(i => i.IsWarpScramblingOrDisruptingMe)
                                .ThenByDescending(i => i.IsFrigate)
                                .ThenBy(i => i.Distance)
                                .FirstOrDefault();
                        if (droneTarget == null)
                            Log.WriteLine("DroneToShoot is Null, this is bad.");
                    }
                }

                if (droneTarget != null)
                {
                    if (droneTarget.IsReadyToShoot && droneTarget.Distance < MaxDroneRange)
                    {
                        if (DebugConfig.DebugDrones)
                            Log.WriteLine(
                                "if (DroneToShoot != null && DroneToShoot.IsReadyToShoot && DroneToShoot.Distance < Cache.Instance.MaxDroneRange)");

                        if (!droneTarget.IsTarget)
                        {
                            if (DebugConfig.DebugDrones) Log.WriteLine("if (!DroneToShoot.IsTarget)");
                            return;
                        }

                        if (droneTarget.IsBadIdea)
                        {
                            if (DebugConfig.DebugDrones)
                                Log.WriteLine("if (DroneToShoot.IsBadIdea && !DroneToShoot.IsAttacking) return;");
                            return;
                        }

                        if (LastTargetIDDronesEngaged != null)
                            if (LastTargetIDDronesEngaged == droneTarget.Id &&
                                ActiveDrones.All(i => i.FollowId == PreferredDroneTargetID && (i.Mode == 1 || i.Mode == 6 || i.Mode == 10)))
                            {
                                if (DebugConfig.DebugDrones)
                                    Log.WriteLine("if (LastDroneTargetID [" + LastTargetIDDronesEngaged + "] == DroneToShoot.Id [" + droneTarget.Id +
                                                  "] && Cache.Instance.ActiveDrones.Any(i => i.FollowId != Cache.Instance.PreferredDroneTargetID) [" +
                                                  ActiveDrones.Any(i => i.FollowId != PreferredDroneTargetID) + "])");
                                return;
                            }

                        if (droneTarget.IsValid && !ESCache.Instance.DirectEve.IsTargetBeingRemoved(droneTarget.Id) &&
                            ESCache.Instance.DirectEve.IsTargetStillValid(droneTarget.Id))
                            if (LastTargetIDDronesEngaged == null || LastTargetIDDronesEngaged != droneTarget.Id ||
                                LastDroneFightCmd.AddMinutes(5) < DateTime.UtcNow)
                                if (LastDroneFightCmd.AddSeconds(10) < DateTime.UtcNow)
                                {
                                    droneTarget.MakeActiveTarget(false);
                                    Log.WriteLine("Engaging [ " + ActiveDrones.Count() + " ] drones on [" + droneTarget.Name + "][ID: " +
                                                  droneTarget.DirectEntity.Id.ToString() + "]" +
                                                  Math.Round(droneTarget.Distance / 1000, 0) + "k away]");
                                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage);
                                    LastDroneFightCmd = DateTime.UtcNow;
                                    LastTargetIDDronesEngaged = droneTarget.Id;
                                }
                    }

                    if (DebugConfig.DebugDrones)
                        Log.WriteLine("if (DroneToShoot != null && DroneToShoot.IsReadyToShoot && DroneToShoot.Distance < Cache.Instance.MaxDroneRange)");
                    return;
                }

                if (DebugConfig.DebugDrones)
                    Log.WriteLine("if (Cache.Instance.PreferredDroneTargetID != null)");
                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private bool FightingDronesState()
        {
            if (DebugConfig.DebugDrones)
                Log.WriteLine("Should we recall our drones? This is a possible list of reasons why we should");

            if (!ActiveDrones.Any())
            {
                Log.WriteLine("Apparently we have lost all our drones");
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (ESCache.Instance.Combat.PotentialCombatTargets.Any(pt => pt.IsWarpScramblingOrDisruptingMe))
            {
                var warpScrambledBy = ESCache.Instance.Targets.OrderBy(d => d.Distance).ThenByDescending(i => i.IsWarpScramblingOrDisruptingMe).FirstOrDefault();
                if (warpScrambledBy != null && DateTime.UtcNow > _nextWarpScrambledWarning)
                {
                    _nextWarpScrambledWarning = DateTime.UtcNow.AddSeconds(20);
                    Log.WriteLine("We are scrambled by: [" + warpScrambledBy.Name + "][" +
                                  Math.Round(warpScrambledBy.Distance, 0) + "][" + warpScrambledBy.Id +
                                  "]");
                    WarpScrambled = true;
                }
            }
            else
            {
                WarpScrambled = false;
            }

            if (ShouldWeRecallDrones())
            {
                ESCache.Instance.Statistics.DroneRecalls++;
                ESCache.Instance.State.CurrentDroneState = DroneState.Recalling;
                return true;
            }

            if (DebugConfig.DebugDrones) Log.WriteLine("EngageTarget(); - before");

            EngageTarget();

            if (DebugConfig.DebugDrones) Log.WriteLine("EngageTarget(); - after");
            if (ActiveDrones.Count() < _lastDroneCount)
                ESCache.Instance.State.CurrentDroneState = DroneState.Launch;

            return true;
        }

        private double GetActiveDroneArmorPercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ArmorPct * 100);
        }

        private double GetActiveDroneArmorTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            if (ActiveDrones.Any(i => i.ArmorPct * 100 < 100))
                ESCache.Instance.Arm.NeedRepair = true;

            return ActiveDrones.Sum(d => d.ArmorHitPoints);
        }

        private double GetActiveDroneShieldPercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ShieldPct * 100);
        }

        private double GetActiveDroneShieldTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.ShieldHitPoints);
        }

        private double GetActiveDroneStructurePercentage()
        {
            if (!ActiveDrones.Any())
                return 0;

            return ActiveDrones.Sum(d => d.StructurePct * 100);
        }

        private double GetActiveDroneStructureTotal()
        {
            if (!ActiveDrones.Any())
                return 0;

            if (ActiveDrones.Any(i => i.StructurePct * 100 < 100))
                ESCache.Instance.Arm.NeedRepair = true;

            return ActiveDrones.Sum(d => d.StructureHitPoints);
        }

        private bool IdleDroneState()
        {
            if (ESCache.Instance.InSpace &&
                ESCache.Instance.ActiveShip.Entity != null &&
                !ESCache.Instance.ActiveShip.Entity.IsCloaked &&
                ESCache.Instance.ActiveShip.GivenName.ToLower().Equals(ESCache.Instance.EveAccount.CS.QMS.CombatShipName.ToLower()) &&
                UseDrones &&
                !ESCache.Instance.InWarp)
            {
                ESCache.Instance.State.CurrentDroneState = DroneState.WaitingForTargets;
                return true;
            }

            return false;
        }

        private bool LaunchDronesState()
        {
            if (DebugConfig.DebugDrones) Log.WriteLine("LaunchAllDrones");
            _launchTimeout = DateTime.UtcNow;
            ESCache.Instance.ActiveShip.LaunchDrones();
            ESCache.Instance.State.CurrentDroneState = DroneState.Launching;
            return true;
        }

        private bool LaunchingDronesState()
        {
            if (DebugConfig.DebugDrones) Log.WriteLine("Entering Launching State...");
            if (!ActiveDrones.Any())
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("No Drones in space yet. waiting");
                if (DateTime.UtcNow.Subtract(_launchTimeout).TotalSeconds > 10)
                {
                    if (_launchTries < 5)
                    {
                        _launchTries++;
                        ESCache.Instance.State.CurrentDroneState = DroneState.Launch;
                        return true;
                    }

                    ESCache.Instance.State.CurrentDroneState = DroneState.OutOfDrones;
                }

                return true;
            }

            if (_lastDroneCount == ActiveDrones.Count())
            {
                Log.WriteLine("[" + ActiveDrones.Count() + "] Drones Launched");
                ESCache.Instance.State.CurrentDroneState = DroneState.Fighting;
                return true;
            }

            return true;
        }

        private bool OnEveryDroneProcessState()
        {
            if (_nextDroneAction > DateTime.UtcNow)
                return false;

            if (ESCache.Instance.InWarp) return false;

            if (DebugConfig.DebugDrones) Log.WriteLine("Entering Drones.ProcessState");
            _nextDroneAction = DateTime.UtcNow.AddMilliseconds(1200);

            if (ESCache.Instance.InDockableLocation ||
                !ESCache.Instance.InSpace ||
                ESCache.Instance.MyShipEntity == null ||
                ESCache.Instance.ActiveShip.Entity.IsCloaked
            )
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("InStation [" + ESCache.Instance.InDockableLocation + "] InSpace [" + ESCache.Instance.InSpace + "] IsCloaked [" +
                                  ESCache.Instance.ActiveShip.Entity.IsCloaked + "] - doing nothing");
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!UseDrones && ActiveDrones.Any())
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("UseDrones [" + UseDrones + "]");
                if (!RecallingDronesState()) return false;
                return false;
            }

            if (ActiveDrones == null)
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("ActiveDrones == null");
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (ESCache.Instance.MyShipEntity.GroupId == (int)Group.Shuttle || ESCache.Instance.MyShipEntity.GroupId == (int)Group.Capsule)
            {
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!ActiveDrones.Any() && ESCache.Instance.InWarp)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("No Active Drones in space and we are InWarp - doing nothing");
                RemoveDronePriorityTargets(DronePriorityEntities.ToList());
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            if (!ESCache.Instance.EntitiesOnGrid.Any())
            {
                if (DebugConfig.DebugDrones) Log.WriteLine("Nothing to shoot on grid - doing nothing");
                RemoveDronePriorityTargets(DronePriorityEntities.ToList());
                ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                return false;
            }

            return true;
        }

        private bool OutOfDronesDronesState()
        {
            if (UseDrones &&
                ESCache.Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.ExecuteMission)
            {
                if (ESCache.Instance.Statistics.OutOfDronesCount >= 3)
                {
                    Log.WriteLine("We are Out of Drones! AGAIN - Headed back to base to stay!");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    ESCache.Instance.Statistics.MissionCompletionErrors = 10;
                    ESCache.Instance.Statistics.OutOfDronesCount++;
                }

                Log.WriteLine("We are Out of Drones! - Headed back to base to Re-Arm");
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                ESCache.Instance.Statistics.OutOfDronesCount++;
                return true;
            }

            return true;
        }

        private bool RecallingDronesState()
        {
            if (DateTime.UtcNow.Subtract(_lastRecallCommand).TotalSeconds > ESCache.Instance.Time.RecallDronesDelayBetweenRetries + ESCache.Instance.RandomNumber(0, 2))
            {
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                LastTargetIDDronesEngaged = null;
                _lastRecallCommand = DateTime.UtcNow;
                return true;
            }

            if (!ActiveDrones.Any())
            {
                _lastRecall = DateTime.UtcNow;
                _nextDroneAction = DateTime.UtcNow.AddSeconds(3);
                if (!UseDrones)
                {
                    ESCache.Instance.State.CurrentDroneState = DroneState.Idle;
                    return false;
                }

                ESCache.Instance.State.CurrentDroneState = DroneState.WaitingForTargets;
                return true;
            }

            return true;
        }

        private bool ShouldWeLaunchDrones()
        {

            if (ESCache.Instance.Combat.PotentialCombatTargets.Any(pt => pt.IsWarpScramblingOrDisruptingMe))
                return true;

            if (!UseDrones)
                return false;

            if (!ESCache.Instance.InSpace || ESCache.Instance.InDockableLocation)
                return false;

            if (ESCache.Instance.IsAgentMissionFinished()) return false;

            if (IsMissionPocketDone)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("IsMissionPocketDone [" + IsMissionPocketDone + "] Not Launching Drones");
                return false;
            }

            if (ESCache.Instance.ActiveShip.ShieldPercentage <= DroneMinimumShieldPct)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("My Ships ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] is below [" +
                                  DroneMinimumShieldPct +
                                  "] Not Launching Drones");
                return false;
            }

            if (ESCache.Instance.ActiveShip.ArmorPercentage <= DroneMinimumArmorPct)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("My Ships ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] is below [" + DroneMinimumArmorPct +
                                  "] Not Launching Drones");
                return false;
            }

            if (ESCache.Instance.ActiveShip.CapacitorPercentage <= DroneMinimumCapacitorPct)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("My Ships CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] is below [" +
                                  DroneMinimumCapacitorPct +
                                  "] Not Launching Drones");
                return false;
            }

            if (ESCache.Instance.Targets.Count(e => (!e.IsSentry || e.IsSentry && e.IsEwarTarget)
                                                    && !e.IsWreck && !e.IsContainer && e.IsInDroneRange) == 0)
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("No targets in range for drones. MaxDroneRange [" + MaxDroneRange + "] DroneControlrange [" + DroneControlRange +
                                  "] TargetingRange [" +
                                  ESCache.Instance.ActiveShip.MaxTargetRange + "]");
                return false;
            }

            if (ESCache.Instance.State.CurrentQuestorState != QuestorState.CombatMissionsBehavior
                    && !ESCache.Instance.EntitiesOnGrid.Any(e =>
                            (!e.IsSentry && !e.IsBadIdea && e.CategoryId == (int)CategoryID.Entity && e.IsNpc && !e.IsContainer && !e.IsLargeCollidable ||
                             e.IsAttacking) && e.Distance < MaxDroneRange))
            {
                if (DebugConfig.DebugDrones)
                    Log.WriteLine("QuestorState is [" + ESCache.Instance.State.CurrentQuestorState.ToString() + "] We have nothing to shoot;");
                return false;
            }

            if (_lastLaunch < _lastRecall && _lastRecall.Subtract(_lastLaunch).TotalSeconds < 30)
            {
                if (_lastRecall.AddSeconds(5 * _recallCount + 5) < DateTime.UtcNow)
                {
                    _recallCount++;

                    if (_recallCount > 5)
                        _recallCount = 5;

                    return true;
                }

                if (DebugConfig.DebugDrones)
                    Log.WriteLine("We are still in _lastRecall delay.");
                return false;
            }

            _recallCount = 0;
            return true;

        }

        private bool ShouldWeRecallDrones()
        {
            try
            {
                var lowShieldWarning = LongRangeDroneRecallShieldPct;
                var lowArmorWarning = LongRangeDroneRecallArmorPct;
                var lowCapWarning = LongRangeDroneRecallCapacitorPct;

                if (ActiveDrones.Average(d => d.Distance) < MaxDroneRange / 2d)
                {
                    lowShieldWarning = DroneRecallShieldPct;
                    lowArmorWarning = DroneRecallArmorPct;
                    lowCapWarning = DroneRecallCapacitorPct;
                }

                if (!UseDrones)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: UseDrones is [" + UseDrones + "]");
                    return true;
                }

                if (ESCache.Instance.IsAgentMissionFinished()) return true;

                var targetedByInDroneRangeCount =
                    ESCache.Instance.Combat.TargetedBy.Count(e => (!e.IsSentry || e.IsSentry && e.IsEwarTarget) && e.IsInDroneRange);
                if (targetedByInDroneRangeCount == 0 && !WarpScrambled)
                {
                    var TargtedByCount = 0;
                    if (ESCache.Instance.Combat.TargetedBy != null && ESCache.Instance.Combat.TargetedBy.Any())
                    {
                        TargtedByCount = ESCache.Instance.Combat.TargetedBy.Count();
                        var __closestTargetedBy =
                            ESCache.Instance.Combat.TargetedBy.OrderBy(i => i.Distance)
                                .FirstOrDefault(e => !e.IsSentry || e.IsSentry && false || e.IsSentry && e.IsEwarTarget);
                        if (__closestTargetedBy != null)
                            Log.WriteLine("The closest target that is targeting ME is at [" + __closestTargetedBy.Distance + "]k");
                    }

                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: There are [" +
                                  ESCache.Instance.Combat.PotentialCombatTargets.Count(
                                      e => e.IsInDroneRange && (!e.IsSentry || e.IsSentry && false || e.IsSentry && e.IsEwarTarget)) +
                                  "] PotentialCombatTargets not targeting us within My MaxDroneRange: [" + Math.Round(MaxDroneRange / 1000, 0) +
                                  "k] Targeting Range Is [" +
                                  Math.Round(ESCache.Instance.ActiveShip.MaxTargetRange / 1000, 0) + "k] We have [" + TargtedByCount + "] total things targeting us and [" +
                                  ESCache.Instance.Combat.PotentialCombatTargets.Count(
                                      e => !e.IsSentry || e.IsSentry && false || e.IsSentry && e.IsEwarTarget) +
                                  "] total PotentialCombatTargets");

                    if (DebugConfig.DebugDrones)
                        foreach (var PCTInDroneRange in ESCache.Instance.Combat.PotentialCombatTargets.Where(i => i.IsInDroneRange && i.IsTargetedBy))
                            Log.WriteLine("Recalling Drones Details:  PCTInDroneRange [" + PCTInDroneRange.Name + "][" + PCTInDroneRange.DirectEntity.Id.ToString() +
                                          "] at [" +
                                          Math.Round(PCTInDroneRange.Distance / 1000, 2) + "] not targeting us yet");

                    return true;
                }

                if (IsMissionPocketDone && !WarpScrambled)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: We are done with this pocket.");
                    return true;
                }

                if (_activeDronesShieldTotalOnLastPulse > GetActiveDroneShieldTotal() + 5)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: shields! [Old: " +
                                  _activeDronesShieldTotalOnLastPulse.ToString("N2") + "][New: " +
                                  GetActiveDroneShieldTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorTotalOnLastPulse > GetActiveDroneArmorTotal() + 5)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: armor! [Old:" + _activeDronesArmorTotalOnLastPulse.ToString("N2") +
                                  "][New: " +
                                  GetActiveDroneArmorTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesStructureTotalOnLastPulse > GetActiveDroneStructureTotal() + 5)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: structure! [Old:" +
                                  _activeDronesStructureTotalOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneStructureTotal().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesShieldPercentageOnLastPulse > GetActiveDroneShieldPercentage() + 1)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: shields! [Old: " +
                                  _activeDronesShieldPercentageOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneShieldPercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesArmorPercentageOnLastPulse > GetActiveDroneArmorPercentage() + 1)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: armor! [Old:" +
                                  _activeDronesArmorPercentageOnLastPulse.ToString("N2") + "][New: " +
                                  GetActiveDroneArmorPercentage().ToString("N2") + "]");
                    return true;
                }

                if (_activeDronesStructurePercentageOnLastPulse > GetActiveDroneStructurePercentage() + 1)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: structure! [Old:" +
                                  _activeDronesStructurePercentageOnLastPulse.ToString("N2") +
                                  "][New: " + GetActiveDroneStructurePercentage().ToString("N2") + "]");
                    return true;
                }

                if (ActiveDrones.Count() < _lastDroneCount)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones: We lost a drone! [Old:" + _lastDroneCount + "][New: " +
                                  ActiveDrones.Count() + "]");
                    return true;
                }

                if (ESCache.Instance.Combat.PotentialCombatTargets.Any() && !ESCache.Instance.Combat.PotentialCombatTargets.Any(i => i.IsTargeting || i.IsTarget))
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to [" + ESCache.Instance.Targets.Count() +
                                  "] targets being locked. Locking [" +
                                  ESCache.Instance.Targeting.Count() + "] targets atm");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.ShieldPercentage < lowShieldWarning && !WarpScrambled)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to shield [" +
                                  Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) +
                                  "%] below [" + lowShieldWarning + "%] minimum");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.ArmorPercentage < lowArmorWarning && !WarpScrambled)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to armor [" +
                                  Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) +
                                  "%] below [" + lowArmorWarning + "%] minimum");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < lowCapWarning && !WarpScrambled)
                {
                    Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to capacitor [" +
                                  Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) +
                                  "%] below [" + lowCapWarning + "%] minimum");
                    return true;
                }

                if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior && !WarpScrambled)
                {
                    if (ESCache.Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoBase && !WarpScrambled)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to gotobase state");
                        return true;
                    }

                    if (ESCache.Instance.State.CurrentCombatMissionBehaviorState == CombatMissionsBehaviorState.GotoMission && !WarpScrambled)
                    {
                        Log.WriteLine("Recalling [ " + ActiveDrones.Count() + " ] drones due to gotomission state");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool WaitingForTargetsDroneState()
        {
            if (ActiveDrones.Any())
            {
                ESCache.Instance.State.CurrentDroneState = DroneState.Fighting;
                return true;
            }

            if (ESCache.Instance.Targets.Any())
            {
                if (!ShouldWeLaunchDrones()) return false;

                _launchTries = 0;
                _lastLaunch = DateTime.UtcNow;
                ESCache.Instance.State.CurrentDroneState = DroneState.Launch;
                return true;
            }

            return true;
        }

        #endregion Methods
    }
}