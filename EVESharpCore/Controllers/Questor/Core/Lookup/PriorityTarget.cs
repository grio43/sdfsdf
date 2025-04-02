// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;

namespace EVESharpCore.Controllers.Questor.Core.Lookup
{
    public class PriorityTarget
    {
        #region Properties

        public DronePriority DronePriority { get; set; }
        public EntityCache Entity => ESCache.Instance.EntityById(EntityID);
        public bool EntityExist => Entity != null;
        public long EntityID { get; set; }

        public string Name { get; set; }

        public WeaponPriority WeaponPriority { get; set; }

        #endregion Properties
    }
}