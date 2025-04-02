/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:07
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.Behaviors;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor
{
    public class QuestorController : BaseController
    {
        #region Constructors

        public QuestorController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            DependsOn = new List<Type>()
            {
                typeof(SalvageController),
                typeof(DefenseController),
                typeof(PanicController)
            };
            CombatMissionsBehaviorInstance = new CombatMissionsBehavior();
            ESCache.Instance.InitInstances();
            ESCache.LootAlreadyUnloaded = false;
            ESCache.Instance.State.CurrentQuestorState = QuestorState.Start;
        }

        #endregion Constructors

        #region Properties

        public CombatMissionsBehavior CombatMissionsBehaviorInstance { get; set; }

        private bool _DirectoriesCreated { get; set; }

        #endregion Properties

        #region Methods

        public void CreateDirectories()
        {

            ESCache.Instance.Statistics.PocketStatisticsPath = Path.Combine(Logging.Log.Logpath, "PocketStats");
            ESCache.Instance.Statistics.PocketStatisticsFile = Path.Combine(ESCache.Instance.Statistics.PocketStatisticsPath,
                ESCache.Instance.EveAccount.CharacterName + "pocketstats-combined.csv");
            ESCache.Instance.Statistics.PocketObjectStatisticsPath = Path.Combine(Logging.Log.Logpath, "PocketObjectStats");
            ESCache.Instance.Statistics.PocketObjectStatisticsFile = Path.Combine(ESCache.Instance.Statistics.PocketObjectStatisticsPath,
                ESCache.Instance.EveAccount.CharacterName + "PocketObjectStats-combined.csv");
            ESCache.Instance.Statistics.MissionDetailsHtmlPath = Path.Combine(Logging.Log.Logpath, "MissionDetailsHTML");

            try
            {
                Directory.CreateDirectory(Logging.Log.Logpath);
                Directory.CreateDirectory(Logging.Log.ConsoleLogPath);
                Directory.CreateDirectory(ESCache.Instance.Statistics.PocketStatisticsPath);
                Directory.CreateDirectory(ESCache.Instance.Statistics.PocketObjectStatisticsPath);
            }
            catch (Exception exception)
            {
                Logging.Log.WriteLine("Problem creating directories for logs [" + exception + "]");
            }

            _DirectoriesCreated = true;
        }

        public override void DoWork()
        {
            try
            {
                if (!_DirectoriesCreated)
                    CreateDirectories();

                if (ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges < 8)
                {
                    Log("Error: MinimumAmmoCharges must be greater or equal 8!");
                    return;
                }

                if (ESCache.Instance.InSpace && ESCache.Instance.Stations.Any(s => s.Distance <= (int)Distances.DockingRange) && (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking) || ESCache.Instance.Entities.Count(e => e.IsTargetedBy && e.IsPlayer) > 1))
                {
                    var station = ESCache.Instance.Stations.FirstOrDefault(s => s.Distance <= (int)Distances.DockingRange);
                    if (ESCache.Instance.InWarp)
                    {
                        Log($"We are outside of a station and being aggressed by another player or targeted by more than 2. Trying to stop the ship and dock.");
                        if (EVESharpCore.Framework.DirectEve.Interval(500, 1000))
                        {
                            ESCache.Instance.DirectEve.ExecuteCommand(EVESharpCore.Framework.DirectCmd.CmdStopShip);
                        }
                        return;
                    }
                    else
                    {
                        Log("Docking attempt.");
                        station.Dock();
                        return;
                    }
                }

                if (ESCache.Instance.InWarp)
                {
                    
                    if(DirectEve.Interval(1500))
                    {
                        ESCache.Instance.MissionSettings.ClearDamageTypeCache();
                    }

                    if (ESCache.Instance.EveAccount.CS.QMS.QS.KeepWeaponsGrouped && ESCache.Instance.GroupWeapons())
                    {
                        LocalPulse = UTCNowAddMilliseconds(1500, 2500);
                        return;
                    }

                    if (ESCache.Instance.Combat.PrimaryWeaponPriorityEntities != null && ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.Any())
                        ESCache.Instance.Combat.RemovePrimaryWeaponPriorityTargets(ESCache.Instance.Combat.PrimaryWeaponPriorityEntities.ToList());

                    if (ESCache.Instance.Drones.UseDrones && ESCache.Instance.Drones.DronePriorityEntities != null && ESCache.Instance.Drones.DronePriorityEntities.Any())
                        ESCache.Instance.Drones.RemoveDronePriorityTargets(ESCache.Instance.Drones.DronePriorityEntities.ToList());
                    return;
                }

                if (ESCache.Instance.InDockableLocation && ESCache.Instance.PauseAfterNextDock)
                {
                    ControllerManager.Instance.SetPause(true);
                    ESCache.Instance.PauseAfterNextDock = false;
                    return;
                }

                switch (ESCache.Instance.State.CurrentQuestorState)
                {
                    case QuestorState.CombatMissionsBehavior:
                        CombatMissionsBehaviorInstance.ProcessState();
                        break;

                    case QuestorState.Start:
                        ESCache.Instance.State.CurrentQuestorState = QuestorState.CombatMissionsBehavior;
                        break;

                    case QuestorState.Error:
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                        ESCache.Instance.DisableThisInstance();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            if (cm.TryGetController<BuyAmmoController>(out _))
            {
                return false;
            }

            if (cm.TryGetController<BuyPlexController>(out _))
            {
                return false;
            }

            if (cm.TryGetController<DumpLootController>(out _))
            {
                return false;
            }

            if (cm.TryGetController<PanicController>(out var panicController))
            {
                // do not run the questor controller if we're actually in a panic state
                if (panicController.PanicState != PanicState.Check)
                    return false;
            }

            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}