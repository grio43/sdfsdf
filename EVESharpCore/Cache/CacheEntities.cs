extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Cache
{
    extern alias SC;

    public partial class ESCache
    {
        #region Fields

        #endregion Fields

        #region Properties

        public IEnumerable<EntityCache> AccelerationGates => EntitiesOnGrid.Where(e =>
                e.Distance < (double)Distances.OnGridWithMe &&
                e.GroupId == (int)Group.AccelerationGate)
            .OrderBy(t => t.Distance);

        public IEnumerable<EntityCache> Citadels => Entities.Where(e => e.CategoryId == (int)CategoryID.Citadel).OrderBy(i => i.Distance);

        public IEnumerable<EntityCache> Containers => EntitiesOnGrid.Where(e => e.IsContainer && e.HaveLootRights)
            .OrderBy(i => i.IsWreck)
            .ThenBy(i => i.IsWreckEmpty);

        public IEnumerable<EntityCache> Entities => DirectEve.Entities
            .Where(e => e.IsValid && !e.HasExploded && !e.HasReleased && e.CategoryId != (int)CategoryID.Charge)
            .Select(i => new EntityCache(i));

        public IEnumerable<EntityCache> EntitiesNotSelf =>
            EntitiesOnGrid.Where(i => i.CategoryId != (int)CategoryID.Asteroid && i.Id != Instance.ActiveShip.ItemId);

        public IEnumerable<EntityCache> EntitiesOnGrid => Entities.Where(e => e.Distance < (double)Distances.OnGridWithMe);

        // the entity we are following (approach, orbit, keep at range)
        public EntityCache FollowingEntity => Instance.ActiveShip.Entity != null && Instance.ActiveShip.FollowingEntity != null ? new EntityCache(Instance.ActiveShip.FollowingEntity) : null;

        public bool InSpace => DirectEve.Session.IsInSpace;
        public bool InDockableLocation => DirectEve.Session.IsInDockableLocation;
        public bool InWarp => DirectEve.Me.IsWarpingByMode;

        public int MaxLockedTargets
        {
            get
            {
                return Math.Min(Instance.DirectEve.Me.MaxLockedTargets, Instance.ActiveShip.MaxLockedTargets);
            }
        } 

        public EntityCache MyShipEntity => new EntityCache(DirectEve.ActiveShip.Entity);

        public IEnumerable<EntityCache> Objects => EntitiesOnGrid.Where(e =>
                !e.IsPlayer &&
                e.GroupId != (int)Group.SpawnContainer &&
                e.GroupId != (int)Group.Wreck &&
                e.Distance < 200000)
            .OrderBy(t => t.Distance);

        public EntityCache Star => Entities.FirstOrDefault(e => e.CategoryId == (int)CategoryID.Celestial && e.GroupId == (int)Group.Star);

        public IEnumerable<EntityCache> Stargates => Entities.Where(e => e.GroupId == (int)Group.Stargate);

        public IEnumerable<EntityCache> Stations => Entities.Where(e => e.CategoryId == (int)CategoryID.Station).OrderBy(i => i.Distance);
        public IEnumerable<EntityCache> Targeting => EntitiesOnGrid.Where(e => e.IsTargeting);

        public int TargetingSlotsNotBeingUsedBySalvager
        {
            get
            {
                if (Instance.EveAccount.CS.QMS.QS.MaximumWreckTargets > 0 && Instance.MaxLockedTargets >= 5)
                    return Instance.MaxLockedTargets - Instance.EveAccount.CS.QMS.QS.MaximumWreckTargets;

                return Instance.MaxLockedTargets;
            }
        }

        public IEnumerable<EntityCache> Targets => EntitiesOnGrid.Where(e => e.IsTarget && !e.IsTargeting);

        public IEnumerable<EntityCache> TotalTargetsandTargeting => Targets.Concat(Instance.Targeting.Where(i => !i.IsTarget));

        public IEnumerable<EntityCache> UnlootedContainers => EntitiesOnGrid.Where(e => e.IsContainer && e.HaveLootRights
                                                                                                      && !LootedContainers.Contains(e.Id))
                                                                                                      .OrderBy(e => e.Distance);

        #endregion Properties

        #region Methods

        public EntityCache EntityById(long id) => DirectEve.EntitiesById.TryGetValue(id, out var de) ? new EntityCache(de) : null;

        #endregion Methods
    }
}