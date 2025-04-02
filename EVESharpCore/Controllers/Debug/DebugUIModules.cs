using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugUIModules : Form
    {
        #region Constructors

        public DebugUIModules()
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

                    var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
                    if (shipsCargo == null)
                    {
                        action.QueueAction();
                        return;
                    }

                    var charges = shipsCargo.Items.Where(i => i.CategoryId == (int)CategoryID.Charge).ToList();
                    var toolStripMenuItem = (ToolStripMenuItem)contextMenuStrip1.Items[1];
                    toolStripMenuItem.DropDownItems.Clear();

                    foreach (var c in charges)
                    {
                        toolStripMenuItem.DropDownItems.Add($"{c.TypeName} ({c.Quantity})", null, new EventHandler(delegate (object s, EventArgs ev)
                        {
                            Int64 itemId = 0;
                            try
                            {
                                var index = dataGridView1.SelectedCells[0].OwningRow.Index;
                                DataGridViewRow selectedRow = dataGridView1.Rows[index];
                                itemId = Convert.ToInt64(selectedRow.Cells["ItemId"].Value);
                            }
                            catch (Exception exception)
                            {
                                Log.WriteLine(exception.ToString());
                                throw;
                            }

                            ModifyButtons();
                            ActionQueueAction ac = null;
                            ac = new ActionQueueAction(new Action(() =>
                            {
                                try
                                {
                                    var module = ESCache.Instance.Modules.FirstOrDefault(m => m.ItemId.Equals(itemId));
                                    var charge = ESCache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == c.TypeId && i.Quantity == c.Quantity);
                                    if (module != null && charge != null)
                                    {
                                        Log.WriteLine($"Changing ammo.");
                                        module.ChangeAmmo(charge);
                                    }
                                    ModifyButtons(true);
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine(ex.ToString());
                                }
                            }));
                            ac.Initialize().QueueAction();
                        }));
                    }

                    var resList = ESCache.Instance.Modules.Select(d => new
                    {
                        d.TypeId,
                        d.HeatDamage,
                        d.OverloadState,
                        d.HeatDamagePercent,
                        d.IsMaster,
                        d.GroupId,
                        d.TypeName,
                        d.IsReloadingAmmo,
                        d.CanBeReloaded,
                        d.IsActivatable,
                        d.IsInLimboState,
                        d.IsBeingRepaired,
                        d.Duration,
                        d.IsActive,
                        d.EffectId,
                        d.EffectName,
                        d.EffectCategory,
                        d.IsEffectOffensive,
                        d.EffectDurationMilliseconds,
                        d.EffectStartedWhen,
                        d.MillisecondsUntilNextCycle,
                        d.TargetId,
                        d.IsDeactivating,
                        d.ItemId,
                        d.ReactivationDelay,
                        ChargeTypeId = d.Charge != null && d.Charge.PyItem.IsValid ? d.Charge.TypeId : 0,
                        ChargeTypeName = d.Charge != null && d.Charge.PyItem.IsValid ? d.Charge.TypeName : String.Empty,
                        ChargeQty = d.CurrentCharges,
                        MaxCharges = d.MaxCharges,
                        ChargeGroupId = d.Charge != null && d.Charge.PyItem.IsValid ? d.Charge.GroupId : 0,
                        ChargeCategoryId = d.Charge != null && d.Charge.PyItem.IsValid ? d.Charge.CategoryId : 0,
                    }).ToList();
                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = resList;
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

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button)
                        button.Enabled = enabled;
            }));
        }

        private void printAllItemAttributesToolStripMenuItem_Click(object sender, EventArgs e)
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

                    Int64 itemId = 0;
                    try
                    {
                        var index = dataGridView1.SelectedCells[0].OwningRow.Index;
                        DataGridViewRow selectedRow = dataGridView1.Rows[index];
                        itemId = Convert.ToInt64(selectedRow.Cells["ItemId"].Value);

                        var module = ESCache.Instance.Modules.FirstOrDefault(m => m.ItemId.Equals(itemId));
                        foreach (var attribute in module.Attributes.GetAttributes())
                        {
                            Log.WriteLine($"Key: {attribute.Key} Value: {attribute.Value}");
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine(exception.ToString());
                        throw;
                    }

                    ModifyButtons(true);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
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

                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdReloadAmmo);
                    ModifyButtons(true);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        #endregion Methods

        private void clickToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = dataGridView1.SelectedCells[0].OwningRow.Index;
            DataGridViewRow selectedRow = dataGridView1.Rows[index];
            var itemId = Convert.ToInt64(selectedRow.Cells["ItemId"].Value);

            ActionQueueAction.Run(() =>
            {
                var module = ESCache.Instance.Modules.FirstOrDefault(m => m.ItemId.Equals(itemId));
                if (module != null)
                {
                    Log.WriteLine($"Executed module click. Module {module.TypeName} {module.TypeId}");
                    module.Click();
                }
            });
        }
    }
}