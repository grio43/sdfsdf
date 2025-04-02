/*
 * User: duketwo
 * Date: 10.16.2020
 * Time: 04:13
 *
 */

extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using SC::SharedComponents.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using SC::SharedComponents.Utility;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of WarpFollowController.
    /// </summary>
    public class WarpFollowController : BaseController
    {

        public WarpFollowController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new WarpFollowControllerForm(this);
        }

        public override void DoWork()
        {
            var warpingEntities = ESCache.Instance.Entities.Where(e => e.Mode == 3);
            var frm = (WarpFollowControllerForm)this.Form;
            var dgv = frm.GetDataGridView1;
            var res = warpingEntities.Select(n => new
            {
                n.Id,
                n.Name,
                n.TypeName,
                WarpDestName = n?.DirectEntity?.WarpDestinationEntity?.Name
            }).ToList();

            dgv.Invoke(new Action(() =>
            {
                dgv.DataSource = Util.ConvertToDataTable(res);
            }));
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }
    }
}