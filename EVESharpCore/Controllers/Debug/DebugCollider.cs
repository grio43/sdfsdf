extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Framework.Lookup;
using ServiceStack;
using System.Numerics;
using SC::SharedComponents.Utility;
using System.Collections.Generic;
using ServiceStack.Text;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugCollider : Form
    {
        #region Constructors

        public DebugCollider()
        {
            InitializeComponent();
        }

        #endregion Constructors

        private DirectEve Framework => ESCache.Instance.DirectEve;

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    var ship = ESCache.Instance.DirectEve.ActiveShip.Entity;
                    var closestColliderEntity = Framework.Entities.Where(e => e.HasAnyNonTraversableColliders).OrderBy(e => e.Distance).FirstOrDefault();

                    if (closestColliderEntity == null)
                        return;

                    closestColliderEntity.ShowDestinyBalls(1);

                    //closestStation.DrawBalls();
                    //closestStation.DisplayAllBallAxes();
                    //closestStation.ShowBallEdges();

                    var shipDirection = ship.GetDirectionVectorFinal().Normalize();
                    var inDirectionFromCurrentShip = ship.DirectAbsolutePosition.GetVector() + shipDirection.Scale(250_000);


                    var capsules = closestColliderEntity.MiniCapsules;
                    var spheres = closestColliderEntity.MiniBalls;
                    var rectangles = closestColliderEntity.MiniBoxes;

                    Logging.Log.WriteLine($" Name {closestColliderEntity.Name} Id {closestColliderEntity.Id} NumCapsules [{capsules.Count}] NumSpheres [{spheres.Count}] NumRects [{rectangles.Count}]");
                    //closestStation.DrawBoxesWithLines();

                    // We need to bring the start and end vector to the same coordinate system of the station
                    var start = ship.DirectAbsolutePosition.GetVector();
                    var end = closestColliderEntity.DirectAbsolutePosition.GetVector();
                    var dir = (end - start).Normalize();

                    var randomVectors = DirectSceneManager.FibonacciSphereWithinCone(samples: 3000, coneAngle: 20);
                    var q = DirectSceneManager.GetRotationQuaternion(new Vec3(0, 0, 1), dir);
                    foreach (var r in randomVectors)
                    {
                        var randomDir = q * r;
                        Logging.Log.WriteLine($"{randomDir}");
                        var IsFree = DirectRayCasting.IsLineOfSightFree(start, start + Vec3.Scale(randomDir, 500_000), ignoreSentryGuns: true);
                        var color = IsFree.Item1 ? new Vector4(0, 255, 0, 255) : new Vector4(255, 0, 0, 255);
                        if (!IsFree.Item1 || true)
                        {
                            ESCache.Instance.DirectEve.SceneManager.DrawLineGradient(new Vec3(0, 0, 0), randomDir.Scale(500_000), color, color);
                        }
                    }

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                finally
                {
                    ModifyButtons(true);
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

        private void button1_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                var cols = ESCache.Instance.DirectEve.Entities.Where(e => e.HasAnyNonTraversableColliders);
                foreach (var col in cols)
                {
                    col.ShowDestinyBalls(0);
                }

                ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();
                ESCache.Instance.DirectEve.SceneManager.RemoveAllDrawnObjects();

            }).Initialize().QueueAction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                var camDir = ESCache.Instance.DirectEve.SceneManager.CameraDirection;
                ESCache.Instance.DirectEve.SceneManager.DrawLine(new Vec3(0, 0, 0), camDir.Scale(500_000));

            }).Initialize().QueueAction();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int.TryParse(textBox2.Text, out var dist);
            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;
                var shipDirection = ship.GetDirectionVectorFinal().Normalize();
                var camDir = Framework.SceneManager.CameraDirection;
                var start = ship.DirectAbsolutePosition.GetVector();
                var end = start + camDir.Scale(dist);

                DirectEntity.MoveToViaAStar(disableMoving: true, forceRecreatePath: true, dest: new DirectWorldPosition(end), destinationEntity: null, stepSize: 5000);

            }).Initialize().QueueAction();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    var ship = ESCache.Instance.DirectEve.ActiveShip.Entity;
                    var closestColliderEntity = Framework.Entities.Where(e => e.HasAnyNonTraversableColliders).OrderBy(e => e.Distance).FirstOrDefault();

                    if (closestColliderEntity == null)
                        return;

                    closestColliderEntity.ShowDestinyBalls(1);
                    //closestStation.DrawBalls();
                    closestColliderEntity.DisplayAllBallAxes();
                    closestColliderEntity.ShowBallEdges();

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                finally
                {
                    ModifyButtons(true);
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    var ship = ESCache.Instance.DirectEve.ActiveShip.Entity;
                    var closestColliderEntity = Framework.Entities.Where(e => e.HasAnyNonTraversableColliders).OrderBy(e => e.Distance).FirstOrDefault();

                    if (closestColliderEntity == null)
                        return;

                    //closestColliderEntity.ShowDestinyBalls(1);
                    //closestStation.DrawBalls();
                    //closestColliderEntity.DisplayAllBallAxes();
                    //closestColliderEntity.ShowBallEdges();
                    closestColliderEntity.DrawBoxesWithLines();

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                finally
                {
                    ModifyButtons(true);
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button7_Click(object sender, EventArgs e)
        {

            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    //var ship = ESCache.Instance.DirectEve.ActiveShip.Entity;
                    var cols = ESCache.Instance.DirectEve.Entities.Where(e => e.HasAnyNonTraversableColliders);
                    foreach (var col in cols)
                    {
                        col.ShowDestinyBalls(1);
                    }

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                finally
                {
                    ModifyButtons(true);
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button8_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;

                var closestColliderEntity = Framework.Entities.Where(e => e.HasAnyNonTraversableColliders).OrderBy(e => e.Distance).FirstOrDefault();

                if (closestColliderEntity == null)
                    return;

                closestColliderEntity.ShowDestinyBalls(1);

                var colliders = closestColliderEntity.GetAllColliders;

                var center = colliders.First().Center;
                var entPos = closestColliderEntity.DirectAbsolutePosition;
                var end = entPos.GetVector() + center;

                DirectEntity.MoveToViaAStar(disableMoving: true, forceRecreatePath: true, dest: new DirectWorldPosition(end), destinationEntity: null, stepSize: 5000);

            }).Initialize().QueueAction();
        }

        private void button9_Click(object sender, EventArgs e)
        {
            bool ignoreAbyssEntities = checkBox1.Checked;
            bool ignoreTrackingPolyons = checkBox2.Checked;
            bool ignoreAutomataPylon = checkBox3.Checked;
            bool ignoreWideAreaAutomataPylon = checkBox4.Checked;
            bool ignoreFilaCouds = checkBox5.Checked;
            bool ignoreBioClouds = checkBox6.Checked;
            bool ignoreTachClouds = checkBox7.Checked;

            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;

                if (ship == null)
                    return;

                var colEnts = DirectRayCasting.IsLineOfSightFree(ship.DirectAbsolutePosition.GetVector(), ship.DirectAbsolutePosition.GetVector(), ignoreAbyssEntities: ignoreAbyssEntities, ignoreTrackingPolyons: ignoreTrackingPolyons, ignoreAutomataPylon: ignoreAutomataPylon, ignoreWideAreaAutomataPylon: ignoreWideAreaAutomataPylon, ignoreFilaCouds: ignoreFilaCouds, ignoreBioClouds: ignoreBioClouds, ignoreTachClouds: ignoreTachClouds, debugLog: true);

                foreach (var vp in colEnts.Item2)
                {
                    // print all colliders TypeNames
                    Console.WriteLine($"[{vp.Key.TypeName}] ColliderCnt [{vp.Value.Count}] ColliderTypes [{string.Join(",", vp.Value.Select(c => c.GetType().Name + " (" + c.MaxBoundingRadius + ")"))}]");
                }

            }).Initialize().QueueAction();
        }

        private void button10_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                var names = Framework.SceneManager.GetCustomDrawnObjectNames();
                int i = 0;
                foreach (var name in names)
                {
                    i++;
                    Console.WriteLine($"[{i}] [{name}]");
                }

                if (names.Count == 0)
                {
                    Console.WriteLine("No custom drawn objects found");
                }

            }).Initialize().QueueAction();
        }

        private void button11_Click(object sender, EventArgs e)
        {
            bool ignoreAbyssEntities = checkBox1.Checked;
            bool ignoreTrackingPolyons = checkBox2.Checked;
            bool ignoreAutomataPylon = checkBox3.Checked;
            bool ignoreWideAreaAutomataPylon = checkBox4.Checked;
            bool ignoreFilaCouds = checkBox5.Checked;
            bool ignoreBioClouds = checkBox6.Checked;
            bool ignoreTachClouds = checkBox7.Checked;

            new ActionQueueAction(() =>
            {
                DirectEntity _midGate = Framework.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);
                DirectEntity _endGate = Framework.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);
                DirectEntity _nextGate = _midGate ?? _endGate;

                if (_nextGate != null)
                {
                    var res = DirectRayCasting.IsLineOfSightFree(_nextGate.DirectAbsolutePosition.GetVector(), _nextGate.DirectAbsolutePosition.GetVector(), ignoreAbyssEntities: ignoreAbyssEntities, ignoreTrackingPolyons: ignoreTrackingPolyons, ignoreAutomataPylon: ignoreAutomataPylon, ignoreWideAreaAutomataPylon: ignoreWideAreaAutomataPylon, ignoreFilaCouds: ignoreFilaCouds, ignoreBioClouds: ignoreBioClouds, ignoreTachClouds: ignoreTachClouds);
                    foreach (var vp in res.Item2)
                    {
                        Console.WriteLine($"[{vp.Key.TypeName}] ColliderCnt [{vp.Value.Count}] ColliderTypes [{string.Join(",", vp.Value.Select(c => c.GetType().Name + " (" + (int)c.MaxBoundingRadius + ")," + "(" + c.Traversable + "),(" + c.Center.ToString() + "),[(" + (new DirectWorldPosition(_nextGate.DirectAbsolutePosition.GetVector() - vp.Key.DirectAbsolutePosition.GetVector()).GetDistance(new DirectWorldPosition(c.Center)).ToString() + ")]")))}]");
                    }
                }

            }).Initialize().QueueAction();
        }

        private void button12_Click(object sender, EventArgs e)
        {
            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;
                var pos = ship.DirectAbsolutePosition.GetVector() + new DirectWorldPosition(50_000, 50_000, 50_000).GetVector();

                if (ship == null)
                    return;

                var t = textBox1.Text.Trim();
                if (!string.IsNullOrEmpty(t))
                {
                    Framework.SceneManager.DrawModel(10_000, t, 0f, 0f, 0f);
                }

            }).Initialize().QueueAction();
        }

        private void button13_Click(object sender, EventArgs e)
        {
            bool ignoreAbyssEntities = checkBox1.Checked;
            bool ignoreTrackingPolyons = checkBox2.Checked;
            bool ignoreAutomataPylon = checkBox3.Checked;
            bool ignoreWideAreaAutomataPylon = checkBox4.Checked;
            bool ignoreFilaCouds = checkBox5.Checked;
            bool ignoreBioClouds = checkBox6.Checked;
            bool ignoreTachClouds = checkBox7.Checked;

            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;
                var pos = ship.DirectAbsolutePosition.GetVector();

                DirectEntity _midGate = Framework.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);
                DirectEntity _endGate = Framework.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);
                DirectEntity _nextGate = _midGate ?? _endGate;

                if (ship == null || _nextGate == null)
                    return;

                var colEnts = DirectRayCasting.IsLineOfSightFree(pos, _nextGate.DirectAbsolutePosition.GetVector(), ignoreAbyssEntities: ignoreAbyssEntities, ignoreTrackingPolyons: ignoreTrackingPolyons, ignoreAutomataPylon: ignoreAutomataPylon, ignoreWideAreaAutomataPylon: ignoreWideAreaAutomataPylon, ignoreFilaCouds: ignoreFilaCouds, ignoreBioClouds: ignoreBioClouds, ignoreTachClouds: ignoreTachClouds, debugLog: true);

                if (colEnts.Item1)
                {
                    Console.WriteLine("Line of sight is free.");
                }
                else
                {
                    foreach (var vp in colEnts.Item2)
                    {
                        Console.WriteLine($"[{vp.Key.TypeName}] ColliderCnt [{vp.Value.Count}] ColliderTypes [{string.Join(",", vp.Value.Select(c => c.GetType().Name + " (" + c.MaxBoundingRadius + ")"))}]");
                    }
                }

            }).Initialize().QueueAction();
        }

        private void button14_Click(object sender, EventArgs e)
        {
            int.TryParse(textBox3.Text, out var dist);
            bool ignoreAbyssEntities = checkBox1.Checked;
            bool ignoreTrackingPolyons = checkBox2.Checked;
            bool ignoreAutomataPylon = checkBox3.Checked;
            bool ignoreWideAreaAutomataPylon = checkBox4.Checked;
            bool ignoreFilaCouds = checkBox5.Checked;
            bool ignoreBioClouds = checkBox6.Checked;
            bool ignoreTachClouds = checkBox7.Checked;

            new ActionQueueAction(() =>
            {
                var ship = Framework.ActiveShip.Entity;

                if (ship == null)
                    return;

                var camDir = Framework.SceneManager.CameraDirection;
                var start = ship.DirectAbsolutePosition.GetVector();
                var end = start + camDir.Scale(dist);

                var colEnts = DirectRayCasting.IsLineOfSightFree(start, end, ignoreAbyssEntities: ignoreAbyssEntities, ignoreTrackingPolyons: ignoreTrackingPolyons, ignoreAutomataPylon: ignoreAutomataPylon, ignoreWideAreaAutomataPylon: ignoreWideAreaAutomataPylon, ignoreFilaCouds: ignoreFilaCouds, ignoreBioClouds: ignoreBioClouds, ignoreTachClouds: ignoreTachClouds, debugLog: true);

                if (colEnts.Item1)
                {
                    Console.WriteLine("Line of sight is free.");
                }
                else
                {
                    int i = 0;
                    Console.WriteLine($"There are {colEnts.Item2.Keys.Count} objects in the way.");
                    foreach (var vp in colEnts.Item2)
                    {
                        i++;
                        Console.WriteLine($"[{vp.Key.TypeName}] ColliderCnt [{vp.Value.Count}] ColliderTypes [{string.Join(",", vp.Value.Select(c => c.GetType().Name + " (" + c.MaxBoundingRadius + ")"))}]");
                        foreach (var collider in vp.Value)
                        {
                            //Console.WriteLine($"Drawing spherical collider with MaxBoundingRadiusSquared [{collider.MaxBoundingRadius}]");
                            Framework.SceneManager.DrawSphere((float)collider.MaxBoundingRadius, vp.Key.Id.ToString(), (float)collider.Center.X, (float)collider.Center.Y, (float)collider.Center.Z, vp.Key.Ball, sphereType: DirectSceneManager.SphereType.Jumprangebubble);
                            Framework.SceneManager.DrawSphere((float)collider.MaxBoundingRadius, vp.Key.Id.ToString(), (float)collider.Center.X, (float)collider.Center.Y, (float)collider.Center.Z, vp.Key.Ball, sphereType: DirectSceneManager.SphereType.Scanconesphere);
                            Framework.SceneManager.DrawSphere((float)collider.MaxBoundingRadius, vp.Key.Id.ToString(), (float)collider.Center.X, (float)collider.Center.Y, (float)collider.Center.Z, vp.Key.Ball, sphereType: DirectSceneManager.SphereType.Scanbubblehitsphere);
                        }
                    }
                }

            }).Initialize().QueueAction();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    var ship = ESCache.Instance.DirectEve.ActiveShip.Entity;
                    var closestColliderEntity = Framework.Entities.Where(e => e.HasAnyNonTraversableColliders).OrderBy(e => e.Distance).FirstOrDefault();

                    if (closestColliderEntity == null)
                        return;

                    closestColliderEntity.ShowDestinyBalls(1);
                    var start = ship.DirectAbsolutePosition.GetVector();
                    var end = closestColliderEntity.DirectAbsolutePosition.GetVector();
                    var dir = (end - start).Normalize();
                    DirectSceneManager.GenerateSafeDirectionVector(dir, start, start, 70_000);


                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                finally
                {
                    ModifyButtons(true);
                }
            }));
            action.Initialize().QueueAction();
        }
    }
}
