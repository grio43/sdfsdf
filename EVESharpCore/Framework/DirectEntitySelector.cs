using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using System;

namespace EVESharpCore.Framework
{
    public partial class DirectEntity
    {
        #region Properties

        public bool IsBadIdea
        {
            get
            {
                var result = false;
                result |= GroupId == (int)Group.ConcordDrone;
                result |= GroupId == (int)Group.PoliceDrone;
                result |= GroupId == (int)Group.CustomsOfficial;
                result |= GroupId == (int)Group.Billboard;
                result |= GroupId == (int)Group.Stargate;
                result |= GroupId == (int)Group.Station;
                result |= GroupId == (int)Group.SentryGun;
                result |= GroupId == (int)Group.Capsule;
                result |= GroupId == (int)Group.MissionContainer;
                result |= GroupId == (int)Group.CustomsOffice;
                result |= GroupId == (int)Group.GasCloud;
                result |= GroupId == (int)Group.ConcordBillboard;
                result |= IsFrigate;
                result |= IsCruiser;
                result |= IsBattlecruiser;
                result |= IsBattleship;
                result |= !IsAttacking && IsPlayer;
                result |= BracketType == BracketType.Navy_Concord_Customs;
                return result;
            }
        }

        public bool IsAbyssalMarshal => TypeId == 56176 || TypeId == 56177 || TypeId == 56178;

        //private bool IsTargetingDrones => IsYellowBoxing || HasTheFollowIdOfAnyOfOurActiveDrones || !IsTargetedBy;

        public bool IsTargetingDrones => IsYellowBoxing && !IsExplicitRemoteRepairEntity;
        // Deals no damage
        public bool IsExplicitRemoteRepairEntity => (IsRemoteArmorRepairingEntity || IsRemoteShieldRepairingEntity) && !GetAttributesInvType().ContainsKey("damageMultiplier");

        private static Random _rnd = new Random();

        private static int? _abyssEntCount = null;
        private static double? _abyssGJNeutOnGrid = null;

        private double? _abyssalTargetPriority;
        public double AbyssalTargetPriority
        {
            get
            {
                if (_abyssalTargetPriority.HasValue)
                    return _abyssalTargetPriority.Value;

                if (!_abyssEntCount.HasValue || DirectEve.Interval(2000))
                {
                    _abyssEntCount = DirectEve.Entities.Count(e => e.IsNPCByBracketType && e.GroupId != 2009);
                }

                if (!_abyssGJNeutOnGrid.HasValue || DirectEve.Interval(2000))
                {
                    _abyssGJNeutOnGrid = DirectEve.Entities.Where(e => e.IsNPCByBracketType && e.GroupId != 2009).Sum(e => e.GigaJouleNeutedPerSecond);
                }

                bool canTankNeuts = _abyssGJNeutOnGrid <= (ESCache.Instance?.EveAccount?.CS?.AbyssalMainSetting?.GigajoulePerSecExcess ?? 0);

                double r = 30;
                if (IsTargetPaintingMe || IsTargetPaintingEntity)
                    r = 25;
                if (IsJammingMe || IsJammingEntity)
                    r = 23;
                if (GroupId == 1997 || NPCHasVortonProjectorGuns) // https://everef.net/groups/1997 abyss drones OR vorton projectors
                    r = 22;
                if (GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate)) // prio nearby rogue drones
                    r = 21;
                if (GroupId == 1997 && IsNPCBattlecruiser) // prio rogue bcs
                    r = 20;
                if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("sparkgrip")) // high em damage rogue drone bcs
                    r = 19;
                if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("blastgrip")) // high explo damage rogue drone bcs
                    r = 17;
                if (IsSensorDampeningEntity || IsSensorDampeningMe)
                    r = 15;
                if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate))
                    r = 13;
                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    r = 12;
                if (TypeName.ToLower().Contains("kikimora"))
                    r = 11;
                if (TypeName.ToLower().Contains("striking") && TypeName.ToLower().Contains("kikimora"))
                    r = 10;
                if ((TypeName.ToLower().Contains("cynabal") || TypeName.ToLower().Contains("dramiel")) && TypeName.ToLower().Contains("elite"))
                    r = 9;
                if (IsWebbingEntity)
                    r = 7.5;
                if (NPCHasVortonProjectorGuns && Distance > 12000)
                    r = 6;
                if (IsKaren && _abyssEntCount <= 5) // prio the drifter bs if there are 5 ships left on grid, so the turrets can also do some work while the drones kill the drifter bs
                    r = 3.8;
                if (TypeName.ToLower().Contains("overmind"))
                    r = 3.7;
                if (IsWebbingMe)
                    r = 3.6;
                if (IsRemoteRepairEntity)
                    r = 3.5 + (0.01d / Math.Max(FlatShieldArmorRemotelRepairAmountCombined, 1.0d));
                if (IsRemoteRepairEntity && IsTargetingDrones)
                    r = 2.9;
                if (Name.ToLower().Contains("scylla tyrannos") && canTankNeuts)
                    r = 2.8;
                if (IsTargetingDrones)
                    r = 2.7;
                if ((IsTargetingDrones || _abyssEntCount <= 3) && TypeName.ToLower().Contains("kikimora"))
                    r = 2.6;
                if (IsNeutingEntity && !canTankNeuts)
                    r = 2.2;
                if (IsNeutralizingMe && !canTankNeuts)
                    r = 2.1;
                if (IsNeutingEntity && IsTargetingDrones)
                    r = 1.9;
                if ((TypeName.ToLower().Contains("cynabal") || TypeName.ToLower().Contains("dramiel")) && !TypeName.ToLower().Contains("elite") && canTankNeuts)
                    r = 1.8;
                if (Name.ToLower().Contains("scylla tyrannos") && canTankNeuts && IsTargetingDrones)
                    r = 1.7;
                if ((IsWarpScramblingEntity || IsWebbingEntity || IsTrackingDisruptingEntity) && IsTargetingDrones && canTankNeuts)
                    r = 1.6;
                if (NPCHasVortonProjectorGuns && IsTargetingDrones) // those just delete our drones, so let's make sure they are being prio'd
                    r = 1.5;
                if (IsAbyssalMarshal) // prio marshals over neuts due their oneshot potential
                    r = 1;
                if (GroupId == 2009) // Caches
                    r = 0;
                // override for frigates in speed clouds
                if (IsNPCFrigate && IsInSpeedCloud)
                    r = 30;

                _abyssalTargetPriority = r;
                return _abyssalTargetPriority.Value;
            }
        }

        public bool IsKaren => (TypeId == 56214 || TypeId == 47957 || TypeId == 47953 || TypeId == 47955 || TypeId == 47954 || TypeId == 47956);

        public bool IsBattlecruiser => BracketType == BracketType.Battlecruiser;

        public bool IsBattleship => BracketType == BracketType.Battleship;

        public bool IsCelestial => CategoryId == (int)CategoryID.Celestial
                                   || CategoryId == (int)CategoryID.Station
                                   || GroupId == (int)Group.Moon
                                   || GroupId == (int)Group.AsteroidBelt;

        public bool IsContainer => GroupId == (int)Group.Wreck
                                   || GroupId == (int)Group.CargoContainer
                                   || GroupId == (int)Group.SpawnContainer
                                   || GroupId == (int)Group.MissionContainer
                                   || GroupId == (int)Group.DeadSpaceOverseersBelongings;

        public bool IsCruiser => BracketType == BracketType.Cruiser;

        public bool IsDestroyer => BracketType == BracketType.Destroyer;

        public bool IsEntityIShouldLeaveAlone
        {
            get
            {
                var result = false;
                result |= GroupId == (int)Group.Merchant;
                result |= GroupId == (int)Group.Mission_Merchant;
                result |= GroupId == (int)Group.FactionWarfareNPC;
                result |= GroupId == (int)Group.Plagioclase;
                result |= GroupId == (int)Group.Spodumain;
                result |= GroupId == (int)Group.Kernite;
                result |= GroupId == (int)Group.Hedbergite;
                result |= GroupId == (int)Group.Arkonor;
                result |= GroupId == (int)Group.Bistot;
                result |= GroupId == (int)Group.Pyroxeres;
                result |= GroupId == (int)Group.Crokite;
                result |= GroupId == (int)Group.Jaspet;
                result |= GroupId == (int)Group.Omber;
                result |= GroupId == (int)Group.Scordite;
                result |= GroupId == (int)Group.Gneiss;
                result |= GroupId == (int)Group.Veldspar;
                result |= GroupId == (int)Group.Hemorphite;
                result |= GroupId == (int)Group.DarkOchre;
                result |= GroupId == (int)Group.Ice;
                return result;
            }
        }

        public bool IsEwarImmune => DirectEve.GetInvType(TypeId).TryGet<double>("disallowOffensiveModifiers") == 1;

        public bool IsEwarTarget
        {
            get
            {
                var result = false;
                result |= IsWarpScramblingMe;
                result |= IsWebbingMe;
                result |= IsNeutralizingMe;
                result |= IsTryingToJamMe;
                result |= IsJammingMe;
                result |= IsSensorDampeningMe;
                result |= IsTargetPaintingMe;
                result |= IsTrackingDisruptingMe;
                result |= IsNeutingEntity;
                result |= IsSensorDampeningEntity;
                result |= IsWarpScramblingEntity;
                result |= IsWarpDisruptingEntity;
                result |= IsTrackingDisruptingEntity;
                result |= IsGuidanceDisruptingEntity;
                result |= IsJammingEntity;
                result |= IsWebbingEntity;
                result |= IsTargetPaintingEntity;
                return result;
            }
        }

        public bool IsFactionWarfareNPC => GroupId == (int)Group.FactionWarfareNPC;

        public bool IsFrigate => BracketType == BracketType.Frigate;

        public bool IsLargeCollidable
        {
            get
            {
                var result = false;
                result |= GroupId == (int)Group.LargeColidableObject;
                result |= GroupId == (int)Group.LargeColidableShip;
                result |= GroupId == (int)Group.LargeColidableStructure;
                result |= GroupId == (int)Group.DeadSpaceOverseersStructure;
                result |= GroupId == (int)Group.DeadSpaceOverseersBelongings;
                return result;
            }
        }

        public bool IsLargeWreck => new[] { 26563, 26569, 26575, 26581, 26587, 26593, 26933, 26939, 27041, 27044, 27047, 27050, 27053, 27056, 27060, 30459 }.Any(x => x == TypeId);

        public bool IsMediumWreck => new[] { 26562, 26568, 26574, 26580, 26586, 26592, 26595, 26934, 26940, 27042, 27045, 27048, 27051, 27054, 27057, 27061, 34440 }.Any(x => x == TypeId);

        public bool IsMiscJunk
        {
            get
            {
                var result = false;
                result |= GroupId == (int)Group.PlayerDrone;
                result |= GroupId == (int)Group.Wreck;
                result |= GroupId == (int)Group.AccelerationGate;
                result |= GroupId == (int)Group.GasCloud;
                return result;
            }
        }

        public bool IsNPCBattlecruiser => BracketType == BracketType.NPC_Battlecruiser;

        public bool IsNPCBattleship => BracketType == BracketType.NPC_Battleship;

        private bool? _isNPCByBracketType;

        public bool IsNPCByBracketType => _isNPCByBracketType ??= BracketType == BracketType.NPC_Frigate
                                          || BracketType == BracketType.NPC_Destroyer
                                          || BracketType == BracketType.NPC_Cruiser
                                          || BracketType == BracketType.NPC_Drone
                                          || BracketType == BracketType.NPC_Rookie_Ship
                                          || BracketType == BracketType.NPC_Drone_EW
                                          || BracketType == BracketType.NPC_Battlecruiser
                                          || BracketType == BracketType.NPC_Battleship
                                          || BracketType == BracketType.NPC_Carrier
                                          || BracketType == BracketType.NPC_Dreadnought
                                          || BracketType == BracketType.NPC_Titan
                                          || BracketType == BracketType.NPC_Fighter
                                          || BracketType == BracketType.NPC_Fighter_Bomber
                                          || BracketType == BracketType.NPC_Shuttle
                                          || BracketType == BracketType.NPC_Industrial
                                          || BracketType == BracketType.NPC_Industrial_Command_Ship
                                          || BracketType == BracketType.NPC_Super_Carrier
                                          || BracketType == BracketType.NPC_Freighter
                                          || BracketType == BracketType.NPC_Mining_Barge
                                          || BracketType == BracketType.NPC_Mining_Frigate;

        public bool IsNPCCruiser => BracketType == BracketType.NPC_Cruiser;

        public bool IsNPCFrigate => BracketType == BracketType.NPC_Frigate || BracketType == BracketType.NPC_Destroyer
                                                                           || BracketType == BracketType.NPC_Drone
                                                                           || BracketType == BracketType.Drone_EW
                                                                           || BracketType == BracketType.NPC_Rookie_Ship;

        public bool IsSentry => BracketType == BracketType.Sentry_Gun;

        public bool IsSmallWreck => new[] { 26561, 26567, 26573, 26579, 26585, 26591, 26594, 26935, 26941, 27043, 27046, 27049, 27052, 27055, 27058, 27062 }.Any(x => x == TypeId);

        #endregion Properties
    }
}
