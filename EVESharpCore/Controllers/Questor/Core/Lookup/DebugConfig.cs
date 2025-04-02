using EVESharpCore.Cache;

namespace EVESharpCore.Controllers.Questor.Core.Lookup
{
    public static class DebugConfig
    {
        #region Properties

        public static bool DebugActivateGate => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugActivateGate;
        public static bool DebugActivateWeapons => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugActivateWeapons;

        public static bool DebugAddDronePriorityTarget =>
            ESCache.Instance.EveAccount.CS.QMS.QDS.DebugAddDronePriorityTarget;

        public static bool DebugAddPrimaryWeaponPriorityTarget =>
            ESCache.Instance.EveAccount.CS.QMS.QDS.DebugAddPrimaryWeaponPriorityTarget;

        public static bool DebugAgentInteractionReplyToAgent =>
            ESCache.Instance.EveAccount.CS.QMS.QDS.DebugAgentInteractionReplyToAgent;

        public static bool DebugArm => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugArm;
        public static bool DebugCleanup => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugCleanup;
        public static bool DebugClearPocket => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugClearPocket;
        public static bool DebugCombat => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugCombat;
        public static bool DebugDecline => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugDecline;
        public static bool DebugDefense => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugDefense;
        public static bool DebugDoneAction => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugDoneAction;
        public static bool DebugDrones => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugDrones;
        public static bool DebugEntityCache => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugEntityCache;
        public static bool DebugFittingMgr => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugFittingMgr;
        public static bool DebugGetBestDroneTarget => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugGetBestDroneTarget;
        public static bool DebugGetBestTarget => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugGetBestTarget;
        public static bool DebugGotobase => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugGotobase;
        public static bool DebugHangars => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugHangars;
        public static bool DebugKillAction => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugKillAction;
        public static bool DebugKillTargets => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugKillTargets;
        public static bool DebugLoadScripts => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugLoadScripts;
        public static bool DebugLootWrecks => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugLootWrecks;
        public static bool DebugMoveTo => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugMoveTo;
        public static bool DebugNavigateOnGrid => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugNavigateOnGrid;
        public static bool DebugOverLoadWeapons => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugOverLoadWeapons;
        public static bool DebugPanic => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugPanic;

        public static bool DebugPreferredPrimaryWeaponTarget =>
            ESCache.Instance.EveAccount.CS.QMS.QDS.DebugPreferredPrimaryWeaponTarget;

        public static bool DebugReloadAll => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugReloadAll;
        public static bool DebugReloadorChangeAmmo => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugReloadorChangeAmmo;
        public static bool DebugSalvage => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugSalvage;
        public static bool DebugSpeedMod => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugSpeedMod;
        public static bool DebugTargetCombatants => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugTargetCombatants;
        public static bool DebugTargetWrecks => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugTargetWrecks;
        public static bool DebugTractorBeams => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugTractorBeams;
        public static bool DebugTraveler => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugTraveler;
        public static bool DebugUndockBookmarks => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugUndockBookmarks;
        public static bool DebugUnloadLoot => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugUnloadLoot;
        public static bool DebugWatchForActiveWars => ESCache.Instance.EveAccount.CS.QMS.QDS.DebugWatchForActiveWars;

        #endregion Properties
    }
}