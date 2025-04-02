using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.AbyssalHunter
{
    public partial class AbyssalHunterControllerForm : Form
    {
        #region Fields

        private AbyssalHunterController _controller;

        #endregion Fields

        #region Constructors

        public AbyssalHunterControllerForm(AbyssalHunterController c)
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

        private void button1_Click(object sender, System.EventArgs e)
        {
            var action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                 
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