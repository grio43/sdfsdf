extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Logging;
using SC::SharedComponents.IPC;
using System;
using System.ServiceModel;
using System.Threading;
using EVESharpCore.Controllers.Questor.Core.States;
using System.Reflection;

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Documents;
using SC::SharedComponents.Utility;

namespace EVESharpCore
{



    [CallbackBehavior(ConcurrencyMode = ConcurrencyMode.Reentrant, UseSynchronizationContext = false)]
    public class LauncherCallback : IDuplexServiceCallback
    {
        #region Methods

        public void GotoHomebaseAndIdle()
        {
            Log.WriteLine("Settings.Instance.AutoStart = false, CurrentCombatMissionBehaviorState  = CombatMissionsBehaviorState.GotoBase");
            ESCache.Instance.State.CurrentQuestorState = QuestorState.Start;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            ESCache.Instance.PauseAfterNextDock = true;
            ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
            ESCache.Instance.Traveler.Destination = null;
            var msg = string.Format("Set [{0}] going to homebase and idle.", ESCache.Instance.EveAccount.CharacterName);
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        [DllImport("MemMan.dll", EntryPoint = "RestartEveSharpCore")]
        public static extern void RestartESCore();

        public void RestartEveSharpCore()
        {
            RestartESCore();
        }

        public void GotoJita()
        {
            new Thread(() =>
            {
                try
                {
                    if (ControllerManager.Instance.GetController<ActionQueueController>().IsActionQueueEmpty)
                    {
                        var msg = string.Format("Set [{0}] going to Jita and pause.", ESCache.Instance.EveAccount.CharacterName);
                        WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
                        Log.WriteLine("Adding GotoJitaAction");
                        new GotoJitaAction().Initialize().QueueAction();
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception:" + ex);
                }
            }).Start();
        }

        public void OnCallback()
        {
        }

        public void PauseAfterNextDock()
        {
            ESCache.Instance.PauseAfterNextDock = true;
            var msg = string.Format("Set [{0}] to pause after next station dock.", ESCache.Instance.EveAccount.CharacterName);
            WCFClient.Instance.GetPipeProxy.RemoteLog(msg);
        }

        public void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            var controller = ControllerManager.Instance.GetController(broadcastMessage.TargetController);
            if (controller != null)
            {
                controller.ReceiveBroadcastMessage(broadcastMessage);
            }
            else
            {
                Logging.Log.WriteLine($"Controller could not be found [{broadcastMessage.TargetController}]");
            }
        }

        #endregion Methods
    }
}