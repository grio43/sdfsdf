using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class JitaLootControllerForm : Form
    {
        #region Fields

        private JitaLootController _controller;

        #endregion Fields

        #region Constructors

        public JitaLootControllerForm(JitaLootController c)
        {
            this._controller = c;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void ModifyButtons(bool enabled = false)
        {
            foreach (var b in Controls)
                if (b is Button)
                    ((Button)b).Enabled = enabled;
        }

        #endregion Methods
    }
}