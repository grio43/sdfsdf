using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Combat
{
    public partial class Combat
    {
        #region Methods

        public void AddPrimaryWeaponPriorityTarget(EntityCache ewarEntity, WeaponPriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (ewarEntity.IsIgnored || PrimaryWeaponPriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                {
                    if (DebugConfig.DebugAddPrimaryWeaponPriorityTarget)
                        Log.WriteLine("if ((target.IsIgnored) || PrimaryWeaponPriorityTargets.Any(p => p.Id == target.Id)) continue");
                    return;
                }

                if (AddEwarTypeToPriorityTargetList)
                {
                    if (AnyTurrets && (ewarEntity.IsNPCFrigate || ewarEntity.IsFrigate))
                    {
                        if (!ewarEntity.IsTooCloseTooFastTooSmallToHit)
                            if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                            {
                                Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                                              Math.Round(ewarEntity.Distance / 1000, 2) + "k] [ID: " + ewarEntity.DirectEntity.Id.ToString() +
                                              "] as a PrimaryWeaponPriorityTarget [" +
                                              priority.ToString() + "]");
                                _primaryWeaponPriorityTargets.Add(new PriorityTarget
                                {
                                    Name = ewarEntity.Name,
                                    EntityID = ewarEntity.Id,
                                    WeaponPriority = priority
                                });
                            }

                        return;
                    }

                    if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                    {
                        Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                                      Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.DirectEntity.Id.ToString() +
                                      "] as a PrimaryWeaponPriorityTarget [" +
                                      priority.ToString() + "]");
                        _primaryWeaponPriorityTargets.Add(new PriorityTarget
                        {
                            Name = ewarEntity.Name,
                            EntityID = ewarEntity.Id,
                            WeaponPriority = priority
                        });
                    }

                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return;
        }

        public void AddPrimaryWeaponPriorityTargets(IEnumerable<EntityCache> ewarEntities, WeaponPriority priority, string module,
            bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                ewarEntities = ewarEntities.ToList();
                if (ewarEntities.Any())
                    foreach (var ewarEntity in ewarEntities)
                        AddPrimaryWeaponPriorityTarget(ewarEntity, priority, module, AddEwarTypeToPriorityTargetList);

                return;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }

            return;
        }

        #endregion Methods
    }
}