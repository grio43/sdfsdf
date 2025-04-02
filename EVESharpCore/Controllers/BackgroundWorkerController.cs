/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 09.09.2016
 * Time: 14:35
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.Utility;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Controllers
{
    public class BackgroundWorkerController : BaseController, IOnFrameController
    {
        #region Constructors

        public BackgroundWorkerController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }


        const string SetAllowFleetInvitesFromMessageType = "SetAllowFleetInvitesFrom";


        #endregion Constructors

        #region Fields

        private DateTime _nextSkillCheck;
        private static ActionQueueAction _fleetHandleAction;

        private string _allowFleetInvitesFroms = null;

        private List<string> _inviteToFleet = new List<string>();


        public void SetInviteMembers(List<string> members)
        {
            _allowFleetInvitesFroms = Framework.Session.Character.Name;
            _inviteToFleet = members;
        }

        private List<string> FleetInviteList => ESCache.Instance.EveAccount.ClientSetting.GlobalMainSetting
            .AutoFleetMembers?.Split(',')?.OrderBy(e => e)?.Select(e => e.Trim())?.Distinct().ToList() ?? new List<string>();



        private Dictionary<long, (DateTime, int)> _nextInvite = new Dictionary<long, (DateTime, int)>();

        private static Random _rnd = new Random();

        #endregion Fields

        #region Methods

        public override void DoWork()
        {
            try
            {
                // Update current ships typeId
                try
                {
                    if (ESCache.Instance.EveAccount.CurrentShipTypeId != Framework.ActiveShip.TypeId)
                    {
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.CurrentShipTypeId), Framework.ActiveShip.TypeId);
                        Log($"Updated CurrentShipTypeId to {Framework.ActiveShip.TypeId}");
                    }
                }
                catch (Exception e)
                {
                    Log("Error in BackgroundWorkerController: " + e.Message);

                }

                // Fleet send invite handling

                if (FleetInviteList.Any() || _inviteToFleet.Any() || _allowFleetInvitesFroms != null)
                {
                    //Log($"#1");
                    // We check for all channels except local
                    // The one who is responsible for the invites is always the character with the lowest index in the chatchannel list (sorted by name)
                    // All other just accept the invites

                    var allChatChannelExceptLocal = Framework.ChatWindows.Where(e => e.ChatChannelCategory != "local");
                    // Select the channel which has the most members of our fleetInviteList, then order the channels by channelId and take the first one
                    var channelMembers = allChatChannelExceptLocal.SelectMany(m => m.Members).DistinctBy(e => e.Name).ToList();

                    if (channelMembers.Any())
                    {
                        //Log($"#2");
                        var allMembers = channelMembers.Select(m => m.Name).OrderBy(e => e).ToList();
                        var invitor = allMembers.FirstOrDefault(m => FleetInviteList.Contains(m)) ?? allMembers.FirstOrDefault(m => m == _allowFleetInvitesFroms);
                        var currentCharacterName = Framework.Session.Character.Name;
                        var fleetMembers = Framework.FleetMembers;
                        if (invitor == currentCharacterName)
                        {
                            //Log($"#3");
                            // Remove members from _nextInvite which are in our fleet
                            var characterIdsInFleet = fleetMembers.Select(f => f.CharacterId).ToList();
                            foreach (var id in characterIdsInFleet)
                            {
                                if (_nextInvite.ContainsKey(id))
                                    _nextInvite.Remove(id);
                            }

                            //Log("We are the fleet invite coordinator.");
                            // We are the lowest member, so we send invites to all other members
                            // Get member list of the channel to invite
                            var members = channelMembers.Where(m => FleetInviteList.Contains(m.Name) || _inviteToFleet.Contains(m.Name)).ToList();
                            //Log($"_inviteToFleet: {string.Join(", ", _inviteToFleet.Select(m => m))}");
                            //Log($"Members to invite: {string.Join(", ", members.Select(m => m.Name))}");
                            // Exclude ourself
                            members = members.Where(m => m.Name != currentCharacterName && m.CharacterId != Framework.Session.CharacterId).ToList();
                            // Get the character IDs
                            var characterIds = members.Select(m => m.CharacterId).Where(m => m > 0).ToList();

                            var characterIdsWhichAreNotInFleetYet =
                                characterIds.Where(c => fleetMembers.All(f => f.CharacterId != c)).ToList();
                            if (characterIdsWhichAreNotInFleetYet.Any())
                            {
                                // Do the invites
                                foreach (var characterId in characterIdsWhichAreNotInFleetYet)
                                {
                                    // Need some error handling, delays and timeouts
                                    // I.e how many times we want to invite someone, to not be spamming if there is a bug
                                    var min = 8;
                                    var max = 16;

                                    if (_nextInvite.ContainsKey(characterId))
                                    {
                                        // If the re-inv is not due yet, skip
                                        if (_nextInvite[characterId].Item1 >= DateTime.UtcNow)
                                        {
                                            //Log("Is not due yet.");
                                            continue;
                                        }
                                        if (_nextInvite[characterId].Item2 < 9)
                                        {
                                            var num = _nextInvite[characterId].Item2 + 1;
                                            _nextInvite[characterId] = (
                                                DateTime.UtcNow.AddSeconds(_rnd.Next(min * num * 2, max * num * 2)), num);
                                        }

                                        // Increase the delay based on tries
                                        //if (_nextInvite[characterId].Item2 < 11)
                                        //{
                                        //    var num = _nextInvite[characterId].Item2 + 1;
                                        //    _nextInvite[characterId] = (
                                        //        DateTime.UtcNow.AddSeconds(_rnd.Next(min * num, max * num)), num);
                                        //}
                                        //else
                                        //{
                                        //    var num = _nextInvite[characterId].Item2 + 1;
                                        //    _nextInvite[characterId] = (
                                        //        DateTime.UtcNow.AddMinutes(_rnd.Next(min * num, max * num)), num);
                                        //}
                                    }
                                    else
                                    {
                                        _nextInvite.Add(characterId,
                                            (DateTime.UtcNow.AddSeconds(_rnd.Next(min / 3, max / 3)), 1));
                                        return;
                                    }

                                    var invCharName = DirectOwner.GetOwner(Framework, characterId)?.Name;
                                    Log(
                                        $"Inviting [{characterId}] Charname [invCharName] to fleet.");


                                    SendBroadcastMessage(invCharName, nameof(BackgroundWorkerController),
                                        SetAllowFleetInvitesFromMessageType.ToString(),
                                        Framework.Session.Character.Name);


                                    Framework.InviteToFleet(characterId);
                                    break; // Only 1 inv per tick
                                }
                            }
                        }
                        else
                        {
                            // If we are not the lowest member, and the fleet coordinator is not within our fleet, we drop the fleet
                            if (fleetMembers.All(e => e.Name != invitor))
                            {
                                if (Framework.IsInFleet)
                                {
                                    Log(
                                        "We are not the fleet invite coordinator, and the fleet coordinator is not within our fleet. Dropping fleet.");
                                    Framework.LeaveFleet();
                                }
                            }
                        }
                    }
                }

                // Fleet recv invite handling
                // TODO: We should ignore fleet invites, or better, any (modal***) window which can be forced by another player (fleet (modal), duel (no modal), trade (no modal), chat inv (no modal) ...) -- Done
                if (Framework.ModalWindows.FirstOrDefault(w => w.MessageKey == "AskJoinFleet") is
                    { MessageKey: "AskJoinFleet" })
                {
                    if (_fleetHandleAction == null)
                    {
                        _fleetHandleAction = new ActionQueueAction(() =>
                        {
                            try
                            {
                                var fleetInviteWindow =
                                    Framework.ModalWindows.FirstOrDefault(w => w.MessageKey == "AskJoinFleet");

                                if (fleetInviteWindow == null)
                                    return;

                                var invitorHtml =
                                    fleetInviteWindow.Html.Substring(0,
                                        fleetInviteWindow.Html.IndexOf(" wants you to join", StringComparison.Ordinal));

                                // Check if the invitor is on our invite list and cancel if not
                                if (FleetInviteList.Concat(new[] { _allowFleetInvitesFroms }).Any(n => invitorHtml.Contains($">{n}<")))
                                {
                                    // Accept the invite
                                    Log(
                                        "The fleet invite request was made from a player on our invite list. Accepting now.");
                                    fleetInviteWindow.AnswerModal("Yes");
                                }
                                else
                                {
                                    // Close the invite
                                    Log($"Window.Html: {fleetInviteWindow.Html}");
                                    Log($"Fleet invite from {invitorHtml}. Will be closed now.");
                                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED,
                                        "Fleet invite received. Invitor name: " + invitorHtml));
                                    Log($"Closed fleet invitation from [{invitorHtml}]");
                                    fleetInviteWindow.Close();
                                }
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                            finally
                            {
                                _fleetHandleAction = null;
                            }
                        });
                        _fleetHandleAction.Initialize().QueueAction(Util.GetRandom(1000, 2000));
                    }
                }

                var walletWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectWalletWindow>().FirstOrDefault();

                if (walletWnd == null)
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenWallet);
                    Log($"Opening wallet.");
                    return;
                }

                // Local chat
                var local = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(w =>
                    w.Name.StartsWith("chatchannel_local"));

                if (local == null || local.Messages == null || !local.Messages.Any())
                    return;

                var msgs = local.Messages.Where(m => m.Message.Contains(ESCache.Instance.EveAccount.CharacterName));

                if (msgs != null && msgs.Any())
                    DirectEventManager.NewEvent(
                        new DirectEvent(DirectEvents.CALLED_LOCALCHAT,
                            "We were called in local chat by: " + msgs.FirstOrDefault().Name + " Message: " +
                            msgs.FirstOrDefault().Message));

                // Mail blink check
                var c = ESCache.Instance;
                var mailSvc = c.DirectEve.GetLocalSvc("mailSvc", false, false);
                if (mailSvc.IsValid && mailSvc.Attribute("blinkNeocom").ToBool())
                {
                    var msg = $"Unread email detected!";
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                }

                // Private chat
                var invWnd = c.DirectEve.Windows.FirstOrDefault(w => w.WindowId.StartsWith("ChatInvitation_"));
                if (invWnd != null && invWnd.PyWindow.IsValid)
                {
                    var invitorNameObj = invWnd.PyWindow.Attribute("invitorName");
                    var invitorName = invitorNameObj.IsValid ? invitorNameObj.ToUnicodeString() : String.Empty;
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED,
                        "Private chat received. Invitor name: " + invitorName));
                }

                var chatWnd = c.DirectEve.ChatWindows.FirstOrDefault(w =>
                    w.Guid.Contains("uicontrols.Window") && w.Caption.Contains("Private Chat"));
                if (chatWnd != null && chatWnd.PyWindow.IsValid)
                {
                    var member =
                        chatWnd.Members.FirstOrDefault(m => !m.Name.Equals(ESCache.Instance.EveAccount.CharacterName));
                    var memberName = String.Empty;
                    if (member != null)
                        memberName = member.Name;

                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.PRIVATE_CHAT_RECEIVED,
                        "Private chat detected. Name: " + memberName));
                }

                // Wallet check
                var cacheInstance = ESCache.Instance;

                if ((long)cacheInstance.MyWalletBalance != (long)cacheInstance.DirectEve.Me.Wealth)
                {
                    cacheInstance.MyWalletBalance = cacheInstance.DirectEve.Me.Wealth;
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Wallet has been changed."));
                }

                // Skill check
                if (_nextSkillCheck < DateTime.UtcNow)
                {
                    _nextSkillCheck = DateTime.UtcNow.AddMinutes(new Random().Next(10, 15));

                    if (ESCache.Instance.DirectEve.Skills.AreMySkillsReady)
                    {
                        var skillInTraining = ESCache.Instance.DirectEve.Skills.SkillInTraining;
                        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                            nameof(EveAccount.SkillInTrain), skillInTraining);
                        if (skillInTraining)
                        {
                            var last = ESCache.Instance.DirectEve.Skills.MySkillQueue.LastOrDefault();
                            if (last != null)
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                                    nameof(EveAccount.SkillQueueEnd), last.TrainingEndTime);
                            }
                        }
                    }
                }

                // Locked by another player
                var targetedByPlayer = ESCache.Instance.EntitiesNotSelf.Count(e => e.IsPlayer && e.IsTargetedBy);

                if (targetedByPlayer > 0)
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.LOCKED_BY_PLAYER,
                        $"Locked by another player. Amount: [{targetedByPlayer}]"));
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
            finally
            {
                LocalPulse = UTCNowAddMilliseconds(900, 1500);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage bc)
        {

            if (bc.Command == SetAllowFleetInvitesFromMessageType.ToString())
            {
                _allowFleetInvitesFroms = bc.Payload;
            }

            Log($"BroadcastMessage received [{bc}]");

        }

        #endregion Methods

        private void HandleDynamicItemRemoteLookup()
        {
            if (!DirectEve.Interval(50, 70))
                return;

            var dynamicItemSvc = Framework.GetLocalSvc("dynamicItemSvc");
            if (!dynamicItemSvc.IsValid)
                return;

            var req = DirectItem.RequestedDynamicItems.ToList();

            foreach (var itemId in req)
            {
                if (DirectItem.FinishedRemoteCallDynamicItems.Contains(itemId))
                    continue;

                DirectItem.FinishedRemoteCallDynamicItems.Add(itemId);
                DirectItem.RequestedDynamicItems.Remove(itemId);
                //Log($"GetDynamicItem [{itemId}]");
                Framework.ThreadedCall(dynamicItemSvc["GetDynamicItem"], itemId);
                return;
            }
        }

        public void OnFrame()
        {
            HandleDynamicItemRemoteLookup();
        }
    }
}