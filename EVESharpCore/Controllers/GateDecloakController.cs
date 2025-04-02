extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers.Questor;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class GateDecloakController : BaseController
    {
        #region Constructors

        public GateDecloakController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new GateDecloakControllerForm(this);
        }

        // The following formula describes the velocity of a ship accelerating from a standstill after a particular time:
        // t = Time in seconds
        // vMax = Ship's maximum velocity in m/s
        // inertia = Ship's inertia modifier, in s/kg
        // mass = Ship's mass in kg
        private static double GetVelocity(double t, double vMax, double inertia, double mass)
        {
            var result = vMax * (1 - (Math.Pow(Math.E, (-t * Math.Pow(10, 6)) / (inertia * mass))));
            return result;
        }

        // Rearranging the formula for t we arrive at the formula for time taken to accelerate from zero to V
        private static double GetTimeToAccelerate(double v, double vMax, double inertia, double mass)
        {
            return -inertia * mass * Math.Pow(10, -6) * Math.Log(1 - (v / vMax));
        }

        private static Random _rnd = new Random();

        private static double GetDistanceTravelled(double lowerBound, double upperBound, double vMax, double inertia, double mass)
        {

            var e1 = Math.Pow(Math.E, -(Math.Pow(10, 6) * upperBound) / (inertia * mass));
            var e2 = Math.Pow(Math.E, -(Math.Pow(10, 6) * lowerBound) / (inertia * mass));
            var result = ((vMax * inertia * mass * (e1 - e2)) / Math.Pow(10, 6)) + ((upperBound * vMax) - (lowerBound * vMax));
            return result;
        }

        static ActionQueueAction action = new ActionQueueAction(null, () =>
        {

            if (ControllerManager.Instance.GetController<GateDecloakController>() == null)
            {
                Log($"Stopping action, no GateDecloakController avail.");
                return;
            }



            // code for the gate camp decloak
            var framework = ESCache.Instance.DirectEve;

            if (DirectEve.Interval(1000))
                Log($"Ping!");

            var cloakyHauler = ESCache.Instance.DirectEve.Entities.FirstOrDefault(e => e.GroupId == (int)Group.BlockadeRunner);

            var activeShip = ESCache.Instance.DirectEve.ActiveShip;

            if (cloakyHauler == null)
            {
                action.QueueAction();
                return;
            }

            var targetMass = cloakyHauler.Ball["mass"].ToDouble();
            var targetMaxVelocity = cloakyHauler.Ball["maxVelocity"].ToDouble();
            var targetAgility = cloakyHauler.Ball["Agility"].ToDouble();
            var targetVelocity = Math.Min(cloakyHauler.Velocity, targetMaxVelocity);
            var targetAccelerationLowerBoundTime = GetTimeToAccelerate(targetVelocity, targetMaxVelocity, targetAgility, targetMass);
            var targetAlignment = new Vec3(cloakyHauler.GotoX, cloakyHauler.GotoY, cloakyHauler.GotoZ);
            targetAccelerationLowerBoundTime = double.IsInfinity(targetAccelerationLowerBoundTime) ? 0 : targetAccelerationLowerBoundTime;

            if (targetAlignment.Magnitude == 0)
            {
                Log("Ship is avail but not moving yet, re-adding action.");
                action.QueueAction();
                return;
            }

            var ourMass = activeShip.Entity.Ball["mass"].ToDouble();
            var ourMaxVelocity = activeShip.Entity.Ball["maxVelocity"].ToDouble();
            var ourAgility = activeShip.Entity.Ball["Agility"].ToDouble();
            var ourVelocity = Math.Min(activeShip.Entity.Velocity, ourMaxVelocity);
            var ourAccelerationLowerBoundTime = GetTimeToAccelerate(ourVelocity, ourMaxVelocity, ourAgility, ourMass);
            ourAccelerationLowerBoundTime = double.IsInfinity(ourAccelerationLowerBoundTime) ? 0 : ourAccelerationLowerBoundTime;

            // Iterative approach, calculate until the current distance is larger than our previous
            var closestCollisionDistance = double.MaxValue;
            var closestCollisionPosition = new Vec3(0, 0, 0);

            var activeShipPosition = activeShip.Entity.Position;


            const double MaxTimeCalculated = 30d;
            const double IntervalSteps = 0.1d;
            for (var time = 0d; time <= MaxTimeCalculated; time += IntervalSteps)
            {
                // Calculate where our target will be
                // Calculate where we will be heading directly to the target position
                // Calculate how far away these positions are
                // If the position is larger than the previous guess then keep previous
                // Else save closer position and iterate more


                var targetDistanceTravelled = GetDistanceTravelled(
                    targetAccelerationLowerBoundTime,
                    targetAccelerationLowerBoundTime + time,
                    targetMaxVelocity,
                    targetAgility,
                    targetMass);
                var targetPostionAtTime = cloakyHauler.Position + (targetAlignment.Normalize() * targetDistanceTravelled);

                var ourDistanceTravelled = GetDistanceTravelled(
                    ourAccelerationLowerBoundTime,
                    ourAccelerationLowerBoundTime + time,
                    ourMaxVelocity,
                    ourAgility,
                    ourMass);
                try
                {
                    var ourAlignment = (targetPostionAtTime - activeShipPosition).Normalize();
                    var ourPositionAtTime = activeShipPosition + (ourAlignment * ourDistanceTravelled);

                    var currentCollisionDistance = (ourPositionAtTime - targetPostionAtTime).Magnitude;

                    if (currentCollisionDistance < closestCollisionDistance)
                    {
                        closestCollisionDistance = currentCollisionDistance;
                        closestCollisionPosition = targetPostionAtTime;
                    }
                    else
                    {
                        // We are now further away from colliding than our last guess, use last guess
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Log($"targetPostionAtTime {targetPostionAtTime} activeShipPosition {activeShipPosition}");
                    Log($"cloakyHauler.Position {cloakyHauler.Position} targetAlignment {targetAlignment} targetDistanceTravelled {targetDistanceTravelled}");
                    Log($"targetAccelerationLowerBoundTime {targetAccelerationLowerBoundTime} targetAccelerationLowerBoundTime + time {targetAccelerationLowerBoundTime + time}");
                    Log($"targetMaxVelocity {targetMaxVelocity} targetAgility {targetAgility} targetMass {targetMass}");
                    Log($"targetVelocity {targetVelocity} targetMaxVelocity {targetMaxVelocity} targetAgility {targetAgility} targetMass {targetMass}");
                    return;
                }
            }

            if (closestCollisionDistance > 100_000_000)
            {
                Log($"Collision distance too far. closestCollisionDistance {closestCollisionDistance} closestCollisionPosition ({closestCollisionPosition})");
                return;
            }

            var directionalVectorToTarget = closestCollisionPosition - activeShipPosition;
            framework.SceneManager.DrawLine(new Vec3(0, 0, 0), directionalVectorToTarget);

            if (directionalVectorToTarget.Magnitude < 5_000)
            {
                // Too close for another correction
                Log("Too close for another correction");
                return;
            }

            Log("MoveTo");
            activeShip.MoveTo(directionalVectorToTarget.X, directionalVectorToTarget.Y, directionalVectorToTarget.Z, true);
            //cloakyHauler.Approach();


            action.QueueAction(_rnd.Next(350, 450));

        });

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            if (ControllerManager.Instance.GetController<DefenseController>() != null)
            {
                var def = ControllerManager.Instance.GetController<DefenseController>();
                ControllerManager.Instance.RemoveController(def.GetType());
            }

            Log($"GetTimeToAccelerate {GetTimeToAccelerate(20, 120, 0.02176875, 1_200_000_000)}");
            Log($"GetVelocity {GetVelocity(5.0, 120, 0.02176875, 1_200_000_000)}");
            Log($"GetDistanceTravelled {GetDistanceTravelled(0, 10.0, 120, 0.02176875, 1_200_000_000)}");

            var mwd = Framework.Modules.FirstOrDefault(e => e.GroupId == (int)Group.Afterburner);

            if (mwd != null && !mwd.IsActive)
            {
                mwd.Click();
            }

            Framework.SceneManager.ClearDebugLines();
            action.ExecuteEveryFrame = true;
            action.Initialize().QueueAction();
            IsPaused = true;
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {



            return true;
        }

        public override void Dispose()
        {
            ControllerManager.Instance.GetController<ActionQueueController>().RemoveAllActions();
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}