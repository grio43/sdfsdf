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
    public partial class DebugChannels : Form
    {
        #region Constructors

        public DebugChannels()
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

                    var chatChannels = ESCache.Instance.DirectEve.ChatWindows;
                    var res = chatChannels.Select(n => new { n.MemberCount, n.ChannelId, n.Guid, n.Caption, n.Name, n.ChatChannelCategory, src = n }).ToList();
                    Log.WriteLine($"{chatChannels.Count()} channel(s) found.");

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

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            dynamic obj = null;
            try
            {
                obj = dataGridView1.CurrentRow.DataBoundItem;
            }
            catch (Exception exception)
            {
                Log.WriteLine(exception.ToString());
            }

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

                    var chatChannel = ESCache.Instance.DirectEve.ChatWindows.FirstOrDefault(c => c.ChannelId.Equals(obj.ChannelId));
                    var res = chatChannel.Members.OrderBy(m => m.Name).Select(m => new { m.Name, m.AllianceId, m.CharacterId, m.CorporationId, m.WarFactionId, MinStanding = ESCache.Instance.DirectEve.Standings.GetMinStanding(m), MaxStanding = ESCache.Instance.DirectEve.Standings.GetMaxStanding(m) }).ToList();
                    var msgs = chatChannel.Messages.OrderBy(m => m.Time).Select(m => new { m.Time, m.Name, m.Message, }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView2.DataSource = res;
                            dataGridView3.DataSource = msgs;
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

        private void DebugChat_Load(object sender, EventArgs e)
        {
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

        private void dataGridView3_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}