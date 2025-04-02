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
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectConstellation : DirectObject
    {
        #region Fields

        private DirectRegion _region;
        private List<DirectSolarSystem> _solarSystems;

        #endregion Fields

        #region Constructors

        internal DirectConstellation(DirectEve directEve, PyObject pyo)
            : base(directEve)
        {
            Id = (long)pyo.Attribute("constellationID");
            Name = (string)DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("evelocations").Call("Get", Id).Attribute("name");
            RegionId = (long)pyo.Attribute("regionID");
            FactionId = (long?)pyo.Attribute("factionID");
        }

        #endregion Constructors

        #region Properties

        public long? FactionId { get; private set; }
        public long Id { get; private set; }
        public string Name { get; private set; }

        public DirectRegion Region
        {
            get
            {
                DirectEve.Regions.TryGetValue(RegionId, out _region);
                return _region;
            }
        }

        public long RegionId { get; private set; }

        /// <summary>
        ///     List all solar systems within this constellation
        /// </summary>
        public List<DirectSolarSystem> SolarSystems
        {
            get { return _solarSystems ?? (_solarSystems = DirectEve.SolarSystems.Values.Where(s => s.ConstellationId == Id).ToList()); }
        }

        #endregion Properties

        #region Methods

        public static Dictionary<long, DirectConstellation> GetConstellations(DirectEve directEve)
        {
            var result = new Dictionary<long, DirectConstellation>();

            var pyDict = directEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("mapConstellationCache").ToDictionary<long>();
            foreach (var pair in pyDict)
                result[pair.Key] = new DirectConstellation(directEve, pair.Value);

            return result;
        }

        #endregion Methods
    }
}