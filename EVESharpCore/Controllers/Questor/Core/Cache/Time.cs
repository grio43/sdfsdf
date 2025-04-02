// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace EVESharpCore.Controllers.Questor.Core.Cache
{
    public class Time
    {
        #region Fields

        public readonly int LootingDelay_milliseconds = 800;
        public readonly int NoGateFoundRetryDelay_seconds = 10;
        public readonly int PainterDelay_milliseconds = 800;
        public readonly int RecallDronesDelayBetweenRetries = 15;
        public readonly Random Rnd = new Random();
        public readonly int SalvageDelayBetweenActions_milliseconds = 500;
        public readonly int SwitchShipsDelay_seconds = 3;

        // Switch Ships Delay before retrying, units: seconds. Default is 10
        public readonly int TargetDelay_milliseconds = 800;

        public readonly int TargetsAreFullDelay_seconds = 2;

        // Delay used when we have determined that all our targeting slots are full
        public readonly int TravelerJumpedGateNextCommandDelay_seconds = 8;

        public readonly int TravelerNoStargatesFoundRetryDelay_seconds = 10;
        public readonly int WarpScrambledNoDelay_seconds = 10;
        public readonly int WarptoDelay_seconds = 10;
        public readonly int WeaponDelay_milliseconds = 100;

        // This is the delay between warpto commands, units: seconds. Default is 10
        public readonly int WebDelay_milliseconds = 220;

        public int AfterburnerDelay_milliseconds = 3500;
        public int DefenceDelay_milliseconds = 1500;
        public Dictionary<long, DateTime> LastActivatedTimeStamp = new Dictionary<long, DateTime>();

        #endregion Fields

        #region Constructors

        public Time()
        {
        }

        #endregion Constructors

        #region Properties

        public DateTime LastGroupWeapons { get; set; }
        public DateTime LastLoggingAction { get; set; }
        public DateTime LastOfflineModuleCheck { get; set; }
        public DateTime LastPreferredDroneTargetDateTime { get; set; }
        public DateTime LastPreferredPrimaryWeaponTargetDateTime { get; set; }
        public DateTime LastUndockAction { get; set; }
        public DateTime NextActivateModules { get; set; }
        public DateTime NextApproachAction { get; set; }
        public DateTime NextArmAction { get; set; }
        public DateTime NextGetBestDroneTarget { get; set; }
        public DateTime NextJumpAction { get; set; }
        public DateTime NextLootAction { get; set; }
        public DateTime NextRepairItemsAction { get; set; }
        public DateTime NextRepModuleAction { get; set; }
        public DateTime NextSalvageAction { get; set; }
        public DateTime NextTargetAction { get; set; }
        public DateTime NextTractorBeamAction { get; set; }
        public DateTime NextTravelerAction { get; set; }
        public DateTime NextUndockAction { get; set; }
        public DateTime NextUnlockTargetOutOfRange { get; set; }
        public DateTime StartedBoosting { get; set; }

        #endregion Properties
    }
}