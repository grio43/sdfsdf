extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Logging;
using SC::SharedComponents.Controls;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace EVESharpCore
{
    extern alias SC;

    public partial class EVESharpCoreForm : Form
    {
        #region Fields

        private static int MAXIMIZED_HEIGHT = 327;
        private static int MINIMIZED_HEIGHT = 16;

        #endregion Fields

        #region Constructors

        public EVESharpCoreForm()
        {
            try
            {
                InitializeComponent();
                InitializeBuildTime();
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Constructors

        #region Methods

        private void InitializeBuildTime()
        {
            var assemblyFile = typeof(EVESharpCoreForm).Assembly.Location;
            if (!string.IsNullOrEmpty(assemblyFile) && File.Exists(assemblyFile))
            {
                Log.WriteLine("Unable to determine build time, assembly file not found");
            }
            var buildTime = File.GetLastWriteTime(assemblyFile);
            this.buildTimeLabel.Text = buildTime.ToString("t");
        }

        public void AddControllerTab(IController controller)
        {
            // just use invoke regardless
            Invoke(new Action(() =>
            {
                var frm = controller.Form;
                if (frm != null && !tabControlMain.TabPages.ContainsKey(frm.Text))
                {
                    Log.WriteLine($"Adding controller form {frm.Text}");
                    frm.FormBorderStyle = FormBorderStyle.None;
                    var tab = new TabPage(frm.Text);
                    controller.TabPage = tab;
                    tab.BackColor = Color.Blue;
                    tab.Name = frm.Text;
                    frm.TopLevel = false;
                    frm.Parent = tab;
                    frm.Visible = true;
                    frm.Dock = DockStyle.Fill;
                    tabControlMain.TabPages.Add(tab);
                    //tabControlMain.SelectedTab = tab;
                }
            }));
        }

        public void RemoveControllerTab(IController controller)
        {
            // just use invoke regardless
            Invoke(new Action(() =>
            {
                if (controller.Form != null && controller.TabPage != null)
                {
                    Log.WriteLine($"Removing controller form {controller.Form.Text}");
                    tabControlMain.TabPages.Remove(controller.TabPage);
                }
            }));
        }

        private void AddLog(string msg, Color? col = null)
        {
            try
            {
                col = col ?? Color.White;
                var item = new ListViewItem();
                item.Text = msg;
                label11.Text = msg;
                item.ForeColor = (Color)col;

                if (listViewLogs.Items.Count >= 1000)
                    listViewLogs.Items.Clear();
                listViewLogs.Items.Add(item);

                if (listViewLogs.Items.Count > 1)
                    listViewLogs.Items[listViewLogs.Items.Count - 1].EnsureVisible();
            }
            catch (Exception)
            {
            }
        }

        private void AddLogInvoker(string msg, Color? col)
        {
            try
            {
                if (!IsHandleCreated)
                    return;

                Invoke((MethodInvoker)delegate { AddLog(msg, col); });
            }
            catch (Exception)
            {
            }
        }

        private void Button2Click(object sender, EventArgs e)
        {
            if (Height == MINIMIZED_HEIGHT)
            {
                Height = MAXIMIZED_HEIGHT;
                tabControlMain.Visible = true;
            }
            else
            {
                Height = MINIMIZED_HEIGHT;
                tabControlMain.Visible = false;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void buttonAddController_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.AddController(((Type)comboBoxControllers.SelectedItem).Name);
        }

        private void buttonOpenLogDirectory_Click(object sender, EventArgs e)
        {
            Process.Start(Log.Logpath);
        }

        private void buttonRemoveController_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.RemoveController((Type)comboBoxControllers.SelectedItem);
        }

        private void dataGridControllers_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            //if (dataGridControllers.Columns[e.ColumnIndex]?.Name == nameof(BaseController.Avg))
            //{
            //    if (e.Value.GetType() == typeof(double))
            //    {
            //        e.Value = Math.Round((double)e.Value, 2);
            //    }
            //}
        }

        private void dataGridControllers_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
        }

        private void DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }

        private void EVESharpCoreFormLoad(object sender, EventArgs e)
        {
            try
            {
                Log.AsyncLogQueue.OnMessage += AddLogInvoker;
                Log.AsyncLogQueue.StartWorker();
                listViewLogs.OwnerDraw = true;
                listViewLogs.DrawColumnHeader += DrawColumnHeader;
                FormUtil.Color = listViewLogs.BackColor;
                FormUtil.Font = listViewLogs.Font;
                listViewLogs.DrawItem += FormUtil.DrawItem;
                listViewLogs.AutoArrange = false;

                listViewLogs.ItemActivate += delegate (object s, EventArgs _args)
                {
                    var i = listViewLogs.SelectedIndices[0];
                    new ListViewItemForm(Regex.Replace(listViewLogs.Items[i].Text, @"\r\n|\n\r|\n|\r", Environment.NewLine)).Show();
                };
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void EVESharpCoreFormShown(object sender, EventArgs e)
        {

            try
            {
                var eveDesktopWindowId = VirtualDesktopHelper.VirtualDesktopManager.GetWindowDesktopId(this.Handle);
                var instVirtDesktopId = ESCache.Instance.EveAccount.StartOnVirtualDesktopId;
                if (instVirtDesktopId.HasValue && eveDesktopWindowId != instVirtDesktopId)
                {
                    VirtualDesktopHelper.VirtualDesktopManager.MoveWindowToDesktop(this.Handle, instVirtDesktopId.Value);
                    Log.RemoteWriteLine($"Moved the E# Core window to desktop [{instVirtDesktopId.Value}]");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            try
            {
                // Late init here because new controllers can have a form as attribute, which will be added as tab page
                ControllerManager.Instance.Initialize();

                label11.Text = String.Empty;
                Height = MINIMIZED_HEIGHT;
                tabControlMain.Visible = false;
                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.EVESharpCoreFormHWnd), (Int64)Handle);

                var controllers = Assembly.GetAssembly(typeof(BaseController)).GetTypes().Where(t => t.IsSubclassOf(typeof(BaseController)) || t.IsSubclassOf(typeof(AbyssalBaseController)));
                comboBoxControllers.DataSource = controllers.Where(k => !ControllerManager.DEFAULT_CONTROLLERS.Contains(k) && !ControllerManager.HIDDEN_CONTROLLERS.Contains(k)).OrderBy(k => k.Name).ToList();
                comboBoxControllers.DisplayMember = "Name";
                dataGridControllers.DataSource = ControllerManager.Instance.ControllerList;
                //dataGridControllers.Columns[nameof(BaseController.Avg)].DefaultCellStyle.Format = "N2";
            }
            catch (Exception exception)
            {
                Log.WriteLine(exception.ToString());
            }
        }

        private void EVESharpFormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                Program.IsShuttingDown = true;
                Log.AsyncLogQueue.OnMessage -= AddLogInvoker;
                ControllerManager.Instance.RemoveAllControllers();
                ControllerManager.Instance.Dispose();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void PauseCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            ControllerManager.Instance.SetPause(((CheckBox)sender).Checked);
        }

        #endregion Methods
    }
}