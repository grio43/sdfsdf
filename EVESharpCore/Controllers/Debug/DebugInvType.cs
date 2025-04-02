using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugInvType : Form
    {
        #region Constructors

        public DebugInvType()
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


                    try
                    {

                        var type = ESCache.Instance.DirectEve.GetInvType(int.Parse(textBox1.Text));
                        if (type != null)
                        {
                            var dgmEff = type.GetDmgEffects();

                            Logging.Log.WriteLine($"Count: {dgmEff.Count}");
                            var res = dgmEff.Select(n => new
                            {
                                n.Key,
                                n.Value.EffectID,
                                n.Value.EffectName,
                                n.Value.DisplayName,
                                n.Value.Guid,
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
                        else
                        {
                            Logging.Log.WriteLine("Type is null?");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex}");
                    }


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
                    if (b is Button button)
                        button.Enabled = enabled;
            }));
        }

        #endregion Methods

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
                    try
                    {

                        var type = ESCache.Instance.DirectEve.GetInvType(int.Parse(textBox1.Text));
                        if (type != null)
                        {
                            var attr = type.GetAttributesInvType();

                            Logging.Log.WriteLine($"Count: {attr.Count}");
                            var res = attr.Select(n => new
                            {
                                n.Key,
                                n.Value,
                            }).ToList();


                            Task.Run(() =>
                            {
                                return Invoke(new Action(() =>
                                {
                                    dataGridView2.DataSource = res;
                                    ModifyButtons(true);
                                }));
                            });
                        }
                        else
                        {
                            Logging.Log.WriteLine("Type is null?");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex}");
                    }


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
                    try
                    {

                        var type = ESCache.Instance.DirectEve.GetInvType(int.Parse(textBox1.Text));
                        if (type != null)
                        {
                            var price = type.AveragePrice();
                            Console.WriteLine($"AvgPrice [{price}]");
                            Task.Run(() =>
                            {
                                return Invoke(new Action(() =>
                                {
                                    dataGridView2.DataSource = new List<double>() { price };
                                    ModifyButtons(true);
                                }));
                            });
                        }
                        else
                        {
                            Logging.Log.WriteLine("Type is null?");
                        }

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Exception: {ex}");
                    }


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