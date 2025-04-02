extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Logging;
using ServiceStack.OrmLite;
using StatisticsEntry = SC::SharedComponents.EVE.StatisticsEntry;
using WriteConn = SC::SharedComponents.SQLite.WriteConn;

namespace EVESharpCore.Controllers.Questor.Core.Stats
{
    public class Statistics
    {
        #region Fields

        public Dictionary<long, double> BountyValues = new Dictionary<long, double>();
        public DateTime FinishedMission = DateTime.UtcNow;
        public DateTime LastMissionCompletionError;
        public bool MissionLoggingCompleted;
        public bool PocketStatsUseIndividualFilesPerPocket = true;
        public DateTime StartedMission = DateTime.UtcNow;
        public DateTime StartedPocket = DateTime.UtcNow;
        public int WrecksThisMission;
        public int WrecksThisPocket;

        #endregion Fields

        #region Constructors

        public Statistics()
        {
        }

        #endregion Constructors

        #region Properties

        public int AgentLPRetrievalAttempts { get; set; }
        public int AmmoConsumption { get; set; }
        public int AmmoValue { get; set; }
        public int DroneRecalls { get; set; }
        public int ISKMissionReward { get; set; }

        public string LastMissionName { get; set; }
        public int LostDrones { get; set; }
        public double LowestArmorPercentageThisMission { get; set; } = 100;
        public double LowestArmorPercentageThisPocket { get; set; } = 100;
        public double LowestCapacitorPercentageThisMission { get; set; } = 100;
        public double LowestCapacitorPercentageThisPocket { get; set; } = 100;
        public double LowestShieldPercentageThisMission { get; set; } = 100;
        public double LowestShieldPercentageThisPocket { get; set; } = 100;
        public int LoyaltyPointsForCurrentMission { get; set; }
        public int LoyaltyPointsTotal { get; set; }
        public int MissionCompletionErrors { get; set; }
        public string MissionDetailsHtmlPath { get; set; }
        public int MissionsThisSession { get; set; }
        public int OutOfDronesCount { get; set; }
        public int PanicAttemptsThisMission { get; set; }
        public int PanicAttemptsThisPocket { get; set; }
        public bool PocketObjectStatisticsBool { get; set; }
        public string PocketObjectStatisticsFile { get; set; }
        public string PocketObjectStatisticsPath { get; set; }
        public string PocketStatisticsFile { get; set; }
        public string PocketStatisticsPath { get; set; }
        public int RepairCycleTimeThisMission { get; set; }
        public int RepairCycleTimeThisPocket { get; set; }

        #endregion Properties

        #region Methods

        public bool AmmoConsumptionStatistics()
        {
            if (ESCache.Instance.CurrentShipsCargo == null)
            {
                Log.WriteLine("if (Cache.Instance.CurrentShipsCargo == null)");
                return false;
            }

            var correctAmmo1 = ESCache.Instance.Combat.Ammo;
            var ammoCargo = ESCache.Instance.CurrentShipsCargo.Items.Where(i => correctAmmo1.Any(a => a.TypeId == i.TypeId));
            try
            {
                AmmoConsumption = 0;
                foreach (var item in ammoCargo)
                {
                    var ammo1 = ESCache.Instance.Combat.Ammo.FirstOrDefault(a => a.TypeId == item.TypeId);
                    if (ammo1 != null)
                        AmmoConsumption += ammo1.Quantity - item.Quantity;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception: " + exception);
            }

            return true;
        }

        public bool PocketObjectStatistics(List<EntityCache> things)
        {
            var currentPocketName = Log.FilterPath("Random-Grid");
            try
            {
                if (!String.IsNullOrEmpty(ESCache.Instance.MissionSettings.MissionName))
                    currentPocketName = Log.FilterPath(ESCache.Instance.MissionSettings.MissionName);
            }
            catch (Exception ex)
            {
                Log.WriteLine("PocketObjectStatistics: is cache.Instance.MissionName null?: exception was [" + ex.Message + "]");
            }

            PocketObjectStatisticsFile = Path.Combine(
                PocketObjectStatisticsPath,
                Log.FilterPath(ESCache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " +
                ActionControl.PocketNumber + " - ObjectStatistics.csv");

            Log.WriteLine("Logging info on the [" + things.Count + "] objects in this pocket to [" + PocketObjectStatisticsFile + "]");

            if (File.Exists(PocketObjectStatisticsFile))
                File.Delete(PocketObjectStatisticsFile);

            var objectline = "Name;Distance;TypeId;GroupId;CategoryId;IsNPC;IsPlayer;TargetValue;Velocity;ID;\r\n";
            File.AppendAllText(PocketObjectStatisticsFile, objectline);

            foreach (var thing in things.OrderBy(i => i.Distance))
            // can we somehow get the X,Y,Z coord? If we could we could use this info to build some kind of grid layout...,or at least know the distances between all the NPCs... thus be able to infer which NPCs were in which 'groups'
            {
                objectline = thing.Name + ";";
                objectline += Math.Round(thing.Distance / 1000, 0) + ";";
                objectline += thing.TypeId + ";";
                objectline += thing.GroupId + ";";
                objectline += thing.CategoryId + ";";
                objectline += thing.IsNpc + ";";
                objectline += thing.IsPlayer + ";";
                objectline += thing.TargetValue + ";";
                objectline += Math.Round(thing.Velocity, 0) + ";";
                objectline += thing.Id + ";\r\n";

                File.AppendAllText(PocketObjectStatisticsFile, objectline);
            }

            return true;
        }

        public void SaveMissionHTMLDetails(string MissionDetailsHtml, string missionName)
        {
            var missionDetailsHtmlFile = Path.Combine(MissionDetailsHtmlPath, missionName + " - " + "mission-description-html.txt");

            if (!Directory.Exists(MissionDetailsHtmlPath))
                Directory.CreateDirectory(MissionDetailsHtmlPath);

            if (!File.Exists(missionDetailsHtmlFile))
            {
                Log.WriteLine("Writing mission details HTML [ " + missionDetailsHtmlFile + " ]");
                File.AppendAllText(missionDetailsHtmlFile, MissionDetailsHtml);
            }
        }

        public void SaveMissionPocketObjectives(string MissionPocketObjectives, string missionName, int pocketNumber)
        {

            var missionPocketObjectivesFile = Path.Combine(MissionDetailsHtmlPath,
                missionName + " - " + "MissionPocketObjectives-Pocket[" + pocketNumber + "].txt");

            if (!Directory.Exists(MissionDetailsHtmlPath))
                Directory.CreateDirectory(MissionDetailsHtmlPath);

            if (!File.Exists(missionPocketObjectivesFile))
            {
                Log.WriteLine("Writing mission details HTML [ " + missionPocketObjectivesFile + " ]");
                File.AppendAllText(missionPocketObjectivesFile, MissionPocketObjectives);
            }
        }

        public void WriteMissionStatistics(long statisticsForThisAgent)
        {


            if (ESCache.Instance.InSpace)
            {
                Log.WriteLine("We have started questor in space, assume we do not need to write any statistics at the moment.");
                MissionLoggingCompleted = true; //if the mission was completed more than 10 min ago assume the logging has been done already.
                return;
            }

            if (AgentLPRetrievalAttempts > 5)
            {
                Log.WriteLine("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" +
                              AgentLPRetrievalAttempts +
                              "] giving up");
                AgentLPRetrievalAttempts = 0;
                MissionLoggingCompleted = true; //if it is not true - this means we should not be trying to log mission stats atm
                return;
            }

            // Seeing as we completed a mission, we will have loyalty points for this agent
            if (ESCache.Instance.Agent.LoyaltyPoints == null)
            {
                AgentLPRetrievalAttempts++;
                Log.WriteLine("WriteMissionStatistics: We do not have loyalty points with the current agent yet, still -1, attempt # [" +
                              AgentLPRetrievalAttempts +
                              "] retrying...");
                return;
            }

            AgentLPRetrievalAttempts = 0;

            var isk = Convert.ToInt32(BountyValues.Sum(x => x.Value));
            long lootValCurrentShipInv = ESCache.Instance.UnloadLoot.CurrentLootValueInCurrentShipInventory();
            long lootValItemHangar = ESCache.Instance.UnloadLoot.CurrentLootValueInItemHangar();

            MissionsThisSession++;
            Log.WriteLine("Printing All Statistics Related Variables to the console log:");
            Log.WriteLine("Mission Name: [" + LastMissionName + "]");
            Log.WriteLine("System: [" + (ESCache.Instance?.MissionSolarSystem?.Name ?? String.Empty) + "]");
            Log.WriteLine("Total Missions completed this session: [" + MissionsThisSession + "]");
            Log.WriteLine("StartedMission: [ " + StartedMission + "]");
            Log.WriteLine("FinishedMission: [ " + FinishedMission + "]");
            Log.WriteLine("Wealth before mission: [ " + ESCache.Instance.Wealth + "]");
            Log.WriteLine("Wealth after mission: [ " + ESCache.Instance.MyWalletBalance + "]");
            Log.WriteLine("Value of Loot from the mission: [" + lootValCurrentShipInv + "]");
            Log.WriteLine("Total LP after mission:  [" + ESCache.Instance.Agent.LoyaltyPoints ?? 0 + "]");
            Log.WriteLine("Total LP before mission: [" + LoyaltyPointsTotal + "]");
            Log.WriteLine("LP from this mission: [" + LoyaltyPointsForCurrentMission + "]");
            Log.WriteLine("ISKBounty from this mission: [" + isk + "]");
            Log.WriteLine("ISKMissionreward from this mission: [" + ISKMissionReward + "]");
            Log.WriteLine("Lootvalue Itemhangar: [" + lootValItemHangar + "]");
            Log.WriteLine("LostDrones: [" + LostDrones + "]");
            Log.WriteLine("DroneRecalls: [" + DroneRecalls + "]");
            Log.WriteLine("AmmoConsumption: [" + AmmoConsumption + "]");
            Log.WriteLine("AmmoValue: [" + AmmoConsumption + "]");
            Log.WriteLine("Panic Attempts: [" + PanicAttemptsThisMission + "]");
            Log.WriteLine("Lowest Shield %: [" + Math.Round(LowestShieldPercentageThisMission, 0) + "]");
            Log.WriteLine("Lowest Armor %: [" + Math.Round(LowestArmorPercentageThisMission, 0) + "]");
            Log.WriteLine("Lowest Capacitor %: [" + Math.Round(LowestCapacitorPercentageThisMission, 0) + "]");
            Log.WriteLine("Repair Cycle Time: [" + RepairCycleTimeThisMission + "]");
            Log.WriteLine("MissionXMLIsAvailable: [" + ESCache.Instance.MissionSettings.MissionXMLIsAvailable + "]");
            Log.WriteLine("MissionCompletionerrors: [" + MissionCompletionErrors + "]");
            Log.WriteLine("Total mission time: [" + FinishedMission.Subtract(StartedMission).TotalMinutes + "min]");

            var statEntry = new StatisticsEntry()
            {
                Charname = ESCache.Instance.CharName,
                Date = DateTime.UtcNow,
                Mission = LastMissionName,
                Time = (int)FinishedMission.Subtract(StartedMission).TotalMinutes,
                TotalMissionTime = (long)FinishedMission.Subtract(StartedMission).TotalMinutes,
                Isk = isk,
                IskReward = ISKMissionReward,
                Loot = lootValCurrentShipInv,
                LP = LoyaltyPointsForCurrentMission,
                LostDrones = LostDrones,
                AmmoConsumption = AmmoConsumption,
                AmmoValue = AmmoValue,
                Panics = PanicAttemptsThisMission,
                LowestShield = (int)LowestShieldPercentageThisMission,
                LowestArmor = (int)LowestArmorPercentageThisMission,
                LowestCap = (int)LowestCapacitorPercentageThisMission,
                RepairCycles = RepairCycleTimeThisMission,
                MissionXMLAvailable = ESCache.Instance.MissionSettings.MissionXMLIsAvailable,
                Faction = ESCache.Instance.MissionSettings.CurrentMissionFaction.ToString(),
                SolarSystem = (ESCache.Instance?.MissionSolarSystem?.Name ?? String.Empty),
                OutOfDronesCount = OutOfDronesCount,
                ISKWallet = (decimal)ESCache.Instance.MyWalletBalance,
                ISKLootHangarItems = lootValItemHangar,
                TotalLP = LoyaltyPointsTotal,
                MinStandingAgentCorpFaction = (decimal)ESCache.Instance.Agent.MinEffectiveStanding,
                FactionStanding = (decimal)ESCache.Instance.Agent.EffectiveFactionStanding,
            };

            if (statEntry.TotalMissionTime >= 3)
            {
                Task.Run(() => // write to db
                {
                    using (var wc = WriteConn.Open())
                    {
                        try
                        {
                            Log.WriteLine("Writing stats to db.");
                            wc.DB.Insert(statEntry);
                            Log.WriteLine("Finished writing stats to db.");
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine($"{e.ToString()}");
                        }
                    }
                });
            }
            else
            {
                Log.WriteLine("Not writing stats. TotalMissionTime was below 3min.");
            }

            MissionLoggingCompleted = true;
            LoyaltyPointsTotal = ESCache.Instance.Agent.LoyaltyPoints ?? 0;
            StartedMission = DateTime.UtcNow;
            FinishedMission = DateTime.UtcNow;

            DroneRecalls = 0;
            LostDrones = 0;
            AmmoConsumption = 0;
            AmmoValue = 0;
            MissionCompletionErrors = 0;
            OutOfDronesCount = 0;

            BountyValues = new Dictionary<long, double>();
            PanicAttemptsThisMission = 0;
            LowestShieldPercentageThisMission = 101;
            LowestArmorPercentageThisMission = 101;
            LowestCapacitorPercentageThisMission = 101;
            RepairCycleTimeThisMission = 0;
            ESCache.Instance.MissionSolarSystem = null;
            ESCache.Instance.OrbitEntityNamed = null;
        }

        public void WritePocketStatistics()
        {

            var currentPocketName = Log.FilterPath(ESCache.Instance.MissionSettings.MissionName);
            if (PocketStatsUseIndividualFilesPerPocket)
                PocketStatisticsFile = Path.Combine(PocketStatisticsPath,
                    Log.FilterPath(ESCache.Instance.DirectEve.Me.Name) + " - " + currentPocketName + " - " + ActionControl.PocketNumber +
                    " - PocketStatistics.csv");
            if (!Directory.Exists(PocketStatisticsPath))
                Directory.CreateDirectory(PocketStatisticsPath);

            if (!File.Exists(PocketStatisticsFile))
                File.AppendAllText(PocketStatisticsFile,
                    "Date and Time;Mission Name ;Pocket;Time to complete;Isk;panics;LowestShields;LowestArmor;LowestCapacitor;RepairCycles;Wrecks\r\n");

            var pocketstatsLine = DateTime.UtcNow + ";"; //Date
            pocketstatsLine += currentPocketName + ";"; //Mission Name
            pocketstatsLine += "pocket" + ActionControl.PocketNumber + ";"; //Pocket number
            pocketstatsLine += (int)DateTime.UtcNow.Subtract(StartedMission).TotalMinutes + ";"; //Time to Complete
            pocketstatsLine += ESCache.Instance.MyWalletBalance - ESCache.Instance.WealthatStartofPocket + ";"; //Isk
            pocketstatsLine += PanicAttemptsThisPocket + ";"; //Panics
            pocketstatsLine += (int)LowestShieldPercentageThisPocket + ";"; //LowestShields
            pocketstatsLine += (int)LowestArmorPercentageThisPocket + ";"; //LowestArmor
            pocketstatsLine += (int)LowestCapacitorPercentageThisPocket + ";"; //LowestCapacitor
            pocketstatsLine += RepairCycleTimeThisPocket + ";"; //repairCycles
            pocketstatsLine += WrecksThisPocket + ";"; //wrecksThisPocket
            pocketstatsLine += "\r\n";

            Log.WriteLine("Writing pocket statistics to [ " + PocketStatisticsFile + " ] and clearing stats for next pocket");
            File.AppendAllText(PocketStatisticsFile, pocketstatsLine);

            // Update statistic values for next pocket stats
            ESCache.Instance.WealthatStartofPocket = ESCache.Instance.MyWalletBalance;
            StartedPocket = DateTime.UtcNow;
            PanicAttemptsThisPocket = 0;
            LowestShieldPercentageThisPocket = 101;
            LowestArmorPercentageThisPocket = 101;
            LowestCapacitorPercentageThisPocket = 101;
            RepairCycleTimeThisPocket = 0;
            WrecksThisMission += WrecksThisPocket;
            WrecksThisPocket = 0;
            ESCache.Instance.OrbitEntityNamed = null;
        }

        #endregion Methods
    }
}