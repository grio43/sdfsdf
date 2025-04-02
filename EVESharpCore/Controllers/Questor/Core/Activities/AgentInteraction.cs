extern alias SC;
using System.IO;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Logging;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Questor.Core.Activities
{
    public class AgentInteraction
    {
        #region Fields

        private int _loyaltyPointCounter;

        #endregion Fields

        #region Properties

        public AgentInteractionPurpose Purpose { get; set; }

        #endregion Properties

        #region Methods

        public void CloseConversation()
        {
            var agentWindow = ESCache.Instance.Agent.Window;
            if (agentWindow == null)
            {
                Log.WriteLine("Done");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Done;
                return;
            }

            if (agentWindow != null && agentWindow.IsReady)
            {
                if (!DirectEve.Interval(800, 1200, nameof(AgentInteraction)))
                    return;

                Log.WriteLine("Attempting to close Agent Window");
                agentWindow.Close();
            }
        }

        public bool OpenAgentWindow()
        {
            if (ESCache.Instance.Agent.Window == null)
            {
                if (!DirectEve.Interval(1500, 1700, nameof(AgentInteraction)))
                    return false;

                ESCache.Instance.Agent.InteractWith();
                return false;
            }
            return ESCache.Instance.Agent.Window.IsReady;
        }

        public bool OpenJournalWindow()
        {
            var journalWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();
            if (journalWindow == null)
            {
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenJournal);
                return false;
            }

            if (!journalWindow.Ready)
                return false;

            return true;
        }

        public void ProcessState()
        {
            if (ESCache.Instance.InDockableLocation && ESCache.Instance.Statistics.MissionCompletionErrors > 3)
                if (ESCache.Instance.MissionSettings.DeclineMissionsWithTooManyMissionCompletionErrors)
                {
                    Log.WriteLine($"Warning: {ESCache.Instance.MissionSettings.MissionName} is not able to complete successfully after 3 tries." +
                                  $" Quitting mission. MissionXMLIsAvailable [{ESCache.Instance.MissionSettings.MissionXMLIsAvailable}]");
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                }
                else
                {
                    Log.WriteLine($"Error: {ESCache.Instance.MissionSettings.MissionName} is not able to complete successfully after 3 tries." +
                                  $" MissionXMLIsAvailable [{ESCache.Instance.MissionSettings.MissionXMLIsAvailable}]");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    return;
                }

            if (!ESCache.Instance.InDockableLocation || ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.Agent == null || !ESCache.Instance.Agent.IsValid)
            {
                Log.WriteLine("Agent is not valid yet.");
                return;
            }

            if (!ESCache.Instance.Agent.CanAccessAgent)
            {
                Log.WriteLine($"Error: Can't access this agent, the standing requirement is not met.");
                Log.WriteLine($"MinEffStd {ESCache.Instance.Agent.MinEffectiveStanding}");
                Log.WriteLine($"MaxEffStd {ESCache.Instance.Agent.MaxEffectiveStanding}");

                Log.WriteLine($"EffectiveAgentStanding {ESCache.Instance.Agent.EffectiveAgentStanding}");
                Log.WriteLine($"EffectiveCorpStanding {ESCache.Instance.Agent.EffectiveCorpStanding}");
                Log.WriteLine($"EffectiveFactionStanding {ESCache.Instance.Agent.EffectiveFactionStanding}");

                Log.WriteLine($"AgentId {ESCache.Instance.Agent.AgentId}");
                Log.WriteLine($"CorpId {ESCache.Instance.Agent.CorpId}");
                Log.WriteLine($"FactionId {ESCache.Instance.Agent.FactionId}");
                return;
            }

            if (ESCache.Instance.Agent.Level == 4 && ESCache.Instance.DirectEve.Me.IsOmegaClone.HasValue &&
                                            !ESCache.Instance.DirectEve.Me.IsOmegaClone.Value)
            {
                Log.WriteLine($"Error: Can't access a level 4 agent while being in alpha state.");
                return;
            }

            if (ESCache.Instance.State.CurrentAgentInteractionState != AgentInteractionState.CloseConversation)
            {
                if (!OpenAgentWindow()) return;

                if (!ESCache.Instance.Agent.Window.Buttons.Any())
                    return;
            }

            if (!OpenJournalWindow()) return;

            switch (ESCache.Instance.State.CurrentAgentInteractionState)
            {
                case AgentInteractionState.Idle:
                    break;

                case AgentInteractionState.Done:
                    break;

                case AgentInteractionState.StartConversation:
                    StartConversation();
                    break;

                case AgentInteractionState.ReplyToAgent:
                    ReplyToAgent();
                    break;

                case AgentInteractionState.PrepareForOfferedMission:
                    PrepareForOfferedMission();
                    break;

                case AgentInteractionState.AcceptMission:
                    AcceptMission();
                    break;

                case AgentInteractionState.DeclineQuitMission:
                    DeclineQuitMission();
                    break;

                case AgentInteractionState.CloseConversation:
                    CloseConversation();
                    break;

                case AgentInteractionState.UnexpectedDialogOptions:
                    Log.WriteLine("UnexpectedDialogOptions AgentInteraction. Pausing.");
                    ControllerManager.Instance.SetPause(true);
                    break;
            }
        }

        private void AcceptMission()
        {
            if (!DirectEve.Interval(800, 1200, nameof(AgentInteraction)))
                return;

            var buttons = ESCache.Instance.Agent.Window.Buttons;

            if (buttons.Count == 0)
                return;

            var accept = buttons.FirstOrDefault(r => r.Type == ButtonType.ACCEPT);
            if (accept == null)
                return;

            if (ESCache.Instance.Agent.LoyaltyPoints == null
                && ESCache.Instance.Agent.Level > 1
                && _loyaltyPointCounter < 5)
            {
                _loyaltyPointCounter++;
                return;
            }

            _loyaltyPointCounter = 0;
            Log.WriteLine("Clicking [Accept]");

            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ACCEPT_MISSION, "Accepting mission."));
            accept.Click();
            ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.CloseConversation;
            return;
        }

        private void DeclineQuitMission()
        {
            if (!DirectEve.Interval(1200, 1600, nameof(AgentInteraction)))
                return;

            var buttons = ESCache.Instance.Agent.Window.Buttons;
            if (buttons == null || buttons.Count == 0)
            {
                if (DebugConfig.DebugDecline)
                    Log.WriteLine("No agent responses.");
                return;
            }

            var decline = buttons.FirstOrDefault(r => r.Type == ButtonType.DECLINE);
            if (decline == null)
            {
                decline = buttons.FirstOrDefault(r => r.Type == ButtonType.QUIT_MISSION);
                if (decline == null)
                {
                    if (DebugConfig.DebugDecline)
                        Log.WriteLine("Decline/Quit button not found.");
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.ReplyToAgent;
                    return;
                }
            }

            var BriefingHtml = ESCache.Instance.Agent.Window.Briefing;
            ESCache.Instance.Statistics.SaveMissionHTMLDetails(BriefingHtml, ESCache.Instance.MissionSettings.MissionName + "-Briefing");

            if (ESCache.Instance.State.CurrentStorylineState == StorylineState.DeclineMission || ESCache.Instance.State.CurrentStorylineState == StorylineState.AcceptMission)
            {
                var jw = ESCache.Instance.DirectEve.Windows.OfType<DirectJournalWindow>().FirstOrDefault();

                if (jw.SelectedMainTab != MainTab.AgentMissions)
                {
                    Log.WriteLine("Journal window agent mission tab is not selected. Switching the tab.");
                    jw.SwitchMaintab(MainTab.AgentMissions);
                    return;
                }

                Log.WriteLine("Storyline: Removing offer.");
                ESCache.Instance.MissionSettings.Mission.RemoveOffer();
                ESCache.Instance.Statistics.MissionCompletionErrors = 0;
                Log.WriteLine("Storyline: Setting StorylineState.Done");
                ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Storyline;
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.Idle;
                return;
            }

            Log.WriteLine("Clicking [Decline/Quit]");
            decline.Click();
            Purpose = AgentInteractionPurpose.StartMission;
            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.DECLINE_MISSION, "Declining mission."));
            ESCache.Instance.Statistics.MissionCompletionErrors = 0;
            ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.StartConversation;
        }

        private void PrepareForOfferedMission()
        {
            if (ESCache.Instance.MissionSettings.Mission == null)
            {
                Log.WriteLine("Mission is null, retrying.");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.ReplyToAgent;
                return;
            }

            if (ESCache.Instance.Agent.Window.ObjectiveEmpty)
            {
                Log.WriteLine("Objective not yet loaded. Waiting.");
                return;
            }

            var ObjectiveHtml = ESCache.Instance.Agent.Window.Objective;
            var isMissionBlacklisted = ESCache.Instance.MissionSettings.MissionBlacklist.Any(m => m.Name.ToLower() == ESCache.Instance.MissionSettings.MissionName.ToLower());

            if (ESCache.Instance.Agent.FactionId == 500004 && ESCache.Instance.Agent.Level == 4 &&
                ESCache.Instance.MissionSettings.MissionName.ToLower().Equals("Cargo Delivery".ToLower()))
            {
                Log.WriteLine("Blacklisting Gallente [Cargo Delivery] L4 because it f****** sucks.");
                isMissionBlacklisted = true;
            }

            ESCache.Instance.MissionSettings.CurrentMissionFaction = ESCache.Instance.Agent.Window.GetFactionType().Value;

            Log.WriteLine($"The faction of the current mission is {ESCache.Instance.MissionSettings.CurrentMissionFaction}");

            var isFactionBlacklisted = ESCache.Instance.MissionSettings.IsBlackListedFaction;

            if (isFactionBlacklisted || isMissionBlacklisted)
            {
                Log.WriteLine($"Attempting to Decline {(isFactionBlacklisted ? "faction" : "mission")} blacklisted mission [" + Log.FilterPath(ESCache.Instance.MissionSettings.Mission.Name) + "] Expires [" +
                              ESCache.Instance.MissionSettings.Mission.ExpiresOn + "]");

                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                return;
            }

            if (ESCache.Instance.MissionSettings.MissionGreylist.Any(m => m.Name.ToLower().Equals(ESCache.Instance.MissionSettings.MissionName.ToLower())))
            {
                if (ESCache.Instance.Agent.MinEffectiveStanding > ESCache.Instance.MissionSettings.MinAgentGreyListStandings)
                {
                    Log.WriteLine($"MinEffectiveStanding {ESCache.Instance.Agent.MinEffectiveStanding} MinAgentGreyListStandings {ESCache.Instance.MissionSettings.MinAgentGreyListStandings}");
                    ESCache.Instance.MissionSettings.LastGreylistMissionDeclined = ESCache.Instance.MissionSettings.MissionName;
                    Log.WriteLine("Declining GreyListed mission [" + ESCache.Instance.MissionSettings.MissionName + "]");
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                    return;
                }

                Log.WriteLine("Unable to decline GreyListed mission: MinEffectiveStanding [" +
                              ESCache.Instance.Agent.MinEffectiveStanding +
                              "] >  MinGreyListStandings [" + ESCache.Instance.MissionSettings.MinAgentGreyListStandings + "]");
            }

            DirectBookmark missionBookmark = ESCache.Instance.MissionSettings.Mission.Bookmarks.FirstOrDefault();
            if (missionBookmark != null)
                Log.WriteLine("Mission bookmark: System: [" + missionBookmark.LocationId.ToString() + "]");
            else
                Log.WriteLine("There are No Bookmarks Associated with " + Log.FilterPath(ESCache.Instance.MissionSettings.Mission.Name) + " yet");

            if (ObjectiveHtml.Contains("The route generated by current autopilot settings contains low security systems!"))
            {
                Log.WriteLine("Declining [" + ESCache.Instance.MissionSettings.MissionName + "] because it was taking us through low-sec.");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                return;
            }

            ESCache.Instance.Statistics.LoyaltyPointsTotal = ESCache.Instance.Agent.LoyaltyPoints ?? 0; // save ISK and LPs before accepting
            ESCache.Instance.Wealth = ESCache.Instance.DirectEve.Me.Wealth;

            ESCache.Instance.Statistics.SaveMissionHTMLDetails(ObjectiveHtml, ESCache.Instance.MissionSettings.MissionName + "-Objective");
            ESCache.Instance.MissionSettings.SetmissionXmlPath(Log.FilterPath(ESCache.Instance.MissionSettings.MissionName));

            ESCache.Instance.MissionSettings.ClearMissionSpecificSettings();

            if (File.Exists(ESCache.Instance.MissionSettings.MissionXmlPath))
            {
                ESCache.Instance.MissionSettings.LoadMissionXmlData();
            }
            else
            {
                Log.WriteLine("Missing mission XML [" + ESCache.Instance.MissionSettings.MissionName + "] from [" + ESCache.Instance.MissionSettings.MissionXmlPath + "].");
                ESCache.Instance.MissionSettings.MissionXMLIsAvailable = false;
                if (ESCache.Instance.EveAccount.CS.QMS.QS.RequireMissionXML)
                {
                    Log.WriteLine("Pausing Questor because RequireMissionXML is true in your character XML settings");
                    Log.WriteLine("You will need to create a mission XML for [" + ESCache.Instance.MissionSettings.MissionName + "]");
                    ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.Error;
                    ControllerManager.Instance.SetPause(true);
                    return;
                }
            }

            if (ESCache.Instance.MissionSettings.Mission.State == (int)MissionState.Offered)
            {
                Log.WriteLine("Accepting mission [" + ESCache.Instance.MissionSettings.MissionName + "]");
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.AcceptMission;
                return;
            }

            ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.CloseConversation; // If we already accepted the mission, close the conversation
        }

        private void ReplyToAgent()
        {
            if (!DirectEve.Interval(800, 1200, nameof(AgentInteraction)))
                return;

            var request = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.REQUEST_MISSION);
            var complete = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.COMPLETE_MISSION);
            var view = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.VIEW_MISSION);
            var accept = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.ACCEPT);
            var decline = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.DECLINE);
            var delay = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.DELAY);
            var quit = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.QUIT_MISSION);
            var close = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.CLOSE);
            var noMoreJobsAvail = ESCache.Instance.Agent.Window.Buttons.FirstOrDefault(r => r.Type == ButtonType.NO_JOBS_AVAILABLE);

            if (noMoreJobsAvail != null)
                ControllerManager.Instance.SetPause(true);

            if (quit != null && Purpose == AgentInteractionPurpose.QuitMission)
            {
                ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.DeclineQuitMission;
                return;
            }

            if (complete != null)
            {
                if (Purpose == AgentInteractionPurpose.CompleteMission)
                {
                    ESCache.Instance.Statistics.LoyaltyPointsForCurrentMission = ESCache.Instance.Agent.Window.TotalLPReward;
                    ESCache.Instance.Statistics.ISKMissionReward = ESCache.Instance.Agent.Window.TotalISKReward;
                    ESCache.Instance.Statistics.LastMissionName = ESCache.Instance.MissionSettings.MissionName;

                    Log.WriteLine("Clicking [Complete Mission] ISKMissionReward [" + ESCache.Instance.Statistics.ISKMissionReward +
                                  "] LoyaltyPointsForCurrentMission [" +
                                  ESCache.Instance.Statistics.LoyaltyPointsForCurrentMission + "]");

                    complete.Click();

                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.COMPLETE_MISSION, "Completing mission."));

                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.CloseConversation;
                    return;
                }
                else
                {
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.PrepareForOfferedMission; // Apparently someone clicked "accept" already
                    return;
                }
            }
            else if (request != null)
            {
                if (Purpose == AgentInteractionPurpose.StartMission)
                {
                    Log.WriteLine("Clicking [Request Mission]");
                    request.Click();
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.PrepareForOfferedMission;
                    return;
                }
                else
                {
                    Log.WriteLine("Unexpected dialog options: requesting mission since we have that button available");
                    request.Click();
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.UnexpectedDialogOptions;
                    return;
                }
            }
            else if (view != null)
            {
                if (!DirectEve.Interval(1500, 2000))
                    return;

                Log.WriteLine("Clicking [View Mission]");
                view.Click();
            }
            else if (accept != null || decline != null)
            {
                if (Purpose == AgentInteractionPurpose.StartMission)
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.PrepareForOfferedMission;
                else
                    ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.UnexpectedDialogOptions;
            }
        }

        private void StartConversation()
        {
            Log.WriteLine($"AgentStanding [{ESCache.Instance.Agent.EffectiveAgentStanding}] " +
                          $"CorpStanding [{ESCache.Instance.Agent.EffectiveCorpStanding}] " +
                          $"FactionStanding [{ESCache.Instance.Agent.EffectiveFactionStanding}]");

            if (!DirectEve.Interval(800, 1200, nameof(AgentInteraction)))
                return;

            Log.WriteLine("Replying to agent");
            ESCache.Instance.State.CurrentAgentInteractionState = AgentInteractionState.ReplyToAgent;
        }

        #endregion Methods
    }
}