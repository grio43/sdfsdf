using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class WarpFollowControllerForm : Form
    {
        #region Fields

        private WarpFollowController _controller;

        #endregion Fields

        #region Constructors

        public WarpFollowControllerForm(WarpFollowController c)
        {
            this._controller = c;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        public DataGridView GetDataGridView1 => this.dataGridView1;

        private void ModifyButtons(bool enabled = false)
        {
            foreach (var b in Controls)
                if (b is Button)
                    ((Button)b).Enabled = enabled;
        }

        #endregion Methods
    }
}