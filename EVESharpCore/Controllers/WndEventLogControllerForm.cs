extern alias SC;
using EVESharpCore.Controllers.Debug;
using System;
using System.Windows.Forms;
using EVESharpCore.Logging;
using SC::SharedComponents.Utility;
using System.Drawing;
using System.Text.RegularExpressions;
using SC::SharedComponents.Controls;
using System.ComponentModel;

namespace EVESharpCore.Controllers
{
    public partial class WndEventLogControllerForm : Form
    {
        #region Fields

        private WndEventLogController _wndEventLogController;

        #endregion Fields

        #region Constructors

        public WndEventLogControllerForm(WndEventLogController wndEventLogController)
        {
            this._wndEventLogController = wndEventLogController;
            InitializeComponent();
        }

        public ListView GetListViewLogs() => this.listViewLogs;

        //public DataGridView GetDataGridView() => this.dataGridView1;

        private void AddLog(string msg, Color? col = null)
        {
            try
            {
                col = col ?? Color.White;
                var item = new ListViewItem
                {
                    Text = msg,
                    ForeColor = (Color)col
                };

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

        public void AddLogInvoker(string msg, Color? col)
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

        //public void UpdateDataSource(object source)
        //{
        //    DataGridView g = dataGridView1;
        //    g.Invoke(new Action(() =>
        //    {
        //        SuspendLayout();

        //        //Save last position and sort order

        //        Int32 idxFirstDisplayedScrollingRow = g.FirstDisplayedScrollingRowIndex;
        //        SortOrder dgvLastSortDirection = g.SortOrder;
        //        Int32 lastSortColumnPos = g.SortedColumn?.Index ?? -1;
        //        Int32 dgvLastCellRow = g.CurrentCell?.RowIndex ?? -1;
        //        Int32 dgvLastCellColumn = g.CurrentCell?.ColumnIndex ?? -1;

        //        //Set new datasource
        //        g.DataSource = source;

        //        //Restore sort order, scroll row, and active cell

        //        if (lastSortColumnPos > -1)
        //        {
        //            DataGridViewColumn newColumn = g.Columns[lastSortColumnPos];
        //            switch (dgvLastSortDirection)
        //            {
        //                case SortOrder.Ascending:
        //                    g.Sort(newColumn, ListSortDirection.Ascending);
        //                    break;
        //                case SortOrder.Descending:
        //                    g.Sort(newColumn, ListSortDirection.Descending);
        //                    break;
        //                case SortOrder.None:
        //                    //No sort
        //                    break;
        //            }
        //        }

        //        if (idxFirstDisplayedScrollingRow >= 0)
        //            g.FirstDisplayedScrollingRowIndex = idxFirstDisplayedScrollingRow;

        //        if (dgvLastCellRow > -1 && dgvLastCellColumn > -1)
        //            g.CurrentCell = g[dgvLastCellColumn, dgvLastCellRow];


        //        ResumeLayout();
        //    }));
        //}

        #endregion Constructors

        #region Methods

        #endregion Methods

        private void WndEventLogControllerForm_Load(object sender, EventArgs e)
        {
            try
            {
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

        private void DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            e.DrawDefault = true;
        }
    }
}