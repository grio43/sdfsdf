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

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectLoyaltyPointOfferRequiredItem : DirectInvType
    {
        #region Constructors

        internal DirectLoyaltyPointOfferRequiredItem(DirectEve directEve, PyObject item) : base(directEve)
        {
            TypeId = (int)item.GetItemAt(0);
            Quantity = (long)item.GetItemAt(1);
        }

        #endregion Constructors

        #region Properties

        public long Quantity { get; private set; }

        #endregion Properties
    }
}