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

using EVESharpCore.Cache;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace EVESharpCore.Framework
{
    public enum IncursionEffect
    {
        effectIcon_cyno,
        effectIcon_tax,
        effectIcon_tank,
        effectIcon_damage,
        effectIcon_armor,
        effectIcon_shield,
        effectIcon_velocity
    }

    public enum NegativeBoosterEffect
    {
        boosterAOEVelocityPenalty,
        boosterCapacitorCapacityPenalty,
        boosterShieldCapacityPenalty,
        boosterArmorHPPenalty,
        boosterMaxVelocityPenalty,
        boosterMissileVelocityPenalty,
        boosterShieldBoostAmountPenalty,
        boosterArmorRepairAmountPenalty,
        boosterTurretFalloffPenalty,
        boosterTurretOptimalRangePenalty,
        boosterMissileExplosionVelocityPenalty,

    }


    public enum DBuffEffect
    {
        dbuffPlaceholder01 = 1,
        dbuffPlaceholder02 = 2,
        dbuffVelocityPenalty = 3,
        dbuffWarpPenalty = 4,
        dbuffDisallowCloak = 5,
        dbuffDisallowJumpOrDock = 6,
        dbuffAntiTargetingDebuffQA = 7,
        dbuffInvulnerability_ShieldResistance = 8,
        dbuffInvulnerability_ShieldRecharge = 9,
        dbuffShieldBurst_ShieldHarmonizing = 10,
        dbuffShieldBurst_ActiveShielding = 11,
        dbuffShieldBurst_ShieldExtension = 12,
        dbuffArmorBurst_ArmorEnergizing = 13,
        dbuffArmorBurst_RapidRepair = 14,
        dbuffArmorBurst_ArmorReinforcement = 15,
        dbuffInformationBurst_SensorOptimization_ScanResolution = 16,
        dbuffInformationBurst_ElectronicSuperiority = 17,
        dbuffInformationBurst_ElectronicHardening_Resistance = 18,
        dbuffInformationBurst_ElectronicHardening_ScanStrength = 19,
        dbuffSkirmishBurst_EvasiveManeuvers_Signature = 20,
        dbuffSkirmishBurst_InterdictionManeuvers = 21,
        dbuffSkirmishBurst_RapidDeployment = 22,
        dbuffMiningBurst_MiningLaserFieldEnhancement = 23,
        dbuffMiningBurst_MiningLaserOptimization = 24,
        dbuffMiningBurst_MiningEquipmentPreservation = 25,
        dbuffInformationBurst_SensorOptimization_TargetingRange = 26,
        dbuffStasisWebificationBurst = 27,
        dbuffWeaponDisruptionBurst_TurretMaxRange = 28,
        dbuffWeaponDisruptionBurst_TurretFalloffRange = 29,
        dbuffWeaponDisruptionBurst_TurretTracking = 30,
        dbuffWeaponDisruptionBurst_MissileVelocity = 31,
        dbuffWeaponDisruptionBurst_MissileDuration = 32,
        dbuffWeaponDisruptionBurst_ExplosionVelocity = 33,
        dbuffWeaponDisruptionBurst_ExplosionRadius = 34,
        dbuffSensorDampeningBurst_ScanResolution = 35,
        dbuffSensorDampeningBurst_MaxTargetRange = 36,
        dbuffTargetIlluminationBurst = 37,
        dbuffECMBurst = 38,
        dbuffAvatarTitanBurst_Bonus1 = 39,
        dbuffAvatarTitanBurst_Bonus2 = 40,
        dbuffAvatarTitanBurst_Penalty1 = 41,
        dbuffErebusTitanBurst_Bonus1 = 42,
        dbuffErebusTitanBurst_Bonus2 = 43,
        dbuffErebusTitanBurst_Penalty1 = 44,
        dbuffRagnarokTitanBurst_Bonus1 = 45,
        dbuffRagnarokTitanBurst_Bonus2 = 46,
        dbuffRagnarokTitanBurst_Penalty1 = 47,
        dbuffLeviathanTitanBurst_Bonus1 = 48,
        dbuffLeviathanTitanBurst_Bonus2 = 49,
        dbuffLeviathanTitanBurst_Penalty1 = 50,
        dbuffAvatarTitanBurst_Penalty2 = 51,
        dbuffErebusTitanBurst_Penalty2 = 52,
        dbuffLeviathanTitanBurst_Penalty2 = 53,
        dbuffRagnarokTitanBurst_Penalty2 = 54,
        dbuffInvulnerability_ScanResolution = 55,
        dbuffInvulnerability_MassIncrease = 56,
        dbuffDisallowTether = 57,
        dbuffInvulnerability_DroneDamagePenalty = 58,
        dbuffInvulnerability_DisallowWeapons = 59,
        dbuffSkirmishBurst_EvasiveManeuvers_Agility = 60,
        dbuffInvulnerability_DisallowEntosis = 61,
        dbuffWeather_MaxTargetRange_Bonus = 65,
        dbuffWeather_TargetLockSpeed_Bonus = 66,
        dbuffWeather_StasisWebRange_Bonus = 67,
        dbuffWeather_EmResistance_Bonus = 68,
        dbuffWeather_ArmorResistance_Bonus = 73,
        dbuffWeather_MaxVelocity_Bonus = 74,
        dbuffWeather_SignatureRadius_Penalty = 75,
        dbuffWeather_WeaponDisruption_Bonus = 76,
        dbuffAoe_BioluminescenceCloud_Signature_Penalty = 79,
        dbuffAoe_CausticCloud_inertia_buff = 80,
        dbuffAoe_CausticCloud_max_velocity_buff = 81,
        dbuffAoe_PulsePlatform_TrackingSpeed_Bonus = 84,
        dbuffAoe_FilamentCloud_ShieldBoostShieldBoost_Penalty = 88,
        dbuffAoe_FilamentCloud_ShieldBoostDuration_Bonus = 89,
        dbuffWeather_ElectricStorm_EmResistance_Penalty = 90,
        dbuffWeather_ElectricStorm_CapacitorRecharge_Bonus = 92,
        dbuffWeather_XenonGas_ExplosiveResistance_Penalty = 93,
        dbuffWeather_XenonGas_ShieldHp_Bonus = 94,
        dbuffWeather_Infernal_ThermalResistance_Penalty = 95,
        dbuffWeather_Infernal_ArmorHp_Bonus = 96,
        dbuffWeather_Darkness_TurretRange_Penalty = 97,
        dbuffWeather_Darkness_Velocity_Bonus = 98,
        dbuffWeather_CausticToxin_KineticResistance_Penalty = 99,
        dbuffWeather_CausticToxin_ScanResolution_Bonus = 100,
        dbuffAoe_DamageBoost = 2103,
        dbuffWreck_WarpScramble = 2104,
        dbuffStablizeCloak = 2113,
        dbuffLinkWithShip_Resists = 2141,
        dbuffSpacetimeNexusVelocity = 2147,
        dbuffSpacetimeNexusInertia = 2148,
        dbuffSpacetimeNexusRecharge = 2149,
        dbuffSpacetimeNexusModuleCycle = 2150,
        dbuffSpacetimeNexusTracking = 2151,
        dbuffSpacetimeNexusCapRecharge = 2167,
        dbuffSpacetimeNexusSigRadius = 2168,
        dbuffSpacetimeNexusTurretMissileDamage = 2169,
        dbuffAoe_proving_tachyon_signatureradius_penalty = 2174,
        dbuffTetheringRestrictedBySecurity = 2185,
    }


    /// <summary>
    /// weatherInfernal == Firestorm, weatherCausticToxin == Exotic, weatherDarkness == Dark, weatherElectricStorm == Electric, weatherXenonGas == Gamma
    /// </summary>
    public enum HudStatusEffect
    {
        LinkedToESSMainBank,
        aoeBioluminescenceCloud,
        warpScramblerMWD,
        ewTrackingDisrupt,
        aoeCausticCloud,
        tetheringRestricted,
        titanBurst,
        electronic,
        LinkedToESSReserveBank,
        webify,
        notTethered,
        shieldTransfer,
        LinkWithShipBonuses,
        remoteHullRepair,
        strangeEffect,
        CloakDisrupt,
        aoePointDefense,
        aoePulsePlatform,
        shieldBurst,
        focusedWarpScrambler,
        skirmishBurst,
        ewEnergyVampire,
        armorBurst,
        informationBurst,
        miningBurst,
        remoteTracking,
        eccmProjector,
        weatherInfernal, // Firestorm
        weatherCausticToxin, // Exotic
        ewTargetPaint,
        ewRemoteSensorDamp,
        RemoteArmorMutadaptiveRepairer,
        weatherDarkness, // Dark
        sensorBooster,
        ewGuidanceDisrupt,
        tethering,
        aoeDamageBoost,
        weatherElectricStorm, // Electric
        ewEnergyNeut,
        aoeFilamentCloud,
        remoteArmorRepair,
        CloakDefense,
        warpScrambler,
        energyTransfer,
        fighterTackle,
        tetheringRepair,
        invulnerabilityBurst,
        weatherXenonGas, // Gamma
    }


    public class DirectMe : DirectObject
    {
        #region Fields

        /// <summary>
        ///     Attribute cache
        /// </summary>
        private DirectItemAttributes _attributes;

        private bool? _IsOmegaClone = null;

        private Dictionary<HudStatusEffect, PyObject> _buffActiveBuffBarEntries = new Dictionary<HudStatusEffect, PyObject>();
        private bool _buffActiveBuffBarEntriesChecked = false;

        #endregion Fields

        #region Constructors

        internal DirectMe(DirectEve directEve) : base(directEve)
        {
            _attributes = new DirectItemAttributes(directEve, directEve.Session.CharacterId ?? -1);
        }

        #endregion Constructors

        #region Properties

        public List<DirectBooster> Boosters =>
            DirectEve.GetLocalSvc("crimewatchSvc").Call("GetMyBoosters").ToList().Select(k => new DirectBooster(DirectEve, k)).ToList();

        public DirectSolarSystem CurrentSolarSystem => DirectEve.SolarSystems.TryGetValue(DirectEve.Session.SolarSystemId.Value, out var ss) ? ss : null;

        public List<IncursionEffect> IncursionEffects => IsIncursionActive
                    ? DirectEve.GetLocalSvc("incursion").Attribute("incursionData").Attribute("effects").ToList<PyObject>().Select(k =>
                (IncursionEffect)Enum.Parse(typeof(IncursionEffect), k.ToDictionary<string>()["name"].ToUnicodeString())).ToList()
            : new List<IncursionEffect>();

        public float IncursionInfluence => IsIncursionActive
            ? DirectEve.GetLocalSvc("incursion").Attribute("incursionData").Attribute("influenceData").Attribute("influence").ToFloat()
            : 0;

        public DateTime? ServerShutDownTime => DirectEve.GetLocalSvc("gameui")?.Attribute("shutdownTime")?.ToDateTime();

        public TimeSpan TimeTillDownTime => (ServerShutDownTime ?? DateTime.MaxValue) - DateTime.UtcNow;

        private Dictionary<DBuffEffect, double?> _dbuffCache = new Dictionary<DBuffEffect, double?>();

        private (DirectDamageType, double)? _abyssResistsDebuff = null;

        /// <summary>
        /// Return true between 10:50 and 11:03 UTC
        /// </summary>
        /// <returns></returns>
        public bool IsDownTimeComingSoon()
        {
            DateTime currentTime = DateTime.UtcNow;
            TimeSpan startTime = new TimeSpan(10, 50, 0);
            TimeSpan endTime = new TimeSpan(11, 3, 0);
            return currentTime.TimeOfDay >= startTime && currentTime.TimeOfDay <= endTime;
        }

        public (DirectDamageType, double)? GetAbyssResistsDebuff()
        {

            //dbuffWeather_ElectricStorm_EmResistance_Penalty // ELECTRICAL - EM
            //dbuffWeather_XenonGas_ExplosiveResistance_Penalty // GAMMA - EXPLO 
            //dbuffWeather_Infernal_ThermalResistance_Penalty // FIRESTORM - THERM
            //dbuffWeather_CausticToxin_KineticResistance_Penalty // EXOTIC - KINETIC

            if (_abyssResistsDebuff != null)
                return _abyssResistsDebuff;

            if (!IsInAbyssalSpace())
                return null;

            var em = GetDbuffValue(DBuffEffect.dbuffWeather_ElectricStorm_EmResistance_Penalty);
            if (em != null)
                _abyssResistsDebuff = (DirectDamageType.EM, em.Value);


            var explo = GetDbuffValue(DBuffEffect.dbuffWeather_XenonGas_ExplosiveResistance_Penalty);
            if (explo != null)
                _abyssResistsDebuff = (DirectDamageType.EXPLO, explo.Value);


            var therm = GetDbuffValue(DBuffEffect.dbuffWeather_Infernal_ThermalResistance_Penalty);
            if (therm != null)
                _abyssResistsDebuff = (DirectDamageType.THERMAL, therm.Value);

            var kin = GetDbuffValue(DBuffEffect.dbuffWeather_CausticToxin_KineticResistance_Penalty);
            if (kin != null)
                _abyssResistsDebuff = (DirectDamageType.KINETIC, kin.Value);


            return _abyssResistsDebuff;
        }

        public bool HasDbuff(DBuffEffect dBuffEffect)
        {

            if (_dbuffCache.ContainsKey(dBuffEffect))
                return true;

            var incDebuffs = DirectEve.GetLocalSvc("dbuffClient")["incomingDbuffTracker"]["incomingDbuffs"];

            if (!incDebuffs.IsValid)
            {
                DirectEve.Log($"PyObject __builtin__.sm.services[dbuffClient].incomingDbuffTracker.incomingDbuffs is not valid!");
            }

            var incDebuffsDict = incDebuffs.ToDictionary<int>();

            if (incDebuffsDict.TryGetValue((int)dBuffEffect, out PyObject result))
            {
                _dbuffCache.Add(dBuffEffect, result.GetItemAt(0).ToDouble());
                return true;
            }
            else
            {
                _dbuffCache.Add(dBuffEffect, null);
            }

            return false;
        }
        /// <summary>
        /// Return a dbuff effect,  i.e dbuffWeather_Infernal_ThermalResistance_Penalty would return 30/50/70 depending on abyss tier.
        /// Will return null if the requested dbuff is not affecting us
        /// </summary>
        /// <param name="dBuffEffect"></param>
        /// <returns></returns>
        public double? GetDbuffValue(DBuffEffect dBuffEffect)
        {
            if (HasDbuff(dBuffEffect))
                return _dbuffCache[dBuffEffect];

            return null;
        }

        public bool CanIWarp(bool ignoreSessionCheck = false)
        {
            if (!ignoreSessionCheck)
            {
                if (!DirectEve.Session.IsInSpace || DirectEve.Session.IsInDockableLocation)
                    return false;
            }
            else
            {
                if (!(DirectEve.Session.LocationId.HasValue &&
                    DirectEve.Session.LocationId == DirectEve.Session.SolarSystemId &&
                    !DirectEve.Session.Structureid.HasValue))
                    return false;
            }

            if (DirectEve?.ActiveShip?.Entity?.IsWarpingByMode ?? false)
                return false;

            // disruptor
            if (IsHudStatusEffectActive(HudStatusEffect.warpScrambler))
                return false;

            // scrambler
            if (IsHudStatusEffectActive(HudStatusEffect.warpScramblerMWD))
                return false;

            // hic point
            if (IsHudStatusEffectActive(HudStatusEffect.focusedWarpScrambler))
                return false;

            // bastion ..
            // warp bubble ..
            // siege ..
            // warp core stabs // point strength // ship specifics .. ?
            // anything else?

            return true;
        }




        public bool IsHudStatusEffectActive(HudStatusEffect eff)
        {
            if (!_buffActiveBuffBarEntriesChecked)
            {
                //int ms = 0;
                //using (new DisposableStopwatch(t => ms = (int)t.TotalMilliseconds))
                //{
                var effects = ActiveHudStatusEffects.ToList();
                //}
                //DirectEve.Log($"Took {ms} ms.");
            }
            return _buffActiveBuffBarEntries.ContainsKey(eff);
        }

        public List<HudStatusEffect> ActiveHudStatusEffects
        {
            get
            {
                if (!_buffActiveBuffBarEntriesChecked)
                {
                    _buffActiveBuffBarEntriesChecked = true;
                    var btnParByEffectType = PySharp.Import("carbonui")["uicore"]["uicore"]["layer"]["shipui"]["buffBarContainer"]["btnParByEffectType"];
                    if (btnParByEffectType.IsValid)
                    {
                        var dict = btnParByEffectType.ToDictionary<string>();
                        // print all effects debug
                        //foreach (var item in dict)
                        //{
                        //    DirectEve.Log($"{item.Key}");
                        //}
                        foreach (var item in dict)
                        {
                            if (item.Value["display"].ToBool())
                            {
                                // should be faster than enum.tryparse (due reflection usage)
                                if (EnumHelpers<HudStatusEffect>.HasKey(item.Key))
                                {
                                    var val = EnumHelpers<HudStatusEffect>.Convert(item.Key);
                                    _buffActiveBuffBarEntries.Add(val, item.Value);
                                }
                                else
                                {
                                    DirectEve.Log($"FIXME: [{item.Key}] does not exist in HudStatusEffect enum.");
                                }
                            }
                        }
                    }
                }
                return _buffActiveBuffBarEntries.Keys.ToList();
            }
        }

        /// <summary>
        ///     Are we in an active war?
        /// </summary>
        /// <returns></returns>
        public bool IsAtWar
        {
            get
            {
                //var id = DirectEve.Session.AllianceId;
                //if (id == null)
                //    id = DirectEve.Session.CorporationId;

                //var atWar = (int)DirectEve.GetLocalSvc("war").Attribute("wars").Call("AreInAnyHostileWarStates", id);
                //if (atWar == 1)
                //    return true;
                //else
                //    return false;
                try
                {
                    var allyId = DirectEve.Session.AllianceId;
                    var corpId = DirectEve.Session.CorporationId;
                    var warSvc = DirectEve.GetLocalSvc("war");
                    var wars = warSvc.Attribute("wars").Attribute("warsByWarID").ToDictionary<long>().Values;
                    //DirectEve.Log($"AllyId {allyId} CorpId {corpId}");
                    foreach (var war in wars)
                    {
                        var timeStarted = war.Attribute("timeStarted").ToDateTime();
                        var timeDeclared = war.Attribute("timeDeclared").ToDateTime();
                        var againstID = war.Attribute("againstID").ToInt();
                        var declaredByID = war.Attribute("declaredByID").ToInt();

                        //DirectEve.Log($"timeStarted {timeStarted} timeDeclared {timeDeclared} againstID {againstID} declaredByID {declaredByID}");

                        if (allyId == againstID || corpId == againstID || allyId == declaredByID || corpId == declaredByID)
                            if (timeStarted.AddMinutes(-240) < DateTime.UtcNow)
                            {
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, $"War detected! War begins at {timeStarted}"));
                                return true;
                            }
                    }

                    return false;
                }
                catch (Exception e)
                {
                    DirectEve.Log(e.ToString());
                    return true;
                }
            }
        }

        public bool DockOrJumpAllowed()
        {
            return DirectSession.LastSessionChange.AddSeconds(8) <= DateTime.UtcNow;
        }

        public void SimulateCurrentFit()
        {
            DirectEve.ThreadedCall(DirectEve.GetLocalSvc("ghostFittingSvc")["SimulateCurrentShip"]);
        }

        public bool IsIncursionActive => !DirectEve.GetLocalSvc("incursion").Attribute("incursionData").IsNone;


        private bool? _isInAbyssalSpace;
        public bool IsInAbyssalSpace(bool skipRemote = false)
        {
            if (_isInAbyssalSpace == null)
            {
                var locationId = DirectEve.Session.LocationId;
                var min = DirectEve.Const.InventoryCommonConst["mapAbyssalSystemMin"].ToInt();
                var max = DirectEve.Const.InventoryCommonConst["mapAbyssalSystemMax"].ToInt();
                _isInAbyssalSpace = min <= locationId && locationId <= max;
            }

            if (!skipRemote && DirectEve.Interval(2500, 3500))
            {
                Task.Run(() =>
                {
                    try
                    {
                        // Set the last known abyss time to the remote eve account
                        if (_isInAbyssalSpace.HasValue && _isInAbyssalSpace.Value &&
                            ESCache.Instance.EveAccount.LastAbyssEntered.AddMinutes(20) < DateTime.UtcNow)
                        {
                            Debug.WriteLine($"Set EveAccount.LastAbyssEntered [{DateTime.UtcNow}] -- IsInAbyss == true");
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                                nameof(EveAccount.LastAbyssEntered), DateTime.UtcNow);
                        }

                        if (_isInAbyssalSpace.HasValue && _isInAbyssalSpace.Value == false)
                        {
                            //Debug.WriteLine($"Set EveAccount.LastAbyssEntered RESET [{DateTime.MinValue}] -- IsInAbyss == false");
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                                nameof(EveAccount.LastAbyssEntered), DateTime.MinValue);
                        }

                        if (_isInAbyssalSpace.HasValue)
                        {
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.IsInAbyss), _isInAbyssalSpace.Value);

                            if (_isInAbyssalSpace.Value)
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.IsDocked), false);
                        }
                    }
                    catch { }
                });
            }

            return _isInAbyssalSpace.Value;
        }

        public bool IsInvuln
        {
            get
            {

                if (InvulnOverrideUntil >= DateTime.UtcNow)
                    return true;

                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var invulnTimer = combatTimerContainer.Attribute("invulnTimer");
                        return !invulnTimer.IsNone;
                    }
                }

                return false;
            }
        }

        public bool AbyssalTimerExist
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("abyssalContentExpirationTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Ship will remain in space on log-off until timer expires (5 minutes)
        /// </summary>
        public bool NPCTimerExists
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("npcTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }
        public float NPCTimerRemainingSeconds => NPCTimerExists
      ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("npcTimer").Attribute("ratio").ToFloat() * 5 * 60
      : 0;


        /// <summary>
        /// Ship will remain in space on log-off until timer expires (15 minutes)
        /// </summary>
        public bool PVPTimerExist
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("pvpTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }
        public float PVPTimerRemainingSeconds => PVPTimerExist
      ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("pvpTimer").Attribute("ratio").ToFloat() * 15 * 60
      : 0;

        /// <summary>
        ///  The criminal timer means that anyone can freely attack you without CONCORD interfering.
        /// </summary>
        public bool CriminalTimerExists
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("criminalTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }
        public float CriminalTimerRemainingSeconds => CriminalTimerExists
      ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("criminalTimer").Attribute("ratio").ToFloat() * 15 * 60
      : 0;

        /// <summary>
        /// The suspect timer means that anyone can freely attack you without CONCORD interfering. If someone engages you, a limited engagement timer is created between both of you, to allow you to shoot back at the aggressor. This causes issues with assistance however, due to interfering with a limited engagement (for more information, see limited engagement below).
        /// </summary>
        public bool SuspectTimerExists
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("engagementTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }

        public float SuspectTimerRemainingSeconds => SuspectTimerExists
      ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("engagementTimer").Attribute("ratio").ToFloat() * 15 * 60
      : 0;

        /// <summary>
        /// Unable to jump, dock or eject from, store, refit or switch ships and prevents tethering (60 seconds)
        /// </summary>
        public bool WeaponsTimerExists
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var abyssalTimer = combatTimerContainer.Attribute("weaponsTimer");
                        return !abyssalTimer.IsNone;
                    }
                }
                return false;
            }
        }

        public float WeaponsTimerRemainingSeconds => WeaponsTimerExists
      ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("weaponsTimer").Attribute("ratio").ToFloat() * 60
      : 0;


        public float AbyssalRemainingSeconds
        {
            get
            {
                var res = AbyssalTimerExist
                    ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer")
                        .Attribute("abyssalContentExpirationTimer").Attribute("ratio").ToFloat() * 20 * 60
                    : 0;

                var secondsSinceLastEntered = Math.Abs((DateTime.UtcNow - ESCache.Instance.EveAccount.LastAbyssEntered).TotalSeconds);
                var remainingSecondsBackup = 1200 - secondsSinceLastEntered;
                remainingSecondsBackup = remainingSecondsBackup <= 0 ? 0 : remainingSecondsBackup;

                if (DirectEve.Interval(5000))
                {
                    Debug.WriteLine($"EveTimerSecLeft  [{res}] BackupTimerSecLeft [{remainingSecondsBackup}]");
                }

                if (res > 0f)
                    return res;

                return (float)remainingSecondsBackup;

            }
        }


        public static DateTime InvulnOverrideUntil { get; set; } = DateTime.MinValue;

        /// <summary>
        /// The base duration is set to 30 seconds (default undock duration), use 60 seconds for abyssal exit invuln duration
        /// </summary>
        /// <param name="baseDuration"></param>
        /// <returns></returns>
        public float InvulnRemainingSeconds(int baseDuration = 30) => InvulnOverrideUntil >= DateTime.UtcNow ? (float)(InvulnOverrideUntil - DateTime.UtcNow).TotalSeconds : IsInvuln
            ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("invulnTimer").Attribute("ratio").ToFloat() * baseDuration
            : 0;

        public bool IsJumpCloakActive
        {
            get
            {
                var panelSvc = DirectEve.GetLocalSvc("infoPanel");
                if (panelSvc.IsValid)
                {
                    var combatTimerContainer = panelSvc.Attribute("combatTimerContainer");
                    if (combatTimerContainer.IsValid)
                    {
                        var jumpCloakTimer = combatTimerContainer.Attribute("jumpCloakTimer");
                        return !jumpCloakTimer.IsNone;
                    }
                }

                //!DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("jumpCloakTimer").IsNone;
                return false;
            }
        }

        public bool? IsOmegaClone
        {
            get
            {
                if (_IsOmegaClone == null)
                {
                    var b = (int)DirectEve.GetLocalSvc("cloneGradeSvc").Call("IsOmega");
                    _IsOmegaClone = b == 1;
                }

                return _IsOmegaClone;
            }
        }


        public bool IsTrialAccount => DirectEve.Session.UserType == 23;

        //  def GetOmegaTime(self):
        //return sm.RemoteSvc('subscriptionMgr').GetSubscriptionTime() - 20.11.2017
        //CLONE_STATE_ALPHA = 0
        //CLONE_STATE_OMEGA = 1
        public float JumpCloakRemainingSeconds => IsJumpCloakActive
            ? DirectEve.GetLocalSvc("infoPanel").Attribute("combatTimerContainer").Attribute("jumpCloakTimer").Attribute("ratio").ToFloat() * 60
            : 0;

        public int MaxActiveDrones => _attributes.TryGet<int>("maxActiveDrones");
        public int MaxLockedTargets => _attributes.TryGet<int>("maxLockedTargets");
        public string Name => DirectEve.GetOwner(DirectEve.Session.CharacterId ?? -1).Name;

        private bool? _isWarpingByMode;
        private static DateTime _lastInWwarp;

        public bool IsWarpingByMode
        {
            get
            {
                if (_isWarpingByMode == null)
                {
                    _isWarpingByMode = DirectEve.Session.IsInSpace && !DirectEve.Session.IsInDockableLocation && DirectEve.ActiveShip.Entity != null && DirectEve.ActiveShip.Entity.IsWarpingByMode;
                    if (_isWarpingByMode.Value)
                    {
                        _lastInWwarp = DateTime.UtcNow;
                    }
                }
                return _isWarpingByMode.Value || _lastInWwarp.AddMilliseconds(1250) > DateTime.UtcNow;
            }
        }

        /// <summary>
        ///     Retrieves days left on account after login
        /// </summary>
        //  daysLeft = sm.GetService('charactersheet').GetSubscriptionDays(force)
        // __builtin__.uicore.layer.charsel.countDownCont.subTimeEnd // duketwo 05.29.2016
        public DateTime SubTimeEnd
        {
            get
            {
                if (DirectEve.Login.AtCharacterSelection)
                {
                    var subTimeEnd = PySharp.Import("carbonui")
                        .Attribute("uicore")
                        .Attribute("uicore")
                        .Attribute("layer")
                        .Attribute("charsel")
                        .Attribute("countDownCont")
                        .Attribute("subTimeEnd")
                        .ToDateTime();
                    //var daysLeft = (int?) PySharp.Import("__builtin__").Attribute("uicore").Attribute("layer").Attribute("charsel").Attribute("details").ToDictionary<long>()[charid.Value].Attribute("daysLeft");
                    //var daysLeft = (int?)DirectEve.GetLocalSvc("charactersheet").Call("GetSubscriptionDays");

                    if (subTimeEnd > DateTime.UtcNow)
                        return subTimeEnd;
                    else
                        return DateTime.MinValue;
                }

                return DateTime.MinValue;
            }
        }

        public double Wealth => (double)DirectEve.GetLocalSvc("wallet").Attribute("wealth");

        #endregion Properties

        #region Methods

        public List<NegativeBoosterEffect> GetAllNegativeBoosterEffects()
        {
            return Boosters.SelectMany(k => GetNegativeBoosterEffects(k)).ToList();
        }

        public List<DirectDgmEffect> GetBoosterEffects(DirectBooster booster, bool negativeOnly = false)
        {
            if (!booster.PyObject.IsValid)
                return new List<DirectDgmEffect>();
            var obj = DirectEve.GetLocalSvc("crimewatchSvc").Call("GetBoosterEffects", booster.PyObject);
            return obj.ToDictionary<string>().Where(k => negativeOnly ? k.Key.Equals("negative") : true).SelectMany(b => b.Value.ToList())
                .Select(b => new DirectDgmEffect(DirectEve, b)).ToList();
        }

        public List<NegativeBoosterEffect> GetNegativeBoosterEffects(DirectBooster booster)
        {
            if (!booster.PyObject.IsValid)
                return new List<NegativeBoosterEffect>();
            var negEffects = GetBoosterEffects(booster, true);
            return negEffects.Select(k => (NegativeBoosterEffect)Enum.Parse(typeof(NegativeBoosterEffect), k.EffectName)).ToList();
        }

        public enum SafetyLevel
        {
            ShipSafetyLevelFull = 2,
            ShipSafetyLevelPartial = 1,
            ShipSafetyLevelNone = 0,
        }

        public SafetyLevel GetSafety()
        {
            var safetyLevel = DirectEve.GetLocalSvc("crimewatchSvc").Attribute("safetyLevel").ToInt();
            return (SafetyLevel)safetyLevel;
        }

        public void SetSafety(SafetyLevel sl)
        {

            if (sl == SafetyLevel.ShipSafetyLevelFull)
            {
                DirectEve.ThreadedCall(DirectEve.GetLocalSvc("crimewatchSvc")["SetSafetyLevel"],
                    DirectEve.Const.ShipSafetyLevelFull);
            }
            else if (sl == SafetyLevel.ShipSafetyLevelPartial)
            {
                DirectEve.ThreadedCall(DirectEve.GetLocalSvc("crimewatchSvc")["SetSafetyLevel"],
                    DirectEve.Const.ShipSafetyLevelPartial);
            }
            else if (sl == SafetyLevel.ShipSafetyLevelNone && (DirectEve.Me.IsOmegaClone ?? false))
            {
                DirectEve.ThreadedCall(DirectEve.GetLocalSvc("crimewatchSvc")["SetSafetyLevel"],
                    DirectEve.Const.ShipSafetyLevelNone);

            }
        }

        public double DistanceFromMe(Vec3 pos)
        {
            var curX = DirectEve.ActiveShip.Entity.X;
            var curY = DirectEve.ActiveShip.Entity.Y;
            var curZ = DirectEve.ActiveShip.Entity.Z;
            return Math.Round(Math.Sqrt((curX - pos.X) * (curX - pos.X) + (curY - pos.Y) * (curY - pos.Y) + (curZ - pos.Z) * (curZ - pos.Z)), 2);
        }

        public double DistanceFromMe(double x, double y, double z)
        {
            return DistanceFromMe(new Vec3(x, y, z));
        }

        #endregion Methods
    }
}