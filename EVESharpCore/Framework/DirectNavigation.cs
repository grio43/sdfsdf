// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectNavigation : DirectObject
    {
        #region Constructors

        internal DirectNavigation(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Methods

        public DirectSolarSystem GetDestinationSolarSystem
        {
            get
            {
                var path = GetDestinationPath();
                if (path.Count > 1)
                {
                    var dest = path.Last();
                    var location = GetLocation(dest);
                    if (location.SolarSystemId.HasValue && location.SolarSystemId.Value <= int.MaxValue)
                    {
                        if (DirectEve.SolarSystems.TryGetValue((int)location.SolarSystemId, out var ss))
                        {
                            return ss;
                        }
                    }
                }
                return null;
            }
        }

        /// <summary>
        ///     Return destination path (locationId's only)
        /// </summary>
        /// <returns></returns>
        public List<long> GetDestinationPath()
        {
            return DirectEve.GetLocalSvc("starmap").Attribute("destinationPath").ToList<long>();
        }

        /// <summary>
        ///     Returns a location based on locationId
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public DirectLocation GetLocation(long locationId)
        {
            return DirectLocation.GetLocation(DirectEve, locationId);
        }

        /// <summary>
        ///     Returns location name based on locationId
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        public string GetLocationName(long locationId)
        {
            return DirectLocation.GetLocationName(DirectEve, locationId);
        }

        /// <summary>
        ///     Set destination to locationId
        /// </summary>
        /// <param name="locationId"></param>
        /// <returns></returns>
        /// <remarks>
        ///     GetLocation is used to find the actual solar system
        /// </remarks>
        public bool SetDestination(long locationId)
        {
            return GetLocation(locationId).SetDestination();
        }

        /// <summary>
        ///     Set destination to locationId without actually retrieving the directLocation ~ CPU Intensive
        /// </summary>
        /// <param name="locationId"></param>
        /// <param name="directEve"></param>
        /// <returns></returns>
        internal static bool SetDestination(long locationId, DirectEve directEve)
        {
            return directEve.ThreadedLocalSvcCall("starmap", "SetWaypoint", locationId, true, true);
        }

        #endregion Methods
    }
}