// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectScannerProbe : DirectObject
    {
        #region Fields

        internal PyObject PyProbe;
        private double? _rangeAU;

        #endregion Fields

        //'typeID': 30488,
        // 'probeID': 9001433634000455522L,
        // 'destination': (-934813827072.0, 88655552512.0, -2063288762368.0),
        //'rangeStep': 3,
        //'pos': (-1165319562341.6548, -19667799734.71859, 3193205517349.8516),
        //'expiry': 131706726098242256L,
        //'delay': 4000000.0,
        //'state': 1,
        //'scanRange': 149597870700.0
        //,'maxScanRange': None,
        //'scanBonuses': {'deviation': {'ship': 0.0, 'modules': 0},
        //'strength': {'ship': 0.0, 'modules': 10.000000000000009}

        #region Constructors

        internal DirectScannerProbe(DirectEve directEve, PyObject pyProbe)
            : base(directEve)
        {
            PyProbe = pyProbe;
            TypeId = (int)pyProbe.Attribute("typeID");
            ProbeId = (long)pyProbe.Attribute("probeID");
            var pos = pyProbe.Attribute("pos");
            var dest = pyProbe.Attribute("destination");
            Pos = new Vec3((double)pos.GetItemAt(0), (double)pos.GetItemAt(1), (double)pos.GetItemAt(2));
            DestinationPos = new Vec3((double)dest.GetItemAt(0), (double)dest.GetItemAt(1), (double)dest.GetItemAt(2));
            Expiry = pyProbe.Attribute("expiry").ToDateTime();
            AllRangesAu = DirectEve.GetLocalSvc("scanSvc")
                .Call("GetScanRangeStepsByTypeID", TypeId)
                .ToList<double>()
                .Select(i => i / (double)directEve.Const.AU)
                .ToList();
        }

        #endregion Constructors

        #region Properties

        public List<double> AllRangesAu { get; internal set; }
        public Vec3 DestinationPos { get; internal set; }
        public DateTime Expiry { get; internal set; }
        public Vec3 Pos { get; internal set; }
        public long ProbeId { get; internal set; }
        public double RangeAu => _rangeAU ?? (double)PyProbe.Attribute("scanRange") / (double)DirectEve.Const.AU;
        public int TypeId { get; internal set; }

        #endregion Properties

        #region Methods

        public void DecreaseProbeRange()
        {
            if (!AllRangesAu.Any(i => i == RangeAu))
                return;
            var index = AllRangesAu.IndexOf(RangeAu);
            if (index - 1 < AllRangesAu.Count && index - 1 >= 0)
                SetProbeRangeAu(AllRangesAu[index - 1]);
        }

        public bool IsAtMinRange
        {
            get
            {
                return AllRangesAu.IndexOf(RangeAu) == 0;
            }
        }

        public bool IsAtMaxRange
        {
            get
            {
                return AllRangesAu.IndexOf(RangeAu) == AllRangesAu.Count - 1;
            }
        }

        public bool DestroyProbe()
        {
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("scanSvc").Attribute("DestroyProbe"), ProbeId);
        }

        public void IncreaseProbeRange()
        {
            if (!AllRangesAu.Any(i => i == RangeAu))
                return;
            var index = AllRangesAu.IndexOf(RangeAu);
            if (index + 1 < AllRangesAu.Count && index + 1 >= 0)
                SetProbeRangeAu(AllRangesAu[index + 1]);
        }

        public bool RecoverProbe()
        {
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("scanSvc").Attribute("RecoverProbe"), ProbeId);
        }

        public void RefreshUI()
        {
            if (!AllRangesAu.Any(i => i == RangeAu))
                return;
            SetProbeRangeAu(RangeAu);
        }

        public bool SetLocation(Vec3 v)
        {
            return PyProbe.SetTriple("destination", v.X, v.Y, v.Z);
        }

        public void SetMaxProbeRange()
        {
            if (!AllRangesAu.Any(i => i == RangeAu))
                return;
            var index = AllRangesAu.Count - 1;
            SetProbeRangeAu(AllRangesAu[index]);
        }

        public bool SetProbeRangeAu(double range)
        {
            if (!AllRangesAu.Any(i => i == range))
                return false;
            var index = AllRangesAu.FindIndex(i => i == range);
            var stepNumber = index + 1;
            _rangeAU = AllRangesAu[index];
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("scanSvc").Attribute("probeTracker").Attribute("SetProbeRangeStep"), ProbeId, stepNumber);
        }

        #endregion Methods
    }
}