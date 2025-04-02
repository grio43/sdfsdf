using System;

namespace EVESharpCore.Controllers.ActionQueue.Actions.Base
{
    public interface IActionQueueAction
    {
        #region Properties

        Action Action { get; set; }
        bool ExecuteEveryFrame { get; set; }
        Action InitializeAction { get; set; }
        bool IsBeingRemoved { get; set; }
        bool IsInitialized { get; set; }

        #endregion Properties

        #region Methods

        ActionQueueAction Initialize();

        ActionQueueAction QueueAction(int delay = 0);

        void RemoveAction();

        #endregion Methods
    }
}