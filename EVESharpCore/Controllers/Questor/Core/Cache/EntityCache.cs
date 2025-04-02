extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using DamageType = SC::SharedComponents.EVE.ClientSettings.DamageType;

namespace EVESharpCore.Controllers.Questor.Core.Cache
{
    public class EntityCache
    {

        #region Fields
        private bool? _isEntityIShouldKeepShooting;
        private bool? _isEntityIShouldKeepShootingWithDrones;
        private bool? _isHigherPriorityPresent;

        #endregion Fields

        #region Constructors

        public EntityCache(DirectEntity entity)
        {
            DirectEntity = entity;
        }

        #endregion Constructors

        #region Properties

        public double ArmorHitPoints => DirectEntity.TotalArmor ?? 0;
        public double ArmorPct => DirectEntity.ArmorPct;
        public List<DamageType> BestDamageTypes => DirectEntity.BestDamageTypes;
        public BracketType BracketType => DirectEntity.BracketType;
        public int CategoryId => DirectEntity.CategoryId;
        public DirectEntity DirectEntity { get; }

        public double Distance => DirectEntity.Distance;

        public long FollowId => DirectEntity.FollowId;
        public double GetBounty => DirectEntity.GetBounty();

        public int GroupId => DirectEntity.GroupId;
        public bool HasExploded => DirectEntity.HasExploded;

        public bool HaveLootRights => GroupId == (int)Group.SpawnContainer ||
                                      ESCache.Instance.ActiveShip.Entity != null
                                      && (DirectEntity.CorpId == ESCache.Instance.ActiveShip.Entity.CorpId
                                          || DirectEntity.OwnerId == ESCache.Instance.ActiveShip.Entity.CharId);

        public long Id => DirectEntity.Id;

        public DronePriority IsActiveDroneEwarType
        {
            get
            {
                if (IsWarpScramblingOrDisruptingMe || DirectEntity.IsWarpDisruptingEntity || DirectEntity.IsWarpScramblingEntity)
                    return DronePriority.WarpScrambler;

                if (IsWebbingMe || DirectEntity.IsWebbingEntity)
                    return DronePriority.Webbing;

                if (IsNeutralizingMe || DirectEntity.IsNeutingEntity)
                    return DronePriority.KillTarget;

                if (IsTryingToJamMe || DirectEntity.IsJammingEntity)
                    return DronePriority.KillTarget;

                if (IsSensorDampeningMe || DirectEntity.IsSensorDampeningEntity)
                    return DronePriority.KillTarget;

                if (IsTargetPaintingMe || DirectEntity.IsTargetPaintingEntity)
                    return DronePriority.KillTarget;

                if (IsTrackingDisruptingMe || DirectEntity.IsTrackingDisruptingEntity || DirectEntity.IsGuidanceDisruptingEntity)
                    return DronePriority.KillTarget;

                return DronePriority.NoPriority;
            }
        }

        public WeaponPriority IsActiveWeaponEwarType
        {
            get
            {

                if (IsWarpScramblingOrDisruptingMe || DirectEntity.IsWarpScramblingEntity || DirectEntity.IsWarpDisruptingEntity)
                    return WeaponPriority.WarpScrambler;

                if (IsWebbingMe || DirectEntity.IsWebbingEntity)
                    return WeaponPriority.Webbing;

                if (IsNeutralizingMe || DirectEntity.IsNeutingEntity)
                    return WeaponPriority.Neutralizing;

                if (IsTryingToJamMe || DirectEntity.IsJammingEntity)
                    return WeaponPriority.Jamming;

                if (IsSensorDampeningMe || DirectEntity.IsSensorDampeningEntity)
                    return WeaponPriority.Dampening;

                if (IsTargetPaintingMe || DirectEntity.IsTargetPaintingEntity)
                    return WeaponPriority.TargetPainting;

                if (IsTrackingDisruptingMe || DirectEntity.IsTrackingDisruptingEntity || DirectEntity.IsGuidanceDisruptingEntity)
                    return WeaponPriority.TrackingDisrupting;

                return WeaponPriority.NoPriority;
            }
        }


        public WeaponPriority WeaponPriority =>
            ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Where(t => t.Entity.IsTarget && t.EntityID == Id)
                .FirstOrDefault()?.WeaponPriority ??
            WeaponPriority.NoPriority;

        public DronePriority DronePriority => ESCache.Instance.Drones.DronePriorityTargets
                                                  .FirstOrDefault(pt => pt.EntityID == DirectEntity.Id)?.DronePriority ?? DronePriority.NoPriority;

        public bool IsActiveTarget => DirectEntity.IsActiveTarget;
        public bool IsApproachedOrKeptAtRangeByActiveShip => DirectEntity.IsApproachedOrKeptAtRangeByActiveShip;

        public bool IsAttacking => DirectEntity.IsAttacking;
        public bool IsBadIdea => DirectEntity.IsBadIdea;
        public bool IsBattlecruiser => DirectEntity.IsBattlecruiser;
        public bool IsBattleship => DirectEntity.IsBattleship;
        public bool IsCelestial => DirectEntity.IsCelestial;
        public bool IsContainer => DirectEntity.IsContainer;

        public bool IsCorrectSizeForMyWeapons => ESCache.Instance.MyShipEntity.IsFrigate && IsFrigate
                                                 || ESCache.Instance.MyShipEntity.IsCruiser && IsCruiser
                                                 || ESCache.Instance.MyShipEntity.IsBattlecruiser && IsBattlecruiser
                                                 || ESCache.Instance.MyShipEntity.IsBattleship && IsBattleship;

        public bool IsCruiser => DirectEntity.IsCruiser;
        public bool IsCurrentTarget => ESCache.Instance.Combat.CurrentWeaponTarget == this;
        public bool IsDronePriorityTarget => ESCache.Instance.Drones.DronePriorityTargets.Any(i => i.EntityID == Id);

        public bool IsEntityIShouldKeepShooting => _isEntityIShouldKeepShooting
                                                   ?? ((_isEntityIShouldKeepShooting = IsReadyToShoot
                                                                                         && IsInOptimalRange
                                                                                         && !IsLargeCollidable
                                                                                         && (!IsFrigate && !IsNPCFrigate ||
                                                                                         !IsTooCloseTooFastTooSmallToHit)).Value);

        public bool IsEntityIShouldKeepShootingWithDrones => _isEntityIShouldKeepShootingWithDrones
                                                             ?? ((_isEntityIShouldKeepShootingWithDrones = IsReadyToShoot
                                                                                                              && IsInDroneRange
                                                                                                              && !IsLargeCollidable
                                                                                                              && (IsFrigate || IsNPCFrigate || ESCache.Instance.Drones.DronesKillHighValueTargets || IsWarpScramblingOrDisruptingMe)
                                                                                                              && ShieldPct * 100 < 80).Value);

        public bool IsEntityIShouldLeaveAlone => DirectEntity.IsEntityIShouldLeaveAlone;
        public bool IsEwarImmune => DirectEntity.IsEwarImmune;
        public bool IsEwarTarget => DirectEntity.IsEwarTarget;
        public bool IsFactionWarfareNPC => DirectEntity.IsFactionWarfareNPC;
        public bool IsFrigate => DirectEntity.IsFrigate;

        public bool IsHigherPriorityPresent
        {
            get
            {
                try
                {
                    if (DirectEntity != null && DirectEntity.IsValid)
                    {
                        if (_isHigherPriorityPresent == null)
                            if (ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any() || ESCache.Instance.Drones.DronePriorityTargets.Any())
                            {
                                if (ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any())
                                {
                                    if (ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id))
                                    {
                                        var _currentPrimaryWeaponPriority =
                                            ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Where(t => t.Id == DirectEntity.Id)
                                                .Select(pt => pt.WeaponPriority)
                                                .FirstOrDefault();

                                        if (
                                            !ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.All(
                                                pt => pt.WeaponPriority < _currentPrimaryWeaponPriority && pt.Distance < ESCache.Instance.Combat.MaxRange))
                                        {
                                            _isHigherPriorityPresent = true;
                                            return (bool)_isHigherPriorityPresent;
                                        }

                                        _isHigherPriorityPresent = false;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    if (ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Any(e => e.Distance < ESCache.Instance.Combat.MaxRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool)_isHigherPriorityPresent;
                                }

                                if (ESCache.Instance.Drones.DronePriorityTargets.Any())
                                {
                                    if (ESCache.Instance.Drones.DronePriorityTargets.Any(pt => pt.EntityID == DirectEntity.Id))
                                    {
                                        var _currentEntityDronePriority =
                                            ESCache.Instance.Drones.DronePriorityEntities.Where(t => t.Id == DirectEntity.Id)
                                                .Select(pt => pt.DronePriority)
                                                .FirstOrDefault();

                                        if (
                                            !ESCache.Instance.Drones.DronePriorityEntities.All(
                                                pt => pt.DronePriority < _currentEntityDronePriority && pt.Distance < ESCache.Instance.Drones.MaxDroneRange))
                                            return true;

                                        return false;
                                    }

                                    if (ESCache.Instance.Drones.DronePriorityEntities.Any(e => e.Distance < ESCache.Instance.Drones.MaxDroneRange))
                                    {
                                        _isHigherPriorityPresent = true;
                                        return (bool)_isHigherPriorityPresent;
                                    }

                                    _isHigherPriorityPresent = false;
                                    return (bool)_isHigherPriorityPresent;
                                }

                                _isHigherPriorityPresent = false;
                                return (bool)_isHigherPriorityPresent;
                            }

                        return _isHigherPriorityPresent ?? false;
                    }

                    return false;
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Exception [" + exception + "]");
                    return false;
                }
            }
        }

        public bool LockTarget(string module)
        {

            if (DateTime.UtcNow < ESCache.Instance.Time.NextTargetAction)
                return false;

            if (DirectEntity == null)
                return false;

            if (!DirectEntity.IsValid)
                return false;


            if (ESCache.Instance.Targets.Any(t => !t.IsContainer && !t.IsIgnored)
                && DirectEntity.BestDamageTypes.FirstOrDefault() != ESCache.Instance.MissionSettings.CurrentDamageType
                && !DirectEntity.IsEwarTarget
                && !DirectEntity.IsContainer
                && ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null
                && ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.DirectEntity != DirectEntity
                )
            {
                if (DirectEve.Interval(5000))
                {
                    Log.WriteLine($"Lock failed. Prefering targets of current damage type: {ESCache.Instance.MissionSettings._currentDamageType}");
                }
                return false;
            }

            if (IsTarget)
                return false;

            if (HasExploded)
                return false;

            if (Distance >= ESCache.Instance.ActiveShip.MaxTargetRange)
                return false;

            if (ESCache.Instance.Targets.Count() >= ESCache.Instance.MaxLockedTargets)
                return false;

            if (IsTargeting)
                return false;

            if (!ESCache.Instance.EntitiesOnGrid.Any(i => i.Id == Id))
                return false;

            if (IsBadIdea && !IsAttacking)
            {
                Log.WriteLine(
                    "[" + module + "] Attempted to target a player or concord entity! [" + Name + "] - aborting");
                return false;
            }

            if (DirectEntity.LockTarget())
            {
                Log.WriteLine("Locking Target ent.id [" + DirectEntity.Id + $"] from module [{module}]");
                if (!ESCache.Instance.Statistics.BountyValues.ContainsKey(DirectEntity.Id))
                {
                    var bounty = DirectEntity.GetBounty();
                    Log.WriteLine("Added bounty [" + bounty + "] ent.id [" + DirectEntity.Id + "]");
                    SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(ESCache.Instance.Statistics.BountyValues, DirectEntity.Id, bounty);
                }
                return true;
            }

            return false;
        }

        public bool IsHighValueTarget => !IsIgnored && !IsContainer && !IsBadIdea
                                         && !(GroupId == (int)Group.CustomsOffice) && !IsFactionWarfareNPC
                                         && !IsPlayer
                                         && TargetValue >= ESCache.Instance.Combat.MinimumTargetValueToConsiderTargetAHighValueTarget;

        public bool IsIgnored => ActionControl.IgnoreTargets != null && ActionControl.IgnoreTargets.Any() && ActionControl.IgnoreTargets.Contains(Name.Trim());


        public bool IsInDroneRange => ESCache.Instance.Drones.MaxDroneRange > 0 && Distance < ESCache.Instance.Drones.MaxDroneRange;

        public bool IsInOptimalRange
        {
            get
            {
                double optimal = ESCache.Instance.EveAccount.CS.QMS.QS.OptimalRange;
                if (optimal > ESCache.Instance.ActiveShip.MaxTargetRange)
                    optimal = ESCache.Instance.ActiveShip.MaxTargetRange - 500;

                return (ESCache.Instance.Combat.AnyTurrets && Distance > ESCache.Instance.Combat.InsideThisRangeIsHardToTrack
                                                           && Distance < optimal * (ESCache.Instance.Combat.AnyProjectileWeapons ? 10.0 : 1.5)
                                                           && Distance < ESCache.Instance.ActiveShip.MaxTargetRange)
                                                           || ESCache.Instance.Combat.AnyMissileLauncher
                                                            && Distance < ESCache.Instance.Combat.MaxRange;
            }
        }

        public bool IsInOptimalRangeOrNothingElseAvail => IsInOptimalRange || !ESCache.Instance.Targets.Where(t => !t.IsContainer).Any(i => i.Id != Id);
        public bool IsTryingToJamMe => DirectEntity.IsTryingToJamMe;
        public bool IsJammingMe => DirectEntity.IsJammingMe;
        public bool IsLargeCollidable => DirectEntity.IsLargeCollidable;
        public bool IsLargeWreck => DirectEntity.IsLargeWreck;
        public bool IsLastTargetDronesWereShooting => ESCache.Instance.Drones.LastTargetIDDronesEngaged != null && Id == ESCache.Instance.Drones.LastTargetIDDronesEngaged;
        public bool IsLootTarget => ESCache.Instance.ListofContainersToLoot.Contains(Id);

        public bool IsLowValueTarget => !IsIgnored && !IsContainer && !IsBadIdea && !(GroupId == (int)Group.CustomsOffice) && !IsFactionWarfareNPC && !IsPlayer
                                        && TargetValue <= ESCache.Instance.Combat.MaximumTargetValueToConsiderTargetALowValueTarget;


        public bool IsMediumWreck => DirectEntity.IsMediumWreck;
        public bool IsMiscJunk => DirectEntity.IsMiscJunk;
        public bool IsNeutralizingMe => DirectEntity.IsNeutralizingMe;

        public bool IsNotYetTargetingMeAndNotYetTargeted => (IsNpc || IsNPCByBracketType) && !IsTargeting && !IsTarget && !IsContainer &&
                                                            CategoryId == (int)CategoryID.Entity &&
                                                            Distance < ESCache.Instance.ActiveShip.MaxTargetRange
                                                            && !IsIgnored && !IsBadIdea && !IsTargetedBy &&
                                                            !IsEntityIShouldLeaveAlone &&
                                                            !IsFactionWarfareNPC && !IsLargeCollidable && !(GroupId == (int)Group.Station);


        public bool IsNpc => DirectEntity.IsNpc;
        public bool IsNPCBattlecruiser => DirectEntity.IsNPCBattlecruiser;
        public bool IsNPCBattleship => DirectEntity.IsNPCBattleship;
        public bool IsNPCByBracketType => DirectEntity.IsNPCByBracketType;
        public bool IsNPCCruiser => DirectEntity.IsNPCCruiser;
        public bool IsNPCFrigate => DirectEntity.IsNPCFrigate;
        public bool IsOrbitedByActiveShip => DirectEntity.IsOrbitedByActiveShip;

        public bool IsPlayer => DirectEntity.IsPlayer;
        public bool IsPreferredDroneTarget => ESCache.Instance.Drones.PreferredDroneTarget != null
                                              && ESCache.Instance.Drones.PreferredDroneTarget.Id == DirectEntity.Id;
        public bool IsPreferredPrimaryWeaponTarget => ESCache.Instance.Combat.PreferredPrimaryWeaponTarget != null
                                                      && ESCache.Instance.Combat.PreferredPrimaryWeaponTarget.Id == Id;
        public bool IsPrimaryWeaponPriorityTarget => ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any(i => i.EntityID == Id);

        public bool IsPriorityWarpScrambler => ESCache.Instance.Combat.PrimaryWeaponPriorityTargets.Any(pt => pt.EntityID == Id) &&
                                               WeaponPriority == WeaponPriority.WarpScrambler
                                               || ESCache.Instance.Drones.DronePriorityTargets.Any(pt => pt.EntityID == Id) &&
                                               DronePriority == DronePriority.WarpScrambler;

        public bool IsReadyToShoot => IsValid && !HasExploded && IsTarget && !IsIgnored && Distance < ESCache.Instance.Combat.MaxRange;
        public bool IsReadyToTarget => !HasExploded && !IsTarget && !IsTargeting && Distance < ESCache.Instance.ActiveShip.MaxTargetRange;
        public bool IsSensorDampeningMe => DirectEntity.IsSensorDampeningMe;
        public bool IsSentry => DirectEntity.IsSentry;
        public bool IsSmallWreck => DirectEntity.IsSmallWreck;
        public bool IsTarget => DirectEntity.IsTarget;
        public bool IsTargetedBy => DirectEntity.IsTargetedBy;
        public bool IsTargeting => DirectEntity.IsTargeting;
        public bool IsTargetingMeAndNotYetTargeted => (IsNpc || IsNPCByBracketType || IsAttacking)
                                                      && CategoryId == (int)CategoryID.Entity
                                                      && Distance < ESCache.Instance.ActiveShip.MaxTargetRange
                                                      && !IsLargeCollidable && !IsTargeting && !IsTarget
                                                      && IsTargetedBy && !IsContainer && !IsIgnored &&
                                                      (!IsBadIdea || IsAttacking) && !IsEntityIShouldLeaveAlone
                                                      && !IsFactionWarfareNPC && !(GroupId == (int)Group.Station);

        public bool IsTargetPaintingMe => DirectEntity.IsTargetPaintingMe;

        public bool IsTargetWeCanShootButHaveNotYetTargeted => IsValid
                                                               && CategoryId == (int)CategoryID.Entity
                                                               && !IsTarget
                                                               && !IsTargeting
                                                               && Distance < ESCache.Instance.ActiveShip.MaxTargetRange
                                                               && !IsIgnored
                                                               && !(GroupId == (int)Group.Station);

        public bool IsTooCloseTooFastTooSmallToHit => (IsNPCFrigate || IsFrigate)
                                                      && ESCache.Instance.Combat.AnyTurrets && ESCache.Instance.Drones.UseDrones
                                                      && Distance < ESCache.Instance.Combat.DistanceNPCFrigatesShouldBeIgnoredByPrimaryWeapons
                                                      && Velocity > ESCache.Instance.Combat.SpeedNPCFrigatesShouldBeIgnoredByPrimaryWeapons;

        public bool IsTrackingDisruptingMe => DirectEntity.IsTrackingDisruptingMe;
        public bool IsValid => DirectEntity.IsValid;
        public bool IsVunlnerableAgainstCurrentDamageType => BestDamageTypes.FirstOrDefault() == ESCache.Instance.MissionSettings._currentDamageType;
        public bool IsWarpScramblingOrDisruptingMe => DirectEntity.IsWarpScramblingOrDisruptingMe;
        public bool IsWarpScramblingMe => DirectEntity.IsWarpScramblingMe;
        public bool IsWarpWarpDisruptingMe => DirectEntity.IsWarpDisruptingMe;
        public bool IsWebbingMe => DirectEntity.IsWebbingMe;
        public bool IsWreck => GroupId == (int)Group.Wreck;
        public bool IsWreckEmpty => IsWreck && DirectEntity.IsEmpty;
        public int Mode => DirectEntity.Mode;
        public string Name => DirectEntity.Name;
        public double ShieldHitPoints => DirectEntity.TotalShield ?? 0;
        public double ShieldPct => DirectEntity.ShieldPct;
        public double StructureHitPoints => DirectEntity.TotalStructure ?? 0;
        public double StructurePct => DirectEntity.StructurePct;
        public int TargetValue => IsNPCBattleship ? 4 : IsNPCBattlecruiser ? 3 : IsNPCCruiser ? 2 : IsNPCFrigate ? 0 : -1;
        public int TypeId => DirectEntity.TypeId;
        public string TypeName => DirectEntity.TypeName;
        public double Velocity => DirectEntity.Velocity;

        public int OwnerID => DirectEntity.OwnerId;

        #endregion Properties

        #region Methods

        public bool Activate() => DirectEntity.Activate();

        public bool ActivateAbyssalEntranceAccelerationGate() => DirectEntity.ActivateAbyssalEntranceAccelerationGate();
        public bool AlignTo() => DirectEntity.AlignTo();
        public bool Approach() => DirectEntity.Approach();
        public bool Dock() => DirectEntity.Dock();
        public bool Jump() => DirectEntity.Jump();
        public bool KeepAtRange(int range) => DirectEntity.KeepAtRange(range);
        public void MakeActiveTarget(bool threaded = true) => DirectEntity.MakeActiveTarget(threaded);
        public bool MoveTo() => DirectEntity.MoveTo();
        public bool OpenCargo() => DirectEntity.OpenCargo();
        public bool Orbit(int orbitRange) => DirectEntity.Orbit(orbitRange);
        public bool UnlockTarget() => DirectEntity.UnlockTarget();
        public bool WarpTo(double range = 0) => DirectEntity.WarpTo(range);
        public bool WarpToAtRandomRange() => DirectEntity.WarpToAtRandomRange();

        #endregion Methods
    }
}