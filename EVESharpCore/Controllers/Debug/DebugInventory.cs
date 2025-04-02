extern alias SC;

using SC::SharedComponents.Utility;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugInventory : Form
    {
        public DebugInventory()
        {
            InitializeComponent();
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

        private void button1_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;

            Type dgvType = dataGridView1.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dataGridView1, true, null);

            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var items = ESCache.Instance.DirectEve.GetItemHangar().Items.ToList();
                    var res = items.Select(n => new
                    {
                        n.ItemId,
                        n.GivenName,
                        n.LocationId,
                        n.TypeName,
                        n.TypeId,
                        n.Quantity,
                        n.Stacksize,
                        n.OwnerId,
                        n.GroupId,
                        n.CategoryId,
                        n.IsSingleton,
                        n.IsDynamicItem,
                        OrignalDynamicItemTypeId = n.OrignalDynamicItem?.TypeId ?? -1,
                        IS_TRASHABLE = n.IsTrashable(),
                        DUMP = n.PyItem.LogObject(),

                    }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = Util.ConvertToDataTable(res);
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
    }
}
