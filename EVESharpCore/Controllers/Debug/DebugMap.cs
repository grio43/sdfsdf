using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugMap : Form
    {
        #region Constructors

        public DebugMap()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button1_Click(object sender, EventArgs e)
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

                    var from = string.Empty;
                    var to = string.Empty;
                    var allowLowSec = false;
                    var allowNullSec = false;

                    this.Invoke(new Action(() =>
                    {
                        from = comboBox1.Text;
                        to = comboBox2.Text;
                        allowLowSec = checkBox2.Checked;
                        allowNullSec = checkBox1.Checked;
                    }));

                    Log.WriteLine($"{from} {to}.");

                    var fromSystem = ESCache.Instance.DirectEve.SolarSystems.Values.FirstOrDefault(k => k.Name.Equals(from));
                    var toSystem = ESCache.Instance.DirectEve.SolarSystems.Values.FirstOrDefault(k => k.Name.Equals(to));
                    //var skip = ESCache.Instance.DirectEve.SolarSystems.Take(20).Select(k => k.Value).ToHashSet();
                    //var path = fromSystem.CalculatePathTo(toSystem, skip, highsecOnly, lowsecOnly);
                    var path = fromSystem.CalculatePathTo(toSystem, null, allowLowSec, allowNullSec);
                    Log.WriteLine($"Calculating path from [{fromSystem.Name}] to [{toSystem.Name}] took {path.Item2} milliseconds.");
                    var res = path.Item1.Select(s => new
                    {
                        s.Id,
                        Security = s.GetSecurity(),
                        s.Name,
                        ConstellationName = s.Constellation.Name,
                        s.ConstellationId,
                        Neighbours = string.Join(", ",
                            s.Neighbours.Select(n => n.Name))
                    }).ToList();

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

        private void button2_Click(object sender, EventArgs e)
        {
            LoadSystems();
        }

        private void dataGridView1_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            using (SolidBrush b = new SolidBrush(dataGridView1.RowHeadersDefaultCellStyle.ForeColor))
            {
                e.Graphics.DrawString((e.RowIndex + 1).ToString(), e.InheritedRowStyle.Font, b, e.RowBounds.Location.X + 10, e.RowBounds.Location.Y + 4);
            }
        }

        private void DebugMap_Load(object sender, EventArgs e)
        {
        }

        private void DebugMap_Shown(object sender, EventArgs e)
        {
            //LoadSystems();
            LoadComboboxes();
        }

        private void LoadComboboxes()
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
                    var systems = ESCache.Instance.DirectEve.SolarSystems.Values.OrderBy(k => k.Name).ToList();
                    var cb1 = systems.ToList();
                    var cb2 = systems.ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            comboBox1.DataSource = cb1;
                            comboBox1.DisplayMember = "Name";
                            comboBox1.SelectedItem = cb1.FirstOrDefault(k => k.Name.ToLower().Equals("jita"));
                            comboBox2.DataSource = cb2;
                            comboBox2.DisplayMember = "Name";
                            comboBox2.SelectedItem = cb2.FirstOrDefault(k => k.Name.ToLower().Equals("oj-ct4"));
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

        private void LoadSystems()
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
                    var systems = ESCache.Instance.DirectEve.SolarSystems.Values.OrderBy(k => k.Name).ToList();
                    var res = systems.Select(s => new
                    {
                        s.Id,
                        Security = s.GetSecurity(),
                        s.Name,
                        ConstellationName = s.Constellation.Name,
                        s.ConstellationId,
                        Neighbours = string.Join(", ",
                            s.Neighbours.Select(n => n.Name))
                    }).ToList();

                    Log.WriteLine($"{systems.Count()} system(s) found.");

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
                {
                    if (b is Button button)
                        button.Enabled = enabled;
                    if (b is ComboBox cb)
                        cb.Enabled = enabled;
                }
            }));
        }

        #endregion Methods

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }
    }
}