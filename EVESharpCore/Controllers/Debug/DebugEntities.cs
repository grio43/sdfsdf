extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugEntities : Form
    {
        #region Constructors

        public DebugEntities()
        {
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void button1_Click(object sender, EventArgs e)
        {
            if (ControllerManager.Instance.TryGetController<ActionQueueController>(out var c))
            {
                c.RemoveAllActions();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;

            Type dgvType = dataGridView2.GetType();
            PropertyInfo pi = dgvType.GetProperty("DoubleBuffered",
                BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dataGridView2, true, null);

            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var ent = ESCache.Instance.DirectEve.Entities;
                    var res = ent.Select(n => new
                    {
                        n.Name,
                        n.TypeName,
                        n.BracketType,
                        n.AngularVelocity,
                        n.TransversalVelocity,
                        BracketName = n.GetBracketName(),
                        BracketTexturePath = n.GetBracketTexturePath(),
                        n.Id,
                        n.OwnerId,
                        n.GroupId,
                        n.TypeId,
                        n.AbyssalTargetPriority,
                        n.CategoryId,
                        n.IsEwarImmune,
                        n.HasReleased,
                        n.HasExploded,
                        n.Velocity,
                        n.Mode,
                        n.DroneState,
                        n.FollowId,
                        n.IsNpc,
                        n.Radius,
                        n.SignatureRadius,
                        n.BallRadius,
                        n.RadiusOverride,
                        n.SlimSignatureRadius,
                        n.MaxRange,
                        n.OptimalRange,
                        n.AccuracyFalloff,
                        n.Distance,
                        DistMeWorldPos = ESCache.Instance.DirectEve.ActiveShip.Entity.WorldPos.Value.Distance(n.WorldPos.Value),
                        DistMeXYZPos = ESCache.Instance.DirectEve.ActiveShip.Entity.Position.Distance(n.Position),
                        //DistDiff = ESCache.Instance.DirectEve.ActiveShip.Entity.WorldPos.Value.Distance(n.WorldPos.Value) - ESCache.Instance.DirectEve.ActiveShip.Entity.Position.Distance(n.Position),
                        n.IsTarget,
                        n.IsTargetedBy,
                        n.IsAttacking,
                        YellowBoxing = n.IsTargetedBy && !n.IsAttacking,
                        n.IsWarpScramblingMe,
                        n.IsJammingMe,
                        n.IsTryingToJamMe,
                        n.IsNeutralizingMe,
                        n.GigaJouleNeutedPerSecond,
                        n.IsRemoteRepairEntity,
                        n.IsTargetPaintingMe,
                        n.IsSensorDampeningMe,
                        n.IsWebbingMe,
                        n.TotalShield,
                        n.ShieldPct,
                        n.CurrentShield,
                        LocalReps = n.FlatShieldArmorLocalRepairAmountCombined,
                        IsCurrentWeaponTarget = n == ESCache.Instance.Combat.CurrentWeaponTarget?.DirectEntity,
                        BestDamageType = n.BestDamageTypes.FirstOrDefault(),
                        //new EntityCache(n).IsVunlnerableAgainstCurrentDamageType,
                        ShieldRes_EM_EXP_KIN_TRM = $"[{n.ShieldResistanceEM}] [{n.ShieldResistanceExplosion}] [{n.ShieldResistanceKinetic}] [{n.ShieldResistanceThermal}]",
                        n.TotalArmor,
                        n.ArmorPct,
                        n.CurrentArmor,
                        ArmorRes_EM_EXP_KIN_TRM = $"[{n.ArmorResistanceEM}] [{n.ArmorResistanceExplosion}] [{n.ArmorResistanceKinetic}] [{n.ArmorResistanceThermal}]",
                        StructRes_EM_EXP_KIN_TRM = $"[{n.StructureResistanceEM}] [{n.StructureResistanceExplosion}] [{n.StructureResistanceKinetic}] [{n.StructureResistanceThermal}]",
                        n.TotalStructure,
                        n.StructurePct,
                        n.CurrentStructure,
                        n.EmEHP,
                        n.ExpEHP,
                        n.KinEHP,
                        n.TrmEHP,
                        n.GotoX,
                        n.GotoY,
                        n.GotoZ,
                        WarpDestEntName = n?.WarpDestinationEntity?.Name,
                        n.ScreenPos,
                        n.X,
                        n.Y,
                        n.Z,
                        n.BallPos,
                        //TestPos = (n.BallPos + new Vec3(-n.ModelBoundingSphereCenterX ?? 0.0, -n.ModelBoundingSphereCenterY ?? 0.0, -n.ModelBoundingSphereCenterZ ?? 0.0)),
                        n.WorldPos,
                        n.ModelBoundingSphereCenterX,
                        n.ModelBoundingSphereCenterY,
                        n.ModelBoundingSphereCenterZ,
                        n.ModelBoundingSphereRadius,
                        n.ModelScale,
                        n.MiniBallAmount,
                        n.MiniBoxesAmount,
                        n.MiniCapsulesAmount,
                        n.HasAnyNonTraversableColliders,
                        MAX_DPS = n.GetMaxDPSFrom().ToString(),
                        CURR_DPS = n.GetCurrentDPSFrom().ToString(),
                    }).OrderBy(n => n.Distance).ToList();

                    Task.Run(() =>
                    {
                        return Invoke(new Action(() =>
                        {
                            dataGridView2.DataSource = Util.ConvertToDataTable(res);
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

        private void copyIdToClipboardToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                Thread thread = new Thread(() => Clipboard.SetText(id.ToString()));
                thread.SetApartmentState(ApartmentState.STA); //Set the thread to STA
                thread.Start();
                thread.Join();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
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

        private void monitorEntityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new MonitorPyObjectAction(() =>
                {
                    return ESCache.Instance.DirectEve.EntitiesById[id].Ball;

                    //}, new List<string>() { }).Initialize().QueueAction();
                }, new List<string>() { "__dict__", "__iroot__", "modelLoadSignal" }).Initialize().QueueAction();
                new MonitorEntityAction(() =>
                {
                    return ESCache.Instance.DirectEve.EntitiesById[id];
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        #endregion Methods


        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowInBlueViewer();
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void uNLOADCOLLISIONINFOToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowDestinyBalls(0);
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void sHOWCOLLISIONDATAToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowDestinyBalls(1);
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void sHOWDESTINYBALLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowDestinyBalls(2);
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void sHOWMODELSPHEREToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowDestinyBalls(3);
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void sHOWBOUNDINGSPHEREToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].ShowDestinyBalls(4);
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        private void dRAWMINIBALLSToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.EntitiesById[id].DrawBalls();
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void rEMOVEALLDRAWNOBJECTSToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                    ESCache.Instance.DirectEve.SceneManager.RemoveAllDrawnObjects();
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }

        }

        private void dRAWMINIBOXESToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    //ESCache.Instance.DirectEve.EntitiesById[id].DrawBoxes();
                    ESCache.Instance.DirectEve.EntitiesById[id].DrawBoxesWithLines();
                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void pRINTDMGEFFECTSToolStripMenuItem_Click(object sender, EventArgs e)
        {

            try
            {
                var id = (long)dataGridView2.SelectedRows[0].Cells["Id"].Value;
                new ActionQueueAction(() =>
                {
                    var typeId = ESCache.Instance.DirectEve.EntitiesById[id].TypeId;

                    var invtype = ESCache.Instance.DirectEve.GetInvType(typeId);

                    foreach (var x in invtype.GetDmgEffectsByGuid())
                    {
                        Logging.Log.WriteLine($"Key {x.Key} EffectName {x.Value.EffectName} Displayname {x.Value.DisplayName} EffectID {x.Value.EffectID}");
                    }

                }).Initialize().QueueAction();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }


        }
    }
}