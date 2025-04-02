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

    public class DirectLoyaltyPointOffer : DirectInvType
    {
        #region Fields

        private PyObject _pyOfferId, _offer;

        #endregion Fields

        #region Constructors

        internal DirectLoyaltyPointOffer(DirectEve directEve, PyObject offer) : base(directEve)
        {
            TypeId = (int)offer.Attribute("typeID");
            IskCost = (long)offer.Attribute("iskCost");
            LoyaltyPointCost = (long)offer.Attribute("lpCost");
            Quantity = (long)offer.Attribute("qty");
            _pyOfferId = offer.Attribute("offerID");
            OfferId = (int)_pyOfferId;
            _offer = offer;

            RequiredItems = new List<DirectLoyaltyPointOfferRequiredItem>();
            foreach (var item in offer.Attribute("reqItems").ToList())
                RequiredItems.Add(new DirectLoyaltyPointOfferRequiredItem(directEve, item));
        }

        #endregion Constructors

        #region Properties

        public long IskCost { get; private set; }
        public long LoyaltyPointCost { get; private set; }
        public int OfferId { get; private set; }
        public long Quantity { get; private set; }
        public List<DirectLoyaltyPointOfferRequiredItem> RequiredItems { get; private set; }

        #endregion Properties

        /*public bool AcceptOffer()
        {
            if (!_pyOfferId.IsValid)
                return false;

            var corpId = DirectEve.GetLocalSvc("lpstore").Attribute("cache").Attribute("corpID");
            if (!corpId.IsValid)
                return false;

            // Dangerous shitz: Get the RemoteSvc and call TakeOffer
            var takeOffer = PySharp.Import("__builtin__").Attribute("sm").Call("RemoteSvc", "LPSvc").Attribute("TakeOffer");
            return DirectEve.ThreadedCall(takeOffer, corpId, _pyOfferId);
        }*/

        #region Methods

        public bool AcceptOfferFromWindow(int quantity = 1)
        {
            return DirectEve.ThreadedCall(DirectEve.GetLocalSvc("lpstore").Attribute("AcceptOffer"), _offer, quantity);
        }

        #endregion Methods
    }
}