extern alias SC;
using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Traveller;
using SC::SharedComponents.IPC;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor
{
    public enum PanicReason
    {
        None,
        Tank,
        MissionInvaded
    }

    /// <summary>
    ///     Description of PanicController.
    /// </summary>
    public class PanicController : BaseController
    {
        #region Constructors

        public PanicController() : base()
        {
        }

        #endregion Constructors

        #region Properties

        public PanicState PanicState { get; set; }

        public PanicReason PanicReason { get; set; }

        public bool SimulatePanic { get; set; }

        public bool SimulateInvasion { get; set; }

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            switch (PanicState)
            {
                case PanicState.Check:
                    Check();
                    break;

                case PanicState.Panic:
                    Panic();
                    break;

                case PanicState.Recover:
                    Recover();
                    break;
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        private void Check()
        {
            if (ESCache.Instance.InDockableLocation)
                return;

            if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                return;

            if (ESCache.Instance.InWarp)
                return;

            if ((long)ESCache.Instance.ActiveShip.StructurePercentage == 0)
                return;

            if (!ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.InMission && ESCache.Instance.ActiveShip.CapacitorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumCapacitorPct)
            {
                var capPct = Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0);
                Log($"Start panicking, capacitor [{capPct}]%");
                ESCache.Instance.Statistics.PanicAttemptsThisMission++;
                ESCache.Instance.Statistics.PanicAttemptsThisPocket++;
                PanicState = PanicState.Panic;
                PanicReason = PanicReason.Tank;
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PANIC, "Panicking (Capacitor)."));
                return;
            }


            if (ESCache.Instance.ActiveShip.ShieldPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumShieldPct || SimulatePanic)
            {
                var shieldPct = Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0);
                Log($"Start panicking, shield [{shieldPct}]%");
                ESCache.Instance.Statistics.PanicAttemptsThisMission++;
                ESCache.Instance.Statistics.PanicAttemptsThisPocket++;
                PanicState = PanicState.Panic;
                PanicReason = PanicReason.Tank;
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PANIC, "Panicking (Shield)."));
                return;
            }

            if (ESCache.Instance.ActiveShip.ArmorPercentage < ESCache.Instance.EveAccount.CS.QMS.QS.MinimumArmorPct)
            {
                var armorPct = Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0);
                Log($"Start panicking, armor [{armorPct}%]");
                ESCache.Instance.Statistics.PanicAttemptsThisMission++;
                ESCache.Instance.Statistics.PanicAttemptsThisPocket++;
                PanicState = PanicState.Panic;
                PanicReason = PanicReason.Tank;
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PANIC, "Panicking (Armor)."));
                return;
            }

            if (!ESCache.Instance.InMission)
                return;

            var missionInvadedBy = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.IsPlayer);
            if (missionInvadedBy != null || SimulateInvasion)
            {
                var name = missionInvadedBy?.Name ?? String.Empty;
                var typeName = missionInvadedBy?.TypeName ?? String.Empty;

                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.MISSION_INVADED,
                    "Mission was invaded by another player: " + name + " Ship: " + typeName));

                PanicState = PanicState.Panic;
                PanicReason = PanicReason.MissionInvaded;
                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PANIC, "Panicking (Invaded)."));
            }
        }

        private void Panic()
        {

            if (ESCache.Instance.InSpace && ESCache.Instance.DirectEve.ActiveDrones.Any())
            {
                if (DirectEve.Interval(8000, 10000))
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);

                if (PanicReason == PanicReason.MissionInvaded)
                {
                    Log("Waiting for the drones to return.");
                    return;
                }
            }

            if (ESCache.Instance.InDockableLocation)
            {
                Log("Entered a station.");
                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                {
                    Log("We are in a capsule. Pausing.");
                    ControllerManager.Instance.SetPause();
                    ESCache.Instance.DisableThisInstance();
                    return;
                }

                PanicState = PanicState.Recover;
                return;
            }

            var destinationId = ESCache.Instance.Agent.StationId;
            var trav = ESCache.Instance.Traveler;

            if (trav.Destination != null)
            {
                if (trav.Destination.GetType() != typeof(DockableLocationDestination))
                {
                    Log($"Reset dest.");
                    trav.Destination = null;
                }
            }

            if (trav.Destination == null || ((DockableLocationDestination)trav.Destination).DockableLocationId != destinationId)
            {
                Logging.Log.WriteLine("StationDestination: [" + destinationId + "]");
                trav.Destination = new DockableLocationDestination(destinationId);

                ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            }

            if (DebugConfig.DebugGotobase)
                if (trav.Destination != null)
                    Logging.Log.WriteLine("Traveler.Destination.SolarSystemId [" + trav.Destination.SolarSystemId + "]");
            trav.ProcessState();
        }

        private void Recover()
        {
            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
            {
                Log("We are in a capsule. Pausing.");
                ControllerManager.Instance.SetPause();
                ESCache.Instance.DisableThisInstance();
                return;
            }

            bool isSafe() => ESCache.Instance.ActiveShip.CapacitorPercentage >= ESCache.Instance.EveAccount.CS.QMS.QS.SafeCapacitorPct
                             && ESCache.Instance.ActiveShip.ShieldPercentage >= ESCache.Instance.EveAccount.CS.QMS.QS.SafeShieldPct
                             && ESCache.Instance.ActiveShip.ArmorPercentage >= ESCache.Instance.EveAccount.CS.QMS.QS.SafeArmorPct;

            if (ESCache.Instance.InDockableLocation || isSafe())
            {

                if (PanicReason == PanicReason.MissionInvaded)
                {
                    Log("Mission was invaded, quiting mission.");
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.QuitMission;
                    ESCache.Instance.AgentInteraction.Purpose = AgentInteractionPurpose.QuitMission;
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                    PanicState = PanicState.Check;
                }
                else
                {
                    Log("We have recovered, resume mission.");
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoMission;
                    PanicState = PanicState.Check;
                }
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}