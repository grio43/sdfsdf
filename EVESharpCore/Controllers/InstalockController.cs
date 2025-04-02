extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using System;
using System.Linq;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;
using System.Threading.Tasks;

namespace EVESharpCore.Controllers
{
    public class InstalockController : BaseController, IOnFrameController
    {
        #region Fields

        private static DateTime _nextMessage;
        private bool _allowedToStartLocking;
        private Random _random = new Random();

        #endregion Fields

        #region Constructors

        public InstalockController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {

        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public void OnFrame()
        {

            var ship = ESCache.Instance.DirectEve.Entities.FirstOrDefault(e => e.GroupId == (int)Group.HeavyAssaultShip);
            if (ship != null)
            {
                //if (ship.Velocity < 1)
                //    return;

                if (!_allowedToStartLocking)
                    return;

                if (!ship.IsTarget && !ship.IsTargeting && DirectEve.Interval(250, 350))
                {
                    ship.LockTarget();
                    Log($"Locking entity  Name [{ship.Name}] ID [{ship.Id}]");
                    return;
                }

                if (ship.IsTarget || ship.IsTargeting)
                {
                    ship.MakeActiveTarget();
                    var disruptor = Framework.Modules.FirstOrDefault(m => m.GroupId == (int)Group.WarpDisruptor);
                    disruptor.Click();
                    // log that we activate the module on the target   
                    Log($"Activating module  Name [{disruptor.TypeName}] ID [{disruptor.ItemId}] on target [{ship.TypeName}]");
                    this.IsPaused = true;
                }
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            switch (broadcastMessage.Command)
            {
                case "WARP_START":
                    Log($"Received broadcast message. [{broadcastMessage}]");
                    Task.Run(async () =>
                    {
                        await Task.Delay(Rnd.Next(500, 550));
                        _allowedToStartLocking = true;
                    });
                    break;
                default:
                    break;
            }
        }

        #endregion Methods
    }
}