extern alias SC;

using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectMapViewWindow : DirectWindow
    {
        #region Fields

        private List<DirectDirectionalScanResult> _scanResults;
        private List<DirectSystemScanResult> _systemScanResults;

        #endregion Fields

        #region Constructors

        internal DirectMapViewWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Properties

        public List<DirectDirectionalScanResult> DirectionalScanResults
        {
            get
            {
                var charId = DirectEve.Session.CharacterId;
                if (_scanResults == null && charId != null)
                {
                    _scanResults = new List<DirectDirectionalScanResult>();
                    foreach (var result in PyWindow.Attribute("mapView").Attribute("directionalScannerPalette").Attribute("scanresult").Attribute("lines")
                        .ToList())
                    {
                        // scan result is a list of tuples
                        var resultAsList = result.ToList();
                        _scanResults.Add(new DirectDirectionalScanResult(DirectEve, resultAsList[0].ToLong(),
                            resultAsList[1].ToInt(), resultAsList[2].ToInt()));
                    }
                }

                return _scanResults;
            }
        }

        public List<DirectSystemScanResult> SystemScanResults
        {
            get
            {
                if (_systemScanResults == null)
                {
                    _systemScanResults = new List<DirectSystemScanResult>();
                    var pyResults = DirectEve.GetLocalSvc("scanSvc").Call("GetResults").GetItemAt(0).ToList();
                    foreach (var pyResult in pyResults) _systemScanResults.Add(new DirectSystemScanResult(DirectEve, pyResult));
                }

                return _systemScanResults;
            }
        }

        #endregion Properties

        #region Methods

        public void DecreaseProbeRange()
        {
            foreach (var p in GetProbes()) p.DecreaseProbeRange();
        }

        public bool IsAnyProbeAtMinRange
        {
            get
            {
                foreach (var p in GetProbes())
                {
                    if (p.IsAtMinRange)
                        return true;
                }
                return false;
            }
        }

        public bool IsAnyProbeAtMaxRange
        {
            get
            {
                foreach (var p in GetProbes())
                {
                    if (p.IsAtMaxRange)
                        return true;
                }
                return false;
            }
        }

        public void DirectionalScan()
        {
            if (!IsDirectionalScanOpen())
                return;

            if (IsDirectionalScanning())
                return;

            DirectEve.ThreadedCall(PyWindow.Attribute("mapView").Attribute("directionalScannerPalette").Attribute("DirectionalScan"));
        }

        public List<Vec3> GetPinPointCoordinates()
        {
            var offsets = new List<Vec3>() { new Vec3(0, 0, 0), new Vec3(0, 0.5, 0), new Vec3(0, -0.5, 0) };

            double GetXValue(double i)
            {
                return 0.5d * Math.Cos(i * 2 * Math.PI / 5);
            }

            double GetZValue(double i)
            {
                return 0.5d * Math.Sin(i * 2 * Math.PI / 5);
            }

            for (var i = 0; i < 5; i++) offsets.Add(new Vec3(GetXValue(i), 0, GetZValue(i)));
            return offsets;
        }

        public List<DirectScannerProbe> GetProbes()
        {
            var Probes = new List<DirectScannerProbe>();
            var pyProbes = DirectEve.GetLocalSvc("scanSvc").Attribute("probeTracker").Attribute("probeData").ToDictionary<long>();
            foreach (var pyProbe in pyProbes)
                //DirectEve.Log($"{pyProbe.Value.LogObject()}");
                Probes.Add(new DirectScannerProbe(DirectEve, pyProbe.Value));

            return Probes;
        }

        public void IncreaseProbeRange()
        {
            foreach (var p in GetProbes()) p.IncreaseProbeRange();
        }

        public bool IsDirectionalScannerDocked()
        {
            return PyWindow.Attribute("mapView").Attribute("directionalScannerPalette").IsValid;
        }

        public bool IsDirectionalScanning()
        {
            return DirectEve.GetLocalSvc("directionalScanSvc").Attribute("isScanning").ToBool();
        }

        public bool IsDirectionalScanOpen()
        {
            return PyWindow.Attribute("mapView").Attribute("directionalScannerPalette").IsValid
                   || DirectEve.IsDirectionalScannerWindowOpen;
        }

        public bool IsProbeScannerDocked()
        {
            return PyWindow.Attribute("mapView").Attribute("probeScannerPalette").IsValid;
        }

        public bool IsProbeScanning()
        {
            return DirectEve.GetLocalSvc("scanSvc").Call("IsScanning").ToBool();
        }

        public bool IsProbeScanOpen()
        {
            return PyWindow.Attribute("mapView").Attribute("probeScannerPalette").IsValid
                || DirectEve.IsProbeScannerWindowOpen;
        }
        public void MoveProbesTo(Vec3 dest, bool randomize = true)
        {
            if (randomize)
            {
                var rand = new Random();
                double randomFactor = 0.001 + (rand.NextDouble() * 0.002); // Ensures factor is between 0.1% and 0.3%
                randomFactor *= rand.Next(2) == 0 ? 1 : -1; // Randomly make it positive or negative
                var newX = dest.X * (1 + randomFactor);

                randomFactor = 0.001 + (rand.NextDouble() * 0.002); // Recalculate for each coordinate
                randomFactor *= rand.Next(2) == 0 ? 1 : -1; // Randomly make it positive or negative
                var newY = dest.Y * (1 + randomFactor);

                randomFactor = 0.001 + (rand.NextDouble() * 0.002); // Recalculate for each coordinate
                randomFactor *= rand.Next(2) == 0 ? 1 : -1; // Randomly make it positive or negative
                var newZ = dest.Z * (1 + randomFactor);

                dest = new Vec3(newX, newY, newZ);
            }

            var pinpointOffsets = GetPinPointCoordinates();
            var i = 0;
            foreach (var p in GetProbes())
            {
                p.SetLocation(dest + pinpointOffsets[i] * (149598000000 * p.RangeAu));
                i++;
            }
        }

        public void ProbeScan()
        {
            {
                if (!IsProbeScanOpen())
                    return;

                if (IsProbeScanning())
                    return;

                if (!GetProbes().Any())
                    return;

                var primaryButton = PyWindow.Attribute("mapView").Attribute("probeScannerPalette").Attribute("primaryButton");
                var primaryButtonController = PyWindow.Attribute("mapView").Attribute("probeScannerPalette").Attribute("primaryButtonController");

                if (!primaryButton.IsValid)
                    return;

                if (!primaryButtonController.IsValid)
                    return;

                var disabled = (bool?)primaryButton.Attribute("disabled");
                var buttonLabel = primaryButtonController.Attribute("label").ToUnicodeString();

                if (buttonLabel != "Analyze")
                    return;

                if (!disabled.HasValue || disabled.Value)
                    return;

                DirectEve.ThreadedCall(PyWindow.Attribute("mapView").Attribute("probeScannerPalette").Attribute("Analyze"));
            }
        }

        public bool RecoverProbes()
        {
            var probes = GetProbes();

            if (probes.Any(p => p.Pos.Distance(DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetVector()) < 2200))
            {
                if (DirectEve.Interval(5000))
                    DirectEve.Log("Warn: probes too close.");
                return false;
            }

            if (probes.Any())
            {
                return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("scanSvc").Attribute("RecoverProbes"), probes.Select(p => p.ProbeId));
            }

            return false;
        }

        public void RefreshUI()
        {
            foreach (var p in GetProbes()) p.RefreshUI();
        }

        public void SetMaxProbeRange()
        {
            foreach (var p in GetProbes()) p.SetMaxProbeRange();
        }

        #endregion Methods
    }
}