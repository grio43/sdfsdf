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
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectContainerWindow : DirectWindow
    {
        #region Fields

        private static Dictionary<long, DateTime> _nextRefreshDict;

        #endregion Fields

        #region Constructors

        static DirectContainerWindow()
        {
            _nextRefreshDict = new Dictionary<long, DateTime>();
        }

        internal DirectContainerWindow(DirectEve directEve, PyObject pyWindow)
                            : base(directEve, pyWindow)
        {
            //IsReady = (bool) pyWindow.Attribute("startedUp");
            IsOneWay = (bool)pyWindow.Attribute("oneWay");
            ItemId = (long)pyWindow.Attribute("itemID");
            LocationFlag = (int)pyWindow.Attribute("locationFlag");
            HasCapacity = (bool)pyWindow.Attribute("hasCapacity");
            var a = PyWindow.Attribute("currInvID").ToList();
            if (a.Any())
            {
                CurrInvIdName = (string)a.First();
                CurrInvIdItem = (long)a.Last();
            }
        }

        #endregion Constructors

        #region Properties

        public long CurrentContainerId => CurrentContainerIdPy.ToLong();

        public long CurrInvIdItem { get; private set; }

        public string CurrInvIdName { get; private set; }

        public bool HasCapacity { get; private set; }

        public PyObject InvCont => PyWindow.Attribute("invCont");

        public PyObject InvController => PyWindow.Attribute("invController");

        // Loot all button path "content main rightCont bottomRightcont specialActionsCont invLootAllBtn"

        /// <summary>
        /// Internally this will use "MultiAdd". Also take care of possible modals
        /// </summary>
        /// <returns></returns>
        public bool LootAll()
        {
            if (!InvController.IsValid)
            {
                DirectEve.Log(("InvController is not valid."));
                return false;
            }
            var call = InvController["LootAll"];

            if (!call.IsValid)
            {
                DirectEve.Log(("InvController['LootAll'] is not valid."));
                return false;
            }

            return DirectEve.ThreadedCall(call);
        }

        public bool IsOneWay { get; private set; }

        public bool IsReady => !(bool)PyWindow.Attribute("startingup") && !(bool)PyWindow.Attribute("loadingInvCont") &&
                                                                                       !(bool)PyWindow.Attribute("loadingTreeView");

        public long ItemId { get; private set; }
        public int LocationFlag { get; private set; }
        private PyObject CurrentContainerIdPy => InvController.Attribute("invID").GetItemAt(1);

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Close the tree entry with the given ID
        /// </summary>
        /// <param name="entryID"></param>
        /// <returns></returns>
        public bool CloseTreeEntry(long entryID)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var entry = dict.Where(d => (long)d.Key.ToList().Last() == entryID);
            if (entry == null) return false;
            if (entry.Count() != 1) return false;

            //can NOT do threaded calls because these need to be executed in order
            PyWindow.Call("RemoveTreeEntry", entry.First().Value);
            PyWindow.Call("RefreshTree");
            return true;
        }

        public bool ExpandCorpHangarView()
        {
            return ExpandTreeEntry("Corporation hangars");
        }

        /// <summary>
        ///     Get a list of IDs as longs associated with each entry of a inventory tree
        /// </summary>
        /// <param name="itemsOnly">only look for wrecks and containers</param>
        /// <returns>List of IDs of the inventory tree</returns>
        public List<long> GetIdsFromTree(bool itemsOnly = true)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var result = new List<long>();
            foreach (var treeItem in dict.Keys)
            {
                var invIDName = (string)treeItem.ToList().First();
                var invID = (long)treeItem.ToList().Last();
                if (itemsOnly)
                {
                    if (invIDName.Contains("Item"))
                        result.Add(invID);
                }
                else
                {
                    result.Add(invID);
                }
            }

            return result;
        }

        public bool IsPrimary()
        {
            return Guid == "form.InventoryPrimary";
        }

        /// <summary>
        ///     Open the current container view in a new window
        /// </summary>
        /// <returns></returns>
        public bool OpenAsSecondary()
        {
            var invID = PyWindow.Attribute("currInvID");
            //check if it's already open, in that case do nothing.
            var windows = DirectEve.Windows.OfType<DirectContainerWindow>();
            var lookForInvID = (string)invID.ToList().First();
            var alreadyOpened = windows.FirstOrDefault(w => w.Name.Contains(lookForInvID) && !w.IsPrimary());
            if (alreadyOpened != null)
                return true;

            var form = PySharp.Import("form");
            var keywords = new Dictionary<string, object>();
            keywords.Add("invID", invID);
            keywords.Add("usePrimary", false);
            return DirectEve.ThreadedCallWithKeywords(form.Attribute("Inventory").Attribute("OpenOrShow"), keywords);
        }

        public void RefreshInvWindowCont()
        {
            if (!InvController.IsValid)
                DirectEve.Log($"InvController is not valid.");

            if (!CurrentContainerIdPy.IsValid)
                DirectEve.Log($"CurrentContainerId is not valid.");

            var id = CurrentContainerId;
            //DirectEve.Log($"Current inv cont id is {id}.");
            if (_nextRefreshDict.TryGetValue(id, out var dt) && dt > DateTime.UtcNow)
            {
                DirectEve.Log($"Can't refresh container window yet.");
                return;
            }
            _nextRefreshDict[id] = DateTime.UtcNow.AddSeconds(new Random().Next(2, 4)); ;
            if (InvCont.IsValid)
            {
                DirectEve.Log($"Refreshing container.");
                DirectEve.ThreadedCall(InvCont.Attribute("Refresh"));
            }
        }

        /// <summary>
        ///     Select the tree entry with the given name and ID
        /// </summary>
        /// <param name="entryName"></param>
        /// <param name="entryID"></param>
        /// <returns></returns>
        public bool SelectTreeEntry(string entryName, long entryID, bool usePrimary = true, bool async = true)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var entry = dict.Where(d => (string)d.Key.ToList().First() == entryName && (long)d.Key.ToList().Last() == entryID);
            if (entry == null) return false;
            if (entry.Count() != 1) return false;

            if (!async)
            {
                PyWindow.Call("OpenOrShow", entry.First().Key, usePrimary);
                return true;
            }

            return DirectEve.ThreadedCall(PyWindow.Attribute("OpenOrShow"), entry.First().Key, usePrimary);
        }

        /// <summary>
        ///     Select the tree entry with the given ID
        /// </summary>
        /// <param name="entryID"></param>
        /// <returns></returns>
        public bool SelectTreeEntryByID(long entryID, bool usePrimary = true, bool async = true)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var entry = dict.Where(d => (long)d.Key.ToList().Last() == entryID);
            if (entry == null) return false;
            //if (entry.Count() != 1) return false;
            if (!async)
            {
                PyWindow.Call("OpenOrShow", entry.First().Key, usePrimary);
                return true;
            }
            return DirectEve.ThreadedCall(PyWindow.Attribute("OpenOrShow"), entry.First().Key, usePrimary);
        }

        /// <summary>
        ///     Select the first tree entry with the given name
        /// </summary>
        /// <param name="entryName"></param>
        /// <returns></returns>
        public bool SelectTreeEntryByName(string entryName, bool usePrimary = true, bool async = true)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var entry = dict.Where(d => (string)d.Key.ToList().First() == entryName);
            if (entry == null) return false;
            if (entry.Count() <= 0) return false;

            if (!async)
            {
                PyWindow.Call("OpenOrShow", entry.First().Key, usePrimary);
                return true;
            }

            return DirectEve.ThreadedCall(PyWindow.Attribute("OpenOrShow"), entry.First().Key, usePrimary);
        }

        /// <summary>
        ///     Expand the tree entry with the given name and ID
        /// </summary>
        /// <param name="entryName"></param>
        /// <returns></returns>
        internal bool ExpandTreeEntry(string entryName)
        {
            var dict = PyWindow.Attribute("treeEntryByID").ToDictionary();
            var entry = dict.Where(d => (string)d.Key.ToList().First() == entryName);
            if (entry == null) return false;
            if (entry.Count() != 1) return false;

            entry.First().Value.Call("ExpandFromRoot");
            PyWindow.Call("RefreshTree");

            return true;
        }

        #endregion Methods
    }
}