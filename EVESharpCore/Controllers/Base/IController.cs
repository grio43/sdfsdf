/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:08
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Base
{
    /// <summary>
    ///     Description of IController.
    /// </summary>
    public interface IController : IDisposable
    {
        #region Properties

        List<Type> DependsOn { get; set; }

        [Browsable(false)]
        Form Form { get; set; }

        bool IgnoreModal { get; set; }

        bool IgnorePause { get; set; }

        bool IsPaused { get; set; }

        bool IsWorkDone { get; set; }

        DateTime LocalPulse { get; set; }

        [ReadOnly(true)]
        String Name { get; }

        bool RunBeforeLoggedIn { get; set; }

        [Browsable(false)]
        TabPage TabPage { get; set; }

        #endregion Properties

        #region Methods

        bool CheckSessionValid();

        void DoWork();

        bool EvaluateDependencies(ControllerManager cm);

        void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage);

        #endregion Methods
    }
}