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

using EVESharpCore.Cache;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public class DirectLocation : DirectObject
    {
        #region Constructors

        public DirectLocation(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long? ConstellationId { get; private set; }
        public bool IsStructureLocation { get; private set; }
        public bool IsValid { get; private set; }
        public long? ItemId { get; private set; }
        public long LocationId { get; private set; }
        public string Name { get; private set; }

        public long? RegionId { get; private set; }
        public long? SolarSystemId { get; private set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Get a location based on locationId
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public static DirectLocation GetLocation(DirectEve directEve, long locationId)
        {
            var isValid = false;
            string name = null;
            DirectRegion region = null;
            DirectConstellation constellation = null;
            DirectSolarSystem solarSystem = null;
            DirectStation station = null;
            var isStructureLocation = false;

            if (directEve.Regions.TryGetValue(locationId, out region))
            {
                isValid = true;
                name = region.Name;
            }
            else if (directEve.Constellations.TryGetValue(locationId, out constellation))
            {
                isValid = true;
                name = constellation.Name;

                region = constellation.Region;
            }
            else if (locationId < int.MaxValue && directEve.SolarSystems.TryGetValue((int)locationId, out solarSystem))
            {
                isValid = true;
                name = solarSystem.Name;

                constellation = solarSystem.Constellation;
                region = constellation.Region;
            }
            else if (locationId < int.MaxValue && directEve.Stations.TryGetValue((int)locationId, out station))
            {
                isValid = true;
                name = station.Name;

                solarSystem = station.SolarSystem;
                constellation = solarSystem.Constellation;
                region = constellation.Region;
            }
            else if (IsStructure(directEve, locationId))
            {
                var strucInfo = GetStructureInfo(directEve, locationId);
                if (strucInfo.IsValid)
                {
                    isValid = true;
                    isStructureLocation = true;
                    ////__header__: [('structureID', 20, 8), ('typeID', 3, 4), ('ownerID', 3, 4), ('solarSystemID', 3, 4), ('itemName', 130, 4)]
                    var structureID = strucInfo.Attribute("structureID").ToLong();
                    var typeID = strucInfo.Attribute("typeId").ToInt();
                    var ownerID = strucInfo.Attribute("ownerID").ToInt();
                    var solarSystemID = strucInfo.Attribute("solarSystemID").ToInt();
                    var itemName = strucInfo.Attribute("itemName").ToUnicodeString();
                    name = itemName;
                    solarSystem = IsSolarSystem(directEve, solarSystemID) ? directEve.SolarSystems[solarSystemID] : null;
                    locationId = structureID;
                    region = solarSystem != null ? ESCache.Instance.DirectEve.Constellations[solarSystem.Constellation.Id].Region : null;
                    constellation = solarSystem != null ? ESCache.Instance.DirectEve.Constellations[solarSystem.Constellation.Id] : null;
                }
            }

            var result = new DirectLocation(directEve);
            result.IsStructureLocation = isStructureLocation;
            result.IsValid = isValid;
            result.Name = name;
            result.LocationId = locationId;
            result.RegionId = region != null ? region.Id : (long?)null;
            result.ConstellationId = constellation != null ? constellation.Id : (long?)null;
            result.SolarSystemId = solarSystem != null ? solarSystem.Id : (long?)null;
            result.ItemId = station != null ? station.Id : (long?)null;
            return result;
        }

        public static PyObject GetStructureInfo(DirectEve directEve, long itemId)
        {
            if (!IsStructure(directEve, itemId))
                return PySharp.PyNone;

            var structureDirectory = directEve.GetLocalSvc("structureDirectory");
            if (structureDirectory.IsValid)
            {
                var strucInfo = structureDirectory.Call("GetStructureInfo", itemId);
                if (strucInfo.IsValid)
                    return strucInfo;
            }
            return PySharp.PyNone;
        }

        public static bool IsConstellation(DirectEve directEve, long itemId)
        {
            return directEve.Constellations.ContainsKey(itemId);
        }

        public static bool IsRegion(DirectEve directEve, long itemId)
        {
            return directEve.Regions.ContainsKey(itemId);
        }

        public static bool IsSolarSystem(DirectEve directEve, long itemId)
        {
            return itemId <= int.MaxValue && directEve.SolarSystems.ContainsKey((int)itemId);
        }

        public static bool IsStation(DirectEve directEve, long itemId)
        {
            return itemId <= int.MaxValue && directEve.Stations.ContainsKey((int)itemId);
        }

        public static bool IsStructure(DirectEve directEve, long itemId)
        {
            if (IsStation(directEve, itemId)
                || IsSolarSystem(directEve, itemId)
                || IsConstellation(directEve, itemId)
                || IsRegion(directEve, itemId))
                return false;

            if (itemId < 1)
                return false;

            var structureDirectory = directEve.GetLocalSvc("structureDirectory");
            if (structureDirectory.IsValid)
            {
                var strucInfo = structureDirectory.Call("GetStructureInfo", itemId);
                if (strucInfo.IsValid)
                    return true;
            }

            return false;
        }

        /// <summary>
        ///     Add a waypoint
        /// </summary>
        /// <returns></returns>
        public bool AddWaypoint()
        {
            return AddWaypoint(false, false);
        }

        /// <summary>
        ///     Add a waypoint
        /// </summary>
        /// <param name="clearOtherWaypoints"></param>
        /// <param name="firstWaypoint"></param>
        /// <returns></returns>
        public bool AddWaypoint(bool clearOtherWaypoints, bool firstWaypoint)
        {
            if (SolarSystemId == null)
                return false;

            return DirectEve.ThreadedLocalSvcCall("starmap", "SetWaypoint", LocationId, clearOtherWaypoints, firstWaypoint);
        }

        /// <summary>
        ///     Set location as destination
        /// </summary>
        /// <returns></returns>
        public bool SetDestination()
        {
            return AddWaypoint(true, true);
        }

        /// <summary>
        ///     Get a location name
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="locationId"></param>
        /// <returns></returns>
        internal static string GetLocationName(DirectEve directEve, long locationId)
        {
            return (string)directEve.PySharp.Import("__builtin__")
                .Attribute("cfg")
                .Attribute("evelocations")
                .Call("GetIfExists", locationId)
                .Attribute("name");
        }

        #endregion Methods
    }
}