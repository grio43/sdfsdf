extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugBoosters : Form
    {
        #region Constructors

        public DebugBoosters()
        {
            InitializeComponent();
        }

        #endregion Constructors



        private void button1_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;

            Type dgvType = dataGridView2.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dataGridView2, true, null);

            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var ent = ESCache.Instance.DirectEve.Me.Boosters;
                    var res = ent.Select(n => new
                    {
                        TypeName = ESCache.Instance.DirectEve.GetInvType(n.TypeID).TypeName,
                        n.TypeID,
                        n.ExpireTime,
                        n.BoosterSlot,
                        BoosterEffects = string.Join(", ", ESCache.Instance.DirectEve.Me.GetBoosterEffects(n).Select(e => e.EffectName).ToArray()),
                        NegativeBoosterEffecets = string.Join(", ", ESCache.Instance.DirectEve.Me.GetNegativeBoosterEffects(n).ToArray()),

                    }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView2.DataSource = Util.ConvertToDataTable(res);
                            ModifyButtons(true);
                        }));
                    });
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button)
                        button.Enabled = enabled;
            }));
        }
    }
}