using System;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using System.Windows.Forms;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.AbyssalHunter
{
    public partial class AbyssalHydraControllerForm : Form
    {
        #region Fields

        private AbyssalHydraController _controller;

        #endregion Fields

        #region Constructors

        public AbyssalHydraControllerForm(AbyssalHydraController c)
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
                    _controller.ComeToMe();
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
            var action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    _controller.ComeToAdjacentSystemSun();
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    _controller.PreloadModules();
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            var action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    _controller.IdleSlaves();
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Log.WriteLine($"Set GankState to [{checkBox1.Checked}]");
            _controller.GankState = checkBox1.Checked;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            _controller.GankTargetCharacterName = textBox1.Text;
        }
    }
}