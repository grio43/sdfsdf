/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 18:07
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Concurrent;
using System.Linq;

namespace EVESharpCore.Controllers
{
    public class ActionQueueController : BaseController
    {
        #region Fields

        private ConcurrentQueue<ActionQueueAction> ActionQueue;
        private ConcurrentQueue<ActionQueueAction> FastActionQueue;

        #endregion Fields

        #region Constructors

        public ActionQueueController() : base()
        {
            ActionQueue = new ConcurrentQueue<ActionQueueAction>();
            FastActionQueue = new ConcurrentQueue<ActionQueueAction>();
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Properties

        public bool IsActionQueueEmpty => ActionQueue.IsEmpty;

        public bool IsFastActionQueueEmpty => FastActionQueue.IsEmpty;

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            try
            {
                if (!ActionQueue.IsEmpty) // one for each frame
                {
                    ActionQueue.TryDequeue(out var action);

                    if (action != null)
                        action.Action.Invoke();
                    if (action.RunUntilRemoveRequest)
                        action.QueueAction();
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        /// <summary>
        /// Runs the action on every frame.
        /// </summary>
        public void DoWorkEveryFrame()
        {
            try
            {
                if (!FastActionQueue.IsEmpty)
                {
                    int c = FastActionQueue.Count; // save the amount because each action could queue itself or other actions during execution
                    for (int i = 0; i < c; i++) // run multiple actions each frame
                    {
                        FastActionQueue.TryDequeue(out var action);
                        if (action != null)
                            action.Action.Invoke();
                        if (action.RunUntilRemoveRequest)
                            action.QueueAction();
                    }
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        public bool ActionQueueContainsType(Type t)
        {
            return ActionQueue.Any(a => a.GetType() == t);
        }

        public bool FastActionQueueContainsType(Type t)
        {
            return FastActionQueue.Any(a => a.GetType() == t);
        }

        public void EnqueueNewAction(ActionQueueAction action)
        {
            if (action.ExecuteEveryFrame)
            {
                if (!FastActionQueue.Contains(action))
                    FastActionQueue.Enqueue(action);
            }
            else
            {
                if (!ActionQueue.Contains(action))
                    ActionQueue.Enqueue(action);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public void RemoveAllActions()
        {
            foreach (var a in ActionQueue.ToList().Concat(FastActionQueue.ToList()))
            {
                a.RemoveAction();
            }
            ActionQueue = new ConcurrentQueue<ActionQueueAction>();
            FastActionQueue = new ConcurrentQueue<ActionQueueAction>();
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}