// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Shapes;
using EVESharpCore.Controllers;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    public class DirectActiveShip : DirectItem
    {
        #region Fields

        /// <summary>
        ///     Entity cache
        /// </summary>
        private DirectEntity _entity;

        private DirectEntity _followingEntity;

        private long? _itemId;

        #endregion Fields

        #region Constructors

        internal DirectActiveShip(DirectEve directEve)
            : base(directEve)
        {
            PyItem = directEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetShip");

            if (!PyItem.IsValid) DirectEve.Log("Warning: DirectActiveShip - GetShip returned null.");
        }

        private static long _cnt;

        /// <summary>
        /// shield, armor, structure values of past 20 pulses ( 10 seconds ). Each range:  0 ... 1.0
        /// </summary>
        public static Tuple<double, double, double>[] PastTwentyPulsesShieldArmorStrucValues = null;

        public static void SimulateLowShields()
        {
            PastTwentyPulsesShieldArmorStrucValues = new Tuple<double, double, double>[20];
            for (int i = 0; i < PastTwentyPulsesShieldArmorStrucValues.Length; i++)
            {
                PastTwentyPulsesShieldArmorStrucValues[i] = new Tuple<double, double, double>(0.1, 1, 1);
            }
        }

        public static void UpdateShieldArmorStrucValues(DirectEve de)
        {
            if (PastTwentyPulsesShieldArmorStrucValues == null)
            {
                PastTwentyPulsesShieldArmorStrucValues = new Tuple<double, double, double>[20];
                for (int i = 0; i < PastTwentyPulsesShieldArmorStrucValues.Length; i++)
                {
                    PastTwentyPulsesShieldArmorStrucValues[i] = new Tuple<double, double, double>(1, 1, 1);
                }
            }

            if (de.Session.IsReady && de.Session.IsInSpace)
            {
                if (de.ActiveShip.Entity != null)
                {
                    var ent = de.ActiveShip.Entity;
                    PastTwentyPulsesShieldArmorStrucValues[_cnt % 20] = new Tuple<double, double, double>(ent.ShieldPct, ent.ArmorPct, ent.StructurePct);
                    _cnt++;
                }
            }
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Your current amount of armor
        /// </summary>
        public double Armor => MaxArmor - Attributes.TryGet<double>("armorDamage");

        /// <summary>
        ///     Armor percentage
        /// </summary>
        public double ArmorPercentage => Armor / MaxArmor * 100;

        /// <summary>
        ///     Your current amount of capacitor
        /// </summary>
        public double Capacitor => Attributes.TryGet<double>("charge");

        /// <summary>
        ///     Capacitor percentage
        /// </summary>
        public double CapacitorPercentage => Capacitor / MaxCapacitor * 100;

        /// <summary>
        ///     DroneBandwidth
        /// </summary>
        public int DroneBandwidth => (int)Attributes.TryGet<double>("droneBandwidth");

        /// <summary>
        ///     DroneCapacity
        /// </summary>
        public int DroneCapacity => (int)Attributes.TryGet<double>("droneCapacity");

        // other checks -> treeData.py
        //    if bool (godmaSM.GetType(typeID).hasShipMaintenanceBay):
        //shipData.append(TreeDataShipMaintenanceBay(parent=self, clsName='ShipMaintenanceBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).hasFleetHangars):
        //shipData.append(TreeDataFleetHangar(parent=self, clsName='ShipFleetHangar', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialFuelBayCapacity):
        //shipData.append(TreeDataInv(parent=self, clsName='ShipFuelBay', itemID=itemID))
        //if bool (godmaSM.GetType(typeID).specialOreHoldCapacity):

        /// <summary>
        ///     The entity associated with your ship
        /// </summary>
        /// <remarks>
        ///     Only works in space, return's null if no entity can be found
        /// </remarks>
        public DirectEntity Entity => _entity ?? (_entity = DirectEve.GetEntityById(DirectEve.Session.ShipId ?? -1));

        public DirectEntity FollowingEntity => _followingEntity ?? (_followingEntity = DirectEve.GetEntityById(Entity != null ? Entity.FollowId : -1));
        public bool HasDroneBay => DroneCapacity > 0;

        /// <summary>
        ///     Inertia Modifier (also called agility)
        /// </summary>
        public double InertiaModifier => Attributes.TryGet<double>("agility");

        private DirectUIModule _bastionUIModule;
        private bool _checkedForBastion;

        public bool CanWeMove
        {
            get
            {
                if (DirectEve.Session.IsInSpace)
                {
                    if (!_checkedForBastion)
                    {
                        _bastionUIModule = DirectEve.Modules.FirstOrDefault(m => m.TypeId == 33400);
                        _checkedForBastion = true;
                    }

                    if (_bastionUIModule != null && _bastionUIModule.IsActive)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public new long ItemId
        {
            get
            {
                if (!_itemId.HasValue)
                    _itemId = (long)DirectEve.Session.ShipId;

                return _itemId.Value;
            }
            internal set => _itemId = value;
        }

        /// <summary>
        ///     The maximum amount of armor
        /// </summary>
        public double MaxArmor => Attributes.TryGet<double>("armorHP");

        /// <summary>
        ///     The maximum amount of capacitor
        /// </summary>
        public double MaxCapacitor => Attributes.TryGet<double>("capacitorCapacity");

        /// <summary>
        ///     Maximum locked targets
        /// </summary>
        /// <remarks>
        ///     Skills may cause you to lock less targets!
        /// </remarks>
        public int MaxLockedTargets => (int)Attributes.TryGet<double>("maxLockedTargets");

        /// <summary>
        ///     The maxmimum amount of shields
        /// </summary>
        public double MaxShield => Attributes.TryGet<double>("shieldCapacity");

        /// <summary>
        ///     The maximum amount of structure
        /// </summary>
        public double MaxStructure => Attributes.TryGet<double>("hp");

        /// <summary>
        ///     The maximum target range, yes this reflects active damps
        /// </summary>
        public double MaxTargetRange => Attributes.TryGet<double>("maxTargetRange");

        /// <summary>
        ///     Maximum velocity
        /// </summary>
        public double MaxVelocity => Attributes.TryGet<double>("maxVelocity");

        /// <summary>
        ///     Your current amount of shields
        /// </summary>
        public double Shield => Attributes.TryGet<double>("shieldCharge");

        /// <summary>
        ///     Shield percentage
        /// </summary>
        public double ShieldPercentage => Shield / MaxShield * 100;

        /// <summary>
        ///     Your current amount of structure
        /// </summary>
        public double Structure => MaxStructure - Attributes.TryGet<double>("damage");

        /// <summary>
        ///     Structure percentage
        /// </summary>
        public double StructurePercentage => Structure / MaxStructure * 100;

        #endregion Properties

        #region Methods

        public bool CanGroupAll()
        {
            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            var canGroupAll = (bool)dogmaLocation.Call("CanGroupAll", DirectEve.Session.ShipId);
            return canGroupAll;
        }

        public float GetDroneControlRange()
        {
            return DirectEve.GetLiveAttribute<float>(DirectEve.Session.CharacterId.Value, DirectEve.Const.AttributeDroneControlDistance.ToInt());
        }

        public float GetShipsAgility(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("agility");


            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeAgility.ToInt());
        }

        public float GetShipsMass(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("mass");

            //DirectEve.Log($"id [{this.ItemId}] attribute id [{DirectEve.Const.AttributeMass.ToInt()}] ShipId [{DirectEve.Session.ShipId}]");
            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeMass.ToInt());
        }

        private double? _getLiveSignatureRadius;

        public double GetSignatureRadius(bool live = true)
        {
            if (!_getLiveSignatureRadius.HasValue && live)
            {
                _getLiveSignatureRadius = DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeSignatureRadius.ToInt());
            }

            if (!live)
                _getLiveSignatureRadius = this.SignatureRadius;

            return _getLiveSignatureRadius.Value;
        }


        public float GetMaxVelocityBase(bool live = true)
        {
            if (!live)
                return this.TryGet<float>("maxVelocity");

            //DirectEve.Log($"id [{this.ItemId}] attribute id [{DirectEve.Const.AttributeMass.ToInt()}] ShipId [{DirectEve.Session.ShipId}]");
            return DirectEve.GetLiveAttribute<float>(this.ItemId, DirectEve.Const.AttributeMaxVelocity.ToInt());
        }

        public float GetMaxVelocityWithPropMod()
        {
            var baseValue = GetMaxVelocityBase();
            var speedFactorAddtion = 0f;
            var thrust = 0f;
            var massAddition = 0f;
            if (DirectEve.Modules.Any(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
            {
                foreach (var mod in DirectEve.Modules.Where(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
                {
                    var mA = DirectEve.GetLiveAttribute<float>(mod.ItemId, (int)DirectEve.Const.AttributeSpeedFactor);
                    if (mA > speedFactorAddtion)
                    {
                        speedFactorAddtion = mA;
                        thrust = DirectEve.GetLiveAttribute<float>(mod.ItemId, (int)DirectEve.Const.AttributeSpeedBoostFactor);
                        massAddition = mod.TryGet<float>("massAddition");
                    }
                }
                float veloRatio = 1f + thrust * (float)speedFactorAddtion * 0.01f / (GetShipsMass() + massAddition);
                return baseValue * veloRatio;
            }
            return baseValue;
        }

        public double GetSecondsToWarp(double warpPerc = 0.75d)
        {
            var agility = GetShipsAgility();
            var mass = GetShipsMass();
            var secondsToWarp = agility * mass * Math.Pow(10, -6) * -Math.Log(1 - warpPerc / 1);
            return secondsToWarp;
        }

        public double GetSecondsToWarpWithPropMod(double warpPerc = 0.75d)
        {
            var massAddition = 0d;
            if (DirectEve.Modules.Any(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
            {
                foreach (var mod in DirectEve.Modules.Where(m => m.GroupId == (int)Group.Afterburner && m.IsOnline))
                {
                    var mA = mod.TryGet<float>("massAddition");
                    if (mA > massAddition)
                    {
                        massAddition = mA;
                    }
                }
                var agility = GetShipsAgility();
                var mass = GetShipsMass() + massAddition;
                var secondsToWarp = agility * mass * Math.Pow(10, -6) * -Math.Log(1 - warpPerc / 1);

                return secondsToWarp;
            }
            return GetSecondsToWarp(warpPerc);
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float LowHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatLow"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public bool SetSpeedFraction(float fraction)
        {
            if (Entity == null)
                return false;

            if (!CanWeMove)
                return false;

            if (!DirectEve.Interval(1900, 3500))
                return false;

            double diff = 0.03d;

            if (Math.Abs(fraction - Entity.SpeedFraction) >= diff)
            {
                var cmd = DirectEve.GetLocalSvc("cmd");
                if (cmd.HasAttrString("SetSpeedFraction"))
                {
                    DirectEve.ThreadedCall(cmd["SetSpeedFraction"], fraction);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float MedHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatMed"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        /// <summary>
        /// 0 ... 1.0 range
        /// </summary>
        /// <returns></returns>
        public float HighHeatRackState()
        {
            var heatStates = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Call("GetCurrentShipHeatStates").ToDictionary<int>();
            var attr = DirectEve.Const["attributeHeatHi"].ToInt();
            if (heatStates.ContainsKey(attr))
            {
                return heatStates[attr].ToFloat();
            }

            return 0;
        }

        public bool IsFrigate => BracketType == BracketType.Frigate;

        public bool IsCruiser => BracketType == BracketType.Cruiser;

        public bool IsDestroyer => BracketType == BracketType.Destroyer;

        /// <summary>
        ///     Eject from your current ship
        /// </summary>
        /// <returns></returns>
        public bool EjectFromShip()
        {

            if (!CanWeMove)
                return false;

            var Eject = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Eject");
            return DirectEve.ThreadedCall(Eject);
        }


        public enum ShipConfigOption
        {
            FleetHangar_AllowFleetAccess,
            FleetHangar_AllowCorpAccess,
            SMB_AllowFleetAccess,
            SMB_AllowCorpAccess
        }

        public bool? GetShipConfigOption(ShipConfigOption option)
        {
            var shipConfigSvc = DirectEve.GetLocalSvc("shipConfig");


            if (!shipConfigSvc.IsValid || !shipConfigSvc.HasAttrString("config"))
            {
                DirectEve.Log("Error: ShipConfigSvc has no attribute 'config'.");
                return null;
            }

            var config = DirectEve.GetLocalSvc("shipConfig")["config"].ToDictionary<string>();

            if (config.ContainsKey(option.ToString()))
            {
                return config[option.ToString()].ToBool();
            }

            return null;
        }
        /// <summary>
        /// Remote call!
        /// </summary>
        /// <param name="option"></param>
        public bool ToggleShipConfigOption(ShipConfigOption option)
        {
            var shipConfigSvc = DirectEve.GetLocalSvc("shipConfig");

            if (!shipConfigSvc.IsValid)
            {
                DirectEve.Log("Error: ShipConfig Svc is not valid!");
                return false;
            }

            if (!DirectEve.Interval(1500, 2000))
                return false;

            switch (option)
            {
                case ShipConfigOption.FleetHangar_AllowFleetAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleFleetHangarFleetAccess"]);
                    break;
                case ShipConfigOption.FleetHangar_AllowCorpAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleFleetHangarCorpAccess"]);
                    break;
                case ShipConfigOption.SMB_AllowFleetAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleShipMaintenanceBayFleetAccess"]);
                    break;
                case ShipConfigOption.SMB_AllowCorpAccess:
                    DirectEve.ThreadedCall(shipConfigSvc["ToggleShipMaintenanceBayCorpAccess"]);
                    break;
            }

            return true;
        }

        /// <summary>
        ///     Groups all weapons if possible
        /// </summary>
        /// <returns>Fails if it's not allowed to group (because there is nothing to group)</returns>
        /// <remarks>Only works in space</remarks>
        public bool GroupAllWeapons()
        {
            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            var canGroupAll = (bool)dogmaLocation.Call("CanGroupAll", DirectEve.Session.ShipId);
            if (!canGroupAll)
                return false;

            return DirectEve.ThreadedCall(dogmaLocation.Attribute("LinkAllWeapons"), DirectEve.Session.ShipId.Value);
        }


        internal static Dictionary<long, DateTime> _droneReturnModeCache = new Dictionary<long, DateTime>();

        public bool ReturnDronesToBay(List<long> droneIds)
        {
            var activeDrones = DirectEve.ActiveDrones.ToList();
            droneIds = droneIds.Distinct().ToList();

            foreach (var id in droneIds.ToList())
            {
                if (!activeDrones.Any(e => e.Id == id))
                {
                    droneIds.Remove(id);
                    continue;
                }

                var d = activeDrones.FirstOrDefault(e => e.Id == id);

                if (d?.DroneState == 4)
                    droneIds.Remove(id);
            }

            if (!droneIds.Any())
                return false;

            if (!DirectEve.Interval(1200, 1900))
                return false;

            foreach (var id in droneIds)
            {
                _droneReturnModeCache[id] = DateTime.UtcNow;
            }

            var ret = DirectEve.ThreadedLocalSvcCall("menu", "ReturnToDroneBay", droneIds);
            return ret;
        }


        public int GetRemainingDroneBandwidth()
        {
            var droneBay = DirectEve.GetShipsDroneBay();

            if (droneBay == null)
                return 0;

            var maxDrones = DirectEve.Me.MaxActiveDrones;
            var currentShipsDroneBandwidth = DirectEve.ActiveShip.DroneBandwidth;
            var activeDrones = DirectEve.ActiveDrones;

            var activeDronesBandwidth = activeDrones.Sum(d => (int)d.TryGet<double>("droneBandwidthUsed"));
            activeDronesBandwidth = activeDronesBandwidth < 0 ? 0 : activeDronesBandwidth;

            var remainingDronesBandwidth = currentShipsDroneBandwidth - activeDronesBandwidth;
            remainingDronesBandwidth = remainingDronesBandwidth < 0 ? 0 : remainingDronesBandwidth;

            return remainingDronesBandwidth;
        }

        public Vec3? MoveToRandomDirection()
        {
            if (!DirectEve.Interval(1500, 2500))
                return null;

            var minimum = 0.1d;
            var maximum = 0.9d;
            var x = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            var y = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            var z = (_rnd.NextDouble() * (maximum - minimum) + minimum) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);

            Vec3 dir = new Vec3(x, y, z).Normalize();
            MoveTo(x, y, z);
            return dir;
        }

        /// <summary>
        ///     Launch all drones
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public bool LaunchDrones(int? typeId = null)
        {
            var droneBay = DirectEve.GetShipsDroneBay();

            if (droneBay == null)
                return false;

            var maxDrones = DirectEve.Me.MaxActiveDrones;
            var currentShipsDroneBandwidth = DirectEve.ActiveShip.DroneBandwidth;
            var activeDrones = DirectEve.ActiveDrones;

            var activeDronesBandwidth = activeDrones.Sum(d => (int)d.TryGet<double>("droneBandwidthUsed"));
            activeDronesBandwidth = activeDronesBandwidth < 0 ? 0 : activeDronesBandwidth;

            var remainingDronesBandwidth = currentShipsDroneBandwidth - activeDronesBandwidth;
            remainingDronesBandwidth = remainingDronesBandwidth < 0 ? 0 : remainingDronesBandwidth;

            var remainingDrones = maxDrones - activeDrones.Count;
            remainingDrones = remainingDrones < 0 ? 0 : remainingDrones;

            if (!droneBay.Items.Any())
                return false;

            if (activeDrones.Count >= 5)
                return false;

            //			DirectEve.Log("remainingDrones: " + remainingDrones);
            //			DirectEve.Log("remainingDronesBandwidth: " + remainingDronesBandwidth);

            var dronesToLaunch = new List<DirectItem>();

            var drones = typeId == null
                ? droneBay.Items.OrderByDescending(d => d.Stacksize)
                : droneBay.Items.Where(d => d.TypeId == typeId).OrderByDescending(d => d.Stacksize);

            if (drones.Count() >= remainingDrones)
                foreach (var d in drones.RandomPermutation())
                {
                    var bandwidth = (int)d.TryGet<double>("droneBandwidthUsed");

                    if (remainingDronesBandwidth - bandwidth >= 0 && remainingDrones - 1 >= 0)
                    {
                        remainingDrones--;
                        remainingDronesBandwidth = remainingDronesBandwidth - bandwidth;
                        dronesToLaunch.Add(d);
                    }
                    else
                    {
                        break;
                    }
                }
            else
                dronesToLaunch = typeId == null ? droneBay.Items.OrderByDescending(d => d.Stacksize).ToList() :
                    droneBay.Items.Where(d => d.TypeId == typeId).OrderByDescending(d => d.Stacksize).ToList();

            if (dronesToLaunch.Any(d => d.Stacksize > 1))
                dronesToLaunch = dronesToLaunch.Where(d => d.Stacksize > 1).ToList();

            dronesToLaunch = dronesToLaunch.RandomPermutation().ToList();

            return LaunchDrones(dronesToLaunch);
        }

        /// <summary>
        ///     Launch a specific list of drones
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Only works in space
        /// </remarks>
        public bool LaunchDrones(IEnumerable<DirectItem> drones)
        {
            if (!drones.Any())
                return false;

            if (!DirectEve.Interval(2000, 4000))
                return false;

            drones = drones.Where(e => e.ItemId > 0).OrderBy(e => e.ItemId).ToList();

            var invItems = drones.Where(d => d.PyItem.IsValid).Select(d => d.PyItem);
            return DirectEve.ThreadedLocalSvcCall("menu", "LaunchDrones", invItems);
        }

        private static Random _rnd = new Random();
        private static int _preventedSameDirectionCount = 0;
        /// <summary>
        /// This is a directional vector!
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns></returns>
        public bool MoveTo(double x, double y, double z, bool doNotRandomize = false, bool ignoreInterval = false)
        {
            var unitVecInput = new Vec3(x, y, z).Normalize();

            x = unitVecInput.X;
            y = unitVecInput.Y;
            z = unitVecInput.Z;

            double minPerc = 0.01d;
            double maxPerc = 0.03d;

            if (doNotRandomize)
            {
                minPerc = 0.001d;
                maxPerc = 0.003d;
            }

            var currentDirectionVector = Entity.GetDirectionVectorFinal();

            //DirectEve.SceneManager.ClearDebugLines();
            DirectEve.SceneManager.DrawLineGradient(new Vec3(0, 0, 0), currentDirectionVector.Scale(15000), new System.Numerics.Vector4(1, 0, 1, 1), new System.Numerics.Vector4(0, 0.5f, 0.5f, 0.5f));

            if (currentDirectionVector != null)
            {

                //DirectEve.Log($"currentDirectionVector {currentDirectionVector}");
                //DirectEve.Log($"XAbs [{Math.Abs(x - currentDirectionVector.X)}] YAbs [{Math.Abs(y - currentDirectionVector.Y)}] ZAbs [{Math.Abs(z - currentDirectionVector.Z)}]");
                if (
                    _preventedSameDirectionCount <= 3
                    && Math.Abs(x - currentDirectionVector.X) <= maxPerc
                    && Math.Abs(y - currentDirectionVector.Y) <= maxPerc
                    && Math.Abs(z - currentDirectionVector.Z) <= maxPerc
                    )
                {
                    if (DirectEve.Interval(3000, 6000))
                        _preventedSameDirectionCount++;
                    if (DirectEve.Interval(5000))
                    {
                        DirectEve.Log($"-- MoveTo skpping. We are already moving into that direction. _preventedSameDirectionCount {_preventedSameDirectionCount}");

                    }
                    return false;
                }
            }
            else
            {
                DirectEve.Log($"Warning: currentDirectionVector == null");
            }
            _preventedSameDirectionCount = 0;

            if (Math.Abs(x) < minPerc)
                x = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // x = 0.01 ... 0.03 

            if (Math.Abs(y) < minPerc)
                y = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);  // y = 0.01 ... 0.03 

            if (Math.Abs(z) < minPerc)
                z = (_rnd.NextDouble() * (maxPerc - minPerc) + minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1); // z = 0.01 ... 0.03 

            x = x + (_rnd.NextDouble() * (x * maxPerc - x * minPerc) + x * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            y = y + (_rnd.NextDouble() * (y * maxPerc - y * minPerc) + y * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);
            z = z + (_rnd.NextDouble() * (z * maxPerc - z * minPerc) + z * minPerc) * (_rnd.NextDouble() >= 0.5 ? 1 : -1);

            if (!CanWeMove)
                return false;

            if (!ignoreInterval && !DirectEve.Interval(1500, 3100))
                return false;

            var unitVec = new Vec3(x, y, z).Normalize();

            if (!unitVec.IsUnitVector(0.00001d))
            {
                Console.WriteLine("Error: MoveTo -- Is not a unit vector");
                DirectEve.Log("Error: MoveTo -- Is not a unit vector");
                return false;
            }

            DirectEve.Log($"-- Framework MoveTo [{unitVec}] ");

            if (doNotRandomize)
                return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), unitVecInput.X, unitVecInput.Y, unitVecInput.Z);

            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("michelle").Call("GetRemotePark").Attribute("CmdGotoDirection"), unitVec.X, unitVec.Y, unitVec.Z);
        }

        public bool MoveTo(DirectWorldPosition pos, bool doNotRandomize = false, bool ignoreInterval = false)
        {
            var a = DirectEve.ActiveShip.Entity;
            return MoveTo(pos.X - a.X, pos.Y - a.Y, pos.Z - a.Z, doNotRandomize, ignoreInterval);
        }

        public bool MoveTo(DirectEntity b, bool doNotRandomize = false, bool ignoreInterval = false)
        {
            var a = DirectEve.ActiveShip.Entity;
            return MoveTo(b.X - a.X, b.Y - a.Y, b.Z - a.Z, doNotRandomize, ignoreInterval);
        }

        /// <summary>
        ///     Strips active ship, use only in station!
        /// </summary>
        /// <returns></returns>
        public bool StripFitting()
        {
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("menu").Attribute("invCache").Call("GetInventoryFromId", ItemId).Attribute("StripFitting"));
        }

        /// <summary>
        ///     Ungroups all weapons
        /// </summary>
        /// <returns>
        ///     Fails if anything can still be grouped. Execute GroupAllWeapons first if not everything is grouped, this is
        ///     done to mimic client behavior.
        /// </returns>
        /// <remarks>Only works in space</remarks>
        public bool UngroupAllWeapons()
        {
            var dogmaLocation = DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation");
            var canGroupAll = (bool)dogmaLocation.Call("CanGroupAll", DirectEve.Session.ShipId.Value);
            if (canGroupAll)
                return false;

            return DirectEve.ThreadedCall(dogmaLocation.Attribute("UnlinkAllWeapons"), DirectEve.Session.ShipId.Value);
        }

        #endregion Methods
    }
}
