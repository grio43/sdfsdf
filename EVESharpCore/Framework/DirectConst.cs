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

namespace EVESharpCore.Framework
{
    extern alias SC;

    internal class DirectConst : DirectObject
    {
        #region Constructors

        internal DirectConst(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public PyObject AttributeArmorEmDamageResonance => Const.Attribute("attributeArmorEmDamageResonance");
        public PyObject AttributeArmorExplosiveDamageResonance => Const.Attribute("attributeArmorExplosiveDamageResonance");
        public PyObject AttributeArmorHP => Const.Attribute("attributeArmorHP");
        public PyObject AttributeArmorKineticDamageResonance => Const.Attribute("attributeArmorKineticDamageResonance");
        public PyObject AttributeArmorThermalDamageResonance => Const.Attribute("attributeArmorThermalDamageResonance");
        public PyObject AttributeEntityKillBounty => Const.Attribute("attributeEntityKillBounty");
        public PyObject AttributeHullHP => Const.Attribute("attributeHp");
        public PyObject AttributeShieldCapacity => Const.Attribute("attributeShieldCapacity");
        public PyObject AttributeShieldEmDamageResonance => Const.Attribute("attributeShieldEmDamageResonance");
        public PyObject AttributeShieldExplosiveDamageResonance => Const.Attribute("attributeShieldExplosiveDamageResonance");
        public PyObject AttributeShieldKineticDamageResonance => Const.Attribute("attributeShieldKineticDamageResonance");
        public PyObject AttributeShieldThermalDamageResonance => Const.Attribute("attributeShieldThermalDamageResonance");
        public PyObject AttributeSignatureRadius => Const.Attribute("attributeSignatureRadius");
        public PyObject AU => Const.Attribute("AU");
        public PyObject CategoryModule => Const.Attribute("categoryModule");
        public PyObject CategoryShip => Const.Attribute("categoryShip");
        public PyObject CategorySkill => Const.Attribute("categorySkill");
        public PyObject CategoryStructure => Const.Attribute("categoryStructure");
        public PyObject ContainerGlobal => Const.Attribute("containerGlobal");
        public PyObject ContainerHangar => Const.Attribute("containerHangar");
        public PyObject FlagCargo => Const.Attribute("flagCargo");
        public PyObject FlagCorpSAG2 => Const.Attribute("flagCorpSAG2");
        public PyObject FlagCorpSAG3 => Const.Attribute("flagCorpSAG3");
        public PyObject FlagCorpSAG4 => Const.Attribute("flagCorpSAG4");
        public PyObject FlagCorpSAG5 => Const.Attribute("flagCorpSAG5");
        public PyObject FlagCorpSAG6 => Const.Attribute("flagCorpSAG6");
        public PyObject FlagCorpSAG7 => Const.Attribute("flagCorpSAG7");
        public PyObject FlagDroneBay => Const.Attribute("flagDroneBay");
        public PyObject FlagHangar => Const.Attribute("flagHangar");
        public PyObject FlagOreHold => Const.Attribute("flagSpecializedOreHold");

        public PyObject FlagFleetHangar => Const.Attribute("flagFleetHangar");

        public PyObject FlagShipHangar => Const.Attribute("flagShipHangar");


        public int FlagSkillInTraining => (int)Const.Attribute("flagSkillInTraining");
        public PyObject FlagUnlocked => Const.Attribute("flagUnlocked");
        public PyObject FleetJobCreator => Const.Attribute("fleetJobCreator");
        public PyObject FleetRoleLeader => Const.Attribute("fleetRoleLeader");
        public PyObject FleetRoleMember => Const.Attribute("fleetRoleMember");
        public PyObject FleetRoleSquadCmdr => Const.Attribute("fleetRoleSquadCmdr");
        public PyObject FleetRoleWingCmdr => Const.Attribute("fleetRoleWingCmdr");
        public PyObject GroupAuditLogSecureContainer => Const.Attribute("groupAuditLogSecureContainer");
        public PyObject GroupWreck => Const.Attribute("groupWreck");
        public PyObject MapWormholeSystemMax => Const.Attribute("mapWormholeSystemMax");
        public PyObject MapWormholeSystemMin => Const.Attribute("mapWormholeSystemMin");
        public PyObject RangeConstellation => Const.Attribute("rangeConstellation");
        public PyObject RangeRegion => Const.Attribute("rangeRegion");
        public PyObject RangeSolarSystem => Const.Attribute("rangeSolarSystem");
        public PyObject RangeStation => Const.Attribute("rangeStation");
        private PyObject Const => PySharp.Import("__builtin__").Attribute("const");

        public PyObject AttributeEmDamage => Const.Attribute("attributeEmDamage");
        public PyObject AttributeExplosiveDamage => Const.Attribute("attributeExplosiveDamage");
        public PyObject AttributeKineticDamage => Const.Attribute("attributeKineticDamage");
        public PyObject AttributeThermalDamage => Const.Attribute("attributeThermalDamage");

        public PyObject AttributeDamageMultiplier => Const.Attribute("attributeDamageMultiplier");
        public PyObject AttributeDroneBandwidthUsed => Const.Attribute("attributeDroneBandwidthUsed");
        public PyObject AttributeRateOfFire => Const.Attribute("attributeRateOfFire");

        public PyObject AttributeDroneControlDistance => Const.Attribute("attributeDroneControlDistance");

        public PyObject AttributeAgility => Const.Attribute("attributeAgility");

        public PyObject AttributeMass => Const.Attribute("attributeMass");

        public PyObject AttributeSpeedFactor => Const.Attribute("attributeSpeedFactor");

        public PyObject AttributeSpeedBoostFactor => Const.Attribute("attributeSpeedBoostFactor");


        public PyObject AttributeMaxVelocity => Const.Attribute("attributeMaxVelocity");

        public PyObject ShipSafetyLevelFull => Const.Attribute("shipSafetyLevelFull");
        public PyObject ShipSafetyLevelPartial => Const.Attribute("shipSafetyLevelPartial");
        public PyObject ShipSafetyLevelNone => Const.Attribute("shipSafetyLevelNone");

        // Addition mass on afterburners for example
        public PyObject AttributeMassAddition => Const.Attribute("attributeMassAddition");

        public PyObject InventoryCommonConst => PySharp.Import("inventorycommon").Attribute("const");

        #endregion Properties

        #region Indexers

        public PyObject this[string flag] => Const.Attribute(flag);

        #endregion Indexers
    }
}