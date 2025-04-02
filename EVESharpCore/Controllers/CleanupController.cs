/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EVESharpCore.Framework;
using static SC::SharedComponents.Extensions.StringExtensions;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.Questor.Core.Actions;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Controllers.Questor.Core.Activities;
using SC::SharedComponents.EveMarshal.Python;

namespace EVESharpCore.Controllers
{
    public class CleanupController : BaseController
    {
        #region Fields



        public bool QtyWindowClosed { get; set; }

        #endregion Fields

        #region Constructors

        public CleanupController() : base()
        {
            IgnorePause = true;
            IgnoreModal = true;
            RunBeforeLoggedIn = true;
            IgnoreValidSession = true;
        }

        #endregion Constructors

        #region Methods

        public void CheckModalWindows()
        {
            if (!ESCache.Instance.DirectEve.AnyModalWindowExceptFleetInvite())
            {
                if (DebugConfig.DebugCleanup)
                    Log("CheckModalWindows: No modal windows.");
                return;
            }

            LocalPulse = UTCNowAddMilliseconds(600, 630);

            foreach (var window in ESCache.Instance.DirectEve.ModalWindows)
            {
                if (!window.Ready)
                {
                    Log($"Modal window not ready (1).");
                    continue;
                }

                if (window.Name == "telecom")
                {
                    Log("Closing telecom message...");
                    Log("Content of telecom window (HTML): [" + (window.Html ?? string.Empty).Replace("\n", "").Replace("\r", "") +
                        "]");
                    window.Close();
                    return;
                }

                if (string.IsNullOrEmpty(window.Html) && string.IsNullOrEmpty(window.Caption) && window.Guid.Equals("form.MessageBox") && window.IsModal)
                {
                    Log($"Modal window not ready (2).");
                    continue;
                }



                if (window.IsModal)
                {
                    var close = false;
                    var restart = false;
                    var gotoBaseNow = false;
                    var sayYes = false;
                    var sayOk = false;
                    var pause = false;
                    var quit = false;
                    var stackHangars = false;
                    var clearPocket = false;
                    var disableInstance = false;
                    var agentRelated = false;

                    if (!string.IsNullOrEmpty(window.Name))
                    {
                        close |= window.Name.Equals("LapseNotifyWindow");
                        close |= window.Name.Equals("NewFeatureNotifyWnd");

                        if (window.Name.Equals("Divide Stack"))
                        {
                            sayOk |= true;
                            this.QtyWindowClosed = true;
                        }
                    }

                    if (!string.IsNullOrEmpty(window.Guid))
                    {
                        close |= window.Guid.Equals("DailyLoginRewardsWnd");
                    }

                    if (!string.IsNullOrEmpty(window.Html))
                    {
                        gotoBaseNow |= window.Html.ContainsIgnoreCase("for a short unscheduled reboot");

                        disableInstance |= window.Html.ContainsIgnoreCase("banned");

                        pause |= window.Html.ContainsIgnoreCase("Not all the items could be fitted");
                        pause |= window.Html.ContainsIgnoreCase("Cannot move");

                        if (window.Guid == "form.MessageBox" && window.IsDialog && window.IsModal && window.IsKillable)
                            sayOk |=
                                window.Html.ContainsIgnoreCase(
                                    "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");

                        close |= window.Html.ContainsIgnoreCase("Do you really want to quit now?");

                        close |= window.Html.ContainsIgnoreCase("Please make sure your characters are out of harm");
                        close |= window.Html.ContainsIgnoreCase("the servers are down for 30 minutes each day for maintenance and updates");

                        close |= window.Html.ContainsIgnoreCase("Item cannot be moved back to a loot container.");
                        close |= window.Html.ContainsIgnoreCase("you do not have the cargo space");
                        close |= window.Html.ContainsIgnoreCase("cargo units would be required to complete this operation.");
                        close |= window.Html.ContainsIgnoreCase("You are too far away from the acceleration gate to activate it!");
                        close |= window.Html.ContainsIgnoreCase("maximum distance is 2500 meters");
                        close |= window.Html.ContainsIgnoreCase("Broker found no match for your order");
                        close |= window.Html.ContainsIgnoreCase("All the weapons in this group are already full");

                        close |=
                            window.Html.ContainsIgnoreCase(
                                "If you decline of fail a mission from an agent he/she might become displeased and lower your standing towards him/her. You can decline a mission every four hours without penalty");
                        close |= window.Html.ContainsIgnoreCase("Do you wish to proceed with this dangerous action?");
                        close |= window.Html.ContainsIgnoreCase("weapons in that group are already full");
                        close |= window.Html.ContainsIgnoreCase("No rigs were added to or removed from the ship");
                        close |= window.Html.ContainsIgnoreCase("You can't fly your active ship into someone else's hangar");
                        close |= window.Html.ContainsIgnoreCase("You can't do this quite so fast");
                        clearPocket |= window.Html.ContainsIgnoreCase("This gate is locked!");

                        close |= window.Html.ContainsIgnoreCase("The Zbikoki's Hacker Card");
                        close |= window.Html.ContainsIgnoreCase(" units free.");
                        close |= window.Html.ContainsIgnoreCase("already full");
                        close |= window.Html.ContainsIgnoreCase("All the weapons in this group are already full");
                        close |=
                            window.Html.ContainsIgnoreCase(
                                "At any time you can log in to the account management page and change your trial account to a paying account");

                        close |= window.Html.ToLower().ContainsIgnoreCase("please make sure your characters are out of harms way");
                        close |= window.Html.ToLower().ContainsIgnoreCase("accepting connections");
                        close |= window.Html.ToLower().ContainsIgnoreCase("could not connect");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the connection to the server was closed");
                        close |= window.Html.ToLower().ContainsIgnoreCase("server was closed");
                        close |= window.Html.ToLower().ContainsIgnoreCase("make sure your characters are out of harm");
                        close |= window.Html.ToLower().ContainsIgnoreCase("connection to server lost");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the socket was closed");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the specified proxy or server node");
                        close |= window.Html.ToLower().ContainsIgnoreCase("starting up");
                        close |= window.Html.ToLower().ContainsIgnoreCase("unable to connect to the selected server");
                        close |= window.Html.ToLower().ContainsIgnoreCase("could not connect to the specified address");
                        close |= window.Html.ToLower().ContainsIgnoreCase("connection timeout");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the cluster is not currently accepting connections");
                        close |= window.Html.ToLower().ContainsIgnoreCase("your character is located within");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the transport has not yet been connected");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the user's connection has been usurped");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the EVE cluster has reached its maximum user limit");
                        close |= window.Html.ToLower().ContainsIgnoreCase("the connection to the server was closed");
                        close |= window.Html.ToLower().ContainsIgnoreCase("client is already connecting to the server");
                        close |= window.Html.ToLower().ContainsIgnoreCase("client update is available and will now be installed");
                        close |= window.Html.ToLower().ContainsIgnoreCase("change your trial account to a paying account");
                        close |= window.Html.ContainsIgnoreCase("You must be docked in a station or a structure to redeem items");
                        close |= window.Html.ContainsIgnoreCase("You do not have permission to execute that command");



                        if (window.Html.ContainsIgnoreCase("You are trying to sell") && window.Html.ContainsIgnoreCase("when you only have"))
                        {
                            close = true;
                            ESCache.Instance.SellError = true;
                        }

                        restart |= window.Html.ContainsIgnoreCase("The user's connection has been usurped on the proxy");
                        restart |= window.Html.ContainsIgnoreCase("The connection to the server was closed");
                        restart |= window.Html.ContainsIgnoreCase("server was closed");
                        restart |= window.Html.ContainsIgnoreCase("The socket was closed");
                        restart |= window.Html.ContainsIgnoreCase("The connection was closed");
                        restart |= window.Html.ContainsIgnoreCase("Connection to server lost");
                        restart |= window.Html.ContainsIgnoreCase("Connection to server was lost");
                        restart |= window.Html.ContainsIgnoreCase("The user connection has been usurped on the proxy");
                        restart |= window.Html.ContainsIgnoreCase("The transport has not yet been connected, or authentication was not successful");
                        restart |= window.Html.ContainsIgnoreCase("Your client has waited");
                        restart |= window.Html.ContainsIgnoreCase("This could mean the server is very loaded");
                        restart |= window.Html.ContainsIgnoreCase("Local cache is corrupt");
                        restart |= window.Html.ContainsIgnoreCase("Local session information is corrupt");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("the socket was closed");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("the connection was closed");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("Network communication between your computer and");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("connection to server lost.");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("local cache is corrupt");
                        //restart |= window.Html.ToLower().ContainsIgnoreCase("You are already performing a");
                        sayOk |= window.Html.ToLower().ContainsIgnoreCase("You are already performing a"); // temp
                        restart |= window.Html.ToLower().ContainsIgnoreCase("Unable to validate the authentication token provided by the launcher");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("item") && window.Html.ToLower().ContainsIgnoreCase("locked.");

                        restart |= window.Html.ToLower().ContainsIgnoreCase("The client's local session");
                        restart |= window.Html.ToLower().ContainsIgnoreCase("restart the client prior to logging in");

                        quit |= window.Html.ToLower().ContainsIgnoreCase("the cluster is shutting down");

                        sayYes |= window.Html.ContainsIgnoreCase("objectives requiring a total capacity");
                        sayYes |= window.Html.ContainsIgnoreCase("your ship only has space for");
                        sayYes |= window.Html.ContainsIgnoreCase("Are you sure you want to remove location");

                        sayYes |= window.Html.ContainsIgnoreCase("Are you sure you would like to decline this mission");
                        sayYes |= window.Html.ContainsIgnoreCase("has no other missions to offer right now. Are you sure you want to decline");
                        sayYes |= window.Html.ContainsIgnoreCase("You are about to remove a storyline mission from your journal");
                        sayYes |= window.Html.ContainsIgnoreCase("If you quit this mission you will lose standings with your agent");
                        sayYes |= window.Html.ContainsIgnoreCase("Repairing these items will cost");
                        sayYes |= window.Html.ContainsIgnoreCase("You are about to undock without the cargo required for");
                        sayYes |= window.Html.ContainsIgnoreCase("The following items are priced well below the average");
                        sayYes |= window.Html.ContainsIgnoreCase("This skill will be automatically injected");
                        sayYes |= window.Html.ContainsIgnoreCase("Are you sure you want to remove this location");
                        sayYes |= window.Html.ContainsIgnoreCase("You are about to throw away the following items");
                        sayYes |= window.Html.ContainsIgnoreCase("Are you sure you want to quit the game?");
                        sayYes |= window.Html.ContainsIgnoreCase("Are you sure you wish to eject from your ship and store");


                        sayOk |= window.Html.ContainsIgnoreCase("The destination system is currently being invaded");
                        sayOk |= window.Html.ContainsIgnoreCase("Are you sure you want to accept this offer?");
                        sayOk |= window.Html.ContainsIgnoreCase("You do not have an outstanding invitation to this fleet.");
                        sayOk |= window.Html.ContainsIgnoreCase("You have already selected a character for this session.");
                        sayOk |= window.Html.ContainsIgnoreCase("If you decline or fail a mission from an agent");
                        sayOk |= window.Html.ContainsIgnoreCase("The transport has not yet been connected, or authentication was not successful");
                        sayOk |= window.Html.ToLower().ContainsIgnoreCase("local session information is corrupt");
                        sayOk |= window.Html.ToLower().ContainsIgnoreCase("How much would you like to repair?");
                        sayOk |= window.Html.ContainsIgnoreCase("You are about to sell");
                        sayOk |= window.Html.ContainsIgnoreCase("It is much more efficient to talk to yourself in person than via the chat system");
                        sayOk |= window.Html.ContainsIgnoreCase("This star system has been secured by EDENCOM forces");
                        sayOk |= window.Html.ContainsIgnoreCase("This star system has been invaded by Triglavian forces");
                        sayOk |= window.Html.ContainsIgnoreCase("has rejected the invitation");
                        sayOk |= window.Html.ContainsIgnoreCase("You do not appear to be in a fleet");
                        sayOk |= window.Html.ContainsIgnoreCase("The star system you are about to enter is in a pirate insurgency zone and under the effects of maximum suppression.");
                        sayOk |= window.Html.ContainsIgnoreCase(
                            "This is extremely dangerous and CONCORD police can not guarantee your safety there. Do you want to proceed?");

                        stackHangars |= window.Html.ContainsIgnoreCase("as there are simply too many items here already");

                        agentRelated |= window.Html.ContainsIgnoreCase("One or more mission objectives have not been completed");
                        agentRelated |= window.Html.ContainsIgnoreCase("Please check your mission journal for further information");
                        agentRelated |= window.Html.ContainsIgnoreCase("You have to be at the drop off location to deliver the items in person");
                    }

                    if (ESCache.Instance.State.CurrentArmState == ArmState.RepairShop || ControllerManager.Instance.TryGetController<AbyssalController>(out _))
                        sayOk |= window.Guid.ContainsIgnoreCase("form.HybridWindow") && window.Caption.ContainsIgnoreCase("Set Quantity");

                    if (disableInstance)
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                            nameof(EveAccount.IsActive), false);

                    if (restart || quit || disableInstance)
                    {
                        Log("Restarting eve...");
                        Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        var msg = string.Format("Restarting character [{0}]", ESCache.Instance.EveAccount.CharacterName);
                        Log(msg);
                        ESCache.Instance.ExitEve($"Restart reason: [{window.Html}]");
                        return;
                    }

                    if (agentRelated)
                    {
                        ESCache.Instance.Statistics.MissionCompletionErrors++;
                        ESCache.Instance.Statistics.LastMissionCompletionError = DateTime.UtcNow;
                        Logging.Log.WriteLine("This window indicates an error completing a mission: [" + ESCache.Instance.Statistics.MissionCompletionErrors +
                                              "] errors already we will stop questor and halt restarting when we reach 3");
                        window.Close();
                        return;
                    }

                    if (sayYes)
                    {
                        Log("Found a window that needs 'yes' chosen...");
                        Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        window.AnswerModal("Yes");
                        break;
                    }

                    if (sayOk)
                    {
                        Log("Found a window that needs 'ok' chosen...");

                        if (window.Html == null)
                        {
                            Log("WINDOW HTML == NULL");
                            break;
                        }
                        else
                        {
                            Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");

                            if (window.Html.ContainsIgnoreCase("Repairing these items will cost"))
                                ESCache.Instance.doneUsingRepairWindow = true;
                            window.AnswerModal("OK");
                        }

                        break;
                    }

                    if (stackHangars)
                    {
                        if (!(ESCache.Instance.DirectEve.GetItemHangar() != null
                              && ESCache.Instance.DirectEve.GetItemHangar().StackAll())) return;
                        break;
                    }

                    if (gotoBaseNow)
                    {
                        Log("Evidently the cluster is dieing... and CCP is restarting the server");
                        Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;

                        window.Close();
                        break;
                    }

                    if (pause)
                    {
                        Log("This window indicates an error fitting the ship. pausing");
                        ControllerManager.Instance.SetPause(true);
                        break;
                    }

                    if (close)
                    {
                        Log("Closing modal window...");
                        Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        window.Close();
                        break;
                    }

                    if (clearPocket)
                    {
                        Log("Closing modal window...");
                        Log("Content of modal window (HTML): [" + window.Html.Replace("\n", "").Replace("\r", "") + "]");
                        window.Close();
                        ActionControl.ReplaceMissionsActions();
                        break;
                    }

                    if (!close && !restart && !gotoBaseNow && !sayYes && !sayOk && !pause && !quit && !stackHangars && !clearPocket && !disableInstance &&
                        !agentRelated)
                    {

                        if (DirectEve.Interval(10000))
                        {
                            LocalAndRemoteLog("Disabling/Exiting instance due unknown modal window: ");
                            LocalAndRemoteLog("--------------------------------------------------");
                            LocalAndRemoteLog("Debug_Window.Name: [" + window.Name + "]");
                            LocalAndRemoteLog("Debug_Window.Html: [" + window.Html + "]");
                            LocalAndRemoteLog("Debug_Window.Type: [" + window.Guid + "]");
                            LocalAndRemoteLog("Debug_Window.IsModal: [" + window.IsModal + "]");
                            LocalAndRemoteLog("Debug_Window.Caption: [" + window.Caption + "]");
                            LocalAndRemoteLog("Debug_Window.Buttons: [" +
                                              string.Join(", ", window.GetModalButtonList()) + "]");

                            LocalAndRemoteLog("--------------------------------------------------");

                        }

                        StringBuilder sb = new StringBuilder();
                        sb.AppendLine("--------------------------------------------------");
                        sb.AppendLine("Debug_Window.Name: [" + window.Name + "]");
                        sb.AppendLine("Debug_Window.Html: [" + window.Html + "]");
                        sb.AppendLine("Debug_Window.Type: [" + window.Guid + "]");
                        sb.AppendLine("Debug_Window.IsModal: [" + window.IsModal + "]");
                        sb.AppendLine("Debug_Window.Caption: [" + window.Caption + "]");
                        sb.AppendLine("Debug_Window.Buttons: [" + string.Join(", ", window.GetModalButtonList()) + "]");
                        sb.AppendLine("--------------------------------------------------");

                        SendDiscordWebHookMessage(sb.ToString());

                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                            nameof(EveAccount.IsActive), false);

                        if (!Framework.Session.IsInDockableLocation && Framework.Session.IsReady &&
                            Framework.Me.CurrentSolarSystem.GetSecurity() < 0.45 && !Framework.Me.IsInAbyssalSpace())
                        {
                            LocalAndRemoteLog("We are in space and not in a dockable location, not in the abyss and the security status is below 0.45, we will exit eve.");
                            Environment.Exit(0);
                            Environment.FailFast("");
                        }

                    }
                }
            }
        }

        private static HashSet<string> _alreadyPublishedMessage = new HashSet<string>();

        public static void SendDiscordWebHookMessage(string message)
        {

            try
            {
                string webhookUrl =
                    "https://discord.com/api/webhooks/1245260590532919480/KOx0weI8_NwELA_1gxjps4bF_y-VAvkOZkSa7XyHZyY8UmLb-Y_yfHQfFXufSe79PZfP";
                string usernameOverride = "CPP_MODAL";
                var msg = RemoveConfidentialInformation(message);

                if (_alreadyPublishedMessage.Contains(msg))
                    return;

                _alreadyPublishedMessage.Add(msg);

                Log("Sending message to discord webhook: " + msg);
                Task.Run(() => WCFClient.Instance.GetPipeProxy.SendDiscordWebhookMessage(webhookUrl, msg, usernameOverride));
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in SendDiscordWebHookMessage: " + ex.Message);
            }
        }
        public static string RemoveConfidentialInformation(string msg)
        {
            string characterName = ESCache.Instance.EveAccount.CharacterName;
            string characterId = ESCache.Instance.DirectEve.Session.CharacterId.ToString();
            string? currentSolarSystemId = ESCache.Instance.DirectEve?.Me?.CurrentSolarSystem?.Id.ToString() ?? null;
            string? currentSolarSystemName = ESCache.Instance.DirectEve.Me.CurrentSolarSystem.Name ?? null;
            string? questorAgentId = ESCache.Instance?.Agent?.AgentId.ToString() ?? null;
            string? questorAgentName = ESCache.Instance?.Agent?.Name ?? null;

            msg = msg.ReplaceWithoutException(characterName, "CHARACTER_NAME");
            msg = msg.ReplaceWithoutException(characterId, "CHARACTER_ID");
            msg = msg.ReplaceWithoutException(currentSolarSystemId, "CURRENT_SOLAR_SYSTEM_ID");
            msg = msg.ReplaceWithoutException(currentSolarSystemName, "CURRENT_SOLAR_SYSTEM_NAME");
            msg = msg.ReplaceWithoutException(questorAgentId, "QUESTOR_AGENT_ID");
            msg = msg.ReplaceWithoutException(questorAgentName, "QUESTOR_AGENT_NAME");

            // Replace numbers with XX
            msg = Regex.Replace(msg, @"\d+", "(num)");

            return msg;
        }

        public override void DoWork()
        {
            CheckModalWindows();
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}