using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Cache
{
    public class NavigateOnGrid
    {
        #region Fields

        public DateTime NextNavigateIntoRange = DateTime.UtcNow;

        #endregion Fields

        #region Properties

        public int OptimalRange => ESCache.Instance.EveAccount.CS.QMS.QS.OptimalRange;

        public int OrbitDistance
        {
            get
            {
                var orbitDist = ESCache.Instance.EveAccount.CS.QMS.QS.OrbitDistance;

                if (!ESCache.Instance.Combat.PotentialCombatTargets.Any())
                    orbitDist = 500;

                if (ESCache.Instance.Combat.AnyTurrets && (ESCache.Instance.Combat.CurrentWeaponTarget?.IsFrigate ?? false))
                {
                    Log.WriteLine("Target is a frigate and we have turrets mounted, using three times the usual orbit distance.");
                    orbitDist = OrbitDistance * 3;
                }
                return orbitDist;
            }
        }

        public bool OrbitStructure => ESCache.Instance.EveAccount.CS.QMS.QS.OrbitStructure;
        public bool SpeedTank => ESCache.Instance.EveAccount.CS.QMS.QS.SpeedTank;

        #endregion Properties

        #region Methods

        public void NavigateIntoRange(EntityCache target, string module, bool moveMyShip)
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp || !moveMyShip)
                return;

            if (DateTime.UtcNow < NextNavigateIntoRange)
                return;

            if (target == null)
            {
                Log.WriteLine($"Target is null.");
                return;
            }

            if (!target.IsValid)
            {
                Log.WriteLine($"Target is not valid.");
                return;
            }

            NextNavigateIntoRange = DateTime.UtcNow.AddSeconds(5);

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("NavigateIntoRange Started");

            if (SpeedTank)
            {
                if (target.Distance > ESCache.Instance.Combat.MaxRange && !target.IsOrbitedByActiveShip)
                {
                    if (target.KeepAtRange((int)(ESCache.Instance.Combat.MaxRange * 0.8d)))
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: SpeedTank: Moving into weapons range before initiating orbit");

                    return;
                }

                if (target.Distance < ESCache.Instance.Combat.MaxRange && !target.IsOrbitedByActiveShip)
                {
                    OrbitGateorTarget(target, module);
                    return;
                }
                return;
            }
            else // not speed tanking
            {
                if (DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                {
                    if (OptimalRange != 0)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: OptimalRange [ " + OptimalRange + "] Current Distance to [" + target.Name + "] is [" +
                                          Math.Round(target.Distance / 1000, 0) + "]");

                        if (target.Distance > OptimalRange + (int)Distances.OptimalRangeCushion)
                            if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                                ESCache.Instance.MyShipEntity.Velocity < 50)
                            {
                                if (target.IsNPCFrigate && ESCache.Instance.Combat.AnyTurrets)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log.WriteLine("NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" +
                                                      Math.Round(target.Distance / 1000, 0) + "]");
                                    OrbitGateorTarget(target, module);
                                    return;
                                }

                                if (target.KeepAtRange(OptimalRange))
                                    Log.WriteLine("Using Optimal Range: Approaching target [" + target.Name + "][ID: " + target.DirectEntity.Id.ToString() + "][" +
                                                  Math.Round(target.Distance / 1000, 0) + "k away]");
                                return;
                            }

                        if (target.Distance <= OptimalRange)
                            if (target.IsNPCFrigate && ESCache.Instance.Combat.AnyTurrets)
                            {
                                if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                                    ESCache.Instance.MyShipEntity.Velocity < 50)
                                {
                                    if (target.KeepAtRange(OptimalRange))
                                    {
                                        Log.WriteLine("Target is NPC Frigate and we got Turrets. Keeping target at Range to hit it.");
                                        Log.WriteLine("Initiating KeepAtRange [" + target.Name + "][at " + Math.Round((double)OptimalRange / 1000, 0) +
                                                      "k][ID: " + target.DirectEntity.Id.ToString() + "]");
                                    }
                                    return;
                                }
                            }
                            else if (ESCache.Instance.FollowingEntity != null && ESCache.Instance.FollowingEntity.GroupId != (int)Group.AccelerationGate
                                                                              && ESCache.Instance.MyShipEntity.Velocity != 0)
                            {
                                if (target.IsNPCFrigate && ESCache.Instance.Combat.AnyTurrets) return;

                                if (!ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip)) return;
                                Log.WriteLine("Using Optimal Range: Stop ship, target at [" + Math.Round(target.Distance / 1000, 0) +
                                              "k away] is inside optimal");
                                return;
                            }
                    }
                    else //if optimalrange is not set use MaxRange (shorter of weapons range and targeting range)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log.WriteLine("NavigateIntoRange: using MaxRange [" + ESCache.Instance.Combat.MaxRange + "] target is [" + target.Name + "][" +
                                          target.Distance + "]");

                        if (target.Distance > ESCache.Instance.Combat.MaxRange)
                            if (ESCache.Instance.FollowingEntity == null || ESCache.Instance.FollowingEntity.Id != target.Id ||
                                ESCache.Instance.MyShipEntity.Velocity < 50)
                            {
                                if (target.IsNPCFrigate && ESCache.Instance.Combat.AnyTurrets)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log.WriteLine("NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]");
                                    OrbitGateorTarget(target, module);
                                    return;
                                }

                                if (target.KeepAtRange((int)(ESCache.Instance.Combat.MaxRange * 0.8d)))
                                    Log.WriteLine("Using Weapons Range * 0.8d [" + Math.Round(ESCache.Instance.Combat.MaxRange * 0.8d / 1000, 0) +
                                                  " k]: Approaching target [" + target.Name + "][ID: " + target.DirectEntity.Id.ToString() + "][" +
                                                  Math.Round(target.Distance / 1000, 0) + "k away]");
                                return;
                            }

                        if (target.Distance <= ESCache.Instance.Combat.MaxRange && ESCache.Instance.FollowingEntity == null)
                            if (target.IsNPCFrigate && ESCache.Instance.Combat.AnyTurrets)
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log.WriteLine("NavigateIntoRange: target is NPC Frigate [" + target.Name + "][" + target.Distance + "]");
                                OrbitGateorTarget(target, module);
                                return;
                            }
                    }
                }
            }
        }

        public bool NavigateToTarget(EntityCache target, string module, bool orbit, int DistanceFromTarget)
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp)
                return false;

            // if we are inside warpto range you need to approach (you cant warp from here)
            if (target.Distance < (int)Distances.WarptoDistance)
            {
                if (orbit && target.GroupId != (int)Group.Station && target.GroupId != (int)Group.Stargate)
                {
                    if (target.Distance < DistanceFromTarget)
                        return true;


                    //Log.WriteLine("StartOrbiting: Target in range");
                    if ((!target.IsOrbitedByActiveShip && !target.IsApproachedOrKeptAtRangeByActiveShip) || target.Distance > DistanceFromTarget)
                    {
                        //Log.WriteLine("We are not approaching nor orbiting");
                        target.Orbit(DistanceFromTarget - 1500);
                        return false;
                    }

                }
                else
                //if we are not speed tanking then check optimalrange setting, if that is not set use the less of targeting range and weapons range to dictate engagement range
                {
                    if (DateTime.UtcNow > ESCache.Instance.Time.NextApproachAction)
                    {
                        if (target.Distance < DistanceFromTarget)
                            return true;

                        if (target.KeepAtRange(DistanceFromTarget - 1500))
                            Log.WriteLine("Approaching target [" + target.Name + "][ID: " + target.DirectEntity.Id.ToString() + "][" + Math.Round(target.Distance / 1000, 0) +
                                          "k away]");
                        return false;
                    }

                    return false;
                }

                //
                // do nothing here. If we havent approached or orbited its because we are waiting before spamming the commands again.
                //
            }
            else if (target.Distance > (int)Distances.WarptoDistance)
            {

                var covOpsCloak = ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);
                var shouldWait = covOpsCloak != null && covOpsCloak.IsInLimboState && ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1400000);

                if (!ESCache.Instance.Combat.TargetedBy.Any(t => t.IsWarpScramblingOrDisruptingMe))
                {
                    if (!shouldWait && target.WarpTo())
                    {
                        ESCache.Instance.ClearPerPocketCache();
                        Log.WriteLine("Warping to [" + target.Name + "][" + Math.Round(target.Distance / 1000 / 149598000, 2) + " AU away]");
                        ESCache.Instance.Drones.IsMissionPocketDone = true;
                        return false;
                    }
                }
                else
                {
                    Log.WriteLine("We are scrambled.");
                }
            }

            return false;
        }

        public void OrbitGateorTarget(EntityCache target, string module)
        {
            if (!ESCache.Instance.InSpace || ESCache.Instance.InWarp)
                return;

            if (DebugConfig.DebugNavigateOnGrid) Log.WriteLine("OrbitGateorTarget Started");

            if (target.Distance + OrbitDistance < ESCache.Instance.Combat.MaxRange - 5000)
            {
                if (DebugConfig.DebugNavigateOnGrid)
                    Log.WriteLine("if (target.Distance + Cache.Instance.OrbitDistance < Combat.MaxRange - 5000)");

                if (!target.IsOrbitedByActiveShip && !target.IsApproachedOrKeptAtRangeByActiveShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                        Log.WriteLine("We are not approaching nor orbiting");

                    // Prefer to orbit the last structure defined in                    
                    EntityCache structure = null;
                    if (!string.IsNullOrEmpty(ESCache.Instance.OrbitEntityNamed))
                        structure =
                            ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.Contains(ESCache.Instance.OrbitEntityNamed))
                                .OrderBy(t => t.Distance)
                                .FirstOrDefault();

                    if (structure == null)
                        structure = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.Contains("Gate")).OrderBy(t => t.Distance).FirstOrDefault();

                    if (OrbitStructure && structure != null)
                    {
                        if (structure.Orbit(OrbitDistance))
                        {
                            Log.WriteLine("Initiating Orbit [" + structure.Name + "][at " + Math.Round((double)OrbitDistance / 1000, 0) +
                                          "k][" +
                                          structure.DirectEntity.Id.ToString() + "]");
                            return;
                        }

                        return;
                    }

                    //
                    // OrbitStructure is false
                    //
                    if (SpeedTank)
                    {
                        if (target.Orbit(OrbitDistance))
                        {
                            Log.WriteLine("Initiating Orbit [" + target.Name + "][at " + Math.Round((double)OrbitDistance / 1000, 0) + "k][ID: " +
                                          target.DirectEntity.Id.ToString() + "]");
                            return;
                        }

                        return;
                    }

                    //
                    // OrbitStructure is false
                    // SpeedTank is false
                    //
                    if (ESCache.Instance.MyShipEntity.Velocity < 300) //this will spam a bit until we know what "mode" our active ship is when aligning
                        if (ESCache.Instance.Combat.AnyTurrets)
                        {
                            if (ESCache.Instance.Star.AlignTo())
                            {
                                Log.WriteLine("Aligning to the Star so we might possibly hit [" + target.Name + "][ID: " + target.DirectEntity.Id.ToString() +
                                              "][ActiveShip.Entity.Mode:[" + ESCache.Instance.ActiveShip.Entity.Mode + "]");
                                return;
                            }

                            return;
                        }
                }
                else
                {
                    if (target.Orbit(OrbitDistance))
                    {
                        Log.WriteLine("Out of range. ignoring orbit around structure.");
                        return;
                    }

                    return;
                }

                return;
            }
        }

        #endregion Methods
    }
}