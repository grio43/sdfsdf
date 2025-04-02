using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void ActivateAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            if (!bool.TryParse(action.GetParameterValue("optional"), out var optional))
                optional = false;

            var target = action.GetParameterValue("target");
            var alternativeTarget = action.GetParameterValue("alternativetarget");

            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            if (string.IsNullOrEmpty(alternativeTarget))
                alternativeTarget = "Acceleration Gate";

            IEnumerable<EntityCache> targets =
                ESCache.Instance.EntitiesOnGrid.Where(i => i.Distance < (int)Distances.OnGridWithMe).Where(e => e.Name.ToLower() == target.ToLower()).ToList().ToList();

            if (!targets.Any())
            {
                if (DirectEve.Interval(15000))
                    Log.WriteLine("First target not found, using alternative target [" + alternativeTarget + "]");
                targets = ESCache.Instance.EntitiesOnGrid.Where(i => i.Distance < (int)Distances.OnGridWithMe).Where(e => e.Name.ToLower() == alternativeTarget.ToLower()).ToList()
                    .ToList();
            }

            if (!targets.Any())
            {
                if (!_waiting)
                {
                    Log.WriteLine("Activate: Can't find [" + target + "] to activate! Waiting 30 seconds before giving up");
                    _waitingSince = DateTime.UtcNow;
                    _waiting = true;
                }
                else if (_waiting)
                {
                    if (DateTime.UtcNow.Subtract(_waitingSince).TotalSeconds > ESCache.Instance.Time.NoGateFoundRetryDelay_seconds)
                    {
                        Log.WriteLine("Activate: After 10 seconds of waiting the gate is still not on grid: CombatMissionCtrlState.Error");
                        if (optional) //if this action has the optional parameter defined as true then we are done if we cant find the gate
                            DoneAction();
                        else
                            ESCache.Instance.State.CurrentActionControlState = ActionControlState.Error;
                    }
                }

                return;
            }

            var closest = targets.OrderBy(t => t.Distance).FirstOrDefault(); // at least one gate must exists at this point

            if (closest.Distance <= (int)Distances.GateActivationRange + 150) // if gate < 2150m => within activation range
            {
                // We cant activate if we have drones out
                if (ESCache.Instance.Drones.ActiveDrones.Any())
                {
                    // Tell the drones module to retract drones
                    ESCache.Instance.Drones.IsMissionPocketDone = true;

                    if (DebugConfig.DebugActivateGate)
                        Log.WriteLine("if (Cache.Instance.ActiveDrones.Any())");
                    return;
                }

                //if (ESCache.Instance.Time.NextApproachAction < DateTime.UtcNow && closest.Distance < 0) // if distance is below 500, we keep at range 1000
                //    if (AttemptsToGetAwayFromGate < 10)
                //    {
                //        ESCache.Instance.Time.NextApproachAction = DateTime.UtcNow.AddMilliseconds(new Random().Next(3000,4000));
                //        AttemptsToGetAwayFromGate++;
                //        if (closest.KeepAtRange(1000))
                //            Log.WriteLine("Activate: We are too close to [" + closest.Name + "] Initiating KeepAtRange(1000)");
                //        return;
                //    }
                //    else
                //    {
                //        ESCache.Instance.State.CurrentActionControlState = ActionControlState.Error;
                //        Log.WriteLine("ERROR: Too many attempts to get away from the gate.");
                //        return;
                //    }

                //// if we reach this point we're between <= 2150m && >=0m to the gate
                /// 


                if (AttemptsToActivateGate < 10)
                {

                    if (closest.Activate())
                    {
                        foreach (var e in ESCache.Instance.EntitiesOnGrid.Where(e => ESCache.Instance.Statistics.BountyValues.TryGetValue(e.Id, out var val) && val > 0))
                            ESCache.Instance.Statistics.BountyValues.Remove(e.Id);
                        AttemptsToActivateGate++;
                        ESCache.Instance.ClearPerPocketCache();
                        Log.WriteLine("Activate: [" + closest.Name + "] Move to next pocket and change state to 'NextPocket'");
                        AttemptsToActivateGate = 0;
                        // Do not change actions, if NextPocket gets a timeout (>2 mins) then it reverts to the last action
                        _moveToNextPocket = DateTime.UtcNow;
                        ESCache.Instance.State.CurrentActionControlState = ActionControlState.NextPocket;
                    }

                }
                else
                {
                    ESCache.Instance.State.CurrentActionControlState = ActionControlState.Error;
                    Log.WriteLine("ERROR: Too many attempts to activate the gate.");
                    return;
                }

                return;
            }

            if (closest.Distance < (int)Distances.WarptoDistance) //if we are inside warpto distance then approach
            {
                if (DebugConfig.DebugActivateGate) Log.WriteLine("if (closest.Distance < (int)Distances.WarptoDistance)");

                // Move to the target
                if (DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                {
                    if (!closest.IsApproachedOrKeptAtRangeByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id ||
                        ESCache.Instance.MyShipEntity.Velocity < 50)
                    {
                        if (closest.Approach())
                        {
                            Log.WriteLine("Approaching target [" + closest.Name + "][" + closest.DirectEntity.Id.ToString() + "][" + Math.Round(closest.Distance / 1000, 0) +
                                          "k away]");
                            return;
                        }

                        return;
                    }

                    if (DebugConfig.DebugActivateGate)
                        Log.WriteLine("Cache.Instance.IsOrbiting [" + closest.IsOrbitedByActiveShip + "] Cache.Instance.MyShip.Velocity [" +
                                      Math.Round(ESCache.Instance.MyShipEntity.Velocity, 0) + "m/s]");
                    if (DebugConfig.DebugActivateGate)
                        if (ESCache.Instance.FollowingEntity != null)
                            Log.WriteLine("Cache.Instance.Approaching.Id [" + ESCache.Instance.FollowingEntity.Id + "][closest.Id: " + closest.Id + "]");
                    if (DebugConfig.DebugActivateGate) Log.WriteLine("------------------");
                    return;
                }

                if (!closest.IsOrbitedByActiveShip || ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != closest.Id)
                {
                    Log.WriteLine("Activate: Delaying approach for: [" +
                                  Math.Round(ESCache.Instance.Time.NextApproachAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) +
                                  "] seconds");
                    return;
                }

                if (DebugConfig.DebugActivateGate) Log.WriteLine("------------------");
                return;
            }

            if (closest.Distance > (int)Distances.WarptoDistance) //we must be outside warpto distance, but we are likely in a DeadSpace so align to the target
            {
                // We cant warp if we have drones out - but we are aligning not warping so we do not care
                //if (Cache.Instance.ActiveDrones.Count() > 0)
                //    return;

                if (closest.AlignTo())
                {
                    Log.WriteLine("Activate: AlignTo: [" + closest.Name + "] This only happens if we are asked to Activate something that is outside [" +
                                  Distances.CloseToGateActivationRange + "]");
                    return;
                }

                return;
            }
        }

        #endregion Methods
    }
}