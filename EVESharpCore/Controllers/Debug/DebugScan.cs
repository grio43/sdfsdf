extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.Extensions;
using System;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugScan : Form
    {
        #region Fields

        private ActionQueueAction _autoProbeAction = null;
        private string _currentSig;
        private bool _onlyLoadPScanResults;

        #endregion Fields

        #region Constructors

        public DebugScan()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Properties



        #endregion Properties

        #region Methods

        private void button1_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            DateTime waitUntil = DateTime.MinValue;
            bool executedScan = false;
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
                    
                    var mapViewWindow = ESCache.Instance.DirectEve.DirectMapViewWindow;

                    if (mapViewWindow == null)
                    {
                        action.QueueAction();
                        return;
                    }

                    if (executedScan)
                    {
                        var list = mapViewWindow.DirectionalScanResults.ToList();
                        Log.WriteLine($"Scan executed. Loading {list.Count} results to datagridview.");
                        var resList = list.Select(d => new { d.TypeId, d.GroupId, d.TypeName, d.Distance, d.Name, }).ToList();
                        Task.Run(() =>
                        {
                            return Invoke(new Action(() =>
                            {
                                dataGridView1.DataSource = resList;
                                ModifyButtons(true);
                            }));
                        });
                    }
                    else
                    {
                        if (!mapViewWindow.IsDirectionalScanning())
                        {
                            Log.WriteLine("Executed directional scan.");
                            mapViewWindow.DirectionalScan();
                            executedScan = true;
                            action.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }
                        else
                        {
                            action.QueueAction();
                            return;
                        }
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

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            DateTime waitUntil = DateTime.MinValue;
            bool executedScan = false;
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

                    var mapViewWindow = ESCache.Instance.DirectEve.DirectMapViewWindow;

                    if (mapViewWindow == null)
                    {
                        action.QueueAction();
                        return;
                    }

                    if ((executedScan || _onlyLoadPScanResults) && !mapViewWindow.IsProbeScanning())
                    {
                        Log.WriteLine($"Probe scan finished.");
                        var list = mapViewWindow.SystemScanResults.ToList();
                        Log.WriteLine($"Scan executed. Loading {list.Count} results to datagridview.");
                        var resList = list.Select(d => new { d.Id, d.Deviation, d.SignalStrength, d.PreviousSignalStrength, d.ScanGroup, d.TypeName, d.GroupName, d.IsPointResult, d.IsSphereResult, d.MultiPointResult, PosVector3 = d.Pos.ToString(), DataVector3 = d.Data.ToString() }).ToList().OrderBy(p => p.SignalStrength).ToList();
                        Task.Run(() =>
                        {
                            return Invoke(new Action(() =>
                            {
                                dataGridView1.DataSource = resList;
                                ModifyButtons(true);
                                _onlyLoadPScanResults = false;
                            }));
                        });
                    }
                    else
                    {
                        if (!mapViewWindow.IsProbeScanning())
                        {
                            Log.WriteLine("Executed probe scan.");
                            mapViewWindow.ProbeScan();
                            executedScan = true;
                            action.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(1);
                            return;
                        }
                        else
                        {
                            action.QueueAction();
                            return;
                        }
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

        private void button3_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            DateTime waitUntil = DateTime.MinValue;
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

                    var mapViewWindow = ESCache.Instance.DirectEve.DirectMapViewWindow;

                    if (mapViewWindow == null)
                    {
                        action.QueueAction();
                        return;
                    }

                    var resList = mapViewWindow.GetProbes().Select(d => new { d.ProbeId, PosVector3 = d.Pos.ToString(), DestVector3 = d.DestinationPos.ToString(), d.Expiry, d.RangeAu }).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView2.DataSource = resList;
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

        private void button4_Click(object sender, EventArgs e)
        {
            _onlyLoadPScanResults = true;
            button2.PerformClick();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            DateTime waitUntil = DateTime.MinValue;
            int _minRangeProbeScanAttempts = 0;
            _autoProbeAction = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (_autoProbeAction == null)
                    {
                        ModifyButtons(true);
                        Log.WriteLine("AutoProbeAction finished.");
                        return;
                    }

                    if (waitUntil > DateTime.UtcNow)
                    {
                        _autoProbeAction.QueueAction();
                        return;
                    }

                    if (ESCache.Instance.InDockableLocation)
                    {
                        Log.WriteLine("That doesn't work in stations.");
                        return;
                    }

                    if (ESCache.Instance.InWarp)
                    {
                        Log.WriteLine("Waiting, in warp.");
                        _autoProbeAction.QueueAction();
                        return;
                    }

                    var mapViewWindow = ESCache.Instance.DirectEve.DirectMapViewWindow;

                    if (mapViewWindow == null)
                    {
                        _autoProbeAction.QueueAction();
                        return;
                    }

                    if (!mapViewWindow.GetProbes().Any())
                    {
                        Log.WriteLine("No probes found in space, launching probes.");
                        var probeLauncher = ESCache.Instance.Modules.FirstOrDefault(m => m.GroupId == 481);
                        if (probeLauncher == null)
                        {
                            Log.WriteLine("No probe launcher found.");
                            return;
                        }

                        if (probeLauncher.IsInLimboState || probeLauncher.IsActive)
                        {
                            Log.WriteLine("Probe launcher is active or reloading.");
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }

                        if (ESCache.Instance.CurrentShipsCargo == null)
                            return;

                        if (ESCache.Instance.CurrentShipsCargo.CanBeStacked)
                        {
                            Log.WriteLine("Stacking current ship hangar.");
                            ESCache.Instance.CurrentShipsCargo.StackAll();
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }

                        // TODO: decloak

                        if (probeLauncher.Charge == null || probeLauncher.CurrentCharges < 8)
                        {
                            var probes = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == 479).OrderBy(i => i.Stacksize);
                            var coreProbes = probes.Where(i => i.TypeName.Contains("Core")).OrderBy(i => i.Stacksize);
                            var combatProbes = probes.Where(i => i.TypeName.Contains("Combat")).OrderBy(i => i.Stacksize);
                            var charge = probes.FirstOrDefault();
                            if (charge == null)
                            {
                                Log.WriteLine("No core probes found in cargohold.");
                                return;
                            }

                            if (charge.Stacksize < 8)
                            {
                                Log.WriteLine("Probe stacksize was smaller than 8.");
                                return;
                            }

                            probeLauncher.ChangeAmmo(charge);
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(11);
                            return;
                        }

                        Log.WriteLine("Launching probes.");
                        probeLauncher.Click();

                        _autoProbeAction.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }

                    if (mapViewWindow.GetProbes().Count != 8)
                    {
                        //TODO: check probe range, can't be retrieved if below CONST.MIN_PROBE_RECOVER_DISTANCE
                        Log.WriteLine("Probe amount is != 8, recovering probes.");
                        _autoProbeAction.QueueAction();
                        mapViewWindow.RecoverProbes();
                        waitUntil = DateTime.UtcNow.AddSeconds(2);
                        return;
                    }

                    // TODO: use cloaks

                    if (mapViewWindow.IsProbeScanning())
                    {
                        Log.WriteLine("Probe scan active, waiting.");
                        _autoProbeAction.QueueAction();
                        waitUntil = DateTime.UtcNow.AddSeconds(1);
                        return;
                    }

                    var list = mapViewWindow.SystemScanResults.ToList();
                    Log.WriteLine($"Loading {list.Count} results to datagridview.");
                    var resList = list.Select(d => new { d.Id, d.Deviation, d.SignalStrength, d.PreviousSignalStrength, d.ScanGroup, d.TypeName, d.GroupName, d.IsPointResult, d.IsSphereResult, d.MultiPointResult, PosVector3 = d.Pos.ToString(), DataVector3 = d.Data.ToString() }).ToList().OrderBy(r => r.Id != _currentSig).ToList();
                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView1.DataSource = resList;
                            _onlyLoadPScanResults = false;
                        }));
                    });

                    DirectSystemScanResult result = null;
                    if (textBox_targetSIG.Text.Equals(string.Empty))
                    {
                        result = mapViewWindow.SystemScanResults.FirstOrDefault(r => r.ScanGroup == ScanGroup.FilamentTrace && r.SignalStrength < 1);
                    }
                    else
                    {
                        result = mapViewWindow.SystemScanResults.FirstOrDefault(r => r.ScanGroup == ScanGroup.FilamentTrace && r.SignalStrength < 1 && r.Id.Equals(textBox_targetSIG.Text));
                    }

                    if (result != null)
                    {
                        if (result.Id != _currentSig)
                        {
                            Log.WriteLine("First sig or sig has changed, setting probe range to max.");
                            mapViewWindow.SetMaxProbeRange();
                            mapViewWindow.MoveProbesTo(result.Pos);
                            mapViewWindow.ProbeScan();
                            _currentSig = result.Id;
                            _autoProbeAction.QueueAction();
                            waitUntil = DateTime.UtcNow.AddSeconds(2);
                            return;
                        }
                        else
                        {
                            if (mapViewWindow.IsAnyProbeAtMinRange)
                            {
                                if (_minRangeProbeScanAttempts < 6 && result.SignalStrength < 1)
                                {
                                    _minRangeProbeScanAttempts++;
                                    Log.WriteLine($"Probe reached minimum range, but signal strength < 1. Trying again. Attempt [{_minRangeProbeScanAttempts}]");
                                    mapViewWindow.RefreshUI();
                                    mapViewWindow.MoveProbesTo(result.Pos);
                                    mapViewWindow.ProbeScan();
                                    _currentSig = result.Id;
                                    waitUntil = DateTime.UtcNow.AddSeconds(4);
                                    _autoProbeAction.QueueAction();
                                    return;
                                }
                                else
                                {

                                    Log.WriteLine("Probes reached minimum range or signal was found. Finished.");
                                    mapViewWindow.SetMaxProbeRange();
                                    mapViewWindow.MoveProbesTo(result.Pos);
                                    mapViewWindow.ProbeScan();
                                    _currentSig = result.Id;
                                    _autoProbeAction.QueueAction();
                                    waitUntil = DateTime.UtcNow.AddSeconds(2);
                                    _minRangeProbeScanAttempts = 0;
                                    return;
                                }
                            }
                            else
                            {

                                Log.WriteLine("Decreasing probe range and initiating scan again.");
                                mapViewWindow.DecreaseProbeRange();
                                _currentSig = result.Id;
                                mapViewWindow.MoveProbesTo(result.Pos);
                                mapViewWindow.ProbeScan();
                                waitUntil = DateTime.UtcNow.AddSeconds(2);
                                _autoProbeAction.QueueAction();
                                return;
                            }

                        }
                    }
                    else
                    {
                        Log.WriteLine("No results found or finished auto probing.");
                    }

                    ModifyButtons(true);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));

            _autoProbeAction.Initialize().QueueAction();
            _currentSig = null;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            _autoProbeAction = null;
            if (ControllerManager.Instance.TryGetController<ActionQueueController>(out var ac))
                ac.RemoveAllActions();
            ModifyButtons(true);
        }

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button && !((Button)b).Text.Equals("Cancel"))
                    {
                        button.Enabled = enabled;
                    }
            }));
        }

        #endregion Methods

        private void setAutoprobeTargetSIGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var index = dataGridView1.SelectedCells[0].OwningRow.Index;
            DataGridViewRow selectedRow = dataGridView1.Rows[index];
            var sig = selectedRow.Cells["Id"].Value.ToString();
            textBox_targetSIG.Text = sig;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox_targetSIG.Text = string.Empty;
        }
    }
}