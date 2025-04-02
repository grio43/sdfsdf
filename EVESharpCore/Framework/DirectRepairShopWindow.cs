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

    public class DirectRepairShopWindow : DirectWindow
    {
        #region Constructors

        internal DirectRepairShopWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Methods

        public string AvgDamage()
        {
            try
            {
                return (string)PyWindow.Attribute("avgDamageLabel").Attribute("text");
            }
            catch
            {
                return "";
            }
        }

        public List<PyObject> GetAll()
        {
            return PyWindow.Call("GetAll").ToList<PyObject>();
        }

        public List<PyObject> GetSelected()
        {
            return PyWindow.Call("GetSelected").ToList<PyObject>();
        }

        public bool IsItemRepairable(DirectItem i)
        {
            var r = PySharp.Import("repair");
            if (r.IsValid)
                return r.Call("IsRepairable", i.PyItem).ToBool();
            return false;
        }

        public bool QuoteItems()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("QuoteItems"));
        }

        public bool RepairAll()
        {
            if (DirectEve.Interval(4500, 5000))
            {
                return DirectEve.ThreadedCall(PyWindow.Attribute("RepairAll"));
            }
            return false;
        }

        public bool RepairItems(List<DirectItem> items)
        {
            var PyItems = items.Where(i => IsItemRepairable(i)).Select(i => i.PyItem);
            if (PyItems.Any())
                return DirectEve.ThreadedCall(PyWindow.Attribute("DisplayRepairQuote"), PyItems);
            return false;
        }

        // OpenWindow -> SelectAll() -> GetSelected() > 0 -> QuoteItems() -> GetAll() > 0 -> RepairAll()

        public bool SelectAll()
        {
            return DirectEve.ThreadedCall(PyWindow.Attribute("scroll").Attribute("SelectAll"));
        }

        #endregion Methods
    }
}