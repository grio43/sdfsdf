using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void MoveToAction(Action action)
        {
            if (DateTime.UtcNow < _nextCombatMissionCtrlAction)
                return;

            if (ESCache.Instance.NormalApproach)
                ESCache.Instance.NormalApproach = false;

            ESCache.Instance.NormalNavigation = false;

            var target = action.GetParameterValue("target");

            // No parameter? Although we should not really allow it, assume its the acceleration gate :)
            if (string.IsNullOrEmpty(target))
                target = "Acceleration Gate";

            if (!int.TryParse(action.GetParameterValue("distance"), out var DistanceToApproach))
                DistanceToApproach = (int)Distances.GateActivationRange;

            if (!bool.TryParse(action.GetParameterValue("StopWhenTargeted"), out var stopWhenTargeted))
                stopWhenTargeted = false;

            if (!bool.TryParse(action.GetParameterValue("StopWhenAggressed"), out var stopWhenAggressed))
                stopWhenAggressed = false;

            if (!bool.TryParse(action.GetParameterValue("OrderDescending"), out var orderDescending))
                orderDescending = false;

            var targets = new List<EntityCache>();
            if (ESCache.Instance.EntitiesOnGrid != null && ESCache.Instance.EntitiesOnGrid.Any())
                if (ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == target.ToLower()).ToList() != null &&
                    ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == target.ToLower()).ToList().Any())
                    targets = ESCache.Instance.EntitiesOnGrid.Where(e => e.Name.ToLower() == target.ToLower()).ToList();

            if (!targets.Any())
            {
                Log.WriteLine("no entities found named [" + target + "] proceeding to next action");
                Nextaction();
                return;
            }

            var moveToTarget = targets.OrderBy(t => t.Distance).FirstOrDefault();

            if (orderDescending)
            {
                Log.WriteLine(" moveTo: orderDescending == true");
                moveToTarget = targets.OrderByDescending(t => t.Distance).FirstOrDefault();
            }

            ESCache.Instance.Combat.GetBestPrimaryWeaponTarget(ESCache.Instance.Combat.MaxRange);

            if (moveToTarget != null)
            {
                if (stopWhenTargeted)
                    if (ESCache.Instance.Combat.TargetedBy != null && ESCache.Instance.Combat.TargetedBy.Any())
                        if (ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                            {
                                if (!ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip)) return;
                                Log.WriteLine("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name +
                                              "][" +
                                              Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                                Nextaction();
                            }

                if (stopWhenAggressed)
                    if (ESCache.Instance.Combat.Aggressed.Any(t => !t.IsSentry))
                        if (ESCache.Instance.FollowingEntity != null)
                            if (ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                            {
                                if (!ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip)) return;
                                Log.WriteLine("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                              Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                                Nextaction();
                            }

                if (moveToTarget.Distance < DistanceToApproach) // if we are inside the range that we are supposed to approach assume we are done
                {
                    Log.WriteLine("We are [" + Math.Round(moveToTarget.Distance, 0) + "] from a [" + target + "] we do not need to go any further");
                    Nextaction();

                    if (ESCache.Instance.FollowingEntity != null)
                        if (ESCache.Instance.MyShipEntity.Velocity != 0 && DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                        {
                            if (!ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip)) return;
                            Log.WriteLine("Stop ship, we have been targeted and are [" + DistanceToApproach + "] from [ID: " + moveToTarget.Name + "][" +
                                          Math.Round(moveToTarget.Distance / 1000, 0) + "k away]");
                        }
                    return;
                }
                ESCache.Instance.NavigateOnGrid.NavigateToTarget(moveToTarget, "CombatMissionCtrl.MoveToAction", ESCache.Instance.NavigateOnGrid.SpeedTank, 500);
            }
        }

        #endregion Methods
    }
}