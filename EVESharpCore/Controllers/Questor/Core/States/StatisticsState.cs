namespace EVESharpCore.Controllers.Questor.Core.States
{
    public enum StatisticsState
    {
        Idle,
        SessionLog,
        MissionLog,
        PocketLog,
        LogAllEntities,
        ListPotentialCombatTargets,
        ListLowValueTargets,
        ListHighValueTargets,
        ModuleInfo,
        ListIgnoredTargets,
        ListPrimaryWeaponPriorityTargets,
        ListDronePriorityTargets,
        ListTargetedandTargeting,
        ListItemHangarItems,
        ListLootHangarItems,
        ListLootContainerItems,
        PocketObjectStatistics,
        LocalStatistics,
        Done
    }
}