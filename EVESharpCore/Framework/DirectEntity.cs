extern alias SC;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Events;
using SC::SharedComponents.Py;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;
using EVESharpCore.Logging;
using SC::SharedComponents.FastPriorityQueue;
using SC::SharedComponents.Utility;
using ServiceStack.Text;
using SharpDX.DXGI;
using EVESharpCore.Cache;
using System.Runtime;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.Events;

namespace EVESharpCore.Framework
{
    extern alias SC;


    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Numerics;
    using static EVESharpCore.Framework.DirectSceneManager;
    using static System.Net.Mime.MediaTypeNames;

    public partial class DirectEntity : DirectInvType
    {
        #region Fields

        private int? _allianceId;
        private double? _angularVelocity;
        private double? _armorPct;
        private PyObject _ball;
        private PyObject _ballpark;
        private List<DamageType> _bestDamageTypes;
        private int? _charId;
        private int? _corpId;
        private double? _currentrmor;
        private double? _currentShield;
        private double? _currentStructure;
        private double? _distance;
        private double? _emEHP;
        private double? _expEHP;
        private long? _followId;
        private string _givenName;
        private bool? _hasExploded;
        private bool? _hasReleased;
        private bool? _isCloaked;
        private bool? _isEmpty;
        private double? _kinEHP;
        private int? _mode;
        private string _name;
        private double? _npcRemoteArmorRepairChance;
        private int? _ownerId;
        private double? _shieldPct;
        private PyObject _slimItem;
        private double? _structurePct;
        private double? _transversalVelocity;
        private double? _trmEHP;
        private double? _velocity;
        private double? _vx;
        private double? _vy;
        private double? _vz;
        private double? _wormholeAge;
        private double? _wormholeSize;
        private double? _x;
        private double? _y;
        private double? _z;
        private double? _gotoX;
        private double? _gotoY;
        private double? _gotoZ;
        private double? _estimatedPixelDiameterWithChildren;
        private Vec3? _ballPos;
        private string _dna;
        public PyObject GetSlimItem => _slimItem;
        private PyObject? _pyDynamicItem;
        private Dictionary<DirectEntityFlag, bool> _entityFlags;

        #endregion Fields

        #region Constructors

        internal DirectEntity(DirectEve directEve, PyObject ballpark, PyObject ball, PyObject slimItem, long id)
            : base(directEve)
        {
            _ballpark = ballpark;
            _ball = ball;
            _slimItem = slimItem;
            _entityFlags = new Dictionary<DirectEntityFlag, bool>();

            Id = id;
            TypeId = (int)slimItem.Attribute("typeID");

            Attacks = new List<string>();
            ElectronicWarfare = new List<string>();
        }

        #endregion Constructors

        #region Properties
        /// <summary>
        /// This seems to lack a bit of performance, do we need to do this for every entity on every frame?
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool HasFlagSet(DirectEntityFlag flag)
        {
            if (_entityFlags.ContainsKey(flag) && _entityFlags[flag])
                return true;

            var svc = DirectEve.GetLocalSvc("stateSvc");
            if (svc.IsValid)
            {
                var states = svc["states"];
                if (states.IsValid)
                {
                    var dict = states.ToDictionary<int>();
                    if (dict.TryGetValue((int)flag, out var pyObj))
                    {
                        var innerDict = pyObj.ToDictionary<long>();
                        if (innerDict.TryGetValue(this.Id, out var innerPyObj))
                        {
                            if (innerPyObj.ToBool())
                            {
                                _entityFlags[flag] = true;
                                //Console.WriteLine($"Set flag [{flag}] to [true] for ent [{this.Id}] TypeName {this.TypeName}");
                                return true;
                            }
                            else
                            {
                                _entityFlags[flag] = false;
                            }
                        }
                    }
                }
            }

            return false;
        }

        public string DNA
        {
            get
            {
                if (_dna == null)
                    _dna = (string)_ball.Attribute("model").Attribute("dna");
                return _dna;
            }
        }

        public int SlimFilamentTypeId => (int)_slimItem.Attribute("abyssFilamentTypeID");

        /// <summary>
        ///     SOLO = 1, COOP = 2, TWO_PLAYER = 3
        /// </summary>
        public int SlimFilamentGameModeId => (int)_slimItem.Attribute("gameModeID");

        public float SlimSignatureRadius => (float)_slimItem.Attribute("signatureRadius");

        // actual mass with items, no static value from inv type
        public float BallMass => (float)_ball.Attribute("mass");


        public double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs, out double effectiveDps)
        {
            return GetSecondsToKill(damagePairs, new List<DirectEntity>() { this }, out effectiveDps);
        }
        /// <summary>
        /// How long does it take to kill that specific entity will all current drones in space
        /// </summary>
        /// <returns></returns>
        public double GetSecondsToKillWithActiveDrones()
        {
            Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();
            foreach (var drone in DirectEve.ActiveDrones.Where(e => e.DroneState != 4))
            {
                foreach (var kv in drone.GetDroneDPS())
                {
                    if (dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] += kv.Value;
                    }
                    else
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }
            }
            return GetSecondsToKill(dict, out _);
        }
        /// <summary>
        /// How long does it take to kill that specific entity with the list of given drones
        /// </summary>
        /// <param name="drones"></param>
        /// <returns></returns>
        public double GetSecondsToKillWithActiveDrones(List<DirectEntity> drones)
        {
            Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();
            foreach (var drone in DirectEve.ActiveDrones.Where(e => e.DroneState != 4))
            {
                if (!drones.Contains(drone))
                    continue;

                foreach (var kv in drone.GetDroneDPS())
                {
                    if (dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] += kv.Value;
                    }
                    else
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }
            }
            return GetSecondsToKill(dict, out _);
        }

        public double CalculateEffectiveDPS(Dictionary<DirectDamageType, float> damagePairs)
        {
            var effDps = 0d;
            var secs = GetSecondsToKill(damagePairs, new List<DirectEntity>() { this }, out effDps);
            return effDps;
        }

        public static double CalculateEffectiveDPS(Dictionary<DirectDamageType, float> damagePairs, List<DirectEntity> entities)
        {
            var effDps = 0d;
            var secs = GetSecondsToKill(damagePairs, entities, out effDps);
            return effDps;
        }

        public static double GetSecondsToKill(Dictionary<DirectDamageType, float> damagePairs, List<DirectEntity> entities, out double effectiveDps)
        {
            // This will return Shield, Armor, Structure base HP combined if resists can't be read
            double effectiveHealthEM = 0;
            double effectiveHealthKinetic = 0;
            double effectiveHealthExplosive = 0;
            double effectiveHealthThermal = 0;

            double combinedHealth = 0;

            effectiveDps = 0;

            foreach (var ent in entities)
            {
                effectiveHealthEM += ent.EmEHP.Value;
                effectiveHealthKinetic += ent.KinEHP.Value;
                effectiveHealthExplosive += ent.ExpEHP.Value;
                effectiveHealthThermal += ent.TrmEHP.Value;
                combinedHealth += (ent.CurrentShield ?? 0) + (ent.CurrentArmor ?? 0) + (ent.CurrentStructure ?? 0);
            }

            if (effectiveHealthEM <= 0 && effectiveHealthKinetic <= 0 && effectiveHealthExplosive <= 0 && effectiveHealthThermal <= 0)
            {
                return 0;
            }

            // Remove entries with 0 DPS
            damagePairs = damagePairs.Where(x => x.Value > 0.0).ToDictionary(x => x.Key, x => x.Value);

            var totalDps = damagePairs.Sum(x => x.Value);

            if (totalDps == 0.0)
            {
                return double.PositiveInfinity;
            }

            var totalEffEHp = 0d;
            // Calculate the damage distribution percentages and total eff ehp
            foreach (var pair in damagePairs)
            {
                var percentage = pair.Value / totalDps;

                switch (pair.Key)
                {
                    case DirectDamageType.EM:

                        if (Double.IsInfinity(effectiveHealthEM) || Double.IsNaN(effectiveHealthEM))
                            continue;

                        effectiveHealthEM *= percentage;
                        totalEffEHp += effectiveHealthEM;
                        break;
                    case DirectDamageType.KINETIC:

                        if (Double.IsInfinity(effectiveHealthKinetic) || Double.IsNaN(effectiveHealthKinetic))
                            continue;

                        effectiveHealthKinetic *= percentage;
                        totalEffEHp += effectiveHealthKinetic;
                        break;
                    case DirectDamageType.EXPLO:

                        if (Double.IsInfinity(effectiveHealthExplosive) || Double.IsNaN(effectiveHealthExplosive))
                            continue;

                        effectiveHealthExplosive *= percentage;
                        totalEffEHp += effectiveHealthExplosive;
                        break;
                    case DirectDamageType.THERMAL:

                        if (Double.IsInfinity(effectiveHealthThermal) || Double.IsNaN(effectiveHealthThermal))
                            continue;

                        effectiveHealthThermal *= percentage;
                        totalEffEHp += effectiveHealthThermal;
                        break;
                }
            }

            double secondsToKill = totalEffEHp / totalDps;

            if (combinedHealth > 0 && totalEffEHp > 0)
            {
                var ratio = combinedHealth / (double)totalEffEHp;
                effectiveDps = totalDps * ratio;
            }

            return secondsToKill;
        }

        private bool? _isAbyssGateOpen;

        public bool IsAbyssGateOpen()
        {
            if (this.TypeId != 47685 && this.TypeId != 47686)
                return false;

            if (_isAbyssGateOpen == null)
            {
                _isAbyssGateOpen = _slimItem["isAbyssGateOpen"].ToBool();
            }

            return _isAbyssGateOpen.Value;
        }

        public PyObject DynamicItem
        {
            get
            {
                _pyDynamicItem ??= DirectEve.GetLocalSvc("dynamicItemSvc")["dynamicItemCache"].DictionaryItem(this.Id);
                return _pyDynamicItem;
            }
        }


        public bool IsDynamicItem
        {
            get
            {
                var evetypes = PySharp.Import("evetypes");
                return evetypes.Call("IsDynamicType", this.TypeId).ToBool();

                //return this.TryGet<bool>("isDynamicType", true);
            }
        }

        public DirectInvType OrignalDynamicItem
        {
            get
            {
                if (IsDynamicItem)
                {
                    var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                    return DirectEve.GetInvType(sourceTypeID);
                }
                return null;
            }
        }

        public override T TryGet<T>(string keyname)
        {
            if (IsDynamicItem)
            {
                var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                var value = DirectEve.GetInvType(sourceTypeID).TryGet<T>(keyname);
                return value;
            }

            return base.TryGet<T>(keyname);
        }

        #region Abyss types

        public bool IsLargeBioCloud => this.TypeId == 47441;
        public bool IsMedBioCloud => this.TypeId == 47440;
        public bool IsSmallBioCloud => this.TypeId == 47439;

        /// <summary>
        /// +300% Signature Radius (4.0x signature radius multiplier). 
        /// </summary>
        public bool IsBioCloud => IsLargeBioCloud || IsMedBioCloud || IsSmallBioCloud;

        public bool IsLargeTachCloud => this.TypeId == 47468;
        public bool IsMedTachCloud => this.TypeId == 47467;
        public bool IsSmallTachCloud => this.TypeId == 47436;

        /// <summary>
        /// +300% Velocity (x4.0 velocity), -50% Inertia Modifier. 
        /// </summary>
        public bool IsTachCloud => IsLargeTachCloud || IsMedTachCloud || IsSmallTachCloud;

        public bool IsLargeFilaCloud => this.TypeId == 47473;
        public bool IsMedFilaCloud => this.TypeId == 47472;
        public bool IsSmallFilaCloud => this.TypeId == 47620;

        /// <summary>
        /// Penalty to Shield Booster boosting (-40%) and reduction to shield booster duration (-40%)
        /// </summary>
        public bool IsFilaCould => IsLargeFilaCloud || IsMedFilaCloud || IsSmallFilaCloud;

        public bool IsMediumRangeAutomataPylon => this.TypeId == 47438;

        public bool IsShortRangeAutomataPylon => this.TypeId == 47437;

        /// <summary>
        /// A Triglavian area-denial structure equipped with a medium-range point-defense system that will target all drones, missiles, and rogue drone frigates within its field of fire.
        /// </summary>
        public bool IsAutomataPylon => IsMediumRangeAutomataPylon || IsShortRangeAutomataPylon;

        public bool IsMediumRangeTrackingPylon => this.TypeId == 47470;

        public bool IsShortRangeTrackingPylon => this.TypeId == 47469;

        /// <summary>
        /// Tracking Pylon: +60% or +80% tracking to all ships in its area of effect. 
        /// </summary>
        public bool IsTrackingPylon => IsMediumRangeTrackingPylon || IsShortRangeTrackingPylon;

        /// <summary>
        /// As of 2022 this pylon is currently unused.
        /// +20% velocity and +10% damage for all local rogue drones, -30% velocity and -15% damage for all local capsuleer drones
        /// </summary>
        public bool IsWideAreaAutomataPylon => this.TypeId == 48254;


        /// <summary>
        /// IsWideAreaAutomataPylon || IsTrackingPylon || IsAutomataPylon || IsFilaCould || IsTachCloud || IsBioCloud
        /// </summary>
        public bool IsAbyssSphereEntity => IsWideAreaAutomataPylon || IsTrackingPylon || IsAutomataPylon ||
                                           IsFilaCould || IsTachCloud || IsBioCloud || GroupId == (int)Group.SentryGun;

        //public bool IsEntityWeWantToAvoidInAbyssals => IsBioCloud || IsBioCloud || IsTachCloud;

        private double? _gigaJouleNeutedPerSecond;

        public double GigaJouleNeutedPerSecond
        {
            get
            {

                if (_gigaJouleNeutedPerSecond.HasValue)
                    return _gigaJouleNeutedPerSecond.Value;

                if (!IsNeutingEntity)
                    return 0d;

                var neutAmount = this.TryGet<double>("energyNeutralizerAmount");
                var duration = this.TryGet<double>("behaviorEnergyNeutralizerDuration");

                if (duration != 0)
                {
                    _gigaJouleNeutedPerSecond = neutAmount / (duration / 1000);

                }
                else
                {
                    _gigaJouleNeutedPerSecond = 0;
                }
                return _gigaJouleNeutedPerSecond.Value;
            }
        }

        public bool IsLocalArmorRepairingEntity => GetDmgEffects().ContainsKey(2197) ||
                                            GetDmgEffectsByGuid().ContainsKey("effects.ArmorRepair");

        public bool IsLocalShieldRepairingEntity => GetDmgEffects().ContainsKey(6990) ||
                                                     GetDmgEffectsByGuid().ContainsKey("effects.ShieldBoosting");


        public double LocalShieldRepairingAmountPerSecond
        {
            get
            {
                if (!IsLocalShieldRepairingEntity)
                    return 0;

                var amount = this.TryGet<double>("behaviorShieldBoosterAmount");
                var duration = this.TryGet<double>("behaviorShieldBoosterDuration");

                if (amount == 0d)
                    return 0;

                return amount / duration;
            }
        }

        public double LocalArmorRepairingAmountPerSecond
        {
            get
            {
                if (!IsLocalArmorRepairingEntity)
                    return 0d;

                var amount = this.TryGet<double>("entityArmorRepairAmount");
                var duration = this.TryGet<double>("entityArmorRepairDuration");

                if (duration == 0d)
                    return 0;

                return amount / duration;
            }
        }

        public bool NPCHasVortonProjectorGuns => GetAttributesInvType().ContainsKey("VortonArcTargets");

        #endregion Abyss types

        private double? _radiusOverride;

        public double? RadiusOverride
        {
            get
            {
                if (_radiusOverride == null)
                {
                    // pylons ... clouds
                    if (this.GroupId == 1971 || this.GroupId == 1981 || this.GroupId == (int)Group.SentryGun) // http://games.chruker.dk/eve_online/inventory.php?group_id=1971 | https://everef.net/groups/1981 | https://everef.net/groups/99 (Sentry Gun -- Debug)
                    {
                        var offset = 1000;

                        if (IsTachCloud)
                            offset = 1500;

                        _radiusOverride = this.MaxRange + this.Radius + offset; // add 4k radius to not always move along the edges of the spheres (clouds, towers.. )

                        // sentry gun override
                        if (this.GroupId == (int)Group.SentryGun)
                            _radiusOverride = +this.Radius + 29000;

                        return _radiusOverride;
                    }

                    var ret = ModelBoundingSphereRadius > BallRadius ? ModelBoundingSphereRadius : BallRadius;
                    _radiusOverride = ret;
                }

                return _radiusOverride;
            }
        }

        /// <summary>
        /// Returns the average distance to the list of entities
        /// </summary>
        /// <param name="entities"></param>
        /// <returns></returns>
        public double DistanceTo(IEnumerable<DirectEntity> entities)
        {
            if (!entities.Any())
                return 0;

            var totalDist = 0d;
            var entCount = entities.Count();

            foreach (var entity in entities)
            {
                totalDist += this.DistanceTo(entity);
            }
            return totalDist / entCount;
        }

        public bool IsInOptimalRange => Distance <= OptimalRange + AccuracyFalloff / 2;

        public bool IsInOptimalRangeTo(DirectEntity ent) => ent.DistanceTo(this) <= OptimalRange + AccuracyFalloff / 2;


        private DirectWorldPosition _directAbsolutePosition;

        public DirectWorldPosition DirectAbsolutePosition =>
            _directAbsolutePosition ??= new DirectWorldPosition(this.X, this.Y, this.Z);
        //public DirectWorldPosition DirectAbsolutePosition => _directAbsolutePosition ??= new DirectWorldPosition(this.WorldPos.Value.X, this.WorldPos.Value.Y, this.WorldPos.Value.Z, DirectEve);


        /// <summary>
        /// // Only collidable entities have a world pos, will return (-1,-1,-1) if there is none
        /// This is relative to current ship. Current ship is always at (0,0,0)
        /// </summary>
        private Vec3? _worldPos = null;
        public Vec3? WorldPos
        {
            get
            {
                if (_worldPos == null)
                {
                    var res = Ball["model"]["worldPosition"].ToList();
                    if (res.Count > 0)
                        _worldPos = new Vec3(res[0].ToDouble(), res[1].ToDouble(), res[2].ToDouble());
                    else
                        _worldPos = new Vec3(-1, -1, -1);
                }

                return _worldPos;
            }
        }


        // This is the relative position to the player. X,Y,Z are absolute positions
        // This is not very accurate --> (GetVectorAt(blue.os.GetSimTime())) -- approximation
        public Vec3 BallPos
        {
            get
            {
                if (_ballPos == null)
                {
                    var camUtil = DirectEve.PySharp.Import("eve.client.script.ui.camera.cameraUtil");
                    var res = camUtil.Call("_GetBallPosition", this.Ball).ToList();
                    _ballPos = new Vec3(res[0].ToDouble(), res[1].ToDouble(), res[2].ToDouble());
                }

                return _ballPos.Value;
            }
        }

        //public void ShowBoxes(bool val)
        //{
        //    this.Ball.SetAttribute("showBoxes", val);
        //}

        // a = next wp
        // b =  final dest

        // moves to the entity via ongrid pathfinding
        // returns true if the destination is near, false if moving
        // use higher stepSize with higher speeds
        private static List<DirectWorldPosition> _currentPath;
        private static DirectWorldPosition _currentDestination;
        private static DateTime _lastPathFind;


        public bool MoveToViaAStar()
        {
            return MoveToViaAStar(ignoreAbyssEntities: true, dest: this.DirectAbsolutePosition);
        }

        public static bool MoveToViaAStar(DirectWorldPosition dest = null)
        {
            return MoveToViaAStar(ignoreAbyssEntities: true, dest: dest);
        }

        public static bool MoveToViaAStar(int stepSize = 5000, int distanceToTarget = 9000,
            int distToNextWaypoint = 5000,
            bool drawPath = true, bool disableMoving = false, bool forceRecreatePath = false,
            DirectWorldPosition dest = null)
        {
            return MoveToViaAStar(stepSize: stepSize, distanceToTarget: distanceToTarget,
                distToNextWaypoint: distToNextWaypoint, drawPath: drawPath, disableMoving: disableMoving,
                forceRecreatePath: forceRecreatePath, dest: dest, ignoreAbyssEntities: true);
        }

        public static bool MoveToViaAStar(int stepSize = 5000, int distanceToTarget = 9000,
            int distToNextWaypoint = 5000,
            bool drawPath = true, bool disableMoving = false, bool forceRecreatePath = false,
            DirectWorldPosition dest = null, bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, DirectEntity destinationEntity = null, bool optimizedPath = true)
        {
            if (!ESCache.Instance.DirectEve.Session.IsInSpace)
                return false;

            if (ESCache.Instance.DirectEve.Me.IsWarpingByMode)
                return false;

            var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;

            var dist = ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(dest);

            if (DirectSceneManager.LastRedrawSceneColliders.AddSeconds(15) < DateTime.UtcNow)
                ESCache.Instance.DirectEve.SceneManager.RedrawSceneColliders(ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds);

            //if (_currentPath.Count == 0 && dist < distanceToTarget)
            if (dist < distanceToTarget)
            {
                _currentDestination = null;
                _currentPath = null;
                //DirectEve.Log($"Destination reached.");
                if (drawPath)
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                }

                return true;
            }

            if (_currentDestination != dest ||
                (_lastPathFind.AddSeconds(3) < DateTime.UtcNow && (_currentPath == null || !_currentPath.Any())) ||
                _currentPath == null || forceRecreatePath)
            {
                _lastPathFind = DateTime.UtcNow;
                _currentDestination = dest;
                List<DirectWorldPosition> path = null;
                int ms = 0;

                using (new DisposableStopwatch(t => ms = (int)t.TotalMilliseconds))
                {
                    path = ESCache.Instance.DirectEve.ActiveShip.Entity.CalculatePathTo(_currentDestination, stepSize,
                        ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon,
                        ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, optimizedPath);
                }

                _currentPath = path;
                var pathCount = _currentPath.Count;
                if (pathCount == 0)
                {
                    if (DirectEve.Interval(10000))
                        ESCache.Instance.DirectEve.Log($"Warning: No path found.");
                    return false;
                }

                ESCache.Instance.DirectEve.Log($"Path calculation finished. Took [{ms}] ms. Count [{pathCount}]");
            }

            if (_currentPath == null)
            {
                if (DirectEve.Interval(10000))
                    ESCache.Instance.DirectEve.Log($"Warning: CurrentPath == null.");
                return false;
            }


            if (_currentPath.Count == 0)
            {
                if (!disableMoving)
                {
                    ESCache.Instance.DirectEve.Log($"No valid path found, moving to destination.");
                    ESCache.Instance.DirectEve.ActiveShip.MoveTo(dest);
                }

                return false;
            }

            var center = ESCache.Instance.DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47465);
            if (center != null && ESCache.Instance.DirectEve.Me.IsInAbyssalSpace() && _currentPath.Count > 0 && activeShip.DirectAbsolutePosition.GetDistanceSquared(center.DirectAbsolutePosition) > DirectEntity.AbyssBoundarySizeSquared)
            {
                if (_currentPath.Any(p => p.GetDistanceSquared(center.DirectAbsolutePosition) > 82000L * 82000L))
                {
                    _currentPath = new List<DirectWorldPosition>() { dest };
                    ESCache.Instance.DirectEve.Log($"Cleared path, we found waypoints further outside than 7k of the abyss boundary sphere.");
                    return false;
                }
            }

            var current = _currentPath.FirstOrDefault();

            if (current.GetDistance(activeShip.DirectAbsolutePosition) < distToNextWaypoint)
            {
                if (DirectEve.Interval(10000))
                    ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{_currentPath.Count}]");
                _currentPath.Remove(current);
                current = _currentPath.FirstOrDefault();
            }

            if (_currentPath != null && _currentPath.Count > 0 && drawPath)
            {
                ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                DrawWayPoints(_currentPath);
            }

            // check if all of the waypoints are a direct path to the destination
            bool approachTo = false;

            if (destinationEntity != null && _currentPath.Count > 0)
            {
                var cnt = _currentPath.Count;
                var flagCnt = _currentPath.Count(e => e.DirectPathFlag);
                if (flagCnt + 1 >= cnt) // add 1 because the start pos has no flag set
                {
                    if (DirectEve.Interval(5000))
                        ESCache.Instance.DirectEve.Log(
                            $"There are no obstacles between us and the final target, using approach instead of moveTo.");
                    approachTo = true;
                }
                // ESCache.Instance.DirectEve.Log(
                //     $"---- _currentPathCount [{_currentPath.Count}] _currentPathCountFlag [{_currentPath.Count(e => e.DirectPathFlag)}]");
            }

            if (current != null)
            {
                if (DirectEve.Interval(800, 900))
                {
                    if (!disableMoving)
                    {
                        if (DirectEve.Interval(10000))
                            ESCache.Instance.DirectEve.Log(
                                $"Moving to next waypoint. Current wp [{current}] Distance to next waypoint [{Math.Round((current.GetDistance(activeShip.DirectAbsolutePosition).Value / 1000), 2)}] km");

                        if (approachTo && destinationEntity != null &&
                            !destinationEntity.IsApproachedOrKeptAtRangeByActiveShip && destinationEntity.Distance < 150_000)
                        {
                            ESCache.Instance.DirectEve.Log($"Approach to [{destinationEntity.Name}]");
                            destinationEntity.Approach();
                        }
                        else
                            ESCache.Instance.DirectEve.ActiveShip.MoveTo(current);
                    }
                }
            }

            return false;
        }

        public static void DrawWayPoints(List<DirectWorldPosition> path)
        {
            var me = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (me != null)
            {
                var meWorldPos = me.DirectAbsolutePosition;
                //DirectEve.SceneManager.ClearDebugLines();
                var prev = me.BallPos;
                foreach (var waypoint in path)
                {
                    var wpPos = meWorldPos.GetDirectionalVectorTo(waypoint);
                    ESCache.Instance.DirectEve.SceneManager.DrawLine(prev, wpPos);
                    prev = wpPos;
                }
            }
        }


        /// <summary>
        /// 
        /// UNLOAD_COLLISION_INFO = 0
        /// SHOW_COLLISION_DATA = 1
        /// SHOW_DESTINY_BALL = 2
        /// SHOW_MODEL_SPHERE = 3
        /// SHOW_BOUNDING_SPHERE = 4
        /// </summary>
        /// <param name="mode"></param>
        public void ShowDestinyBalls(int mode)
        {
            var id = DirectEve.ActiveShip.Entity.Id;
            var eve = DirectEve.PySharp.Import("eve");
            var modelDebugFunctions = eve["client"]["script"]["ui"]["services"]["menuSvcExtras"]["modelDebugFunctions"];
            if (modelDebugFunctions.IsValid && modelDebugFunctions["ShowDestinyBalls"].IsValid)
            {
                modelDebugFunctions.Call("ShowDestinyBalls", this.Id, mode);
                //DirectEve.ThreadedCall(modelDebugFunctions["ShowDestinyBalls"], this.Id, mode);
            }
            else
            {
                DirectEve.Log("Warning: modelDebugFunctions not valid!");
            }
        }

        // Note: This opens a window. Window operations are logged. Don't cry later
        public void ShowInBlueViewer()
        {
            var qatools = DirectEve.PySharp.Import("eveclientqatools");
            var blueviewer = qatools["blueobjectviewer"];
            var model = this.Ball.Call("GetModel");
            var call = blueviewer["Show"];
            if (model.IsValid && blueviewer.IsValid && call.IsValid)
            {
                DirectEve.Log($"Opening blue viewer with modal of [{this.Id}]");
                DirectEve.ThreadedCall(call, model);
            }
        }

        private int? _miniBallAmount;
        public int MiniBallAmount => _miniBallAmount ??= MiniBalls.Count;

        private int? _miniBoxesAmount;
        public int MiniBoxesAmount => _miniBoxesAmount ??= Ball["miniBoxes"].Size();

        public bool IsMassive => Ball["isMassive"].ToBool();

        public bool IsInvulnerable => !IsMassive;

        private int? _miniCapsuleAmount;
        public int MiniCapsulesAmount => _miniCapsuleAmount ??= Ball["miniCapsules"].Size();

        private bool? _hasAnyColliders;
        private static Dictionary<long, bool> _hasAnyCollidersCacheByTypeId = new Dictionary<long, bool>();

        public List<IGeometry> GetAllColliders => MiniBalls.Cast<IGeometry>().Concat(MiniBoxes).Concat(MiniCapsules).ToList();

        public bool HasAnyNonTraversableColliders
        {
            get
            {
                if (_hasAnyCollidersCacheByTypeId.TryGetValue(this.Id, out var res))
                {
                    _hasAnyColliders = res;
                }

                if (_hasAnyColliders == null)
                {
                    var miniBallAmount = MiniBalls.Count(e => !e.Traversable);
                    _hasAnyColliders = miniBallAmount > 0 || MiniBoxesAmount > 0 || MiniCapsulesAmount > 0;
                    _hasAnyCollidersCacheByTypeId[this.TypeId] = _hasAnyColliders.Value;
                }

                return _hasAnyColliders.Value;
            }
        }

        public static void OnSessionChange()
        {
            ResetColliderCaches();
        }

        public static int AStarErrors = 0;

        public static void ResetColliderCaches()
        {
            _miniBallsCacheByTypeId = new Dictionary<string, List<DirectMiniBall>>();
            _miniBoxesCacheByTypeId = new Dictionary<string, List<DirectMiniBox>>();
            _miniCapsulesCacheByTypeId = new Dictionary<string, List<DirectMiniCapsule>>();
            _boundingSphereRadiusCache = new Dictionary<long, double>();
            _hasAnyCollidersCacheByTypeId = new Dictionary<long, bool>();
        }

        private static Dictionary<long, double> _boundingSphereRadiusCache = new Dictionary<long, double>();
        public double BoundingSphereRadius()
        {
            if (_boundingSphereRadiusCache.TryGetValue(this.Id, out var max))
                return max;

            foreach (var ball in MiniBalls)
            {
                var val = ball.Center.Magnitude + ball.MaxBoundingRadius;
                if (val > max)
                    max = val;
            }

            foreach (var capsule in MiniCapsules)
            {
                var val = capsule.Center.Magnitude + capsule.MaxBoundingRadius;
                if (val > max)
                    max = val;
            }

            foreach (var rect in MiniBoxes)
            {
                var val = rect.Center.Magnitude + rect.MaxBoundingRadius;
                if (val > max)
                    max = val;
            }

            //Console.WriteLine($"Ent [{this.Id}] TypeName [{this.TypeName}] Max [{max}]");

            _boundingSphereRadiusCache[this.Id] = max;
            return max;
        }

        private List<DirectMiniBall> _miniBalls;

        private static Dictionary<string, List<DirectMiniBall>> _miniBallsCacheByTypeId =
            new Dictionary<string, List<DirectMiniBall>>();

        public List<DirectMiniBall> MiniBalls
        {
            get
            {
                if (_miniBallsCacheByTypeId.TryGetValue("" + this.Position + this.Id,
                        out var res)) // TODO: create a on session change event and clear those there, also is there a better way to cache those?
                {
                    _miniBalls = res;
                    //Console.WriteLine($"Returned cached balls.");
                }

                if (_miniBalls == null)
                {
                    var list = this.Ball["miniBalls"].ToList();
                    _miniBalls = new List<DirectMiniBall>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniBall(obj);
                        _miniBalls.Add(mb);
                    }

                    if (this.IsAbyssSphereEntity)
                    {
                        var mb = new DirectMiniBall(0, 0, 0, (float)this.RadiusOverride.Value, true);
                        _miniBalls.Add(mb);
                    }

                    _miniBallsCacheByTypeId["" + this.Position + this.Id] = _miniBalls;
                }

                return _miniBalls;
            }
        }


        private List<DirectMiniBox> _miniBoxes;

        private static Dictionary<string, List<DirectMiniBox>> _miniBoxesCacheByTypeId =
            new Dictionary<string, List<DirectMiniBox>>();

        public List<DirectMiniBox> MiniBoxes
        {
            get
            {
                if (_miniBoxesCacheByTypeId.TryGetValue("" + this.Position + this.Id, out var res))
                {
                    _miniBoxes = res;
                }

                if (_miniBoxes == null)
                {
                    var list = this.Ball["miniBoxes"].ToList();
                    _miniBoxes = new List<DirectMiniBox>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniBox(obj);
                        _miniBoxes.Add(mb);
                    }

                    _miniBoxesCacheByTypeId["" + this.Position + this.Id] = _miniBoxes;
                }

                return _miniBoxes;
            }
        }


        private List<DirectMiniCapsule> _miniCapsules;

        private static Dictionary<string, List<DirectMiniCapsule>> _miniCapsulesCacheByTypeId =
            new Dictionary<string, List<DirectMiniCapsule>>();

        public List<DirectMiniCapsule> MiniCapsules
        {
            get
            {
                if (_miniCapsulesCacheByTypeId.TryGetValue("" + this.Position + this.Id, out var res))
                {
                    _miniCapsules = res;
                }

                if (_miniCapsules == null)
                {
                    var list = this.Ball["miniCapsules"].ToList();
                    _miniCapsules = new List<DirectMiniCapsule>();
                    foreach (var obj in list)
                    {
                        var mb = new DirectMiniCapsule(obj);
                        _miniCapsules.Add(mb);
                    }

                    _miniCapsulesCacheByTypeId["" + this.Position + this.Id] = _miniCapsules;
                }

                return _miniCapsules;
            }
        }

        public void DrawSphere(float? radius = null, bool forceRedraw = false)
        {
            if (radius == null)
                radius = (float)this.RadiusOverride;


            var name = this.Id.ToString();

            if (!forceRedraw)
            {
                if (DirectEve.SceneManager.DefaultSceneObjectsDict.ContainsKey(name + "_CCPEndorsedRenderObject"))
                {
                    return;
                }
            }

            var x = 0f;
            var y = 0f;
            var z = 0f;

            SphereType sphereType;

            if (this.IsBioCloud)
                sphereType = SphereType.Jumprangebubble; // correct size
            else if (this.IsTachCloud)
                sphereType = SphereType.Scanbubblehitsphere; // correct size
            else if (this.IsFilaCould)
                sphereType = SphereType.Scanconesphere; // correct size
            else
                sphereType = SphereType.Miniball;

            DirectEve.SceneManager.DrawSphere(radius.Value, name, (float)x, (float)y, (float)z, this.Ball, sphereType);
        }

        public void DrawBalls()
        {
            if (this.MiniBallAmount > 0)
            {
                var mbs = this.Ball["miniBalls"].ToList();
                int n = 0;
                foreach (var mb in mbs)
                {
                    var x = mb["x"].ToFloat();
                    var y = mb["y"].ToFloat();
                    var z = mb["z"].ToFloat();
                    var radius = mb["radius"].ToFloat();
                    var name = this.Id + "_" + n;
                    DirectEve.SceneManager.DrawSphere(radius, name, x, y, z, this.Ball);
                    n++;
                }
            }
        }

        public void DisplayAllBallAxes()
        {
            if (this.MiniBallAmount > 0)
            {
                var mbs = this.Ball["miniBalls"].ToList();
                int n = 0;
                foreach (var mb in mbs)
                {
                    var x = mb["x"].ToFloat();
                    var y = mb["y"].ToFloat();
                    var z = mb["z"].ToFloat();
                    var radius = mb["radius"].ToFloat();
                    var name = this.Id + "_" + n;
                    var pos = new DirectWorldPosition(x + this.WorldPos.Value.X, y + this.WorldPos.Value.Y, z + this.WorldPos.Value.Z).GetVector();
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X + 15000, pos.Y, pos.Z).GetVector());
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X, pos.Y + 15000, pos.Z).GetVector());
                    DirectEve.SceneManager.DrawLine(pos, new DirectWorldPosition(pos.X, pos.Y, pos.Z + 15000).GetVector());
                    n++;
                }
            }
        }

        public void ShowBallEdges()
        {
            if (this.MiniBallAmount > 0)
            {
                foreach (var mb in this.MiniBalls)
                {
                    var pos = new DirectWorldPosition(mb.X + this.WorldPos.Value.X, mb.Y + this.WorldPos.Value.Y, mb.Z + this.WorldPos.Value.Z).GetVector();
                    for (int i = 0; i < mb.Radius + mb.Radius * 0.3; i = i + 100)
                    {
                        var pNew = new DirectWorldPosition(pos.X, pos.Y, pos.Z + i);
                        if (mb.IsPointWithin(pNew.GetVector(), this.WorldPos.Value))
                        {
                            DirectEve.SceneManager.DrawLine(pNew.GetVector(), new DirectWorldPosition(pNew.X + 15000, pNew.Y, pNew.Z).GetVector(), 0, 1, 0, 1); // green
                        }
                        else
                        {
                            DirectEve.SceneManager.DrawLine(pNew.GetVector(), new DirectWorldPosition(pNew.X + 15000, pNew.Y, pNew.Z).GetVector(), 1, 0, 0, 1); // yellow
                        }
                    }
                }
            }
        }

        public void DrawBoxes()
        {
            if (this.MiniBoxesAmount > 0)
            {
                var boxes = this.MiniBoxes;
                foreach (var mb in boxes)
                {
                    DirectEve.SceneManager.DrawBox(mb, this.Ball);
                }
            }
        }

        public void DrawCapsules()
        {
            if (this.MiniCapsulesAmount > 0)
            {
                foreach (var caps in this.MiniCapsules)
                {
                    DirectEve.SceneManager.DrawCapsule(caps, this.Ball);
                }
            }
        }

        public void DrawBoxesWithLines()
        {
            if (this.MiniBoxesAmount > 0)
            {
                var boxes = this.MiniBoxes;

                foreach (var mb in boxes)
                {
                    if (this.WorldPos.HasValue)
                    {
                        var pos = this.DirectAbsolutePosition.GetVector() - DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetVector();
                        //var pos = this.WorldPos.Value;

                        DirectEve.SceneManager.DrawLine(mb.P8 + pos, mb.P4 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P8 + pos, mb.P7 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P7 + pos, mb.P3 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P3 + pos, mb.P4 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P4 + pos, mb.P2 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P2 + pos, mb.P6 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P6 + pos, mb.P5 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P5 + pos, mb.P1 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P1 + pos, mb.P2 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P6 + pos, mb.P8 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P7 + pos, mb.P5 + pos, 0, 1, 1, 1);
                        DirectEve.SceneManager.DrawLine(mb.P1 + pos, mb.P3 + pos, 0, 1, 1, 1);

                        //    p6 +--------+ p2
                        //      /        /|
                        //     /        / |
                        // p5 +--------+p1|
                        //    |        |  |
                        //    |   p8   |  + p4 
                        //    |        | /
                        //    |        |/
                        // p7 +--------+ p3
                    }
                }
            }
        }

        public void RemoveDrawnSphere()
        {
            var name = this.Id.ToString();
            DirectEve.SceneManager.RemoveDrawnObject(name);
        }

        private DirectEntity _abyssalCenter;

        private DirectEntity AbyssalCenter =>
            _abyssalCenter ??= DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47465);



        private bool? _isInSpeedCloud = null;

        public bool IsInSpeedCloud
        {
            get
            {
                if (!_isInSpeedCloud.HasValue)
                {
                    if (!DirectEve.Entities.Any(e => e.IsTachCloud))
                        return false;

                    _isInSpeedCloud = AnyIntersectionAtThisPosition(DirectAbsolutePosition, false, true, true, true, true, true, false).Any(e => e.IsTachCloud);
                }
                return _isInSpeedCloud.Value;
            }
        }

        public List<DirectEntity> GetIntersectionEntities
        {
            get
            {
                return AnyIntersectionAtThisPosition(DirectAbsolutePosition);
            }
        }

        private static List<DirectEntity> _colliderEntities = null;
        public static List<DirectEntity> ColliderEntities
        {
            get
            {
                if (_colliderEntities == null || DirectEve.HasFrameChanged())
                {
                    _colliderEntities = ESCache.Instance.DirectEve.Entities.Where(e => e.Distance < 3_000_000 && (e.HasAnyNonTraversableColliders || e.IsAbyssSphereEntity)).ToList();
                }
                return _colliderEntities;
            }
        }

        private (float, bool) Cost(DirectWorldPosition current, DirectWorldPosition next, DirectWorldPosition destination, bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, List<DirectEntity> ingoredEntities = null)
        {
            // Calculate the distance from current to the next waypoint
            float distCurrentNext = (float)current.GetDistanceSquared(next).Value;

            //// TODO: Should we use a multiple of the distance if we encountered this waypoint before to discourage using the same path waypoint multiple times?
            if (next.Visits > 0)
                distCurrentNext *= next.Visits;

            // Use raycasting to check if there is a line of sight between the current and the next waypoint
            var colliders = DirectRayCasting.IsLineOfSightFree(current.GetVector(), next.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ingoredEntities);

            if (AbyssalCenter != null && AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(next) >= AbyssBoundarySizeSquared && AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(destination) <= AbyssBoundarySizeSquared)
            {
                var factor = 10000.0f;
                var distNext = AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(next);
                var distCurrent = AbyssalCenter.DirectAbsolutePosition.GetDistanceSquared(current);

                if (distCurrentNext > distCurrent)
                {
                    factor *= 5;
                }

                distCurrentNext *= (float)factor;
                return (distCurrentNext, true);
            }

            if (!colliders.Item1)
            {
                double factor = 1000.0f;
                var cols = colliders.Item2;

                if (cols.Any(e => e.Value.Any(c => !c.Traversable)))
                {
                    return (float.MaxValue, false);
                }

                //Scale the distance by the number of colliders hit and by the distance to the center of the colliding unit
                if (cols.Count == 1)
                {
                    var colCenter = new DirectWorldPosition(cols.First().Value.First().Center);
                    if (colCenter.GetDistanceSquared(destination) > cols.First().Value.First().MaxBoundingRadiusSquared)
                    {
                        foreach (var ent in cols)
                        {
                            foreach (var col in ent.Value)
                            {
                                // if we are in a sphere, choose the shortest way out
                                var center = col.Center;
                                var radiusSq = col.Radius * col.Radius;
                                var distToCenter = (float)next.GetDistanceSquared(new DirectWorldPosition(center + ent.Key.DirectAbsolutePosition.GetVector())).Value;
                                //Console.WriteLine($"Distance to center: {distToCenter} radius {radius}");
                                factor = 800.0f + 200.0d * (radiusSq - distToCenter) / radiusSq;
                                distCurrentNext *= (float)factor;
                                break;
                            }
                        }
                        return (distCurrentNext, true);
                    }
                }

                // Penalize overlapping colliders even harder
                if (cols.Count > 1)
                    factor *= cols.Count;

                distCurrentNext *= (float)factor;
            }

            return (distCurrentNext, true);
        }

        // To ensure admissibility (... ,triangle inequality) we only use the distance to the final destination as heuristic
        public float Heuristic(DirectWorldPosition current, DirectWorldPosition next, DirectWorldPosition destination)
        {
            // Calculate the distance from next waypoint to destination waypoint
            var d = (float)next.GetDistanceSquared(destination).Value;
            return d;
        }

        public static List<DirectEntity> AnyIntersectionAtThisPosition(DirectWorldPosition worldPos,
            bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, IEnumerable<DirectEntity> excludeEnts = null)
        {
            var intersectingEntsRet = new List<DirectEntity>();
            var ret = DirectRayCasting.IsLineOfSightFree(worldPos.GetVector(), worldPos.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, false, excludeEnts?.ToList() ?? new List<DirectEntity>());
            if (ret.Item1)
            {
                return intersectingEntsRet;
            }
            else
            {
                return ret.Item2.Keys.ToList();
            }
        }

        public static long AbyssBoundarySize = 74999;

        public static long AbyssBoundarySizeSquared = AbyssBoundarySize * AbyssBoundarySize;

        public List<DirectWorldPosition> CalculatePathTo(DirectWorldPosition destination, int stepSize = 5000,
            bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, bool optimizedPath = false)
        {
            var dest = destination;
            var path = new List<DirectWorldPosition>();

            //var intereSectingEntsStart = AnyIntersectionAtThisPosition(start);
            var intersectingEntsDest = AnyIntersectionAtThisPosition(dest);
            //var intersectingEntsStartDest = intereSectingEntsStart.Concat(intersectingEntsDest).Distinct().ToList();

            //if (intereSectingEntsStart.Any())
            //{
            //    DirectEve.Log($"INFO: Start position is intersecting with {intereSectingEntsStart.Count()} entities");
            //}

            var ignoredEnts = intersectingEntsDest;

            if (intersectingEntsDest.Any())
            {
                DirectEve.Log($"INFO: Destination position is intersecting with [{intersectingEntsDest.Count()}] entities. Entities [{String.Join(", ", intersectingEntsDest.Select(e => e.TypeName))}]");
            }

            var isLineOfSightFreeDestination = DirectRayCasting.IsLineOfSightFree(this.DirectAbsolutePosition.GetVector(), dest.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ignoredEnts);

            if (isLineOfSightFreeDestination.Item1)
            {
                //Console.WriteLine($"Direct Path found, returning direct path.");
                destination.DirectPathFlag = true;
                this.DirectAbsolutePosition.DirectPathFlag = true;
                path.Add(this.DirectAbsolutePosition);
                path.Add(destination);
                return path;
            }

            if (destination == null || destination == this.DirectAbsolutePosition ||
                DirectEve.Session.IsInDockableLocation)
                return new List<DirectWorldPosition>() { this.DirectAbsolutePosition };

            var start = this.DirectAbsolutePosition;
            var cameFrom = new Dictionary<DirectWorldPosition, DirectWorldPosition>();
            var costSoFar = new Dictionary<DirectWorldPosition, float>();
            var frontier = new SimplePriorityQueue<DirectWorldPosition>();

            // round start and dest to whole integers
            start = new DirectWorldPosition((long)start.X, (long)start.Y, (long)start.Z);
            dest = new DirectWorldPosition((long)dest.X, (long)dest.Y, (long)dest.Z);
            destination = dest;

            frontier.Enqueue(start, 0);
            cameFrom[start] = start;
            costSoFar[start] = 0;
            var amount = 0;

            HashSet<DirectWorldPosition> neighbours = new HashSet<DirectWorldPosition>();
            int duplicateNeighbours = 0;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();
                current.Visits++;
                amount++;
                var dist = current.GetDistanceSquared(destination);

                if (amount > 1000)
                {
                    AStarErrors++;
                    DirectEve.Log($"WARNING: Hit limit. Dist [{dist}] AStarError [{AStarErrors}]");
                    break;
                }

                if (dist <= Math.Pow((stepSize + 50), 2))
                {
                    destination = current;
                    break;
                }

                foreach (var next in current.GenerateNeighbours(stepSize, destination))
                {
                    var n = next;
                    if (neighbours.TryGetValue(next, out var nx))
                    {
                        n = nx;
                        duplicateNeighbours++;
                    }
                    else
                    {
                        neighbours.Add(next);
                    }

                    var cost = Cost(current, next, destination, ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEnts);
                    var newCost = costSoFar[current] + 1 + cost.Item1;
                    if (cost.Item2 && (!costSoFar.ContainsKey(next) || newCost < costSoFar[next]))
                    {
                        var heur = Heuristic(current, next, destination);
                        var priority = newCost + heur;
                        costSoFar[next] = newCost;
                        frontier.Enqueue(next, priority);
                        cameFrom[next] = current;
                    }
                }
            }

            var e = destination;
            while (e != start)
            {
                path.Add(e);
                if (!cameFrom.ContainsKey(e))
                {
                    path.Clear();
                    break;
                }
                e = cameFrom[e];
            }

            if (path.Contains(destination))
                path.Add(start);
            path.Reverse();
            if (path.Any())
                path.Add(dest);


            if (optimizedPath && path.Any())
            {
                var optimizedPathStartIndex = 0;
                var optimizedPathList = new List<DirectWorldPosition>();
                optimizedPathList.Add(path.First());
                var isFreeCnt = 0;
                for (int i = 1; i < path.Count - 1; i++)
                {
                    var current = path[optimizedPathStartIndex];
                    var target = path[i + 1];
                    // Check to see if we can skip the current index in navigation
                    var isFree = DirectRayCasting.IsLineOfSightFree(current.GetVector(), target.GetVector(), ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoredEntities: ignoredEnts);
                    // Not able to navigate, take until this index
                    if (!isFree.Item1)
                    {
                        optimizedPathList.Add(path[i]);
                        optimizedPathStartIndex = i;
                        i++;
                        isFreeCnt++;
                    }
                }
                if (optimizedPathList.Last() != path.Last())
                {
                    optimizedPathList.Add(path.Last());
                }
                if (isFreeCnt == 0)
                {
                    optimizedPathList.All(p => p.DirectPathFlag = true);
                }
                return optimizedPathList;
            }

            return path;
        }

        public double EstimatedPixelDiameterWithChildren
        {
            get
            {
                _estimatedPixelDiameterWithChildren ??=
                    (double)_ball.Attribute("model").Attribute("estimatedPixelDiameterWithChildren");

                return _estimatedPixelDiameterWithChildren.Value;
            }
        }

        public double GotoX
        {
            get
            {
                if (!_gotoX.HasValue)
                    _gotoX = (double)_ball.Attribute("gotoX");

                return _gotoX.Value;
            }
        }

        private double? _maxVelocity;

        public double MaxVelocity
        {
            get
            {
                if (!_maxVelocity.HasValue)
                    _maxVelocity = (double)_ball.Attribute("maxVelocity");

                return _maxVelocity.Value;
            }
        }

        private double? _speedFraction;

        public double SpeedFraction
        {
            get
            {
                if (!_speedFraction.HasValue)
                    _speedFraction = (double)_ball.Attribute("speedFraction");

                return _speedFraction.Value;
            }
        }

        public double GotoY
        {
            get
            {
                if (!_gotoY.HasValue)
                    _gotoY = (double)_ball.Attribute("gotoY");

                return _gotoY.Value;
            }
        }


        public double GotoZ
        {
            get
            {
                if (!_gotoZ.HasValue)
                    _gotoZ = (double)_ball.Attribute("gotoZ");

                return _gotoZ.Value;
            }
        }

        public int AllianceId
        {
            get
            {
                if (!_allianceId.HasValue)
                    _allianceId = (int)_ball.Attribute("allianceID");

                return _allianceId.Value;
            }
        }

        public double AngularVelocity
        {
            get
            {
                if (_angularVelocity == null)
                    _angularVelocity = (double)TransversalVelocity / Math.Max(1, Distance);

                return _angularVelocity.Value;
            }
        }

        public double ArmorPct
        {
            get
            {
                if (!_armorPct.HasValue)
                    GetDamageState();

                return _armorPct ?? 0;
            }
        }

        public void DrawLineTo(DirectEntity to)
        {
            Vec3 start = new Vec3((float)this.BallPos.X, (float)this.BallPos.Y, (float)this.BallPos.Z);
            Vec3 end = new Vec3((float)to.BallPos.X, (float)to.BallPos.Y, (float)to.BallPos.Z);
            DirectEve.SceneManager.DrawLine(start, end);
        }

        public List<string> Attacks { get; private set; }
        public PyObject Ball => _ball;

        public List<DamageType> BestDamageTypes
        {
            get
            {
                if (_bestDamageTypes == null)
                {
                    _bestDamageTypes = new List<DamageType>();
                    ulong emEHP = ulong.MaxValue;
                    ulong expEHP = ulong.MaxValue;
                    ulong kinEHP = ulong.MaxValue;
                    ulong trmEHP = ulong.MaxValue;
                    try
                    {
                        emEHP = EmEHP != null && (Double.IsInfinity(EmEHP.Value) || Double.IsNaN(EmEHP.Value))
                            ? ulong.MaxValue
                            : (ulong)EmEHP;
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        expEHP = ExpEHP != null && (Double.IsInfinity(ExpEHP.Value) || Double.IsNaN(ExpEHP.Value))
                            ? ulong.MaxValue
                            : (ulong)ExpEHP;
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        kinEHP = KinEHP != null && (Double.IsInfinity(KinEHP.Value) || Double.IsNaN(KinEHP.Value))
                            ? ulong.MaxValue
                            : (ulong)KinEHP;
                    }
                    catch (Exception)
                    {
                    }

                    try
                    {
                        trmEHP = TrmEHP != null && (Double.IsInfinity(TrmEHP.Value) || Double.IsNaN(TrmEHP.Value))
                            ? ulong.MaxValue
                            : (ulong)TrmEHP;
                    }
                    catch (Exception)
                    {
                    }

                    var dict = new Dictionary<DamageType, ulong>();
                    dict.Add(DamageType.EM, emEHP);
                    dict.Add(DamageType.Explosive, expEHP);
                    dict.Add(DamageType.Kinetic, kinEHP);
                    dict.Add(DamageType.Thermal, trmEHP);
                    //_bestDamageType = dict.FirstOrDefault(e => e.Value == Math.Min(Math.Min(Math.Min(emEHP, expEHP), kinEHP), trmEHP)).Key;
                    _bestDamageTypes = dict.OrderBy(e => e.Value).Select(e => e.Key).ToList();
                }

                return _bestDamageTypes;
            }
        }


        public int CharId
        {
            get
            {
                if (!_charId.HasValue)
                    _charId = (int)_slimItem.Attribute("charID");

                return _charId.Value;
            }
        }

        /// <summary>
        /// Tries to calcuate the entity which this entity is warping to
        /// </summary>
        public DirectEntity WarpDestinationEntity
        {
            get
            {
                if (!this.IsWarpingByMode)
                {
                    return null;
                }

                var dist = double.MaxValue;
                var entities = DirectEve.Entities.Where(e =>
                    e.GroupId == (int)Group.Stargate || e.GroupId == (int)Group.Station ||
                    e.GroupId == (int)Group.Moon ||
                    e.GroupId == (int)Group.Planet);

                DirectEntity result = null;
                foreach (var e in entities)
                {
                    //var d = Math.Round(Math.Sqrt((GotoX - e.X) * (GotoX - e.X) + (GotoY - e.Y) * (GotoY - e.Y) + (GotoZ - e.Z) * (GotoZ - e.Z)), 2);
                    var x = this.GotoX - e.X;
                    var y = this.GotoY - e.Y;
                    var z = this.GotoZ - e.Z;

                    var d = Math.Sqrt(x * x + y * y + z * z);
                    if (d < dist)
                    {
                        dist = d;
                        result = e;
                    }
                }

                return result;
            }
        }

        public int CorpId
        {
            get
            {
                if (!_corpId.HasValue)
                    _corpId = (int)_slimItem.Attribute("corpID");

                return _corpId.Value;
            }
        }

        public double? CurrentArmor
        {
            get
            {
                if (!_currentrmor.HasValue)
                    _currentrmor = TotalArmor * ArmorPct;
                return _currentrmor;
            }
        }

        public double? CurrentShield
        {
            get
            {
                if (!_currentShield.HasValue)
                    _currentShield = TotalShield * ShieldPct;
                return _currentShield;
            }
        }

        public double? CurrentStructure
        {
            get
            {
                if (!_currentStructure.HasValue)
                    _currentStructure = TotalStructure * StructurePct;
                return _currentStructure;
            }
        }

        public double Distance
        {
            get
            {
                if (!_distance.HasValue)
                    _distance = (double)_ball.Attribute("surfaceDist");

                return _distance.Value;
            }
        }

        public double DistanceTo(DirectEntity ent)
        {
            return ent.DirectAbsolutePosition.GetDistance(this.DirectAbsolutePosition).Value - (double)BallRadius -
                   (double)ent.BallRadius;
        }

        public List<string> ElectronicWarfare { get; private set; }

        public double? EmEHP => _emEHP ??= GetEmEHP();

        private double? GetEmEHP()
        {
            var shield = (1 / (1 - ShieldResistanceEM)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceEM)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceEM)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? ExpEHP => _expEHP ??= GetExpEHP();

        private double? GetExpEHP()
        {
            var shield = (1 / (1 - ShieldResistanceExplosion)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceExplosion)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceExplosion)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? TrmEHP => _trmEHP ??= GetTrmEHP();

        private double? GetTrmEHP()
        {
            var shield = (1 / (1 - ShieldResistanceThermal)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceThermal)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceThermal)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        private double? GetKinEHP()
        {
            var shield = (1 / (1 - ShieldResistanceKinetic)) * CurrentShield ?? 0;
            var armor = (1 / (1 - ArmorResistanceKinetic)) * CurrentArmor ?? 0;
            var struc = (1 / (1 - StructureResistanceKinetic)) * CurrentStructure ?? 0;

            if (double.IsNaN(shield))
                shield = 0;

            if (double.IsNaN(armor))
                armor = 0;

            if (double.IsNaN(struc))
                struc = 0;

            return shield + armor + struc;
        }

        public double? KinEHP => _kinEHP ??= GetKinEHP();

        public bool IsBoarded => IsPlayer;

        private static Dictionary<long, (long, DateTime)> _followIdCacheDrones =
            new Dictionary<long, (long, DateTime)>();

        public long FollowId
        {
            get
            {
                if (_followIdCacheDrones.TryGetValue(this.Id, out var f) && f.Item2.AddSeconds(3) >= DateTime.UtcNow)
                {
                    return f.Item1;
                }

                if (!_followId.HasValue)
                    _followId = (long)_ball.Attribute("followId");

                return _followId.Value;
            }
        }

        private DirectEntity _followEntity;

        public DirectEntity FollowEntity
        {
            get
            {
                if (_followEntity != null)
                    return _followEntity;

                if (DirectEve.EntitiesById.TryGetValue(FollowId, out var ent))
                {
                    _followEntity = ent;
                }

                return _followEntity;
            }
        }

        /// <summary>
        /// Ensure you have access!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetFleetHangarContainer()
        {
            var emptyCont = new DirectContainer(DirectEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return emptyCont;
            }

            //if (Distance > 2500)
            //{
            //    DirectEve.Log($"Entity is too far away to access the fleet hangar container. Dist [{Distance}] Max Dist is [2500]");
            //    return emptyCont;
            //}

            if (!DirectEve.Session.ShipId.HasValue)
                return emptyCont;


            // check if the target has a fleet hangar
            var godma = DirectEve.GetLocalSvc("godma");
            var hasFleetHangar = godma.Call("GetType", this.TypeId)["hasFleetHangars"].ToBool();
            if (!hasFleetHangar)
            {
                DirectEve.Log("Target ship has no fleet hangar.");
                return emptyCont;
            }

            var ofh = DirectEve.PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions")["OpenFleetHangar"];

            if (!ofh.IsValid)
                DirectEve.Log($"Error: eve.client.script.ui.services.menuSvcExtras.openFunctions.OpenFleetHangar is not valid.");

            if (this.Id <= 0)
            {
                DirectEve.Log("Error: Id is <= 0.");
                return emptyCont;
            }

            DirectEve.ThreadedCall(ofh, this.Id);

            var inventory = DirectContainer.GetInventory(DirectEve, "GetInventoryFromId", this.Id);
            return new DirectContainer(DirectEve, inventory, DirectEve.Const.FlagFleetHangar, this.Id);
        }

        /// <summary>
        /// Ensure you have access!
        /// </summary>
        /// <returns></returns>
        public DirectContainer GetShipMaintenaceBayContainer()
        {
            var emptyCont = new DirectContainer(DirectEve, PySharp.PyZero, PySharp.PyZero, string.Empty);

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return emptyCont;
            }

            if (Distance > 2500)
            {
                DirectEve.Log($"Entity is too far away to access the ship maintenance container. Dist [{Distance}] Max Dist is [2500]");
                return emptyCont;
            }

            if (!DirectEve.Session.ShipId.HasValue)
                return emptyCont;

            // check if the target has a fleet hangar
            var godma = DirectEve.GetLocalSvc("godma");
            var hasSMA = godma.Call("GetType", this.TypeId)["hasShipMaintenanceBay"].ToBool();
            if (!hasSMA)
            {
                DirectEve.Log("Target ship has no ship maint bay.");
                return emptyCont;
            }

            var ofs = DirectEve.PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions")["OpenShipMaintenanceBayShip"];

            if (!ofs.IsValid)
                DirectEve.Log($"Error: eve.client.script.ui.services.menuSvcExtras.openFunctions.OpenShipMaintenanceBayShip is not valid.");

            if (this.Id <= 0)
            {
                DirectEve.Log("Error: Id is <= 0.");
                return emptyCont;
            }

            DirectEve.ThreadedCall(ofs, this.Id, "");
            var inventory = DirectContainer.GetInventory(DirectEve, "GetInventoryFromId", this.Id);
            return new DirectContainer(DirectEve, inventory, DirectEve.Const.FlagShipHangar, this.Id);
        }

        /// <summary>
        /// This opens a modal
        /// Use ONLY on targets which are in same fleet AND/OR in same corp and ensure that the rights are set correctly, we do not check for that here
        /// </summary>
        public void StoreCurrentShipInShipMaintenanceBay()
        {

            // TODO: CHECK IF TARGET IS IN FLEET OR CORP

            // check if it's a ship
            if (this.CategoryId != (int)CategoryID.Ship)
            {
                DirectEve.Log("Target is not a ship?");
                return;
            }

            // check if the target has a SMB
            var godma = DirectEve.GetLocalSvc("godma");
            var hasSMA = godma.Call("GetType", this.TypeId)["hasShipMaintenanceBay"].ToBool();
            if (!hasSMA)
            {
                DirectEve.Log("Target ship has no maintenance bay.");
                return;
            }

            if (DirectEve.ActiveShip.GroupId == (int)Group.Capsule)
            {
                DirectEve.Log("You can't store a capsule in a SMB.");
                return;
            }

            if (Distance > 2500)
            {
                DirectEve.Log($"Entity is too far away to store the ship. Dist [{Distance}] Max Dist is [2500]");
                return;
            }

            var menuSvc = DirectEve.GetLocalSvc("menu");

            if (!menuSvc.IsValid)
            {
                DirectEve.Log("Menu svc ref is not valid.");
                return;
            }

            var currentShipId = DirectEve.Session.ShipId;

            if (currentShipId <= 0)
            {
                DirectEve.Log("Current ship id is <= 0");
                return;
            }

            if (this.Id <= 0)
            {
                DirectEve.Log("Dest ship id is <= 0");
            }

            DirectEve.ThreadedCall(menuSvc["StoreVessel"], this.Id, currentShipId);
        }

        public bool EngageTargetWithDrones(List<long> droneIds)
        {
            if (!IsTarget)
                return false;

            var activeDrones = DirectEve.ActiveDrones.ToList();
            foreach (var id in droneIds)
            {
                if (activeDrones.All(e => e.Id != id))
                    return false;
            }

            foreach (var id in droneIds)
            {
                _followIdCacheDrones[id] = (this.Id, DateTime.UtcNow);
            }

            if (!DirectEve.Interval(900, 1500))
                return false;

            if (!IsActiveTarget)
                MakeActiveTarget(false);

            var ret = DirectEve.ThreadedLocalSvcCall("menu", "EngageTarget", droneIds);
            return ret;
        }

        public bool EngageTargetWithDrones(IEnumerable<DirectEntity> drones)
        {
            var ids = drones.Select(e => e.Id).ToList();
            if (ids.Any())
                return EngageTargetWithDrones(ids);
            return false;
        }

        public string GivenName
        {
            get
            {
                if (_givenName == null)
                    _givenName = DirectEve.GetLocationName(Id);

                return _givenName;
            }
        }

        public bool HasExploded
        {
            get
            {
                if (!_hasExploded.HasValue)
                    _hasExploded = _ball.Attribute("exploded").ToBool();
                return _hasExploded.Value;
            }
        }

        public bool HasReleased
        {
            get
            {
                if (!_hasReleased.HasValue)
                    _hasReleased = _ball.Attribute("released").ToBool();
                return _hasReleased.Value;
            }
        }

        public long Id { get; internal set; }

        public bool IsActiveTarget { get; internal set; }

        public bool IsApproachedOrKeptAtRangeByActiveShip => DirectEve.ActiveShip.Entity.FollowId == Id
                                                             && DirectEve.ActiveShip.Entity.IsApproachingOrKeptAtRange
                                                             && DirectEve.GetEntityById(Id) != null;

        public bool IsApproachingOrKeptAtRange => Mode == 1;

        public bool IsAligning => Mode == 0;

        public bool IsAttacking => HasFlagSet(DirectEntityFlag.threatAttackingMe);

        public bool IsCloaked
        {
            get
            {
                if (!_isCloaked.HasValue)
                    _isCloaked = (int)_ball.Attribute("isCloaked") != 0;

                return _isCloaked.Value;
            }
        }

        public bool IsEmpty
        {
            get
            {
                if (!_isEmpty.HasValue)
                    _isEmpty = (bool?)_slimItem.Attribute("isEmpty") ?? true;

                return _isEmpty.Value;
            }
        }

        public bool IsTryingToJamMe { get; private set; }

        public bool IsJammingMe { get; private set; }

        public bool IsKeepingAtRange => IsApproachingOrKeptAtRange;

        public bool IsNeutralizingMe { get; private set; }

        //public bool IsNpc => (bool?)PySharp.Import("util").Call("IsNPC", OwnerId) ?? false;

        public bool IsNpc => IsNPCByBracketType;

        public bool IsOrbitedByActiveShip => DirectEve.ActiveShip.Entity.FollowId == Id
                                             && DirectEve.ActiveShip.Entity.IsOrbiting
                                             && DirectEve.GetEntityById(Id) != null;

        public bool IsOrbiting => Mode == 4;

        public bool IsPlayer => CharId > 0 && !IsNPCByBracketType;

        public bool IsSensorDampeningMe { get; private set; }

        public bool IsTarget { get; internal set; }

        public bool IsTargetedBy { get; internal set; }

        public bool IsTargeting { get; internal set; }

        public bool IsTargetPaintingMe { get; private set; }

        public bool IsTrackingDisruptingMe { get; private set; }

        /// <summary>
        ///     Is it a valid entity?
        /// </summary>
        public bool IsValid => _ball.IsValid && _slimItem.IsValid && Id > 0 && !DirectEve.IsTargetBeingRemoved(Id);

        public bool IsWarpingByMode => Mode == 3;

        public bool IsWarping => IsWarpingByMode && Velocity >= (MaxVelocity * 0.75);
        public bool IsInitalizingWarp => IsWarpingByMode && Velocity < (MaxVelocity * 0.75);

        public bool IsWarpScramblingMe { get; private set; }

        public bool IsWarpDisruptingMe { get; private set; }

        public bool IsWarpScramblingOrDisruptingMe => IsWarpScramblingMe || IsWarpDisruptingMe;

        public bool IsWebbingMe { get; private set; }

        public void Dump()
        {
            DirectEve.Log($"{_ball.LogObject()}");
            DirectEve.Log($"{_slimItem.LogObject()}");
            DirectEve.Log($"{_ball.Attribute("model").LogObject()}");
        }



        public int Mode
        {
            get
            {
                if (!_mode.HasValue)
                    _mode = (int)_ball.Attribute("mode");

                return _mode.Value;
            }
        }

        //0 = idle
        //1 = attacking
        //4 = returning to bay

        private int? _droneState;
        /// <summary>
        /// 0 = idle, 1 = attacking, 4 = returning to bay
        /// </summary>
        public int? DroneState
        {
            get
            {
                if (_droneState == null)
                {
                    // if we recently started to return that drone, keep the state (4) for 2 seconds
                    if (DirectActiveShip._droneReturnModeCache.TryGetValue(Id, out var dt) &&
                        dt.AddSeconds(2) > DateTime.UtcNow)
                        return 4;

                    var stateByDroneId = DirectEve.GetLocalSvc("michelle")["_Michelle__bp"]["stateByDroneID"];
                    if (stateByDroneId.IsValid)
                    {
                        var item = stateByDroneId.DictionaryItem(this.Id);
                        if (item.IsValid)
                        {
                            _droneState = item["activityState"].ToInt();
                        }
                    }
                }

                return _droneState;
            }
        }

        private double? _modelBoundingSphereCenterX;
        private double? _modelBoundingSphereCenterY;
        private double? _modelBoundingSphereCenterZ;

        private double? _modelBoundingSphereRadius;

        private Vec3? _modelBoundingSphereCenter;

        public Vec3 ModelBoundingSphereCenter => _modelBoundingSphereCenter ??= new Vec3(
            ModelBoundingSphereCenterX.Value,
            ModelBoundingSphereCenterY.Value, ModelBoundingSphereCenterZ.Value);

        public double? ModelBoundingSphereCenterX => _modelBoundingSphereCenterX ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(0).ToDouble();

        public double? ModelBoundingSphereCenterY => _modelBoundingSphereCenterY ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(1).ToDouble();

        public double? ModelBoundingSphereCenterZ => _modelBoundingSphereCenterZ ??=
            Ball["model"]["boundingSphereCenter"].GetItemAt(2).ToDouble();

        public double? ModelBoundingSphereRadius => _modelBoundingSphereRadius ??=
            Ball["model"]["boundingSphereRadius"].ToDouble();

        private Vec3? _screenPos;

        private float? _modelScale;
        public float ModelScale => _modelScale ??= Ball["model"]["modelScale"].ToFloat();

        private float? _ballRadius;
        public double BallRadius => _ballRadius ??= Ball["radius"].ToFloat();

        public bool Display => Ball["model"]["display"].ToBool();

        public void SetDisplay(bool val)
        {
            Ball["model"].SetAttribute("display", val);
        }

        public Vec3? ScreenPos
        {
            get
            {
                if (_screenPos == null)
                {
                    var ballPos = DirectEve.SceneManager.CamUtil.Call("_GetBallPosition", this.Ball);
                    if (!ballPos.IsValid)
                    {
                        DirectEve.Log("BallPos not valid.");
                        return null;
                    }

                    if (!DirectEve.SceneManager.IsSeenByCamera(ballPos))
                        return null;

                    var viewPort = PySharp.Import("trinity")["device"]["viewport"];
                    if (!viewPort.IsValid)
                    {
                        DirectEve.Log("Trinity.device.viewport not valid.");
                        return null;
                    }

                    var viewPortTuple = PyObject.CreateTuple(PySharp, viewPort["x"].ToInt(), viewPort["y"].ToInt(),
                        viewPort["width"].ToInt(), viewPort["height"].ToInt(), viewPort["minZ"].ToInt(),
                        viewPort["maxZ"].ToInt());

                    if (!viewPortTuple.IsValid)
                    {
                        DirectEve.Log("ViewPortTuple is not valid.");
                        return null;
                    }

                    var geo2 = DirectEve.SceneManager.Geo2;
                    if (!geo2.IsValid)
                        return null;

                    var cam = DirectEve.SceneManager.Camera;
                    var matrixIdent = DirectEve.SceneManager.MatrixIdentity;

                    var sm = DirectEve.SceneManager;

                    //DirectEve.Log(ballPos.LogObject());
                    //DirectEve.Log(viewPortTuple.LogObject());
                    //DirectEve.Log(sm.ProjectionMatrix.LogObject());
                    //DirectEve.Log(sm.ViewMatrix.LogObject());
                    //DirectEve.Log(sm.MatrixIdentity.LogObject());

                    var res = geo2.Call("Vec3Project", ballPos, viewPortTuple, sm.ProjectionMatrix, sm.ViewMatrix,
                        sm.MatrixIdentity);

                    if (!res.IsValid)
                    {
                        DirectEve.Log("Result not valid.");
                        return null;
                    }

                    var r = res.ToList();

                    _screenPos = new Vec3(r[0].ToInt(), r[1].ToInt(), r[2].ToInt());
                }

                return _screenPos.Value;
            }
        }


        public string Name
        {
            get
            {
                if (string.IsNullOrEmpty(_name))
                    _name = (string)PySharp.Import("eve.client.script.ui.util.uix").Call("GetSlimItemName", _slimItem);

                return _name;
            }
        }

        public double NpcRemoteArmorRepairChance
        {
            get
            {
                if (!_npcRemoteArmorRepairChance.HasValue)
                    _npcRemoteArmorRepairChance = (double)_ball.Attribute("npcRemoteArmorRepairChance");

                return _npcRemoteArmorRepairChance.Value;
            }
        }

        public int OwnerId
        {
            get
            {
                if (!_ownerId.HasValue)
                    _ownerId = (int)_slimItem.Attribute("ownerID");

                return _ownerId.Value;
            }
        }

        public double ShieldPct
        {
            get
            {
                if (!_shieldPct.HasValue)
                    GetDamageState();

                return _shieldPct ?? 0;
            }
        }

        public double StructurePct
        {
            get
            {
                if (!_structurePct.HasValue)
                    GetDamageState();

                return _structurePct ?? 0;
            }
        }

        public double TransversalVelocity
        {
            get
            {
                if (_transversalVelocity == null)
                {
                    var myBall = DirectEve.ActiveShip.Entity;
                    var CombinedVelocity = new List<double>() { Vx - myBall.Vx, Vy - myBall.Vy, Vz - myBall.Vz };
                    var Radius = new List<double>() { X - myBall.X, Y - myBall.Y, Z - myBall.Z };
                    var RadiusVectorNormalized = Radius.Select(i =>
                            i / Math.Sqrt(Radius[0] * Radius[0] + Radius[1] * Radius[1] + Radius[2] * Radius[2]))
                        .ToList();
                    var RadialVelocity = CombinedVelocity[0] * RadiusVectorNormalized[0] +
                                         CombinedVelocity[1] * RadiusVectorNormalized[1] +
                                         CombinedVelocity[2] * RadiusVectorNormalized[2];
                    var ScaledRadiusVector = RadiusVectorNormalized.Select(i => i * RadialVelocity).ToList();
                    _transversalVelocity =
                        (double)
                        Math.Sqrt((CombinedVelocity[0] - ScaledRadiusVector[0]) *
                                  (CombinedVelocity[0] - ScaledRadiusVector[0]) +
                                  (CombinedVelocity[1] - ScaledRadiusVector[1]) *
                                  (CombinedVelocity[1] - ScaledRadiusVector[1]) +
                                  (CombinedVelocity[2] - ScaledRadiusVector[2]) *
                                  (CombinedVelocity[2] - ScaledRadiusVector[2]));
                }

                return _transversalVelocity.Value;
            }
        }

        public Vec3 Position => new Vec3(X, Y, Z);



        public double Velocity
        {
            get
            {
                if (_velocity == null)
                    _velocity = (double)_ball
                        .Call("GetVectorDotAt", PySharp.Import("blue").Attribute("os").Call("GetSimTime"))
                        .Call("Length");

                return _velocity.Value;
            }
        }

        /// <summary>
        /// This is the current direction vector of the ship
        /// </summary>
        /// <returns></returns>
        public Vec3? GetDirectionVector()
        {
            var simTime = PySharp.Import("blue").Attribute("os").Call("GetSimTime");
            if (simTime.IsValid)
            {
                var ret = _ball.Call("GetQuaternionAt", simTime);
                if (ret.IsValid)
                {
                    Quaternion q = new Quaternion(ret["x"].ToFloat(), ret["y"].ToFloat(), ret["z"].ToFloat(),
                        ret["w"].ToFloat());
                    Vec3 v3 = new Vec3(0, 0, 1);
                    var res = q * v3;
                    return res;
                }
            }

            return null;
        }

        public void SetDisplayName(string html)
        {
            var bracket =
                ESCache.Instance.DirectEve.PySharp.Import("__builtin__")["sm"]["services"].DictionaryItem("bracket")
                    ["brackets"].DictionaryItem(Id);
            if (bracket.IsValid)
            {
                bracket.SetAttribute<string>("_displayName", html);
                //var name = bracket["_displayName"].ToUnicodeString();
                //bracket.SetAttribute<bool>("overrideLabel", false);
                //bracket.Call("SetOrder", 0);
                //bracket.Call("ShowLabel");
                //Log($"DisplayName [{bracket.LogObject()}]");
                //bracket.SetAttribute<bool>("overrideLabel", true);
                //bracket.SetAttribute<bool>("showLabel", true);
            }
        }

        /// <summary>
        /// This is the directional vector the ship aligns to
        /// </summary>
        /// <returns></returns>
        public Vec3 GetDirectionVectorFinal()
        {
            var x = this.GotoX - X;
            var y = this.GotoY - Y;
            var z = this.GotoZ - Z;
            return new Vec3(x, y, z).Normalize();
        }

        public double Vx
        {
            get
            {
                if (!_vx.HasValue)
                    _vx = (double)_ball.Attribute("vx");

                return _vx.Value;
            }
        }

        public double Vy
        {
            get
            {
                if (!_vy.HasValue)
                    _vy = (double)_ball.Attribute("vy");

                return _vy.Value;
            }
        }

        public double Vz
        {
            get
            {
                if (!_vz.HasValue)
                    _vz = (double)_ball.Attribute("vz");

                return _vz.Value;
            }
        }

        public double? WormholeAge
        {
            get
            {
                if (_wormholeAge == null)
                    _wormholeAge = (double)_slimItem.Attribute("wormholeAge");

                return _wormholeAge.Value;
            }
        }

        public double? WormholeSize
        {
            get
            {
                if (_wormholeSize == null)
                    _wormholeSize = (double)_slimItem.Attribute("wormholeSize");

                return _wormholeSize.Value;
            }
        }

        public double X
        {
            get
            {
                if (!_x.HasValue)
                    _x = (double)_ball.Attribute("x");

                return _x.Value;
            }
        }

        public double Y
        {
            get
            {
                if (!_y.HasValue)
                    _y = (double)_ball.Attribute("y");

                return _y.Value;
            }
        }

        public double Z
        {
            get
            {
                if (!_z.HasValue)
                    _z = (double)_ball.Attribute("z");

                return _z.Value;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Abandons all wrecks. Make sure to only call this on a wreck.
        /// </summary>
        /// <returns>false if entity is not a wreck</returns>
        public bool AbandonAllWrecks()
        {
            if (GroupId != (int)DirectEve.Const.GroupWreck)
                return false;

            var AbandonAllLoot = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions")
                .Attribute("AbandonAllLoot");
            return DirectEve.ThreadedCall(AbandonAllLoot, Id);
        }

        /// <summary>
        ///     Activate (Acceleration Gates only)
        /// </summary>
        /// <returns></returns>
        public bool Activate()
        {
            if (!IsValid)
                return false;

            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(8000, 12000))
                return false;

            //DirectEvent
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Activating."));

            var DockOrJumpOrActivateGate = PySharp
                .Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            //DirectSession.SetSessionNextSessionReady(); Not required if used ONLY for acceleration gates
            return DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id);
        }

        /// <summary>
        ///     Activate Abyssal Gate
        /// </summary>
        /// <returns></returns>
        public bool ActivateAbyssalEntranceAccelerationGate()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            DirectSession.SetSessionNextSessionReady();
            if (DirectEve.Const["typeAbyssEntranceGate"].ToInt() != TypeId)
            {
                DirectEve.Log("Warning: ActivateAbyssalAccelerationGate typeId missmatch.");
                return false;
            }

            if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalEntranceAccelerationGate", Id))
            {
                //DirectEvent
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping."));
                DirectSession.SetSessionNextSessionReady();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Activate Abyssal Gate
        /// </summary>
        /// <returns></returns>
        public bool ActivateAbyssalAccelerationGate()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            DirectSession.SetSessionNextSessionReady();
            if (DirectEve.Const["typeAbyssEncounterGate"].ToInt() != TypeId)
            {
                DirectEve.Log("Warning: ActivateAbyssalAccelerationGate typeId missmatch.");
                return false;
            }

            if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalAccelerationGate", Id))
            {
                //DirectEvent
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping."));
                DirectSession.SetSessionNextSessionReady();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Activate Abyssal Gate
        /// </summary>
        /// <returns></returns>
        public bool ActivateAbyssalEndGate()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            DirectSession.SetSessionNextSessionReady();
            if (DirectEve.Const["typeAbyssExitGate"].ToInt() != TypeId)
            {
                DirectEve.Log("Warning: ActivateAbyssalEndGate typeId missmatch.");
                return false;
            }

            if (DirectEve.ThreadedLocalSvcCall("menu", "ActivateAbyssalEndGate", Id))
            {
                //DirectEvent
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping."));
                DirectSession.SetSessionNextSessionReady();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Warp to target
        /// </summary>
        /// <returns></returns>
        public bool AlignTo()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            DirectEve.Log("AlignTo called.");

            if (IsValid)
                return DirectEve.ThreadedLocalSvcCall("menu", "AlignTo", Id);
            return false;
        }

        /// <summary>
        ///     Approach target
        /// </summary>
        /// <returns></returns>
        public bool Approach()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(3000, 6000))
                return false;

            //if (IsApproachedOrKeptAtRangeByActiveShip)
            //    return true;

            if (IsValid)
            {
                DirectEve.Log("Approach called.");
                var Approach = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                    .Attribute("Approach");
                return DirectEve.ThreadedCall(Approach, Id);
            }

            return false;
        }

        /// <summary>
        ///     Board this ship
        /// </summary>
        /// <returns>false if entity is player or out of range</returns>
        public bool BoardShip()
        {
            if (IsPlayer)
                return false;

            if (Distance > 6500)
                return false;

            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            var Board = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Board");
            return DirectEve.ThreadedCall(Board, Id);
        }

        /// <summary>
        ///     Warp to target and dock
        /// </summary>
        /// <returns></returns>
        public bool Dock()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (!IsValid)
                return false;

            if (!DirectEve.DWM.ActivateWindow(typeof(DirectSelectedItemWindow), true))
                return false;

            if (DirectEve.Me.WeaponsTimerExists)
                return false;

            //DirectEvent
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Docking."));

            var DockOrJumpOrActivateGate = PySharp
                .Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            //DirectEve.Log("Dbg: Dock");

            DirectSession.SetSessionNextSessionReady();

            return DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id);
        }

        public double GetBounty()
        {
            var bountyRow = DirectEve.GetLocalSvc("godma")
                .Call("GetType", TypeId)
                .Attribute("displayAttributes")
                .ToList()
                .FirstOrDefault(i =>
                    i.Attribute("attributeID").ToInt() == (int)DirectEve.Const.AttributeEntityKillBounty);
            if (bountyRow == null || !bountyRow.IsValid)
                return 0;

            return (double)bountyRow.Attribute("value");
        }

        public string GetResistInfo()
        {
            return $"Name [{Name}] ShieldHitPoints [{TotalShield}] " +
                   $" ArmorHitPoints [{TotalArmor}]" +
                   $" StructureHitPoints[{TotalStructure}]" +
                   $" Shield-Res-EM/EXP/KIN/TRM [{ShieldResistanceEM}%," +
                   $" {ShieldResistanceExplosion}%," +
                   $" {ShieldResistanceKinetic}%," +
                   $" {ShieldResistanceThermal}%]" +
                   $" Armor-Res-EM/EXP/KIN/TRM [{ArmorResistanceEM}%," +
                   $" {ArmorResistanceExplosion}%," +
                   $" {ArmorResistanceKinetic}%," +
                   $" {ArmorResistanceThermal}%]";
        }

        /// <summary>
        ///     Jump (Stargates only)
        /// </summary>
        /// <returns></returns>
        public bool Jump()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (!DirectEve.Session.IsInSpace)
                return false;

            if (Distance >= 2500)
                return false;

            if (!IsValid)
                return false;

            if (!DirectEve.DWM.ActivateWindow(typeof(DirectOverviewWindow), true))
                return false;

            if (DirectEve.Me.WeaponsTimerExists)
                return false;

            //DirectEvent
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DOCK_JUMP_ACTIVATE, "Jumping."));

            var DockOrJumpOrActivateGate = PySharp
                .Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("DockOrJumpOrActivateGate");

            DirectSession.SetSessionNextSessionReady();
            return DirectEve.ThreadedCall(DockOrJumpOrActivateGate, Id);
        }

        /// <summary>
        ///     Jump Wormhole (Wormholes only)
        /// </summary>
        /// <returns></returns>
        public bool JumpWormhole()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            return DirectEve.ThreadedLocalSvcCall("menu", "EnterWormhole", Id);
        }

        private static int? _prevKeepAtRangeDist;

        /// <summary>
        ///     KeepAtRange target
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool KeepAtRange(int range)
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(8000, 10000))
                return false;

            if (IsApproachedOrKeptAtRangeByActiveShip && _prevKeepAtRangeDist.HasValue && _prevKeepAtRangeDist == range)
                return true;

            _prevKeepAtRangeDist = range;

            if (range < 50) // min keep at range
                range = 50;

            DirectEve.Log("KeepAtRange called.");

            var KeepAtRange = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("KeepAtRange");
            if (IsValid)
                return DirectEve.ThreadedCall(KeepAtRange, Id, range);
            return false;
        }

        /// <summary>
        ///     Lock target
        /// </summary>
        /// <returns></returns>
        public bool LockTarget()
        {
            // It's already a target!
            if (IsTarget || IsTargeting)
                return false;

            // We can't target this entity yet!
            if (!DirectEve.CanTarget(Id))
                return false;

            // Set target timer
            DirectEve.SetTargetTimer(Id);


            if (!DirectEve.DWM.ActivateWindow(typeof(DirectOverviewWindow), true))
                return false;

            if (IsValid)
            {
                //DirectEvent
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCK_TARGET, "Targeting [" + Id + "]"));
                return DirectEve.ThreadedLocalSvcCall("menu", "LockTarget", Id);
            }

            return false;
        }

        /// <summary>
        ///     Make this your active target
        /// </summary>
        /// <returns></returns>
        public bool MakeActiveTarget(bool threaded = true)
        {
            if (!IsTarget)
                return false;

            if (DirectEve.IsTargetBeingRemoved(Id))
                return false;

            if (HasExploded)
                return false;

            if (HasReleased)
                return false;

            if (IsActiveTarget)
                return true;

            if (!IsValid)
                return false;

            // Even though we uthread the thing, expect it to be instant
            var currentActiveTarget = DirectEve.Entities.FirstOrDefault(t => t.IsActiveTarget);
            if (currentActiveTarget != null)
                currentActiveTarget.IsActiveTarget = false;

            this.IsActiveTarget = true;

            if (threaded)
            {
                // Switch active targets
                var activeTarget = PySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
                return IsActiveTarget = DirectEve.ThreadedLocalSvcCall("stateSvc", "SetState", Id, activeTarget, 1);
            }
            else
            {
                // Switch active targets
                var activeTarget = PySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
                var stateSvc = DirectEve.GetLocalSvc("stateSvc");
                stateSvc.Call("SetState", Id, activeTarget, 1);
                return true;
            }
        }

        public bool MoveTo()
        {
            if (!DirectEve.Interval(1800, 2500))
                return false;

            return DirectEve.ActiveShip.MoveTo(this);
        }


        /// <summary>
        ///     Open cargo window (only valid on containers in space, or own ship)
        /// </summary>
        /// <returns></returns>
        public bool OpenCargo()
        {
            if (!DirectEve.Interval(1300, 2500))
                return false;

            if (!DirectEve.DWM.ActivateWindow(typeof(DirectSelectedItemWindow), true))
                return false;

            if (IsValid)
                return DirectEve.ThreadedLocalSvcCall("menu", "OpenCargo", Id);
            return false;
        }


        public bool Scoop()
        {
            if (this.GroupId == 1250)
            {
                var call = DirectEve.GetLocalSvc("menu")["Scoop"];
                if (call.IsValid)
                {
                    DirectEve.ThreadedCall(call, this.Id, this.TypeId);
                }

                return true;
            }

            DirectEve.Log("Couldnt scoop for self. Probably wrong type.");
            return false;
        }

        // align: mode 0 / no following entity
        // stop: mode 2 / no following entity
        // warp: mode 3 / ??
        // approach: 1 / has a following entity
        // keep at range: 1 / has a following entity
        // orbit: 4 / has a following entity

        /// <summary>
        ///     Orbit target at 5000m
        /// </summary>
        /// <returns></returns>
        public bool Orbit()
        {
            if (!DirectEve.Interval(4000, 6000))
                return false;

            return Orbit(5000);
        }

        private static int? _prevOrbitDist;

        /// <summary>
        ///     Orbit target
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public bool Orbit(int range)
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (IsOrbitedByActiveShip && _prevOrbitDist.HasValue && range == _prevOrbitDist)
                return true;

            _prevOrbitDist = range;

            var Orbit = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("Orbit");

            DirectEve.Log("Orbit called.");

            if (IsValid)
                return DirectEve.ThreadedCall(Orbit, Id, range);
            return false;
        }

        /// <summary>
        ///     Unlock target
        /// </summary>
        /// <returns></returns>
        public bool UnlockTarget()
        {
            // Its not a target, why are you unlocking?!?!
            if (!IsTarget)
                return false;

            if (!DirectEve.DWM.ActivateWindow(typeof(DirectOverviewWindow), true))
                return false;

            // Clear target information
            if (IsValid)
            {
                DirectEve.ClearTargetTimer(Id);
                return DirectEve.ThreadedLocalSvcCall("menu", "UnlockTarget", Id);
            }

            return false;
        }

        /// <summary>
        ///     Warp fleet to target, make sure you have the fleetposition to warp the fleet
        /// </summary>
        /// <returns></returns>
        public bool WarpFleetTo()
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (DirectEve.Session.FleetId == null)
                return false;

            var myDirectFleetMember =
                DirectEve.FleetMembers.FirstOrDefault(i => i.CharacterId == DirectEve.Session.CharacterId);
            if (myDirectFleetMember.Role == DirectFleetMember.FleetRole.Member)
                return false;

            return DirectEve.ThreadedLocalSvcCall("menu", "WarpFleet", Id);
        }

        /// <summary>
        ///     Warp fleet to target at range, make sure you have the fleetposition to warp the fleet
        /// </summary>
        /// <returns></returns>
        public bool WarpFleetTo(double range)
        {
            if (!DirectEve.ActiveShip.CanWeMove)
                return false;

            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (DirectEve.Session.FleetId == null)
                return false;

            var myDirectFleetMember =
                DirectEve.FleetMembers.FirstOrDefault(i => i.CharacterId == DirectEve.Session.CharacterId);
            if (myDirectFleetMember.Role == DirectFleetMember.FleetRole.Member)
                return false;

            return DirectEve.ThreadedLocalSvcCall("menu", "WarpFleet", Id, range);
        }


        public bool IsJammingEntity => GetDmgEffects().ContainsKey(6695) ||
                                       GetDmgEffectsByGuid().ContainsKey("effects.ElectronicAttributeModifyTarget");

        public bool IsNeutingEntity => GetDmgEffects().ContainsKey(6756) ||
                                       GetDmgEffectsByGuid().ContainsKey("effects.EnergyDestabilization");

        public bool IsWarpScramblingEntity => GetDmgEffects().ContainsKey(6745) ||
                                              GetDmgEffectsByGuid().ContainsKey("effects.WarpScramble");

        public bool IsWarpDisruptingEntity => GetDmgEffectsByGuid().ContainsKey("effects.WarpDisrupt");

        public bool IsWebbingEntity => GetDmgEffects().ContainsKey(6743) ||
                                       GetDmgEffectsByGuid().ContainsKey("effects.ModifyTargetSpeed");

        public bool IsTargetPaintingEntity => GetDmgEffects().ContainsKey(6754) ||
                                              GetDmgEffectsByGuid().ContainsKey("effects.TargetPaint");

        public bool IsRemoteArmorRepairingEntity => GetDmgEffects().ContainsKey(6741) ||
                                                    GetDmgEffectsByGuid().ContainsKey("effects.RemoteArmourRepair");

        public bool IsRemoteShieldRepairingEntity => GetDmgEffects().ContainsKey(6742) ||
                                                     GetDmgEffectsByGuid().ContainsKey("effects.ShieldTransfer");

        public bool IsRemoteRepairEntity => IsRemoteArmorRepairingEntity || IsRemoteShieldRepairingEntity;

        public bool IsSensorDampeningEntity => GetDmgEffects().ContainsKey(6755) ||
                                               GetDmgEffectsByGuid().ContainsKey("effects.SensorDampening");

        public bool IsTrackingDisruptingEntity => GetDmgEffects().ContainsKey(6747) ||
                                                  GetDmgEffectsByGuid().ContainsKey("effects.TrackingDisruption");

        public bool IsGuidanceDisruptingEntity => GetDmgEffects().ContainsKey(6746) ||
                                                  GetDmgEffectsByGuid()
                                                      .ContainsKey("effects.ElectronicAttributeModifyTarget");

        public bool IsHeavyDeepsIntegrating => GetDmgEffects().ContainsKey(6995); // targetDisintegratorAttack


        /// <summary>
        ///     Warp to target at range
        /// </summary>
        /// <returns></returns>
        public bool WarpTo(double range = 0, bool ignoreSecurityChecks = false)
        {
            if (!IsValid)
                return false;

            if (ignoreSecurityChecks == false)
            {
                if (!DirectEve.Session.IsInSpace)
                    return false;

                if (!DirectEve.ActiveShip.CanWeMove)
                    return false;

                if (!DirectEve.Interval(4000, 6000))
                    return false;
            }

            if (Distance > (long)Distances.HalfOfALightYearInAU)
                return false;

            if (Distance <= (int)Distances.WarptoDistance)
                return false;


            if (!DirectEve.DWM.ActivateWindow(typeof(DirectOverviewWindow), true))
                return false;

            //DirectEvent
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.WARP, "Warping."));
            var WarpToItem = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.movementFunctions")
                .Attribute("WarpToItem");
            return range == 0 ? DirectEve.ThreadedCall(WarpToItem, Id) : DirectEve.ThreadedCall(WarpToItem, Id, range);
        }

        private List<float> _warpRanges = new List<float>() {
                            //10_000,
                            20_000,
                            30_000,
                            50_000,
                            70_000,
                            100_000,
                        };

        public bool WarpToAtRandomRange()
        {
            var randomRange = ListExtensions.Random(_warpRanges);
            if (randomRange < 0 || randomRange > 100_000)
                randomRange = 0;

            var res = WarpTo(randomRange);

            if (res)
                DirectEve.Log($"Warping at range [{randomRange}]");

            return res;
        }

        internal static long GetBallparkCount(DirectEve directEve)
        {
            var michelle = directEve.GetLocalSvc("michelle");
            var ballpark = michelle["_Michelle__bp"];
            var bpReady = michelle["bpReady"].ToBool();
            if (ballpark.IsValid && bpReady && ballpark.Attribute("balls").IsValid)
                return ballpark.Attribute("balls").Call("keys").Size();
            return 0;
        }

        private bool? _isTargetingOurDrones;

        /// <summary>
        /// Any entity that has the follow id of one of our active drones
        /// </summary>
        public bool HasTheFollowIdOfAnyOfOurActiveDrones
        {
            get
            {
                if (_isTargetingOurDrones == null)
                {
                    var droneIds = DirectEve.ActiveDrones.Select(e => e.Id);
                    var followId = this.FollowId;
                    _isTargetingOurDrones = droneIds.Contains(followId);
                }

                return _isTargetingOurDrones.Value;
            }
        }

        private bool? _isFollowedByAnyOfOurActiveDrones;
        public bool IsFollowedByAnyOfOurActiveDrones
        {
            get
            {
                if (_isFollowedByAnyOfOurActiveDrones == null)
                {
                    _isFollowedByAnyOfOurActiveDrones = DirectEve.ActiveDrones.Any(e => e.FollowId == this.FollowId);
                }
                return _isFollowedByAnyOfOurActiveDrones.Value;
            }
        }

        public bool IsYellowBoxing => !IsAttacking && IsTargetedBy;

        internal static Dictionary<long, DirectEntity> GetEntities(DirectEve directEve)
        {
            var pySharp = directEve.PySharp;
            var entitiesById = new Dictionary<long, DirectEntity>();

            // Used by various loops, etc
            var ballpark = directEve.GetLocalSvc("michelle").Call("GetBallpark");
            var balls = ballpark.Attribute("balls").Call("keys").ToList<long>();
            var target = directEve.GetLocalSvc("target");
            var targetsBeingRemoved = target.Attribute("deadShipsBeingRemoved");

            if (!targetsBeingRemoved.IsValid)
            {
                directEve.Log($"Target.deadShipsBeingRemoved is not valid!");
                return entitiesById;
            }

            var targetsBeingRemovedDict = targetsBeingRemoved.ToList<long>().ToDictionary(x => x, y => true);
            foreach (var id in balls)
            {
                if (id < 0)
                    continue;

                // Get slim item
                var slimItem = ballpark.Call("GetInvItem", id);

                // Get ball
                var ball = ballpark.Call("GetBall", id);

                // Create the entity
                if (slimItem.IsValid && ball.IsValid
                                     && !targetsBeingRemovedDict.ContainsKey(id)
                                     && !directEve.GetTargetsBeingRemoved().ContainsKey(id)
                                     && !(bool)ball.Attribute("exploded")
                                     && !(bool)ball.Attribute("released")
                                     && ball.Attribute("ballpark").IsValid)

                    entitiesById[id] = new DirectEntity(directEve, ballpark, ball, slimItem, id);
                //entitiesById[id].SetDisplay(false); // TODO: configuration option: this hides the entitites, may increase performance
            }

            // Mark active target
            var activeTarget = pySharp.Import("eve.client.script.parklife.states").Attribute("activeTarget");
            var activeTargetId = (long)directEve.GetLocalSvc("stateSvc").Call("GetExclState", activeTarget);
            if (entitiesById.TryGetValue(activeTargetId, out var entity))
                entity.IsActiveTarget = true;


            var targets = target.Attribute("targetsByID").ToDictionary().Keys;
            foreach (var targetId in targets)
            {
                if (!entitiesById.TryGetValue((long)targetId, out entity))
                    continue;

                entity.IsTarget = true;
            }

            var targeting = target.Attribute("targeting").ToDictionary<long>().Keys;
            foreach (var targetId in targeting)
            {
                if (!entitiesById.TryGetValue(targetId, out entity))
                    continue;

                entity.IsTargeting = true;
            }

            var targetedBy = target.Attribute("targetedBy").ToList<long>();
            foreach (var targetId in targetedBy)
            {
                if (!entitiesById.TryGetValue(targetId, out entity))
                    continue;

                entity.IsTargetedBy = true;
            }

            var attackers = directEve.GetLocalSvc("tactical").Attribute("attackers").ToDictionary<long>();
            foreach (var attacker in attackers)
            {
                if (!entitiesById.TryGetValue(attacker.Key, out entity))
                    continue;

                if (entity.IsTargetedBy)
                {
                    //entity.IsAttacking = true;

                    var attacks = attacker.Value.ToList();
                    foreach (var attack in attacks.Select(a => (string)a.GetItemAt(1)))
                    {
                        entity.IsWarpScramblingMe |= attack == "effects.WarpScramble";
                        entity.IsWebbingMe |= attack == "effects.ModifyTargetSpeed";
                        entity.IsWarpDisruptingMe |= attack == "effects.WarpDisrupt";
                        entity.Attacks.Add(attack);
                    }
                }
            }

            var jammers = directEve.GetLocalSvc("tactical").Attribute("jammers").ToDictionary<long>();
            foreach (var jammer in jammers)
            {
                if (!entitiesById.TryGetValue(jammer.Key, out entity))
                    continue;

                var ews = jammer.Value.ToDictionary<string>().Keys;
                foreach (var ew in ews)
                {
                    entity.IsNeutralizingMe |= ew == "ewEnergyNeut";
                    entity.IsTryingToJamMe |= ew == "electronic";
                    entity.IsSensorDampeningMe |= ew == "ewRemoteSensorDamp";
                    entity.IsTargetPaintingMe |= ew == "ewTargetPaint";
                    entity.IsTrackingDisruptingMe |= ew == "ewTrackingDisrupt";
                    entity.ElectronicWarfare.Add(ew);
                }
            }

            // Find active jammers
            var godma = directEve.GetLocalSvc("godma");
            if (godma.IsValid)
            {
                var activeJams = godma.Attribute("activeJams").ToList();
                if (activeJams.Any())
                {
                    foreach (var jam in activeJams)
                    {
                        var jamAttr = jam.ToList();
                        if (jamAttr[3].ToUnicodeString() == "electronic")
                        {
                            var sourceEntityId = jamAttr[0].ToLong();
                            if (!entitiesById.TryGetValue((long)sourceEntityId, out entity))
                                continue;
                            entity.IsJammingMe = true;
                        }
                    }
                }
            }

            return entitiesById;
        }


        /// <summary>
        /// range 0 .. 1.0
        /// </summary>
        internal void GetDamageState()
        {
            _shieldPct = 0;
            _armorPct = 0;
            _structurePct = 0;

            // Get damage state properties
            var damageState = _ballpark.Call("GetDamageState", Id).ToList();
            if ((damageState.Count == 3) ^ (damageState.Count == 5))
            {
                _shieldPct = (double)damageState[0];
                _armorPct = (double)damageState[1];
                _structurePct = (double)damageState[2];
            }

            if (DirectEve._entityHealthPercOverrides.TryGetValue(this.Id, out var res))
            {
                _shieldPct = res.Item1;
                _armorPct = res.Item2;
                _structurePct = res.Item3;
            }
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFromTurrets()
        {
            var invType = ESCache.Instance.DirectEve.GetInvType(this.TypeId);

            if (invType == null)
                return (0, 0, 0, 0);

            if (invType.RateOfFire <= 0)
                return (0, 0, 0, 0);

            var myShip = DirectEve.ActiveShip.Entity;

            if (myShip == null)
                return (0, 0, 0, 0);


            var mySig = DirectEve.ActiveShip.GetSignatureRadius();

            var optimalSigRadius = invType.OptimalSigRadius == 0 ? 40_000 : invType.OptimalSigRadius;
            var falloff = invType.AccuracyFalloff == 0 ? 1 : invType.AccuracyFalloff;
            var tracking = invType.TurretTracking == 0 ? 1000 : invType.TurretTracking;

            double angularTerm = Math.Pow(((this.AngularVelocity * optimalSigRadius) / (tracking * mySig)), 2);
            double distanceTerm = Math.Pow(Math.Max(0, this.Distance - invType.OptimalRange) / falloff, 2);

            //Console.WriteLine($"angularTerm {angularTerm} distanceTerm {distanceTerm}");

            double hitChance = Math.Pow(0.5d, (angularTerm + distanceTerm));

            //Console.WriteLine($"TypeName {this.TypeName}  mySig {mySig} HitChance {hitChance} this.Distance {this.Distance} AngularVelocity {this.AngularVelocity} TurretTracking {invType.TurretTracking} OptimalRange {invType.OptimalRange} AccuracyFalloff {invType.AccuracyFalloff}");

            var normalizedTurretDamage = 0.5 *
                                        Math.Min(
                                            Math.Pow(hitChance, 2) + 0.98 * hitChance + 0.0501,
                                            6 * hitChance
                                        );

            // Desintegrator bonus // TODO: we need to be able to retrieve the "live" value
            if (DamageMultiplierBonusMax > 0)
                normalizedTurretDamage *= 1 + DamageMultiplierBonusMax;

            var dmgMulti = invType.TurretDamageMultiplier;

            var rateOfFire = invType.RateOfFire;

            var emDmg = invType.DamageEm;
            var expDmg = invType.DamageExplosive;
            var kinDmg = invType.Damagekinetic;
            var theDmg = invType.DamageThermal;


            var em = (emDmg * dmgMulti);
            var exp = (expDmg * dmgMulti);
            var kin = (kinDmg * dmgMulti);
            var the = (theDmg * dmgMulti);

            var emDps = normalizedTurretDamage * (em / (rateOfFire / 1000));
            var expDps = normalizedTurretDamage * (exp / (rateOfFire / 1000));
            var kinDps = normalizedTurretDamage * (kin / (rateOfFire / 1000));
            var theDps = normalizedTurretDamage * (the / (rateOfFire / 1000));

            return (emDps, expDps, kinDps, theDps);
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFromMissiles()
        {

            if (MissileEntityAoeVelocityMultiplier <= 0)
                return (0, 0, 0, 0);

            var missileInvType = ESCache.Instance.DirectEve.GetInvType((int)EntityMissileTypeID);

            if (missileInvType == null)
                return (0, 0, 0, 0);

            var myShip = DirectEve.ActiveShip.Entity;

            if (myShip == null)
                return (0, 0, 0, 0);

            // Damage formula D * min(1, S/E, (SVM/EVT)^drf)
            // D = base damage
            // S = signature radius of target
            // E = explosion radius of missile
            // SVM = signature radius of target * explosion velocity of missile
            // EVT = (explosion radius of missile * explosion velocity of the target)^drf
            // drf = damage reduction factor (1 for rockets, 0.5 for light missiles, 0.25 for heavy missiles, 0.125 for cruise missiles, 0.0625 for torpedoes)

            var d = missileInvType.DamageEm + missileInvType.DamageExplosive + missileInvType.Damagekinetic +
                              missileInvType.DamageThermal;

            var missileRateOfFire = MissileLaunchDuration / 1000;
            var missleDamageMulti = MissileDamageMultiplier;

            var emDamage = (missileInvType.DamageEm * missleDamageMulti) / missileRateOfFire;
            var explosiveDamage = (missileInvType.DamageExplosive * missleDamageMulti) / missileRateOfFire;
            var kineticDamage = (missileInvType.Damagekinetic * missleDamageMulti) / missileRateOfFire;
            var thermalDamage = (missileInvType.DamageThermal * missleDamageMulti) / missileRateOfFire;

            var drf = missileInvType.AoeDamageReductionFactor;
            var targetSigRadius = DirectEve.ActiveShip.GetSignatureRadius();

            emDamage = emDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            explosiveDamage = explosiveDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            kineticDamage = kineticDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));
            thermalDamage = thermalDamage * Math.Min(Math.Min(1, targetSigRadius / missileInvType.Radius), Math.Pow((targetSigRadius * missileInvType.ExplosionVelocity) / (missileInvType.Radius * MissileEntityAoeVelocityMultiplier), drf));

            return (emDamage, explosiveDamage, kineticDamage, thermalDamage);

        }
        public (double EM, double Explosive, double Kinetic, double Thermal) GetCurrentDPSFrom()
        {
            var turretDps = GetCurrentDPSFromTurrets();
            var missileDps = GetCurrentDPSFromMissiles();
            // return sum of both bot round to two decimals
            var roundedDps = (
               Math.Round(turretDps.EM + missileDps.EM, 2),
               Math.Round(turretDps.Explosive + missileDps.Explosive, 2),
               Math.Round(turretDps.Kinetic + missileDps.Kinetic, 2),
               Math.Round(turretDps.Thermal + missileDps.Thermal, 2)
            );
            return roundedDps;
        }


        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFrom()
        {
            var turretDps = GetMaxDPSFromTurrets();
            var missileDps = GetMaxDPSFromMissiles();
            // return sum of both bot round to two decimals

            var roundedDps = (
             Math.Round(turretDps.EM + missileDps.EM, 2),
             Math.Round(turretDps.Explosive + missileDps.Explosive, 2),
             Math.Round(turretDps.Kinetic + missileDps.Kinetic, 2),
             Math.Round(turretDps.Thermal + missileDps.Thermal, 2)
             );
            return roundedDps;
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFromMissiles()
        {

            if (MissileEntityAoeVelocityMultiplier <= 0)
                return (0, 0, 0, 0);

            var invType = ESCache.Instance.DirectEve.GetInvType((int)EntityMissileTypeID);

            if (invType == null)
                return (0, 0, 0, 0);

            var missileRateOfFire = MissileLaunchDuration / 1000;
            var missleDamageMulti = MissileDamageMultiplier;

            var emDamage = (invType.DamageEm * missleDamageMulti) / missileRateOfFire;
            var explosiveDamage = (invType.DamageExplosive * missleDamageMulti) / missileRateOfFire;
            var kineticDamage = (invType.Damagekinetic * missleDamageMulti) / missileRateOfFire;
            var thermalDamage = (invType.DamageThermal * missleDamageMulti) / missileRateOfFire;

            return (emDamage, explosiveDamage, kineticDamage, thermalDamage);
        }

        public (double EM, double Explosive, double Kinetic, double Thermal) GetMaxDPSFromTurrets()
        {
            var invType = ESCache.Instance.DirectEve.GetInvType(this.TypeId);

            if (invType == null)
                return (0, 0, 0, 0);

            if (invType.RateOfFire <= 0)
                return (0, 0, 0, 0);

            var myShip = ESCache.Instance.MyShipEntity.DirectEntity;

            if (myShip == null)
                return (0, 0, 0, 0);

            // Turret calculations
            var hitChance = 1.0;

            var normalizedTurretDamage = 0.5 *
                                         Math.Min(
                                             Math.Pow(hitChance, 2) + 0.98 * hitChance + 0.0501,
                                             6 * hitChance
                                         );

            // Desintegrator bonus // TODO: we need to be able to retrieve the "live" value
            if (DamageMultiplierBonusMax > 0)
                normalizedTurretDamage *= 1 + DamageMultiplierBonusMax;

            var dmgMulti = invType.TurretDamageMultiplier;

            var rateOfFire = invType.RateOfFire;

            var emDmg = invType.DamageEm;
            var expDmg = invType.DamageExplosive;
            var kinDmg = invType.Damagekinetic;
            var theDmg = invType.DamageThermal;

            var em = (emDmg * dmgMulti);
            var exp = (expDmg * dmgMulti);
            var kin = (kinDmg * dmgMulti);
            var the = (theDmg * dmgMulti);

            var emDps = normalizedTurretDamage * (em / (rateOfFire / 1000));
            var expDps = normalizedTurretDamage * (exp / (rateOfFire / 1000));
            var kinDps = normalizedTurretDamage * (kin / (rateOfFire / 1000));
            var theDps = normalizedTurretDamage * (the / (rateOfFire / 1000));
            // return them but round two intergers
            return (emDps, expDps, kinDps, theDps);
        }

        #endregion Methods
    }
}