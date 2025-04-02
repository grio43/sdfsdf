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

    public class DirectLoyaltyPointStoreWindow : DirectWindow
    {
        #region Fields

        private List<DirectLoyaltyPointOffer> _offers;

        #endregion Fields

        #region Constructors

        internal DirectLoyaltyPointStoreWindow(DirectEve directEve, PyObject pyWindow) :
            base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Properties

        public long LoyaltyPoints
        {
            get
            {
                var corpId = GetCorpId();
                if (corpId > 0)
                    return (long)DirectEve.GetLocalSvc("lpstore").Attribute("cache").Attribute("lps").ToDictionary<int>()[corpId].ToLong();
                return -1;
            }
        }

        public List<DirectLoyaltyPointOffer> Offers
        {
            get
            {
                if (_offers == null)
                {
                    var corpId = GetCorpId();
                    if (corpId > 0)
                    {
                        _offers = new List<DirectLoyaltyPointOffer>();
                        foreach (var offer in DirectEve.GetLocalSvc("lpstore").Attribute("cache").Attribute("offers").ToDictionary<int>()[corpId].ToList())
                            _offers.Add(new DirectLoyaltyPointOffer(DirectEve, offer));
                    }
                }

                return _offers;
            }
        }

        #endregion Properties

        #region Methods

        public int GetCorpId()
        {
            var offers = DirectEve.GetLocalSvc("lpstore").Attribute("cache").Attribute("offers");
            if (offers.IsValid)
                return offers.ToDictionary<int>().Aggregate((l, r) => l.Value.ToList().Count > r.Value.ToList().Count ? l : r).Key;
            return -1;
        }

        public bool RefreshLoyaltyPoints()
        {
            // Delete saved LPs
            var corpId = GetCorpId();
            if (corpId > 0)
            {
                DirectEve.GetLocalSvc("lpstore").Attribute("cache").Attribute("lps").Clear();
                return DirectEve.ThreadedLocalSvcCall("lpstore", "GetMyLPs", corpId);
            }

            return false;
        }

        #endregion Methods
    }
}