/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;

using EVESharpCore.Controllers.Base;
using System;
using System.Collections.Generic;
using EVESharpCore.Controllers.Questor;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class UITravellerController : BaseController
    {
        #region Constructors

        public UITravellerController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new UITravellerControllerForm(this);
            DependsOn = new List<Type>() { typeof(DefenseController) };
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            IsWorkDone = true;
            IsPaused = true;
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