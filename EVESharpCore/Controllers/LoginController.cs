/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:51
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using SC::SharedComponents.SharedMemory;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of LoginController.
    /// </summary>
    public class LoginController : BaseController
    {
        #region Constructors

        public LoginController() : base()
        {
            IgnorePause = true;
            IgnoreModal = false;
            RunBeforeLoggedIn = true;
            IgnoreValidSession = true;
        }

        private SharedArray<bool> _sharedArray;
        #endregion Constructors

        #region Properties

        public DateTime LoginTimeout { get; set; } = DateTime.UtcNow.AddSeconds(120);

        public static bool LoggedIn;

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            try
            {
                if (LoginTimeout < DateTime.UtcNow)
                    ESCache.Instance.ExitEve("Login timed out. Exiting.");

                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                    nameof(EveAccount.LastSessionReady), DateTime.UtcNow);

                if (ESCache.Instance.DirectEve.Session.IsReady)
                {
                    // check if the rcode return values have been verified
                    _sharedArray = new SharedArray<bool>(ESCache.Instance.CharName + nameof(UsedSharedMemoryNames.RcodeVerified));
                    if (!_sharedArray[0])
                    {
                        ESCache.Instance.ExitEve("ERROR: RCode values not verified! Disabling this instance.");
                        ESCache.Instance.DisableThisInstance();
                    }


                    Log("RCode return values verified and successfully logged in.", Color.Green);
                    LoggedIn = true;
                    IsWorkDone = true;


                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                        nameof(EveAccount.LoggedIn), true);
                    return;
                }

                if (ESCache.Instance.DirectEve.Login.IsConnecting || ESCache.Instance.DirectEve.Login.IsLoading)
                {
                    Log("Waiting for the login to be ready.");
                    LocalPulse = UTCNowAddSeconds(1, 1);
                    return;
                }

                if (ESCache.Instance.DirectEve.Login.AtCharacterSelection && ESCache.Instance.DirectEve.Login.IsCharacterSelectionReady)
                {
                    if (ESCache.Instance.EveAccount.SelectedController.Equals("None"))
                    {
                        Log("No controller was selected, waiting for the session to become valid.");
                        LocalPulse = UTCNowAddSeconds(3, 4);
                        return;
                    }

                    if (DateTime.UtcNow > ESCache.NextSlotActivate)
                    {
                        var myCharacterName = ESCache.Instance.EveAccount.CharacterName;
                        var slots = ESCache.Instance.DirectEve.Login.CharacterSlots;

                        if (slots.Any())
                        {
                            var charsOnAccount = slots.Select(c => c.CharName).ToList();
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                                nameof(EveAccount.CharsOnAccount), charsOnAccount);
                        }

                        foreach (var slot in slots)
                        {
                            if (slot.CharId.ToString(CultureInfo.InvariantCulture) != myCharacterName &&
                                String.Compare(slot.CharName, myCharacterName,
                                    StringComparison.OrdinalIgnoreCase) != 0)
                                continue;

                            Log("Activating character [" + slot.CharName + "]");
                            ESCache.NextSlotActivate = DateTime.UtcNow.AddSeconds(5);
                            slot.Activate();
                            LocalPulse = UTCNowAddSeconds(1, 2);
                            return;
                        }
                        Log("Character id/name [" + myCharacterName + "] not found, retrying in 10 seconds");
                    }
                }
                else
                {
                    if (!ESCache.Instance.DirectEve.Session.IsReady)
                    {
                        LocalPulse = UTCNowAddSeconds(1, 1);
                        Log("Session not ready yet, waiting.");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
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