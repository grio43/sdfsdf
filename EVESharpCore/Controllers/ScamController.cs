/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of ExampleController.
    /// </summary>
    public class ScamController : BaseController
    {
        #region Fields

        private static DateTime _nextMessage;

        #endregion Fields

        #region Constructors

        public ScamController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            if (_nextMessage >= DateTime.UtcNow)
                return;

            _nextMessage = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(35, 55));

            try
            {
                var local = (DirectChatWindow)ESCache.Instance.DirectEve.Windows.FirstOrDefault(w => w.Name.StartsWith("chatchannel_local"));

                if (local == null)
                    return;

                local.Speak("Send me three million ISK for the best joke you've ever heard. Serving eve fellows with epic jokes since 2009.");
            }
            catch (Exception e)
            {
                Log(String.Format("Exception {0}", e.ToString()));
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