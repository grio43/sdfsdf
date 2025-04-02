using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugModules : Form
    {
        #region Constructors

        public DebugModules()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    if (!ESCache.Instance.DirectEve.Session.IsInDockableLocation)
                    {
                        Log.WriteLine($"Not docked!");
                        return;
                    }

                    var wnd = ESCache.Instance.DirectEve.GetFittingWindow();

                    if (wnd == null)
                    {
                        action.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(1);
                        return;
                    }
                    var modules = wnd.GetAllModules(false);
                    var res = modules.Select(n => new { n.HasItemFit, n.SlotGroup, n.SlotExists, n?.TypeId, n?.InvType?.TypeName, IsOnline = n.IsOnline(), n?.InvType?.GroupId }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = res;
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

        #endregion Methods
    }
}