using System;
using System.Threading.Tasks;

namespace EVESharpCore.Controllers.ActionQueue.Actions.Base
{
    public class ActionQueueAction : IActionQueueAction
    {
        #region Constructors

        public ActionQueueAction()
        {
        }

        public ActionQueueAction(Action action, bool executeEveryFrame = false)
        {
            ExecuteEveryFrame = executeEveryFrame;
            Action = action;
        }

        public ActionQueueAction(Action initializeAction, Action action)
        {
            InitializeAction = initializeAction;
            Action = action;
        }

        /// <summary>
        /// Runs a given action in context of the on frame event.
        /// </summary>
        /// <param name="action">The action.</param>
        /// <param name="runUntilRemoveAction">Runs the action in a loop until RemoveAction is being called.</param>
        public static ActionQueueAction Run(Action action, bool ExecuteEveryFrame = false, bool runUntilRemoveAction = false)
        {
            ActionQueueAction a = new ActionQueueAction(action);
            a.RunUntilRemoveRequest = runUntilRemoveAction;
            a.ExecuteEveryFrame = ExecuteEveryFrame;
            a.Initialize().QueueAction();
            return a;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Main action of the ActionQueueAction.
        ///     This is being called by the ActionQueueActionController,
        ///     hence executed by the endscene/present hook.
        /// </summary>
        public Action Action { get; set; }

        public bool ExecuteEveryFrame { get; set; }

        /// <summary>
        ///     InitializeAction
        ///     This is NOT being executed within endscene/present hook.
        ///     Only use atomic operations on anything which is also being
        ///     used by DirectEve.
        /// </summary>
        public Action InitializeAction { get; set; }

        public bool IsBeingRemoved { get; set; }
        public bool IsInitialized { get; set; }
        public bool RunUntilRemoveRequest { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Initializes the ActionQueueAction by running the InitializeAction.
        ///     This is NOT being executed within endscene/present hook.
        /// </summary>
        public ActionQueueAction Initialize()
        {
            if (InitializeAction != null)
                InitializeAction.Invoke();

            IsInitialized = true;
            return this;
        }

        /// <summary>
        ///  Puts the Action into the queue and will be executed by the ActionQueueController.
        ///  This will be executed by the endscene/present hook.
        /// </summary>
        /// <param name="delay">Delay before adding the task in milliseconds</param>
        /// <returns></returns>
        public ActionQueueAction QueueAction(int delay = 0)
        {
            if (!IsInitialized || IsBeingRemoved)
                return this;

            if (delay == 0)
                ControllerManager.Instance.GetController<ActionQueueController>().EnqueueNewAction(this);
            else
            {
                Task.Run(async delegate
                {
                    await Task.Delay(delay);
                    ControllerManager.Instance.GetController<ActionQueueController>().EnqueueNewAction(this);
                });
            }

            return this;
        }

        /// <summary>
        ///     Puts the Action into 'being removed' state.
        ///     Will be executed once at most after calling.
        /// </summary>
        public void RemoveAction()
        {
            IsBeingRemoved = true;
        }

        #endregion Methods
    }
}