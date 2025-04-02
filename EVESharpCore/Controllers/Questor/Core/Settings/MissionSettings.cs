extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using DamageType = SC::SharedComponents.EVE.ClientSettings.DamageType;
using EveAccount = SC::SharedComponents.EVE.EveAccount;
using FactionFitting = SC::SharedComponents.EVE.ClientSettings.FactionFitting;
using FactionType = SC::SharedComponents.EVE.FactionType;
using MissionFitting = SC::SharedComponents.EVE.ClientSettings.MissionFitting;
using QuestorFaction = SC::SharedComponents.EVE.ClientSettings.QuestorFaction;
using QuestorMission = SC::SharedComponents.EVE.ClientSettings.QuestorMission;
using Util = SC::SharedComponents.Utility.Util;
using WCFClient = SC::SharedComponents.IPC.WCFClient;

namespace EVESharpCore.Controllers.Questor.Core.Settings
{
    public class MissionSettings
    {
        #region Constructors

        public MissionSettings()
        {
            MissionItems = new List<string>();
            MissionUseDrones = null;
            _currentBestDamageTypes = new List<DamageType>();
        }

        #endregion Constructors

        #region Fields

        public string LastGreylistMissionDeclined = string.Empty;
        public bool? MissionDronesKillHighValueTargets = null;
        public double? MissionOptimalRange = null;
        public DamageType? _currentDamageType;
        private List<DamageType> _currentBestDamageTypes;
        private DateTime _nextSecondBestDamageTypeSwap = DateTime.MinValue;
        private DateTime _nextThirdBestDamageTypeSwap = DateTime.MinValue;
        private DamageType? _previousDamageType;

        #endregion Fields

        #region Properties


        public void ClearDamageTypeCache()
        {
            _currentDamageType = null;
            _currentBestDamageTypes = null;
            _nextSecondBestDamageTypeSwap = DateTime.MinValue;
            _nextThirdBestDamageTypeSwap = DateTime.MinValue;
            _previousDamageType = null;
        }

        public DamageType? CurrentDamageType
        {
            get
            {
                if (DirectEve.HasFrameChanged(nameof(CurrentDamageType)) || !_currentDamageType.HasValue)
                {
                    _currentBestDamageTypes = GetBestDamageTypes;
                    var bestDamageType = _currentBestDamageTypes.FirstOrDefault();

                    if (bestDamageType != _previousDamageType)
                    {
                        if (ESCache.Instance.InMission && _previousDamageType.HasValue && _currentBestDamageTypes.Any())
                        {
                            var weaponTarget = ESCache.Instance.Combat.CurrentWeaponTarget;
                            var isBattleship = weaponTarget != null && weaponTarget.BracketType == BracketType.NPC_Battleship;
                            // get the index of the previous damage type to choose when to swap the damage type
                            var indexPreviousDamageType = _currentBestDamageTypes.IndexOf(_previousDamageType.Value);
                            switch (indexPreviousDamageType)
                            {
                                case 0: // don't swap
                                    Log.WriteLine($"Index 0. (this case shouldn't happen at all)");
                                    break;

                                case 1: // swap every 2 minutes?
                                    Log.WriteLine($"Index 1. (2nd best)");
                                    if (_nextSecondBestDamageTypeSwap < DateTime.UtcNow)
                                    {
                                        Log.WriteLine($"Swapping damage type {bestDamageType}, next 2nd best swap will in 2 minutes.");
                                        _nextSecondBestDamageTypeSwap = DateTime.UtcNow.AddSeconds(120);
                                    }
                                    else
                                    {
                                        bestDamageType = _previousDamageType.Value;
                                        Log.WriteLine($"Keeping previous damage type {bestDamageType}");
                                    }

                                    break;

                                case 2: // swap every 1 minute?
                                    Log.WriteLine($"Index 2. (3rd best)");
                                    if (_nextThirdBestDamageTypeSwap < DateTime.UtcNow || isBattleship)
                                    {
                                        Log.WriteLine($"Swapping damage type {bestDamageType}, next 3rd best swap will in 60 seconds.");
                                        _nextThirdBestDamageTypeSwap = DateTime.UtcNow.AddSeconds(60);
                                    }
                                    else
                                    {
                                        bestDamageType = _previousDamageType.Value;
                                        Log.WriteLine($"Keeping previous damage type {bestDamageType}");
                                    }

                                    break;

                                case 3: // swap now
                                    Log.WriteLine($"Index 3. (4th best)");
                                    break;
                            }
                        }

                        _previousDamageType = bestDamageType;
                    }

                    if (ESCache.Instance.InSpace && !AnyAmmoOfTypeLeft(bestDamageType)) // pick the next best ammo if there is nothing left of the best ammo
                        if (_currentBestDamageTypes.Any())
                            for (var j = 0; j < _currentBestDamageTypes.Count; j++)
                            {
                                var ammo = ESCache.Instance.Combat.Ammo.FirstOrDefault(a => a.DamageType == _currentBestDamageTypes[j]);
                                if (ammo != null)
                                    if (AnyAmmoOfTypeLeft(ammo.DamageType))
                                    {
                                        bestDamageType = ammo.DamageType;
                                        _previousDamageType = bestDamageType;
                                        //Log.WriteLine($"We have enough ammo of type [{bestDamageType}] left in cargo / launchers.");
                                        break;
                                    }
                                    else
                                    {
                                        Log.WriteLine(
                                            $"We DON'T have more ammo of type {ammo.DamageType} left in our cargo / launchers. Picking next best ammo.");
                                        continue;
                                    }
                            }

                    _currentDamageType = bestDamageType;
                }

                return _currentDamageType;
            }
        }

        public int CurrentDroneTypeId => MissionFittingCurrentMission?.DronetypeId
                                                ?? FactionFittingCurrentMission?.DronetypeId
                                                ?? ESCache.Instance.EveAccount.CS.QMS.QS.DroneTypeId;

        public string CurrentFittingName
        {
            get => ESCache.Instance.EveAccount.CurrentFit?.ToLower();
            set => WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.CurrentFit), value.ToLower());
        }

        public FactionType CurrentMissionFaction { get; set; }

        public bool DeclineMissionsWithTooManyMissionCompletionErrors =>
            ESCache.Instance.EveAccount.CS.QMS.QS.DeclineMissionsWithTooManyMissionCompletionErrors;

        public FactionFitting DefaultFitting => FactionFittings.FirstOrDefault(f => f.FactionType == FactionType.Unknown);

        public bool? DelayBlitzIfBattleshipOnGrid { get; set; }
        public bool? DelayBlitzIfLargeWreckOnGrid { get; set; }

        public bool? DisableBlitz { get; set; }

        public List<QuestorFaction> FactionBlacklist => ESCache.Instance.EveAccount.CS.QMS.QS.Factionblacklist.ToList();

        public FactionFitting FactionFittingCurrentMission =>
            FactionFittings.FirstOrDefault(f => f.FactionType == CurrentMissionFaction);

        public List<FactionFitting> FactionFittings => ESCache.Instance.EveAccount.CS.QMS.QS.Factionfittings.ToList();

        public string FittingToLoad => MissionFittingCurrentMission?.FittingName?.ToLower()
                                                      ?? FactionFittingCurrentMission?.FittingName?.ToLower()
                                              ?? DefaultFitting.FittingName.ToLower();

        private static List<DamageType> _defaultDamgeTypes;

        private static List<DamageType> DefaultDamageTypes
        {
            get
            {
                if (_defaultDamgeTypes == null)
                    _defaultDamgeTypes = Enum.GetValues(typeof(DamageType)).Cast<DamageType>().ToList();
                return _defaultDamgeTypes;
            }
        }

        public List<DamageType> GetBestDamageTypes
        {
            get
            {
                var bestDamageTypes = ESCache.Instance.Targets.FirstOrDefault(e => e.IsCurrentTarget)?.BestDamageTypes
                                      ?? ESCache.Instance.Targets.FirstOrDefault(e => e.BracketType == BracketType.NPC_Frigate
                                                                                      || e.BracketType == BracketType.Battlecruiser
                                                                                      || e.BracketType == BracketType.NPC_Cruiser
                                                                                      || e.BracketType == BracketType.NPC_Battleship
                                                                                      || e.BracketType == BracketType.NPC_Destroyer)?.BestDamageTypes
                                      ?? ESCache.Instance.EntitiesOnGrid.FirstOrDefault(e => e.BracketType == BracketType.NPC_Frigate
                                                                                                                || e.BracketType == BracketType.Battlecruiser
                                                                                                                || e.BracketType == BracketType.NPC_Cruiser
                                                                                                                || e.BracketType == BracketType.NPC_Battleship
                                                                                                                || e.BracketType == BracketType.NPC_Destroyer)?.BestDamageTypes
                                      ?? DefaultDamageTypes;
                return bestDamageTypes;
            }
        }

        public bool IsBlackListedFaction => FactionBlacklist.Any(m => m.FactionType == CurrentMissionFaction);
        public float MinAgentGreyListStandings => ESCache.Instance.EveAccount.CS.QMS.QS.MinAgentGreyListStandings;
        public DirectAgentMission Mission => ESCache.Instance.Agent.Mission;
        public int? MissionActivateRepairModulesAtThisPerc { get; set; }
        public List<QuestorMission> MissionBlacklist => ESCache.Instance.EveAccount.CS.QMS.QS.Blacklist.Where(m => m.Name != null).ToList();
        public MissionFitting MissionFittingCurrentMission => MissionFittings.FirstOrDefault(f => f.Mission.ToLower().Equals(MissionName.ToLower()));
        public List<MissionFitting> MissionFittings => ESCache.Instance.EveAccount.CS.QMS.QS.Missionfittings.ToList();
        public List<QuestorMission> MissionGreylist => ESCache.Instance.EveAccount.CS.QMS.QS.Greylist.Where(m => m.Name != null).ToList();
        public List<string> MissionItems { get; private set; }
        public bool? MissionKillSentries { get; set; }
        public string MissionName => Log.FilterPath(Mission?.Name);

        public string MissionsPath => Path.Combine(Util.AssemblyPath, "QuestorMissions",
            ESCache.Instance.EveAccount.CS.QMS.QS.MissionLevel.ToString().Replace("_", " "));

        public bool? MissionUseDrones { get; set; }
        public double MissionWarpAtDistanceRange { get; set; }
        public bool MissionXMLIsAvailable { get; set; }
        public string MissionXmlPath { get; set; }
        public string MoveMissionItems { get; set; }
        public int MoveMissionItemsQuantity { get; set; }
        public int MoveOptionalMissionItemQuantity { get; set; }
        public string MoveOptionalMissionItems { get; set; }
        public bool OfflineModulesFound { get; set; }
        public bool? PocketUseDrones { get; set; }

        #endregion Properties

        #region Methods


        public bool AnyAmmoOfTypeLeft(DamageType t)
        {
            var ammo = ESCache.Instance.Combat.Ammo.FirstOrDefault(a => a.DamageType == t);
            if (ammo != null)
                if (ESCache.Instance.CurrentShipsCargo?.Items.Any(i =>
                        !i.IsSingleton && i.TypeId == ammo.TypeId && i.Quantity > ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges) ?? false
                    || ESCache.Instance.DirectEve.Weapons.Where(w => w.Charge != null && w.Charge.TypeId == ammo.TypeId).Sum(w => w.CurrentCharges) > 0)
                    return true;
            return false;
        }

        public void ClearMissionSpecificSettings()
        {
            MissionDronesKillHighValueTargets = null;
            MissionWarpAtDistanceRange = 0;
            MissionXMLIsAvailable = true;
            MissionKillSentries = null;
            MissionUseDrones = null;
            MissionOptimalRange = null;
        }

        public void ClearPocketSpecificSettings()
        {
            PocketUseDrones = null;
        }

        public void LoadMissionXmlData()
        {
            Log.WriteLine("Loading mission xml [" + MissionName + "] from [" + MissionXmlPath + "]");
            XDocument missionXml;
            try
            {
                missionXml = XDocument.Load(MissionXmlPath);

                if (missionXml.Root != null)
                {
                    MissionUseDrones = (bool?)missionXml.Root.Element("useDrones");
                    MissionKillSentries = (bool?)missionXml.Root.Element("killSentries");
                    DelayBlitzIfBattleshipOnGrid = (bool?)missionXml.Root.Element("DelayBlitzIfBattleshipOnGrid");
                    DelayBlitzIfLargeWreckOnGrid = (bool?)missionXml.Root.Element("DelayBlitzIfLargeWreckOnGrid");
                    DisableBlitz = (bool?)missionXml.Root.Element("DisableBlitz");
                    MissionWarpAtDistanceRange = (int?)missionXml.Root.Element("missionWarpAtDistanceRange") ?? 0;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Error in mission (not pocket) specific XML tags [" + MissionName + "], " + ex.Message);
            }
        }

        public void SetmissionXmlPath(string missionName)
        {
            try
            {
                var factionName = CurrentMissionFaction.ToString().Replace("_", " ");
                var shipTypeName = ESCache.Instance.ActiveShip.TypeName;

                var missionXmlPathFactionTypeName = Path.Combine(MissionsPath, $"{Log.FilterPath(missionName)}-{factionName}-{shipTypeName}.xml");
                var missionXmlPathFaction = Path.Combine(MissionsPath, $"{Log.FilterPath(missionName)}-{factionName}.xml");
                var missionXmlPathTypeName = Path.Combine(MissionsPath, $"{Log.FilterPath(missionName)}-{shipTypeName}.xml");
                var missionXmlPath = Path.Combine(MissionsPath, $"{Log.FilterPath(missionName)}.xml");

                Log.WriteLine($"XML filename/exist (used in that order):" +
                              $" {missionXmlPathFactionTypeName.Replace(MissionsPath, "")}" +
                              $" [{(File.Exists(missionXmlPathFactionTypeName) ? "X" : " ")}]" +
                              $" {missionXmlPathFaction.Replace(MissionsPath, "")}" +
                              $" [{(File.Exists(missionXmlPathFaction) ? "X" : " ")}]" +
                              $" {missionXmlPathTypeName.Replace(MissionsPath, "")}" +
                              $" [{(File.Exists(missionXmlPathTypeName) ? "X" : " ")}]" +
                              $" {missionXmlPath.Replace(MissionsPath, "")}" +
                              $" [{(File.Exists(missionXmlPath) ? "X" : " ")}]");

                MissionXmlPath = File.Exists(missionXmlPathFactionTypeName) ? missionXmlPathFactionTypeName :
                    File.Exists(missionXmlPathFaction) ? missionXmlPathFaction :
                    File.Exists(missionXmlPathTypeName) ? missionXmlPathTypeName :
                    missionXmlPath;

            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        #endregion Methods
    }
}