extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;
using AmmoType = SC::SharedComponents.EVE.ClientSettings.AmmoType;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    public enum GenericCombatStorylineState
    {
        WarpOutStation,
        GotoMission,
        ExecuteMission,
    }

    public class GenericCombatStoryline : IStoryline
    {
        #region Fields

        private readonly ActionControl _actionControl;
        private readonly List<AmmoType> _neededAmmo;
        private long _agentId;

        private GenericCombatStorylineState _state;

        #endregion Fields

        #region Constructors

        public GenericCombatStoryline()
        {
            _neededAmmo = new List<AmmoType>();
            _actionControl = new ActionControl();
        }

        #endregion Constructors

        #region Properties

        public GenericCombatStorylineState State
        {
            get => _state;
            set => _state = value;
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     We check what ammo we need by starting a conversation with the agent and load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_agentId != ESCache.Instance.Agent.AgentId)
            {
                _neededAmmo.Clear();
                _agentId = ESCache.Instance.Agent.AgentId;

                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                ESCache.Instance.State.CurrentArmState = ArmState.Idle;
                ESCache.Instance.State.CurrentActionControlState = ActionControlState.Start;
                ESCache.Instance.State.CurrentDroneState = DroneState.WaitingForTargets;
            }

            try
            {
                if (!LoadAmmo())
                    return StorylineState.Arm;

                // We are done, reset agent id
                _agentId = 0;

                return StorylineState.GotoAgent;
            }
            catch (Exception ex)
            {
                // Something went wrong!
                Log.WriteLine("Something went wrong, blacklist this agent [" + ex.Message + "]");
                return StorylineState.BlacklistAgent;
            }
        }

        /// <summary>
        ///     Do a mini-questor here (goto mission, execute mission, goto base)
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            switch (_state)
            {
                case GenericCombatStorylineState.WarpOutStation:

                    DirectBookmark warpOutBookMark = null;
                    try
                    {
                        warpOutBookMark =
                            ESCache.Instance.DirectEve.Bookmarks
                                .Where(b => !string.IsNullOrEmpty(b.Title) && b.LocationId != null && b.Title.ToLower().StartsWith(ESCache.Instance.EveAccount.CS.QMS.QS.UndockBookmarkPrefix.ToLower())).ToList()
                                .OrderByDescending(b => b.CreatedOn)
                                .FirstOrDefault(b => b.LocationId == ESCache.Instance.DirectEve.Session.SolarSystemId);
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Exception: " + ex);
                    }

                    long solarid = ESCache.Instance.DirectEve.Session.SolarSystemId ?? -1;

                    if (warpOutBookMark == null)
                    {
                        Log.WriteLine("No Bookmark");
                        _state = GenericCombatStorylineState.GotoMission;
                        break;
                    }

                    if (warpOutBookMark.LocationId == solarid)
                    {
                        if (ESCache.Instance.Traveler.Destination == null)
                        {
                            Log.WriteLine("Warp at " + warpOutBookMark.Title);
                            ESCache.Instance.Traveler.Destination = new BookmarkDestination(warpOutBookMark);
                        }

                        ESCache.Instance.Traveler.ProcessState();
                        if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log.WriteLine("Safe!");
                            _state = GenericCombatStorylineState.GotoMission;
                            ESCache.Instance.Traveler.Destination = null;
                            break;
                        }

                        if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                        {
                            Log.WriteLine("Traveller state = Error. Blacklisting this agent.");
                            ESCache.Instance.State.CurrentStorylineState = StorylineState.BlacklistAgent;
                        }

                        break;
                    }

                    Log.WriteLine("No Bookmark in System");
                    _state = GenericCombatStorylineState.GotoMission;
                    break;

                case GenericCombatStorylineState.GotoMission:
                    var missionDestination = ESCache.Instance.Traveler.Destination as MissionBookmarkDestination;
                    //
                    // if we have no destination yet... OR if missionDestination.AgentId != storyline.CurrentStorylineAgentId
                    //
                    if (missionDestination == null || missionDestination.AgentId != ESCache.Instance.Agent.AgentId)
                    // We assume that this will always work "correctly" (tm)
                    {
                        var nameOfBookmark = "Encounter";
                        Log.WriteLine("Setting Destination to 1st bookmark from AgentID: [" + ESCache.Instance.Agent.AgentId + "] with [" +
                                      nameOfBookmark +
                                      "] in the title");
                        ESCache.Instance.Traveler.Destination =
                            new MissionBookmarkDestination(ESCache.Instance.Agent.GetMissionBookmark(nameOfBookmark));
                    }

                    ESCache.Instance.Traveler.ProcessState();
                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        _state = GenericCombatStorylineState.ExecuteMission;

                        //_States.CurrentCombatState = CombatState.CheckTargets;
                        ESCache.Instance.Traveler.Destination = null;
                    }

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                    {
                        Log.WriteLine("Traveller state = Error. Blacklisting this agent.");
                        ESCache.Instance.State.CurrentStorylineState = StorylineState.BlacklistAgent;
                    }

                    break;

                case GenericCombatStorylineState.ExecuteMission:
                    ESCache.Instance.Combat.ProcessState();
                    ESCache.Instance.Drones.ProcessState();
                    _actionControl.ProcessState();

                    // If we are out of ammo, return to base, the mission will fail to complete and the bot will reload the ship
                    // and try the mission again
                    if (ESCache.Instance.State.CurrentCombatState == CombatState.OutOfAmmo)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();

                        Log.WriteLine("Out of Ammo! - Not enough [" + ESCache.Instance.MissionSettings.CurrentDamageType + "] ammo in cargohold: MinimumCharges: [" +
                                      ESCache.Instance.EveAccount.CS.QMS.QS.MinimumAmmoCharges + "]");
                        return StorylineState.ReturnToAgent;
                    }

                    if (ESCache.Instance.State.CurrentActionControlState == ActionControlState.Done)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();
                        return StorylineState.ReturnToAgent;
                    }

                    // If in error state, just go home and stop the bot
                    if (ESCache.Instance.State.CurrentActionControlState == ActionControlState.Error)
                    {
                        // Clear looted containers
                        ESCache.Instance.LootedContainers.Clear();

                        Log.WriteLine("Error");
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "Questor Error."));
                        return StorylineState.ReturnToAgent;
                    }
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     We have no pre-accept steps
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            // Not really a step is it? :)
            _state = GenericCombatStorylineState.WarpOutStation;
            return StorylineState.AcceptMission;
        }

        /// <summary>
        ///     Load the appropriate ammo
        /// </summary>
        /// <returns></returns>
        private bool LoadAmmo()
        {
            if (ESCache.Instance.State.CurrentArmState == ArmState.Done)
                return true;

            if (ESCache.Instance.State.CurrentArmState == ArmState.Idle)
                ESCache.Instance.State.CurrentArmState = ArmState.Begin;

            ESCache.Instance.Arm.ProcessState();

            if (ESCache.Instance.State.CurrentArmState == ArmState.Done)
            {
                ESCache.Instance.State.CurrentArmState = ArmState.Idle;
                return true;
            }

            return false;
        }

        #endregion Methods
    }
}