using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers
{
    public partial class UITravellerControllerForm : Form
    {
        #region Fields

        public UITravellerController uiTravellerController;

        #endregion Fields

        #region Constructors

        public UITravellerControllerForm(UITravellerController uiTravellerController)
        {
            this.uiTravellerController = uiTravellerController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button1_Click_1(object sender, EventArgs e)
        {
            button1.Enabled = false;
            new ActionQueueAction(new Action(() =>
                {
                    var bms = ESCache.Instance.DirectEve.Bookmarks.ToList();
                    Task.Run(() =>
                    {
                        try
                        {
                            Invoke(new Action(() =>
                            {
                                try
                                {
                                    comboBox1.DataSource = bms;
                                    comboBox1.DisplayMember = "Title";
                                    button1.Enabled = true;
                                }
                                catch (Exception exception)
                                {
                                    Console.WriteLine(exception);
                                }
                            }));
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(ex.ToString());
                        }
                    });
                })).Initialize()
                .QueueAction();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.Destination = null;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            Invoke(new Action(() => { ModifyControls(false); }));

            ActionQueueAction actionQueueAction = null;
            actionQueueAction = new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                        {
                            try
                            {
                                ESCache.Instance.Traveler.TravelToSetWaypoint();
                            }
                            catch (Exception exception)
                            {
                                Log.WriteLine(exception.ToString());
                            }

                            actionQueueAction.QueueAction();
                        }
                        else
                        {
                            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                            ESCache.Instance.Traveler.Destination = null;
                            Invoke(new Action(() => { ModifyControls(true); }));
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                }
            ));

            actionQueueAction.Initialize().QueueAction();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var dest = comboBox1.SelectedItem as DirectBookmark;
            ESCache.Instance.Traveler.Destination = null;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;

            if (dest != null)
            {
                Invoke(new Action(() => { ModifyControls(); }));

                ActionQueueAction actionQueueAction = null;
                actionQueueAction = new ActionQueueAction(new Action(() =>
                    {
                        try
                        {
                            if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                            {
                                try
                                {
                                    ESCache.Instance.Traveler.TravelToBookmark(dest);
                                }
                                catch (Exception exception)
                                {
                                    Log.WriteLine(exception.ToString());
                                }

                                actionQueueAction.QueueAction();
                            }
                            else
                            {
                                ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                                ESCache.Instance.Traveler.Destination = null;

                                Invoke(new Action(() => { ModifyControls(true); }));
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.WriteLine(ex.ToString());
                        }
                    }
                ));

                actionQueueAction.Initialize().QueueAction();
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.GetController<ActionQueueController>().RemoveAllActions();
            ESCache.Instance.Traveler.Destination = null;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            ModifyControls(true);
        }

        private void ModifyControls(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in panel1.Controls)
                {
                    if (b is Button button && button != button5)
                        button.Enabled = enabled;
                    if (b is ComboBox cb)
                        cb.Enabled = enabled;
                }
            }));
        }

        #endregion Methods

        private void checkBox1_CheckedChanged_1(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.AllowLowSec = ((CheckBox)sender).Checked;

        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.AllowNullSec = ((CheckBox)sender).Checked;
        }

        private void button3_Click(object sender, EventArgs e)
        {

            ESCache.Instance.Traveler.Destination = null;
            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;


            Invoke(new Action(() => { ModifyControls(); }));

            ActionQueueAction actionQueueAction = null;

            actionQueueAction = new ActionQueueAction(new Action(() =>
            {

                if (ESCache.Instance.Traveler.Destination == null)
                {
                    ESCache.Instance.Traveler.Destination = new DockableLocationDestination(60003760);
                    ESCache.Instance.Traveler.SetStationDestination(60003760);
                }

                try
                {
                    if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        try
                        {
                            ESCache.Instance.Traveler.ProcessState();
                        }
                        catch (Exception exception)
                        {
                            Log.WriteLine(exception.ToString());
                        }

                        actionQueueAction.QueueAction();
                    }
                    else
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        ESCache.Instance.Traveler.Destination = null;

                        Invoke(new Action(() => { ModifyControls(true); }));
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }
            ));

            actionQueueAction.Initialize().QueueAction();

        }

        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.AvoidGateCamps = ((CheckBox)sender).Checked;
        }

        private void checkBox4_CheckedChanged(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.AvoidBubbles = ((CheckBox)sender).Checked;
        }

        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.AvoidSmartbombs = ((CheckBox)sender).Checked;
        }

        private void ReloadSettings()
        {
            Log.WriteLine("ReloadSettings");
            this.Invoke(new Action(() =>
            {
                checkBox1.Checked = ESCache.Instance.Traveler.AllowLowSec;
                checkBox2.Checked = ESCache.Instance.Traveler.AllowNullSec;
                checkBox3.Checked = ESCache.Instance.Traveler.AvoidGateCamps;
                checkBox4.Checked = ESCache.Instance.Traveler.AvoidBubbles;
                checkBox5.Checked = ESCache.Instance.Traveler.AvoidSmartbombs;
                checkBox6.Checked = ESCache.Instance.Traveler.IgnoreDestinationChecks;
            }));
        }

        void TravelerOnOnSettingsChanged(object o, EventArgs eventArgs)
        {
            Log.WriteLine("TravelerOnOnSettingsChanged");
            ReloadSettings();
        }

        private void UITravellerControllerForm_Load(object sender, EventArgs e)
        {
            ReloadSettings();
            ESCache.Instance.Traveler.OnSettingsChanged += TravelerOnOnSettingsChanged;

        }

        private void checkBox6_CheckedChanged(object sender, EventArgs e)
        {
            ESCache.Instance.Traveler.IgnoreDestinationChecks = ((CheckBox)sender).Checked;
        }

        private void UITravellerControllerForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            ESCache.Instance.Traveler.OnSettingsChanged -= TravelerOnOnSettingsChanged;
        }
    }
}