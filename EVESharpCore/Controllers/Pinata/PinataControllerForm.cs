using System.Windows.Forms;

namespace EVESharpCore.Controllers.Pinata
{
    public partial class PinataControllerForm : Form
    {
        #region Fields

        private PinataController _pinataController;

        #endregion Fields

        #region Constructors

        public PinataControllerForm(PinataController pinataController)
        {
            this._pinataController = pinataController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        public DataGridView GetDataGridView1 => this.dataGridView1;

        public DataGridView GetDataGridView2 => this.dataGridView2;

        private void ModifyButtons(bool enabled = false)
        {
            foreach (var b in Controls)
                if (b is Button)
                    ((Button)b).Enabled = enabled;
        }

        #endregion Methods
    }
}