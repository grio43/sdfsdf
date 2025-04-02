extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.Extensions;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugMutaplasmid : Form
    {

        protected class MutaplasmidFilter
        {


            public int AttributeId { get; set; }

            public string Name { get; set; }

            public float Min { get; set; }

            public float Max { get; set; }

            public float ThresholdMin { get; set; }
            public float ThresholdMax { get; set; }

        }

        #region Constructors

        public DebugMutaplasmid()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is System.Windows.Forms.Button button)
                        button.Enabled = enabled;
            }));
        }

        #endregion Methods


        private void button3_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {

                    var hangar = ESCache.Instance.DirectEve.GetItemHangar();

                    if (hangar == null)
                        return;

                    var mutaplasmids = hangar.Items.Where(i => i.IsDynamicItem).ToList().DistinctBy(i => i.TypeId).ToList();

                    //Logging.Log.WriteLine($"Size {mutaplasmids.Count}");

                    var list = mutaplasmids.Select(e => new { e.TypeName, e.TypeId, OrigName = e.OrignalDynamicItem.TypeName }).ToList();

                    this.Invoke(new Action(() =>
                    {
                        comboBox1.Items.Clear();

                        foreach (var item in list)
                        {
                            comboBox1.Items.Add(item.OrigName);
                        }

                    }));

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
                finally
                {
                    ModifyButtons(true);
                }

            }));
            action.Initialize().QueueAction();
        }

        private List<MutaplasmidFilter> _filters = new List<MutaplasmidFilter>();

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            var selectedItem = comboBox1.SelectedItem as String;

            if (selectedItem == null)
                return;

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {

                    var hangar = ESCache.Instance.DirectEve.GetItemHangar();

                    if (hangar == null)
                        return;

                    var modifiedItem = hangar.Items.Where(i => i.IsDynamicItem && i.OrignalDynamicItem.TypeName == selectedItem.ToString()).FirstOrDefault();

                    if (modifiedItem == null)
                        return;

                    var mutatorTypeId = modifiedItem.DynamicItem["mutatorTypeID"].ToInt();

                    if (mutatorTypeId <= 0)
                        return;

                    var mutators = ESCache.Instance.DirectEve.DirectStaticDataLoader.DynamicItemAttributeMutators[mutatorTypeId];


                    List<MutaplasmidFilter> filters = new List<MutaplasmidFilter>();

                    _filters = filters;

                    float calculateValue(float attributeValue, float min, float max, float modifier)
                    {
                        return attributeValue * (modifier * (max - min) + min);
                    }

                    foreach (var mut in mutators.Attributes)
                    {
                        var val = ESCache.Instance.DirectEve.GetTypeAttribute<float>(modifiedItem.OrignalDynamicItem.TypeId, mut.Key); // absolute value of the base
                        var min = calculateValue(val, mut.Value.Min, mut.Value.Max, 0f);
                        var max = calculateValue(val, mut.Value.Min, mut.Value.Max, 1.0f);

                        var filter = new MutaplasmidFilter();
                        filter.AttributeId = mut.Key;
                        filter.Name = ESCache.Instance.DirectEve.GetAttributeDisplayName(mut.Key);
                        filter.Min = min;
                        filter.Max = max;
                        filter.ThresholdMin = min;
                        filter.ThresholdMax = max;
                        filters.Add(filter);
                    }

                    this.Invoke(new Action(() =>
                    {
                        dataGridView1.DataSource = filters;
                    }));

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
                finally
                {
                    ModifyButtons(true);
                }

            }));
            action.Initialize().QueueAction();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var selectedItem = comboBox1.SelectedItem as String;

            if (selectedItem == null)
                return;

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {

                    var hangar = ESCache.Instance.DirectEve.GetItemHangar();

                    if (hangar == null)
                        return;

                    var modifiedItems = hangar.Items.Where(i => i.IsDynamicItem && i.OrignalDynamicItem.TypeName == selectedItem.ToString()).ToList();

                    if (!modifiedItems.Any())
                        return;

                    List<DirectItem> results = new List<DirectItem>();

                    foreach (var item in modifiedItems)
                    {
                        var isValid = true;

                        foreach (var filter in _filters)
                        {
                            var itemAttributeValue = ESCache.Instance.DirectEve.GetDynamicItemAttribute<float>(item.ItemId, filter.AttributeId);
                            if (itemAttributeValue < filter.ThresholdMin || itemAttributeValue > filter.ThresholdMax)
                            {
                                isValid = false;
                                break;
                            }
                        }

                        if (!isValid)
                            continue;

                        results.Add(item);
                        //Logging.Log.WriteLine($"Item TypeName {item.TypeName} TypeId {item.TypeId}");
                    }

                    var selectedComboTwo = this.comboBox2.GetItemText(this.comboBox2.SelectedItem);
                    //Logging.Log.WriteLine($"selectedComboTwo {selectedComboTwo}");
                    if (!String.IsNullOrEmpty(selectedComboTwo))
                    {
                        var conts = DirectContainer.GetStationContainers(ESCache.Instance.DirectEve);

                        foreach (var cont in conts)
                        {
                            var item = hangar.Items.FirstOrDefault(e => e.ItemId == cont.ItemId);

                            if (item == null)
                                continue;

                            if (item.GivenName == selectedComboTwo)
                            {
                                cont.Add(results);
                                break;
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
                finally
                {
                    ModifyButtons(true);
                }

            }));
            action.Initialize().QueueAction();
        }

        private void button2_Click(object sender, EventArgs e)
        {

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    this.Invoke(new Action(() =>
                    {
                        comboBox2.Items.Clear();
                    }));

                    var hangar = ESCache.Instance.DirectEve.GetItemHangar();

                    if (hangar == null)
                        return;


                    var conts = DirectContainer.GetStationContainers(ESCache.Instance.DirectEve);

                    foreach (var cont in conts)
                    {
                        var item = hangar.Items.FirstOrDefault(e => e.ItemId == cont.ItemId);

                        if (item == null)
                            continue;

                        this.Invoke(new Action(() =>
                        {
                            comboBox2.Items.Add(item.GivenName);
                        }));
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex}");
                }
                finally
                {
                    ModifyButtons(true);
                }

            }));
            action.Initialize().QueueAction();
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}