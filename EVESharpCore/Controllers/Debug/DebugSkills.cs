using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugSkills : Form
    {
        #region Constructors

        public DebugSkills()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void addSkillToEndOfQueueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dgv = ActiveControl as DataGridView;
            if (dgv == null) return;

            var typeId = int.Parse(dgv.SelectedCells[0].OwningRow.Cells[0].Value.ToString());
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        ESCache.Instance.DirectEve.Skills.AddSkillToEndOfQueue(typeId);
                        Invoke(new Action(() => { ModifyButtons(true); }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void addSkillToFrontOfQueueToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dgv = ActiveControl as DataGridView;
            if (dgv == null) return;

            var typeId = int.Parse(dgv.SelectedCells[0].OwningRow.Cells[0].Value.ToString());
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        ESCache.Instance.DirectEve.Skills.AddSkillToFrontOfQueue(typeId);
                        Invoke(new Action(() => { ModifyButtons(true); }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ModifyButtons();

            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = ESCache.Instance.DirectEve.Skills.MySkills.ToList()
                            .Select(d => new { d.TypeId, Typename = GetTypeNameForTypeId(d.TypeId), d.Level, d.MaxCloneSkillLevel, d.SkillRank, d.InTraining, d.SkillPoints })
                            .ToList();

                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = ESCache.Instance.DirectEve.Skills.AllSkills.ToList()
                            .Select(d => new { d.TypeId, Typename = GetTypeNameForTypeId(d.TypeId) })
                            .ToList();

                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = ESCache.Instance.DirectEve.Skills.MySkillQueue.ToList()
                            .Select(d => new { d.TypeId, Typename = GetTypeNameForTypeId(d.TypeId), d.Level, d.TrainingStartSP, d.TrainingDestinationSP, d.QueuePosition, d.TrainingStartTime, d.TrainingEndTime, d.TrainingToLevel })
                            .ToList();

                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = new List<TimeSpan>() { ESCache.Instance.DirectEve.Skills.SkillQueueLength };
                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = new List<TimeSpan>() { ESCache.Instance.DirectEve.Skills.MaxQueueLength };
                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void excludeMySkillsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dgv = ActiveControl as DataGridView;
            if (dgv == null) return;

            var typeId = int.Parse(dgv.SelectedCells[0].OwningRow.Cells[0].Value.ToString());
            ShowRequirements(typeId);
        }

        private String GetTypeNameForTypeId(int typeId)
        {
            return ESCache.Instance.DirectEve.GetInvType(typeId).TypeName;
        }

        private void includeMySkillsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var dgv = ActiveControl as DataGridView;
            if (dgv == null) return;

            var typeId = int.Parse(dgv.SelectedCells[0].OwningRow.Cells[0].Value.ToString());
            ShowRequirements(typeId, false);
        }

        private void ModifyButtons(bool enabled = false)
        {
            foreach (var b in Controls)
                if (b is Button)
                    ((Button)b).Enabled = enabled;
        }

        private void ShowRequirements(int typeId, bool excludeMySkills = true)
        {
            ModifyButtons();
            new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        var list = ESCache.Instance.DirectEve.Skills.GetRequiredSkillsForType(typeId, excludeMySkills)
                            .ToList()
                            .Select(d => new { TypeId = d.Item1, Typename = GetTypeNameForTypeId(d.Item1), Level = d.Item2 })
                            .ToList();

                        Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = list;
                            ModifyButtons(true);
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        #endregion Methods
    }
}