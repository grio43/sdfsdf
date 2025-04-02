extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.Utility;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of GridWatchController.
    /// </summary>
    public class GridWatchController : BaseController
    {
        #region Fields

        #endregion Fields

        #region Constructors

        public GridWatchController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            try
            {
                // this works only for corp standings!
                var anyNonBluePlayerOnGrid = ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1000000 && ESCache.Instance.DirectEve.Standings.GetCorporationRelationship(e.DirectEntity.CorpId) <= 0);
                if (anyNonBluePlayerOnGrid && DirectEve.Interval(3000))
                {
                    Log($"Non blue player on grid detected, playing notice sounds.");
                    Util.PlayNoticeSound();
                }
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