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
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectStation : DirectInvType
    {
        #region Fields

        private DirectSolarSystem _solarSystem;

        #endregion Fields

        #region Constructors

        internal DirectStation(DirectEve directEve, PyObject pyo) : base(directEve)
        {
            Id = (int)pyo.Attribute("stationID");
            Name = (string)pyo.Attribute("stationName");
            X = (double)pyo.Attribute("x");
            Y = (double)pyo.Attribute("y");
            Z = (double)pyo.Attribute("z");
            TypeId = (int)pyo.Attribute("stationTypeID");
            SolarSystemId = (int)pyo.Attribute("solarSystemID");
        }

        #endregion Constructors

        #region Properties

        public int Id { get; private set; }
        public string Name { get; private set; }

        public DirectSolarSystem SolarSystem
        {
            get
            {
                DirectEve.SolarSystems.TryGetValue(SolarSystemId, out _solarSystem);
                return _solarSystem;
            }
        }

        public int SolarSystemId { get; private set; }
        public double X { get; private set; }
        public double Y { get; private set; }
        public double Z { get; private set; }

        #endregion Properties

        #region Methods

        public static Dictionary<int, DirectStation> GetStations(DirectEve directEve)
        {
            var result = new Dictionary<int, DirectStation>();

            var pyDict = directEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("stations").Attribute("data").ToDictionary<int>();
            foreach (var pair in pyDict)
                result[pair.Key] = new DirectStation(directEve, pair.Value);

            return result;
        }

        #endregion Methods
    }
}