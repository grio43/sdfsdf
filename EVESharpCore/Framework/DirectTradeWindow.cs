﻿// ------------------------------------------------------------------------------
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

    public class DirectTradeWindow : DirectWindow
    {
        #region Constructors

        internal DirectTradeWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            MyAccepted = (int)PyWindow.Attribute("sr").Attribute("myAccept").Attribute("state") == 1;
            HerAccepted = (int)PyWindow.Attribute("sr").Attribute("herAccept").Attribute("state") == 1;
            HerCharacterId = (int)PyWindow.Attribute("sr").Attribute("herinfo").Attribute("ownerID");
            HerName = (string)PyWindow.Attribute("sr").Attribute("herinfo").Attribute("ownerName").ToUnicodeString();
            MyOfferedIsk = (string)PyWindow.Attribute("sr").Attribute("myMoney").Attribute("text").ToUnicodeString();
            HerOfferedIsk = (string)PyWindow.Attribute("sr").Attribute("herMoney").Attribute("text").ToUnicodeString();
        }

        #endregion Constructors

        #region Properties

        public bool HerAccepted { get; internal set; }
        public int HerCharacterId { get; internal set; }
        public string HerName { get; internal set; }
        public string HerOfferedIsk { get; internal set; }

        public List<DirectItem> HerTradeItems
        {
            get
            {
                var items = new List<DirectItem>();
                var pyItems = PyWindow.Attribute("sr").Attribute("her").Attribute("items").ToList();
                foreach (var pyItem in pyItems)
                {
                    if (!pyItem.IsValid) //the list ends with 2x None
                        continue;

                    var item = new DirectItem(DirectEve);
                    item.PyItem = pyItem;
                    items.Add(item);
                }

                return items;
            }
        }

        public bool MyAccepted { get; internal set; }
        public string MyOfferedIsk { get; internal set; }

        public List<DirectItem> MyTradeItems
        {
            get
            {
                var items = new List<DirectItem>();
                var pyItems = PyWindow.Attribute("sr").Attribute("my").Attribute("items").ToList();
                foreach (var pyItem in pyItems)
                {
                    if (!pyItem.IsValid) //the list ends with 2x None
                        continue;

                    var item = new DirectItem(DirectEve);
                    item.PyItem = pyItem;
                    items.Add(item);
                }

                return items;
            }
        }

        #endregion Properties

        #region Methods

        public bool Add(DirectItem item)
        {
            if (item.LocationId == -1)
                return false;

            //This method instead of _AddItem to prevent quantity popup
            return DirectEve.ThreadedCall(PyWindow.Attribute("sr").Attribute("my").Attribute("invController").Attribute("_BaseInvContainer__AddItem"),
                item.ItemId, item.LocationId, item.Quantity);
        }

        public bool OfferMoney(double amount)
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("tradeSession").Attribute("OfferMoney"), amount);
        }

        public bool ToggleAccept()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("OnClickAccept"));
        }

        #endregion Methods
    }
}