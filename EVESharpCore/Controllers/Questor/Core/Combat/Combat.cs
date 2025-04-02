extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using AmmoType = SC::SharedComponents.EVE.ClientSettings.AmmoType;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Fields

        public IEnumerable<EntityCache> highValueTargetsTargeted;
        public IEnumerable<EntityCache> lowValueTargetsTargeted;
        public long? PreferredPrimaryWeaponTargetID;
        private List<EntityCache> _aggressed;
        private List<EntityCache> _combatTargets;
        private bool _isJammed;
        private double? _maxrange;
        private List<EntityCache> _potentialCombatTargets;
        private EntityCache _preferredPrimaryWeaponTarget;
        private IEnumerable<EntityCache> _primaryWeaponPriorityEntities;
        private List<PriorityTarget> _primaryWeaponPriorityTargets;
        private List<PriorityTarget> _primaryWeaponPriorityTargetsPerFrameCaching;
        private List<EntityCache> _targetedBy;

        #endregion Fields

        #region Properties

        public IEnumerable<EntityCache> Aggressed
        {
            get { return _aggressed ?? (_aggressed = PotentialCombatTargets.Where(e => e.IsAttacking).ToList()); }
        }

        public List<AmmoType> Ammo => ESCache.Instance.EveAccount.CS.QMS.QS.AmmoTypes.ToList();
        public bool AnyProjectileWeapons => ESCache.Instance.DirectEve.Weapons.Any(m => m.GroupId == (int)Group.ProjectileWeapon);

        public bool AnyTurrets => ESCache.Instance.DirectEve.Weapons.Any(m => m.GroupId == (int)Group.ProjectileWeapon
                                                                                              || m.GroupId == (int)Group.EnergyWeapon
                                                                                              || m.GroupId == (int)Group.HybridWeapon);

        public bool AnyMissileLauncher => ESCache.Instance.DirectEve.Weapons.Any(m => 
                                                               m.GroupId == (int)Group.CruiseMissileLaunchers ||
                                                               m.GroupId == (int)Group.RocketLaunchers ||
                                                               m.GroupId == (int)Group.StandardMissileLaunchers ||
                                                               m.GroupId == (int)Group.TorpedoLaunchers ||
                                                               m.GroupId == (int)Group.AssaultMissileLaunchers ||
                                                               m.GroupId == (int)Group.LightMissileLaunchers ||
                                                               m.GroupId == (int)Group.DefenderMissileLaunchers ||
                                                               m.GroupId == (int)Group.CitadelCruiseLaunchers ||
                                                               m.GroupId == (int)Group.CitadelTorpLaunchers ||
                                                               m.GroupId == (int)Group.RapidHeavyMissileLaunchers ||
                                                               m.GroupId == (int)Group.RapidLightMissileLaunchers ||
                                                               m.GroupId == (int)Group.HeavyMissileLaunchers ||
                                                               m.GroupId == (int)Group.HeavyAssaultMissileLaunchers);


        public IEnumerable<EntityCache> CombatTargets
        {
            get
            {
             
                if (_combatTargets == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        if (_combatTargets == null)
                        {
                            var targets = new List<EntityCache>();
                            targets.AddRange(ESCache.Instance.Targets);
                            targets.AddRange(ESCache.Instance.Targeting);

                            _combatTargets = targets.Where(e => e.CategoryId == (int)CategoryID.Entity && e.Distance < (double)Distances.OnGridWithMe
                                                                && !e.IsIgnored
                                                                && (!e.IsSentry || e.IsSentry && false || e.IsSentry && e.IsEwarTarget)
                                                                && (e.IsNpc || e.IsNPCByBracketType)
                                                                && e.Distance < MaxRange
                                                                && !e.IsContainer
                                                                && !e.IsFactionWarfareNPC
                                                                && !e.IsEntityIShouldLeaveAlone
                                                                && !e.IsBadIdea
                                                                && !e.IsCelestial
                                                                && !(e.CategoryId == (int)CategoryID.Asteroid))
                                .ToList();

                            return _combatTargets;
                        }

                        return _combatTargets;
                    }

                    return ESCache.Instance.Targets.ToList();
                }

                return _combatTargets;
            }
        }

        public EntityCache CurrentWeaponTarget => ESCache.Instance.DirectEve.EntitiesById.TryGetValue(_currentTargetId ?? -1, out var ent) ? new EntityCache(ent) : null;

        public double DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons =>
            ESCache.Instance.EveAccount.CS.QMS.QS.DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons;

        public int DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage =>
            ESCache.Instance.EveAccount.CS.QMS.QS.DoNotSwitchTargetsIfTargetHasMoreThanThisArmorDamagePercentage;

        public double InsideThisRangeIsHardToTrack => ESCache.Instance.EveAccount.CS.QMS.QS.InsideThisRangeIsHardToTrack;

        public int MaxHighValueTargets => ESCache.Instance.EveAccount.CS.QMS.QS.MaximumHighValueTargets;
        public int MaximumTargetValueToConsiderTargetALowValueTarget => ESCache.Instance.EveAccount.CS.QMS.QS.MaximumTargetValueToConsiderTargetALowValueTarget;
        public int MaxLowValueTargets => ESCache.Instance.EveAccount.CS.QMS.QS.MaximumLowValueTargets;

        public double MaxRange => _maxrange ?? (_maxrange = Math.Min(ESCache.Instance.WeaponRange, ESCache.Instance.ActiveShip.MaxTargetRange)).Value;

        public int MinimumTargetValueToConsiderTargetAHighValueTarget => ESCache.Instance.EveAccount.CS.QMS.QS.MinimumTargetValueToConsiderTargetAHighValueTarget;
        public List<EntityCache> NotYetTargetingMeAndNotYetTargeted { get; set; }

        public IEnumerable<EntityCache> PotentialCombatTargets
        {
            get
            {
                if (_potentialCombatTargets == null)
                {
                    if (ESCache.Instance.InSpace)
                    {
                        _potentialCombatTargets = ESCache.Instance.EntitiesOnGrid.Where(e => e.CategoryId == (int)CategoryID.Entity
                                                                                            && !e.IsIgnored
                                                                                            &&
                                                                                            (!e.IsSentry ||
                                                                                             e.IsSentry && e.IsEwarTarget)
                                                                                            && (e.IsNPCByBracketType || e.IsAttacking)
                                                                                            && !e.IsContainer
                                                                                            && !e.IsFactionWarfareNPC
                                                                                            && !e.IsEntityIShouldLeaveAlone
                                                                                            && !e.IsBadIdea
                                                                                            && (!e.IsPlayer || e.IsPlayer && e.IsAttacking)
                                                                                            && !e.IsMiscJunk
                                                                                            && (!e.IsLargeCollidable || e.IsPrimaryWeaponPriorityTarget)
                            )
                            .ToList();

                        if (_potentialCombatTargets == null || !_potentialCombatTargets.Any())
                            _potentialCombatTargets = new List<EntityCache>();

                        return _potentialCombatTargets;
                    }

                    return new List<EntityCache>();
                }

                return _potentialCombatTargets;
            }
        }

        public EntityCache PreferredPrimaryWeaponTarget
        {
            get
            {
                if (_preferredPrimaryWeaponTarget == null)
                {
                    if (PreferredPrimaryWeaponTargetID != null)
                    {
                        _preferredPrimaryWeaponTarget = ESCache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.Id == PreferredPrimaryWeaponTargetID);

                        return _preferredPrimaryWeaponTarget ?? null;
                    }

                    return null;
                }

                return _preferredPrimaryWeaponTarget;
            }
            set
            {
                if (value == null)
                {
                    if (_preferredPrimaryWeaponTarget != null)
                    {
                        _preferredPrimaryWeaponTarget = null;
                        PreferredPrimaryWeaponTargetID = null;
                        if (DebugConfig.DebugPreferredPrimaryWeaponTarget)
                            Log.WriteLine("[ null ]");
                        return;
                    }
                }
                else if (_preferredPrimaryWeaponTarget != null && _preferredPrimaryWeaponTarget.Id != value.Id || _preferredPrimaryWeaponTarget == null)
                {
                    _preferredPrimaryWeaponTarget = value;
                    PreferredPrimaryWeaponTargetID = value.Id;
                    if (DebugConfig.DebugPreferredPrimaryWeaponTarget)
                        Log.WriteLine(value.Name + " [" + value.DirectEntity.Id.ToString() + "][" + Math.Round(value.Distance / 1000, 0) + "k] isTarget [" +
                                      value.IsTarget + "]");
                    return;
                }

                return;
            }
        }

        public IEnumerable<EntityCache> PrimaryWeaponPriorityEntities
        {
            get
            {
                try
                {
                    if (_primaryWeaponPriorityEntities == null)
                    {
                        if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any())
                        {
                            _primaryWeaponPriorityEntities =
                                PrimaryWeaponPriorityTargets.OrderByDescending(pt => pt.WeaponPriority)
                                    .ThenBy(pt => pt.Entity.Distance)
                                    .Select(pt => pt.Entity)
                                    .ToList();
                            return _primaryWeaponPriorityEntities;
                        }

                        if (DebugConfig.DebugAddPrimaryWeaponPriorityTarget)
                            Log.WriteLine("if (_primaryWeaponPriorityTargets.Any()) none available yet");
                        _primaryWeaponPriorityEntities = new List<EntityCache>();
                        return _primaryWeaponPriorityEntities;
                    }

                    return _primaryWeaponPriorityEntities;
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

        public List<PriorityTarget> PrimaryWeaponPriorityTargets
        {
            get
            {
                try
                {
                    if (_primaryWeaponPriorityTargetsPerFrameCaching == null)
                    {
                        if (_primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any())
                        {
                            foreach (var _primaryWeaponPriorityTarget in _primaryWeaponPriorityTargets)
                                if (ESCache.Instance.EntitiesOnGrid.All(e => e.Id != _primaryWeaponPriorityTarget.EntityID))
                                {
                                    Log.WriteLine("Remove Target that is no longer in the Entities list [" + _primaryWeaponPriorityTarget.Name + "]ID[" +
                                                  _primaryWeaponPriorityTarget.EntityID + "] PriorityLevel [" +
                                                  _primaryWeaponPriorityTarget.WeaponPriority + "]");
                                    _primaryWeaponPriorityTargets.Remove(_primaryWeaponPriorityTarget);
                                    break;
                                }

                            _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                            return _primaryWeaponPriorityTargets;
                        }

                        _primaryWeaponPriorityTargets = new List<PriorityTarget>();
                        _primaryWeaponPriorityTargetsPerFrameCaching = _primaryWeaponPriorityTargets;
                        return _primaryWeaponPriorityTargets;
                    }

                    return _primaryWeaponPriorityTargetsPerFrameCaching;
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

        public double SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons =>
            ESCache.Instance.EveAccount.CS.QMS.QS.SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons;

        public IEnumerable<EntityCache> TargetedBy
        {
            get { return _targetedBy ?? (_targetedBy = PotentialCombatTargets.Where(e => e.IsTargetedBy).ToList()); }
        }

        public List<EntityCache> TargetingMe { get; set; }

        private int MaxTotalTargets => MaxHighValueTargets + MaxLowValueTargets;

        #endregion Properties

        #region Methods

        public bool CheckForECMPriorityTargetsInOrder(EntityCache currentTarget, double distance)
        {
            try
            {
                return
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.WarpScrambler, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.Jamming, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.Webbing, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.TrackingDisrupting, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.Neutralizing, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.TargetPainting, distance) ||
                    SetPrimaryWeaponPriorityTarget(currentTarget, WeaponPriority.Dampening, distance);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public bool RemovePrimaryWeaponPriorityTargets(List<EntityCache> targets)
        {
            try
            {
                targets = targets.ToList();

                if (targets.Any() && _primaryWeaponPriorityTargets != null && _primaryWeaponPriorityTargets.Any() &&
                    _primaryWeaponPriorityTargets.Any(pt => targets.Any(t => t.Id == pt.EntityID)))
                {
                    _primaryWeaponPriorityTargets.RemoveAll(pt => targets.Any(t => t.Id == pt.EntityID));
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return false;
        }

        public bool SetPrimaryWeaponPriorityTarget(EntityCache currentTarget, WeaponPriority priorityType, double Distance)

        {
            try
            {
                EntityCache target = null;
                try
                {
                    if (PrimaryWeaponPriorityEntities.Any(pt => pt.WeaponPriority == priorityType))
                        target =
                            PrimaryWeaponPriorityEntities.Where(
                                    pt =>
                                        pt.IsReadyToShoot && currentTarget != null && pt.Id == currentTarget.Id &&
                                        pt.Distance < Distance && pt.IsActiveWeaponEwarType == priorityType && !pt.IsTooCloseTooFastTooSmallToHit
                                        ||
                                        pt.IsReadyToShoot && pt.Distance < Distance && pt.WeaponPriority == priorityType &&
                                        !pt.IsTooCloseTooFastTooSmallToHit)
                                .OrderByDescending(pt => pt.IsCurrentTarget)
                                .ThenByDescending(pt => pt.IsVunlnerableAgainstCurrentDamageType)
                                .ThenByDescending(pt => pt.IsReadyToShoot)
                                .ThenByDescending(pt => !pt.IsNPCFrigate)
                                .ThenByDescending(pt => pt.IsInOptimalRange)
                                .ThenBy(pt => pt.ShieldPct + pt.ArmorPct + pt.StructurePct)
                                .ThenBy(pt => pt.Distance)
                                .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

                if (target != null)
                {
                    Log.WriteLine("Combat.PreferredPrimaryWeaponTargetID = [ " + target.Name + "][" + target.DirectEntity.Id.ToString() + "]");
                    PreferredPrimaryWeaponTarget = target;
                    ESCache.Instance.Time.LastPreferredPrimaryWeaponTargetDateTime = DateTime.UtcNow;
                    return true;
                }

                return false;
            }
            catch (NullReferenceException)
            {
            }

            return false;
        }

        private bool UnlockHighValueTarget(string reason, bool OutOfRangeOnly = false)
        {
            EntityCache unlockThisHighValueTarget = null;
            var preferredId = PreferredPrimaryWeaponTarget != null ? PreferredPrimaryWeaponTarget.Id : -1;

            if (!OutOfRangeOnly)
            {
                if (lowValueTargetsTargeted.Count() > MaxLowValueTargets &&
                    MaxTotalTargets <= lowValueTargetsTargeted.Count() + highValueTargetsTargeted.Count())
                    return UnlockLowValueTarget(reason, OutOfRangeOnly);

                try
                {
                    if (highValueTargetsTargeted.Count(t => t.Id != preferredId) >= MaxHighValueTargets)
                        unlockThisHighValueTarget = highValueTargetsTargeted.Where(h => h.IsTarget && h.IsIgnored
                                                                                        ||
                                                                                        h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                        !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                        !h.IsPriorityWarpScrambler && !h.IsInOptimalRange &&
                                                                                        PotentialCombatTargets.Count() >= 3
                                                                                        ||
                                                                                        h.IsTarget && !h.IsPreferredPrimaryWeaponTarget &&
                                                                                        !h.IsDronePriorityTarget && h.IsHigherPriorityPresent &&
                                                                                        !h.IsPrimaryWeaponPriorityTarget &&
                                                                                        highValueTargetsTargeted.Count() == MaxHighValueTargets &&
                                                                                        !h.IsPriorityWarpScrambler)
                            .OrderByDescending(t => t.Distance > MaxRange)
                            .ThenByDescending(t => t.Distance)
                            .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            }
            else
            {
                try
                {
                    unlockThisHighValueTarget = highValueTargetsTargeted.Where(t => t.Distance > MaxRange)
                        .Where(h => h.IsTarget && h.IsIgnored && !h.IsPriorityWarpScrambler || h.IsTarget && !h.IsPreferredDroneTarget &&
                                    !h.IsDronePriorityTarget && !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                    !h.IsPriorityWarpScrambler)
                        .OrderByDescending(t => t.Distance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            }

            if (unlockThisHighValueTarget != null)
            {

                if (unlockThisHighValueTarget.IsWarpScramblingOrDisruptingMe)
                    return true;

                if (unlockThisHighValueTarget.IsNeutralizingMe)
                    return true;

                Log.WriteLine("Unlocking HighValue " + unlockThisHighValueTarget.Name + "[" + Math.Round(unlockThisHighValueTarget.Distance / 1000, 0) +
                              "k] myTargtingRange:[" + ESCache.Instance.ActiveShip.MaxTargetRange + "] myWeaponRange[:" + ESCache.Instance.WeaponRange + "] to make room for [" +
                              reason + "]");

                unlockThisHighValueTarget.UnlockTarget();
                return false;
            }

            return true;
        }

        private bool UnlockLowValueTarget(string reason, bool OutOfWeaponsRange = false)
        {
            EntityCache unlockThisLowValueTarget = null;
            if (!OutOfWeaponsRange)
                try
                {
                    unlockThisLowValueTarget = lowValueTargetsTargeted.Where(h => h.IsTarget && h.IsIgnored
                                                                                  ||
                                                                                  h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  !h.IsPriorityWarpScrambler && !h.IsInOptimalRange &&
                                                                                  PotentialCombatTargets.Count() >= 3
                                                                                  ||
                                                                                  h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  !h.IsPriorityWarpScrambler && lowValueTargetsTargeted.Count() ==
                                                                                  MaxLowValueTargets
                                                                                  ||
                                                                                  h.IsTarget && !h.IsPreferredDroneTarget && !h.IsDronePriorityTarget &&
                                                                                  !h.IsPreferredPrimaryWeaponTarget && !h.IsPrimaryWeaponPriorityTarget &&
                                                                                  h.IsHigherPriorityPresent && !h.IsPriorityWarpScrambler &&
                                                                                  lowValueTargetsTargeted.Count() == MaxLowValueTargets)
                        .OrderByDescending(t => t.Distance < (ESCache.Instance.Drones.UseDrones ? ESCache.Instance.Drones.MaxDroneRange : ESCache.Instance.WeaponRange))
                        .ThenByDescending(t => t.Distance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }
            else
                try
                {
                    unlockThisLowValueTarget = lowValueTargetsTargeted.Where(t => t.Distance > ESCache.Instance.ActiveShip.MaxTargetRange)
                        .Where(h => h.IsTarget && h.IsIgnored
                                    ||
                                    h.IsTarget &&
                                    !h.IsPreferredDroneTarget &&
                                    !h.IsDronePriorityTarget &&
                                    !h.IsPreferredPrimaryWeaponTarget &&
                                    !h.IsPrimaryWeaponPriorityTarget &&
                                    !h.IsPriorityWarpScrambler)
                        .OrderByDescending(t => t.Distance)
                        .ThenByDescending(t => t.Distance)
                        .FirstOrDefault();
                }
                catch (NullReferenceException)
                {
                }

            if (unlockThisLowValueTarget != null)
            {
                if (unlockThisLowValueTarget.IsWarpScramblingOrDisruptingMe)
                    return true;

                if (unlockThisLowValueTarget.IsNeutralizingMe)
                    return true;

                Log.WriteLine("Unlocking LowValue " + unlockThisLowValueTarget.Name + "[" + Math.Round(unlockThisLowValueTarget.Distance / 1000, 0) +
                              "k] myTargtingRange:[" +
                              ESCache.Instance.ActiveShip.MaxTargetRange + "] myWeaponRange[:" + ESCache.Instance.WeaponRange + "] to make room for [" + reason + "]");
                unlockThisLowValueTarget.UnlockTarget();
                return false;
            }
            return true;
        }

        #endregion Methods
    }
}