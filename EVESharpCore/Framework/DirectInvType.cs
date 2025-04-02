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

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using EVESharpCore.Logging;
using SC::SharedComponents.Utility;
using SC::SharedComponents.IPC;
using System.Globalization;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum BracketType
    {
        Unknown,
        Navy_Concord_Customs,
        Sun,
        Planet,
        Moon,
        Asteroid_Belt,
        Stargate,
        Asteroid_Small,
        Cargo_container,
        Biomass,
        Station,
        Frigate,
        Rookie_ship,
        Cruiser,
        Battleship,
        Industrial_Ship,
        Capsule,
        Titan,
        Shuttle,
        Sentry_Gun,
        Drone,
        Drone_Mining,
        Large_Collidable_Structure,
        NPC_Rookie_Ship,
        Cargo_Container_NPC,
        NPC_Frigate,
        NPC_Battleship,
        NPC_Battlecruiser,
        NPC_Destroyer,
        NPC_Cruiser,
        NPC_Super_Carrier,
        Planetary_Customs_Office,
        NPC_Shuttle,
        Agents_in_Space,
        NPC_Freighter,
        NPC_Industrial,
        Fighter_Squadron,
        NPC_Fighter_Bomber,
        NPC_Industrial_Command_Ship,
        Wreck_NPC,
        Entity,
        Super_Carrier,
        Control_Tower,
        Battlecruiser,
        Warp_Gate,
        Planetary_Customs_Office_NPC,
        Jump_Portal_Array,
        Mobile_Jump_Disruptor,
        Carrier,
        Satellite_Beacon,
        Platform,
        NPC_Drone,
        Asteroid_Billboard,
        Mobile_Warp_Disruptor,
        Ship_Maintenance_Array,
        Reprocessing_Array,
        Compression_Array,
        Mobile_Storage,
        Assembly_Array,
        Silo,
        NPC_Mining_Barge,
        Force_Field,
        NPC_Drone_EW,
        Laboratory,
        Mobile_Power_Core,
        Moon_Mining,
        Starbase_Missile_Battery,
        Mobile_Shield_Generator,
        Destroyer,
        Ice_Small,
        Starbase_Projectile_Battery,
        Starbase_Hybrid_Battery,
        Starbase_Laser_Battery,
        Reactor,
        Starbase_Electronic_Warfare_Battery,
        Starbase_Stasis_Webification_Battery,
        Starbase_Sensor_Dampening_Battery,
        Starbase_Warp_Scrambling_Battery,
        Starbase_Shield_Hardening_Array,
        Structure,
        Asteroid_Medium,
        Asteroid_Large,
        Mining_Barge,
        Corporate_Hangar_Array,
        Ice_Field,
        Ice_Large,
        Celestial_Beacon_II,
        Dreadnought,
        Freighter,
        Cynosural_Field,
        Drone_EW,
        Drone_Logistics,
        Scanner_Probe,
        Drone_Sentry,
        XL_Ship_maintenance_Array,
        Harvestable_Cloud,
        Celestial_Agent_Site_Beacon,
        Wreck,
        Starbase_Energy_Neutralizing_Battery,
        Cynosural_Generator_Array,
        Cynosural_System_Jammer,
        Scanner_Array,
        NPC_Carrier,
        NPC_Fighter,
        Bomb,
        Destructible_Station_Service,
        Industrial_Command_Ship,
        Capture_Point,
        FW_Infrastructure_Hub,
        Wormhole,
        Territorial_Claim_Unit,
        Sovereignty_Blockade_Unit,
        Infrastructure_Hub,
        Drone_Salvaging,
        Mining_Frigate,
        Personal_Hangar_Array,
        Mobile_Depot,
        Mobile_Tractor_Unit,
        Mobile_Cynosural_Inhibitor,
        Mobile_Siphon_Unit,
        Encounter_Surveillance_System,
        Mobile_Scan_Inhibitor,
        Mobile_Micro_Jump_Unit,
        Medium_Engineering_Complex,
        Large_Engineering_Complex,
        Extra_Large_Engineering_Complex,
        Medium_Structure,
        Large_Structure,
        Extra_Large_Structure,
        Medium_Refinery,
        Large_Refinery,
        Command_Node_Beacon,
        NPC_Dreadnought,
        NPC_Titan,
        Force_Auxiliary,
        NO_BRACKET,
        NPC_Mining_Frigate,
        NPC_Force_Auxiliary,
        NPC_Extra_Large_Engineering_Complex,
        Moon_Asteroid,
        Moon_Asteroid_Jackpot
    }

    public class DirectInvType : DirectObject
    {
        #region Fields

        /// <summary>
        ///     TypeId; Bracketname dict
        /// </summary>
        private static Dictionary<int, string> _bracketNameDictionary = new Dictionary<int, string>();

        private static Dictionary<int, string> _bracketTexturePathDictionary = new Dictionary<int, string>();
        private static Dictionary<int, BracketType> _bracketTypeDictionary = new Dictionary<int, BracketType>();
        private static Dictionary<int, DirectInvType> invTypeCache = new Dictionary<int, DirectInvType>();
        private double? _armorResistanceEM;
        private double? _armorResistanceExplosion;
        private double? _armorResistanceKinetic;
        private double? _armorResistanceThermal;

        private Dictionary<string, object> _attrdictionary;
        private double? _averagePrice;
        private double? _basePrice;
        private double? _capacity;

        private int? _categoryId;

        //private string _categoryName;
        private double? _chanceOfDuplicating;

        private int? _dataId;
        private string _description;

        //private List<PyObject> _dmgAttributes;
        private int? _graphicId;
        private int? _groupId;
        private string _groupName;
        private int? _iconId;
        private int? _marketGroupId;
        private double? _mass;
        private int? _portionSize;

        private bool? _published;


        private int? _raceId;
        private double? _radius;

        private double? _shieldResistanceEM;
        private double? _shieldResistanceExplosion;
        private double? _shieldResistanceKinetic;
        private double? _shieldResistanceThermal;


        private double? _structureResistanceEM;
        private double? _structureResistanceExplosion;
        private double? _structureResistanceKinetic;
        private double? _structureResistanceThermal;

        private double? _signatureRadius;
        private int? _soundId;
        private double? _totalArmor;
        private double? _totalShield;
        private double? _totalStructure;
        private string _typeName;
        private double? _volume;

        // These refer to NPC attributes
        // ingame this refers to "speed" for some nasty reason
        private double? rateOfFire;
        private double? missileLaunchDuration;
        private double? optimalRange;
        private double? optimalSigRadius;
        private double? damageModifier;
        private double? maxtargetingRange;
        private double? damageEm;
        private double? damageExplosive;
        private double? damagekinetic;
        private double? missileEntityAoeVelocityMultiplier;
        private double? aoeVelocity;
        private double? aoeDamageReductionFactor;

        private double? entityMissileTypeID;
        private double? damageThermal;
        private double? accuracyFalloff;
        private double? turretTracking;
        private double? damageMultiplierBonusMax;
        private double? missileDamageMultiplier;
        private int? maxLockedTargets;

        #endregion Fields

        #region Constructors

        public DirectInvType(DirectEve directEve, int typeId)
            : base(directEve)
        {
            TypeId = typeId;
        }

        internal DirectInvType(DirectEve directEve)
                    : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        private string _categoryName;


        public double? ArmorResistanceEM
        {
            get
            {
                if (!_armorResistanceEM.HasValue)
                {
                    _armorResistanceEM = Math.Round(1.0d - TryGet<float>("armorEmDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _armorResistanceEM = Math.Round(1.0d - (1.0d - _armorResistanceEM.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceEM < 0d)
                            _armorResistanceEM = 0d;
                    }
                }
                return _armorResistanceEM;
            }
        }

        public double? ArmorResistanceExplosion
        {
            get
            {
                if (!_armorResistanceExplosion.HasValue)
                {
                    _armorResistanceExplosion = Math.Round(1.0d - TryGet<float>("armorExplosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _armorResistanceExplosion = Math.Round(1.0d - (1.0d - _armorResistanceExplosion.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceExplosion < 0d)
                            _armorResistanceExplosion = 0d;
                    }
                }
                return _armorResistanceExplosion;
            }
        }

        public double? ArmorResistanceKinetic
        {
            get
            {
                if (!_armorResistanceKinetic.HasValue)
                {
                    _armorResistanceKinetic = Math.Round(1.0d - TryGet<float>("armorKineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _armorResistanceKinetic = Math.Round(1.0d - (1.0d - _armorResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceKinetic < 0d)
                            _armorResistanceKinetic = 0d;
                    }
                }
                return _armorResistanceKinetic;

            }
        }

        public double? ArmorResistanceThermal
        {
            get
            {
                if (!_armorResistanceThermal.HasValue)
                {
                    _armorResistanceThermal = Math.Round(1.0d - TryGet<float>("armorThermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _armorResistanceThermal = Math.Round(1.0d - (1.0d - _armorResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_armorResistanceThermal < 0d)
                            _armorResistanceThermal = 0d;
                    }
                }
                return _armorResistanceThermal;
            }
        }

        public double BasePrice
        {
            get
            {
                if (!_basePrice.HasValue)
                    _basePrice = (double)PyInvType.Attribute("basePrice");

                return _basePrice.Value;
            }
        }

        /// <summary>
        /// This works also with mutated drones.
        /// </summary>
        /// <returns></returns>
        public Dictionary<DirectDamageType, float> GetDroneDPS()
        {

            long id = 0;

            if (this.GetType() == typeof(DirectItem))
                id = ((DirectItem)this).ItemId;
            else if (this.GetType() == typeof(DirectEntity))
                id = ((DirectEntity)this).Id;
            else
            {
                throw new NotImplementedException();
            }

            var result = new Dictionary<DirectDamageType, float>();

            // Get the attributes for each damage type
            var emDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeEmDamage.ToInt());
            var exploDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeExplosiveDamage.ToInt());
            var thermDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeThermalDamage.ToInt());
            var kinDmg = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeKineticDamage.ToInt());

            // Get the attribute for damage multi and duration
            var multi = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeDamageMultiplier.ToInt());
            var rof = DirectEve.GetLiveAttribute<float>(id, DirectEve.Const.AttributeRateOfFire.ToInt());

            //DirectEve.Log($"emDmg {emDmg} exploDmg {exploDmg} thermDmg {thermDmg} kinDmg {kinDmg} multi {multi} rof {rof}");
            // Calculate drone dps => droneDps = damage * damageMultiplier / duration
            if (rof > 0d)
            {
                result[DirectDamageType.EM] = 1000 * ((emDmg * multi) / rof);
                result[DirectDamageType.EXPLO] = 1000 * ((exploDmg * multi) / rof);
                result[DirectDamageType.THERMAL] = 1000 * ((thermDmg * multi) / rof);
                result[DirectDamageType.KINETIC] = 1000 * ((kinDmg * multi) / rof);
            }
            else
            {
                result[DirectDamageType.EM] = 0;
                result[DirectDamageType.EXPLO] = 0;
                result[DirectDamageType.THERMAL] = 0;
                result[DirectDamageType.KINETIC] = 0;
            }

            return result;
        }

        public BracketType BracketType
        {
            get
            {
                if (_bracketTypeDictionary.TryGetValue(TypeId, out var bracketType)) return bracketType;

                var r = default(BracketType);
                if (Enum.TryParse<BracketType>(GetBracketName().Replace(" ", "_"), out var type)) r = type;

                _bracketTypeDictionary[TypeId] = r;
                return r;
            }
        }

        public double Capacity
        {
            get
            {
                if (!_capacity.HasValue)
                    _capacity = (double)PyInvType.Attribute("capacity");

                return _capacity.Value;
            }
        }

        public int CategoryId
        {
            get
            {
                if (!_categoryId.HasValue)
                    _categoryId = (int)PyInvGroup.Attribute("categoryID");

                return _categoryId.Value;
            }
        }

        public string CategoryName
        {
            get
            {
                if (string.IsNullOrEmpty(_categoryName))
                {
                    _categoryName = (string)PySharp.Import("evetypes")
                        .Attribute("localizationUtils")
                        .Call("GetLocalizedCategoryName", (int)PyInvCategory.Attribute("categoryNameID"), "en-us");
                }
                return _categoryName;
            }
        }

        private static Dictionary<int, double> _cachedPrices = new Dictionary<int, double>();

        private static System.Text.RegularExpressions.Regex _regex = new System.Text.RegularExpressions.Regex(@"(?:\""sell\"".*\""wavg\"":(?<Wavg>[\d\.]+))", System.Text.RegularExpressions.RegexOptions.Compiled);

        public double AveragePrice()
        {
            var avgPrice = -1d;
            if (_cachedPrices.TryGetValue(this.TypeId, out double price))
                return price;

            try
            {
                return Util.MeasureTime(() =>
                {
                    //TODO: use https://evetycoon.com/api/v1/market/stats/10000002/47784 (regex needs fix or json parsing)
                    var url = $"https://api.evemarketer.com/ec/marketstat/json?typeid={this.TypeId}&usesystem=30000142";
                    var result = WCFClient.Instance.GetPipeProxy.GetPage(url, TimeSpan.FromDays(1));

                    if (string.IsNullOrWhiteSpace(result))
                    {
                        throw new Exception($"GetPage - No cached value for [{url}]");
                    }

                    var match = _regex.Match(result).Groups["Wavg"].Value;
                    var val = double.Parse(match, CultureInfo.InvariantCulture);

                    if (val <= 0)
                        val = (double)PySharp.Import("inventorycommon.typeHelpers").Call("GetAveragePrice", this.TypeId);

                    return _cachedPrices[this.TypeId] = val;
                });

            }
            catch (Exception e)
            {
                avgPrice = (double)PySharp.Import("inventorycommon.typeHelpers").Call("GetAveragePrice", this.TypeId);
                Console.WriteLine($"Info: Returning average price fallback value. TypeId [{this.TypeId}] AvgPrice [{avgPrice}]");
            }
            return avgPrice;
        }

        public int GraphicId
        {
            get
            {
                if (!_graphicId.HasValue)
                    _graphicId = (int)PyInvType.Attribute("graphicID");

                return _graphicId.Value;
            }
        }

        public int GroupId
        {
            get
            {
                if (!_groupId.HasValue)
                    _groupId = (int)PyInvType.Attribute("groupID");

                return _groupId.Value;
            }
        }

        public string GroupName
        {
            get
            {
                if (string.IsNullOrEmpty(_groupName))
                {
                    _groupName = (string)PySharp.Import("evetypes")
                            .Attribute("localizationUtils")
                            .Call("GetLocalizedGroupName", (int)PyInvGroup.Attribute("groupNameID"), "en-us");
                }
                return _groupName;
            }
        }

        public int IconId
        {
            get
            {
                if (!_iconId.HasValue)
                    _iconId = (int)PyInvType.Attribute("iconID");

                return _iconId.Value;
            }
        }

        public int MarketGroupId
        {
            get
            {
                if (!_marketGroupId.HasValue)
                    _marketGroupId = (int)PyInvType.Attribute("marketGroupID");

                return _marketGroupId.Value;
            }
        }

        public double Mass
        {
            get
            {
                if (!_mass.HasValue)
                    _mass = (double)PyInvType.Attribute("mass");

                return _mass.Value;
            }
        }

        public int PortionSize
        {
            get
            {
                if (!_portionSize.HasValue)
                    _portionSize = (int)PyInvType.Attribute("portionSize");

                return _portionSize.Value;
            }
        }

        public bool Published
        {
            get
            {
                if (!_published.HasValue)
                    _published = (bool)PyInvType.Attribute("published");

                return _published.Value;
            }
        }

        public int RaceId
        {
            get
            {
                if (!_raceId.HasValue)
                    _raceId = (int)PyInvType.Attribute("raceID");

                return _raceId.Value;
            }
        }

        public double Radius
        {
            get
            {
                if (!_radius.HasValue)
                    _radius = (double)PyInvType.Attribute("radius");

                return _radius.Value;
            }
        }


        public double? StructureResistanceEM
        {
            get
            {
                if (!_structureResistanceEM.HasValue)
                {
                    _structureResistanceEM = Math.Round(1.0d - TryGet<float>("emDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _structureResistanceEM = Math.Round(1.0d - (1.0d - _structureResistanceEM.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }
                    if (_structureResistanceEM < 0d || _structureResistanceEM >= 1.0d)
                        _structureResistanceEM = 0d;
                }
                return _structureResistanceEM;
            }
        }

        public double? StructureResistanceExplosion
        {
            get
            {
                if (!_structureResistanceExplosion.HasValue)
                {
                    _structureResistanceExplosion = Math.Round(1.0d - TryGet<float>("explosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _structureResistanceExplosion = Math.Round(1.0d - (1.0d - _structureResistanceExplosion.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }
                    if (_structureResistanceExplosion < 0d || _structureResistanceExplosion >= 1.0d)
                        _structureResistanceExplosion = 0d;
                }
                return _structureResistanceExplosion;
            }
        }

        public double? StructureResistanceKinetic
        {
            get
            {
                if (!_structureResistanceKinetic.HasValue)
                {
                    _structureResistanceKinetic = Math.Round(1.0d - TryGet<float>("kineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _structureResistanceKinetic = Math.Round(1.0d - (1.0d - _structureResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }
                    if (_structureResistanceKinetic < 0d || _structureResistanceKinetic >= 1.0d)
                        _structureResistanceKinetic = 0d;
                }
                return _structureResistanceKinetic;
            }
        }

        public double? StructureResistanceThermal
        {
            get
            {
                if (!_structureResistanceThermal.HasValue)
                {
                    _structureResistanceThermal = Math.Round(1.0d - TryGet<float>("thermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _structureResistanceThermal = Math.Round(1.0d - (1.0d - _structureResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                    }
                    if (_structureResistanceThermal < 0d || _structureResistanceThermal >= 1.0d)
                        _structureResistanceThermal = 0d;
                }
                return _structureResistanceThermal;
            }
        }

        public double? ShieldResistanceEM
        {
            get
            {
                if (!_shieldResistanceEM.HasValue)
                {
                    _shieldResistanceEM = Math.Round(1.0d - TryGet<float>("shieldEmDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EM)
                    {
                        _shieldResistanceEM = Math.Round(1.0d - (1.0d - _shieldResistanceEM.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceEM < 0d)
                            _shieldResistanceEM = 0d;
                    }
                }
                return _shieldResistanceEM;
            }
        }

        public double? ShieldResistanceExplosion
        {
            get
            {
                if (!_shieldResistanceExplosion.HasValue)
                {
                    _shieldResistanceExplosion = Math.Round(1.0d - TryGet<float>("shieldExplosiveDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.EXPLO)
                    {
                        _shieldResistanceExplosion = Math.Round(1.0d - (1.0d - _shieldResistanceExplosion.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceExplosion < 0d)
                            _shieldResistanceExplosion = 0d;
                    }
                }
                return _shieldResistanceExplosion;
            }
        }

        public double? ShieldResistanceKinetic
        {
            get
            {
                if (!_shieldResistanceKinetic.HasValue)
                {
                    _shieldResistanceKinetic = Math.Round(1.0d - TryGet<float>("shieldKineticDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.KINETIC)
                    {
                        _shieldResistanceKinetic = Math.Round(1.0d - (1.0d - _shieldResistanceKinetic.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceKinetic < 0d)
                            _shieldResistanceKinetic = 0d;
                    }
                }
                return _shieldResistanceKinetic;
            }
        }

        public double? ShieldResistanceThermal
        {
            get
            {
                if (!_shieldResistanceThermal.HasValue)
                {

                    _shieldResistanceThermal = Math.Round(1.0d - TryGet<float>("shieldThermalDamageResonance"), 3);
                    if (DirectEve.Me.GetAbyssResistsDebuff()?.Item1 == DirectDamageType.THERMAL)
                    {
                        _shieldResistanceThermal = Math.Round(1.0d - (1.0d - _shieldResistanceThermal.Value) * (1.0d + (DirectEve.Me.GetAbyssResistsDebuff().Value.Item2 / 100)), 3);
                        if (_shieldResistanceThermal < 0d)
                            _shieldResistanceThermal = 0d;
                    }

                }
                return _shieldResistanceThermal;
            }
        }

        public double SignatureRadius
        {
            get
            {
                if (!_signatureRadius.HasValue)
                    _signatureRadius = new int?((int)TryGet<float>("signatureRadius")) ?? 0;
                return _signatureRadius.Value;
            }
        }

        public int SoundId
        {
            get
            {
                if (!_soundId.HasValue)
                    _soundId = (int)PyInvType.Attribute("soundID");

                return _soundId.Value;
            }
        }

        public double? TotalArmor
        {
            get
            {
                if (!_totalArmor.HasValue)
                {
                    _totalArmor = TryGet<float>("armorHP");
                    if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.weatherInfernal))
                        _totalArmor *= 1.5;
                }
                return _totalArmor;
            }
        }

        public double? TotalShield
        {
            get
            {
                if (!_totalShield.HasValue)
                {
                    _totalShield = TryGet<float>("shieldCapacity");
                    if (DirectEve.Me.IsHudStatusEffectActive(HudStatusEffect.weatherXenonGas))
                        _totalShield *= 1.5;
                }
                return _totalShield;
            }
        }

        public double? TotalStructure
        {
            get
            {
                if (!_totalStructure.HasValue)
                    _totalStructure = TryGet<float>("hp");
                return _totalStructure;
            }
        }

        private float? _maxRange;

        public float? MaxRange
        {
            get
            {
                if (!_maxRange.HasValue)
                    _maxRange = TryGet<float>("maxRange");
                return _maxRange;
            }
        }

        public int TypeId { get; internal set; }

        public string TypeName
        {
            get
            {
                if (string.IsNullOrEmpty(_typeName))
                    _typeName = (string)PySharp.Import("evetypes")
                        .Attribute("localizationUtils")
                        .Call("GetLocalizedTypeName", (int)PyInvType.Attribute("typeNameID"), "en-us");

                return _typeName;
            }
        }

        public double Volume
        {
            get
            {
                if (!_volume.HasValue)
                    _volume = (double)PyInvType.Attribute("volume");

                return _volume.Value;
            }
        }


        public double MissileLaunchDuration => missileLaunchDuration ??= TryGet<double>("missileLaunchDuration");
        public double RateOfFire => rateOfFire ??= TryGet<double>("speed");
        public double OptimalRange => optimalRange ??= TryGet<double>("maxRange");

        public double OptimalSigRadius => optimalSigRadius ??= TryGet<double>("optimalSigRadius");

        public double TurretDamageMultiplier => damageModifier ??= TryGet<double>("damageMultiplier");
        public double MaxtargetingRange => maxtargetingRange ??= TryGet<double>("maxTargetRange");
        public double DamageEm => damageEm ??= TryGet<double>("emDamage");
        public double DamageExplosive => damageExplosive ??= TryGet<double>("explosiveDamage");
        public double Damagekinetic => damagekinetic ??= TryGet<double>("kineticDamage");

        public double AoeDamageReductionFactor => aoeDamageReductionFactor ??= TryGet<double>("aoeDamageReductionFactor");


        public double MissileEntityAoeVelocityMultiplier => missileEntityAoeVelocityMultiplier ??= TryGet<double>("missileEntityAoeVelocityMultiplier");

        public double ExplosionVelocity => aoeVelocity ??= TryGet<double>("aoeVelocity");

        public double EntityMissileTypeID => entityMissileTypeID ??= TryGet<double>("entityMissileTypeID");
        public double DamageThermal => damageThermal ??= TryGet<double>("thermalDamage");
        public double AccuracyFalloff => accuracyFalloff ??= TryGet<double>("falloff");
        public double TurretTracking => turretTracking ??= TryGet<double>("trackingSpeed");
        public double DamageMultiplierBonusMax => damageMultiplierBonusMax ??= TryGet<double>("damageMultiplierBonusMax");
        public double MissileDamageMultiplier => missileDamageMultiplier ??= TryGet<double>("missileDamageMultiplier");

        public int MaxLockedTargets => maxLockedTargets ??= TryGet<int>("maxLockedTargets");

        // Have to find the ingame names for these
        //public double DisintergratorDamageMultiplierPerCycle => disintergratorDamageMultiplierPerCycle ??= (double)PyInvType.Attribute("thermalDamage");
        //public double DisintergratorMaxDamageMultiplier => disintergratorMaxDamageMultiplier ??= (double)PyInvType.Attribute("thermalDamage");


        internal PyObject PyInvCategory => PySharp.Import("evetypes").Call("GetCategory", this.CategoryId);

        internal PyObject PyInvGroup => PySharp.Import("evetypes").Call("GetGroup", this.GroupId);

        internal PyObject PyInvType => PySharp.Import("evetypes").Call("GetType", this.TypeId);

        #endregion Properties

        #region Methods

        private static Dictionary<int, string> _attributeNamesById;
        private Dictionary<int, string> GetAttributeNamesById()
        {
            if (_attributeNamesById == null)
            {
                _attributeNamesById = new Dictionary<int, string>();
                var attributeNameById = DirectEve.PySharp.Import("dogma.data").Call("get_attribute_names_by_id").ToDictionary<int>();
                foreach (var k in attributeNameById)
                {
                    _attributeNamesById.Add(k.Key, k.Value.ToUnicodeString());
                }
            }
            return _attributeNamesById;
        }

        private Dictionary<int, DirectDgmEffect> _dgmEffects;

        private static Dictionary<int, Dictionary<int, DirectDgmEffect>> _dgmEffectsCache = new Dictionary<int, Dictionary<int, DirectDgmEffect>>();
        public Dictionary<int, DirectDgmEffect> GetDmgEffects()
        {
            if (_dgmEffects == null && _dgmEffectsCache.TryGetValue(this.TypeId, out var val))
                _dgmEffects = val;

            if (_dgmEffects == null)
            {
                var ret = new Dictionary<int, DirectDgmEffect>();
                var dogmaIM = DirectEve.GetLocalSvc("clientDogmaStaticSvc");
                if (dogmaIM.IsValid)
                {
                    var effectsForThisType = dogmaIM.Call("TypeGetEffects", this.TypeId).ToDictionary<int>();
                    //var hasTypeEffects = dogmaData.Call("has_type_effects", this.TypeId).ToBool();
                    foreach (var eff in effectsForThisType)
                    {
                        var effect = dogmaIM.Call("GetEffect", eff.Key);
                        ret[eff.Key] = new DirectDgmEffect(DirectEve, effect);
                    }
                    _dgmEffects = ret;
                    _dgmEffectsCache[this.TypeId] = _dgmEffects;
                }
            }
            return _dgmEffects;
        }

        private Dictionary<string, DirectDgmEffect> _dgmEffectsByGuid;

        private static Dictionary<int, Dictionary<string, DirectDgmEffect>> _dgmEffectsByGuidCache = new Dictionary<int, Dictionary<string, DirectDgmEffect>>();
        public Dictionary<string, DirectDgmEffect> GetDmgEffectsByGuid()
        {
            if (_dgmEffectsByGuid == null && _dgmEffectsByGuidCache.TryGetValue(this.TypeId, out var val))
                _dgmEffectsByGuid = val;

            if (_dgmEffectsByGuid == null)
            {
                var ret = new Dictionary<string, DirectDgmEffect>();
                var dogmaIM = DirectEve.GetLocalSvc("clientDogmaStaticSvc");
                if (dogmaIM.IsValid)
                {
                    var effectsForThisType = dogmaIM.Call("TypeGetEffects", this.TypeId).ToDictionary<int>();
                    foreach (var eff in effectsForThisType)
                    {
                        var effect = dogmaIM.Call("GetEffect", eff.Key);
                        var dgmEffect = new DirectDgmEffect(DirectEve, effect);
                        if (!String.IsNullOrWhiteSpace(dgmEffect.Guid))
                        {
                            ret[dgmEffect.Guid] = dgmEffect;
                        }
                    }
                    _dgmEffectsByGuid = ret;
                    _dgmEffectsByGuidCache[this.TypeId] = _dgmEffectsByGuid;
                }
            }
            return _dgmEffectsByGuid;
        }

        private DateTime? _getBoosterConsumbableUntil = null;

        public DateTime GetBoosterConsumbableUntil()
        {

            if (_getBoosterConsumbableUntil.HasValue)
                return _getBoosterConsumbableUntil.Value;

            var ret = DateTime.MaxValue;
           
                var s = "boosterLastInjectionDatetime";
                if (this.GetAttributesInvType().ContainsKey(s))
                {
                    var boosterLastInjectionDatetime = this.TryGet<double>("boosterLastInjectionDatetime");
                    if (boosterLastInjectionDatetime > 0)
                    {
                        var result = PyObject.PY_EPOCH_TIME_TIME.AddDays(boosterLastInjectionDatetime);
                        _getBoosterConsumbableUntil = result;
                        return result;
                    }
                }

            _getBoosterConsumbableUntil = ret;
            return ret;
        }

        public Dictionary<string, object> GetAttributesInvType()
        {
            if (invTypeCache.TryGetValue(this.TypeId, out var cachedInvType) && cachedInvType._attrdictionary != null)
            {
                return cachedInvType._attrdictionary;
            }

            if (_attrdictionary == null)
            {
                _attrdictionary = new Dictionary<string, object>();
                var _dmgAttribute = DirectEve.PySharp.Import("dogma.data").Call("get_type_attributes_by_id", this.TypeId).ToDictionary<int>();
                var a = (int)DirectEve.Const["attributeCapacity"];
                var b = (int)DirectEve.Const["attributeVolume"];
                if (!_dmgAttribute.ContainsKey(a))
                    _dmgAttribute.Add(a, PySharp.PyNone);
                if (!_dmgAttribute.ContainsKey(b))
                    _dmgAttribute.Add(b, PySharp.PyNone);
                var attributeNamesbyId = GetAttributeNamesById();

                var attributeDataTypeTypeMirror = (int)DirectEve.Const["attributeDataTypeTypeMirror"];

                foreach (var k in _dmgAttribute)
                {
                    var attribute = DirectEve.PySharp.Import("dogma.data").Call("get_attribute", k.Key);
                    if (!attribute.IsValid)
                    {
                        DirectEve.Log($"ERROR: dogma.data.get_attribute return an invalid object.");
                        break;
                    }
                    var dataType = attribute.Attribute("dataType").ToInt();
                    if (dataType == attributeDataTypeTypeMirror)
                    {
                        if (attributeNamesbyId.TryGetValue(k.Key, out var key))
                        {
                            var value = DirectEve.PySharp.Import("evetypes").Call("GetAttributeForType", this.TypeId, key).GetValue(out var newVal, out var type);
                            _attrdictionary.Add(key, newVal);
                        }
                    }
                    else
                    {
                        if (attributeNamesbyId.TryGetValue(k.Key, out var key))
                        {
                            var val = k.Value.Attribute("value").GetValue(out var newVal, out var type);
                            _attrdictionary.Add(key, newVal);
                        }
                    }
                }
            }

            var exists = DirectEve.PySharp.Import("evetypes").Call("Exists", this.TypeId).ToBool();
            if (exists)
            {
                var invType = new DirectInvType(DirectEve, this.TypeId);
                invType._attrdictionary = _attrdictionary;
                invTypeCache[this.TypeId] = invType;
            }
            //invTypeCache[this.TypeId] = this; // add to cache
            return _attrdictionary;
        }

        /// <summary>
        ///     Retrieves the bracket data
        /// </summary>
        /// <returns></returns>
        public PyObject GetBracketData()
        {
            var bracketSvc = DirectEve.GetLocalSvc("bracket");

            int getBracketId()
            {
                if (bracketSvc.IsValid)
                {
                    var bracketDataByTypeID = bracketSvc.Attribute("bracketDataByTypeID");
                    if (bracketDataByTypeID.IsValid)
                    {
                        var bd = bracketDataByTypeID.Call("get", TypeId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }

                    var bracketDataByGroupID = bracketSvc.Attribute("bracketDataByGroupID");
                    if (bracketDataByGroupID.IsValid)
                    {
                        var bd = bracketDataByGroupID.Call("get", GroupId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }

                    var bracketDataByCategoryID = bracketSvc.Attribute("bracketDataByCategoryID");
                    if (bracketDataByCategoryID.IsValid)
                    {
                        var bd = bracketDataByCategoryID.Call("get", CategoryId, PySharp.PyNone);
                        if (bd.IsValid)
                            return bd.ToInt();
                    }
                }

                return 0;
            }

            var bracketId = getBracketId();
            if (bracketId != 0)
            {
                var bracketData = bracketSvc.Call("GetBrackeDatatByID", bracketId);
                return bracketData;
            }

            return PySharp.PyZero;
        }

        /// <summary>
        ///     Retrieves the bracket name, 'NPC Battleship' for example
        /// </summary>
        /// <returns></returns>
        public String GetBracketName()
        {
            if (_bracketNameDictionary.TryGetValue(TypeId, out var name))
                return name;

            name = string.Empty;
            var bd = GetBracketData();
            if (bd.IsValid) name = (string)bd.Attribute("name");
            if (GroupId == 446)
            {
                _bracketNameDictionary[TypeId] = BracketType.Navy_Concord_Customs.ToString();
            }
            else
            {
                _bracketNameDictionary[TypeId] = name;
            }
            return _bracketNameDictionary[TypeId];
        }

        public String GetBracketTexturePath()
        {
            if (_bracketTexturePathDictionary.TryGetValue(TypeId, out var texturePath))
                return texturePath;

            texturePath = string.Empty;
            var bd = GetBracketData();
            if (bd.IsValid) texturePath = (string)bd.Attribute("texturePath");
            if (string.IsNullOrEmpty(texturePath))
                texturePath = string.Empty;
            _bracketTexturePathDictionary[TypeId] = texturePath;
            return texturePath;
        }

        public virtual T TryGet<T>(string keyname)
        {
            object obj = null;
            if (GetAttributesInvType().ContainsKey(keyname))
            {
                var item = GetAttributesInvType()[keyname];
                if (item != null)
                {
                    if (typeof(T) == typeof(bool))
                    {
                        obj = (int)item;
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(string))
                    {
                        obj = (string)item;
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(int))
                    {
                        obj = (int)item;
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(long))
                    {
                        obj = (long)item;
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(float))
                    {
                        obj = (float)item;
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(double))
                    {
                        obj = Convert.ToDouble(item);
                        return (T)obj;
                    }

                    if (typeof(T) == typeof(DateTime))
                    {
                        obj = Convert.ToDateTime(item);
                        return (T)obj;
                    }
                }
            }

            return default(T);
        }

        public static DirectInvType GetInvType(DirectEve directEve, int typeId)
        {
            if (invTypeCache.TryGetValue(typeId, out var cachedInvType))
            {
                return cachedInvType;
            }
            else
            {
                var exists = directEve.PySharp.Import("evetypes").Call("Exists", typeId).ToBool();
                DirectInvType ret = null;
                if (exists)
                {
                    ret = new DirectInvType(directEve, typeId);
                    invTypeCache[typeId] = ret;
                }
                return ret;
            }
        }

        /// <summary>
        /// Per second
        /// </summary>
        public double FlatArmorLocalRepairAmount
        {
            get
            {
                var amount = TryGet<float>("behaviorArmorRepairerAmount");
                var duration = TryGet<float>("behaviorArmorRepairerDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }
        /// <summary>
        /// Per second
        /// </summary>
        public double FlatShieldLocalRepairAmount
        {
            get
            {
                var amount = TryGet<float>("behaviorShieldBoosterAmount");
                var duration = TryGet<float>("behaviorShieldBoosterDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }

        /// <summary>
        /// Per second
        /// </summary>
        public double FlatArmorRemoteRepairAmount
        {
            get
            {
                var amount = TryGet<float>("armorDamageAmount");
                var duration = TryGet<float>("behaviorRemoteArmorRepairDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }

        /// <summary>
        /// Per second (take care, there can be a chance 'npcRemoteShieldBoostChance'. also for armor)
        /// </summary>
        public double FlatShieldRemoteRepairAmount
        {
            get
            {
                var amount = TryGet<float>("npcRemoteShieldBoostAmount");
                var duration = TryGet<float>("npcRemoteShieldBoostDuration"); // milliseconds

                if (duration <= 0)
                    return 0;

                if (amount <= 0)
                    return 0;

                return amount / (duration / 1000);
            }
        }


        public double FlatShieldArmorRemotelRepairAmountCombined
        {
            get
            {
                return FlatArmorRemoteRepairAmount + FlatShieldRemoteRepairAmount;
            }
        }


        public double FlatShieldArmorLocalRepairAmountCombined
        {
            get
            {
                return FlatArmorLocalRepairAmount + FlatShieldLocalRepairAmount;
            }
        }


        //public static Dictionary<string, int> GetInvTypeNames(DirectEve directEve)
        //{
        //    var result = new Dictionary<string, int>();
        //    var pyDict = directEve.PySharp.Import("evetypes").Attribute("storages").Attribute("TypeStorage").Attribute("_storage").ToDictionary<int>();
        //    foreach (var pair in pyDict) result[new DirectInvType(directEve, pair.Key).TypeName] = pair.Key;
        //    return result;
        //}

        #endregion Methods
    }
}