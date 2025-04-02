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
using System.Windows.Forms;
using ServiceStack.IO;
using SC::SharedComponents.Utility;
using System;
using EVESharpCore.Controllers;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectItem : DirectInvType
    {
        #region Fields

        private DirectItemAttributes _attributes;

        private int? _flagId;
        private string _givenName;
        private bool? _isSingleton;
        private long? _itemId;
        private long? _locationId;
        private List<DirectItem> _materials;
        private int? _ownerId;
        private PyObject _pyItem;
        private PyObject? _pyDynamicItem;
        private int? _quantity;
        private int? _stacksize;

        #endregion Fields

        #region Constructors

        internal DirectItem(DirectEve directEve) : base(directEve)
        {
            PyItem = PySharp.PyZero;
        }

        #endregion Constructors

        #region Properties

        public DirectItemAttributes Attributes
        {
            get
            {
                if (_attributes == null && PyItem.IsValid)
                {
                    var pyItemId = PyItem.Attribute("itemID");
                    if (pyItemId.IsValid)
                        _attributes = new DirectItemAttributes(DirectEve, pyItemId);
                }

                _attributes = _attributes ?? new DirectItemAttributes(DirectEve, ItemId);
                return _attributes;
            }
        }

        public bool LaunchForSelf()
        {
            if (this.GroupId == 1250)
            {
                var call = DirectEve.GetLocalSvc("menu")["LaunchForSelf"];
                if (call.IsValid && this.PyItem.IsValid)
                {
                    DirectEve.ThreadedCall(call, new List<PyObject>() { this.PyItem });
                }
                return true;
            }

            DirectEve.Log("Couldnt launch for self. Probably wrong type.");
            return false;
        }

        /// <summary>
        ///     Consume Booster
        /// </summary>
        /// <returns></returns>
        public bool ConsumeBooster()
        {
            if (GetBoosterConsumbableUntil() <= DateTime.UtcNow)
                return false;

            if (GroupId != (int)Group.Booster)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            PyObject consumeBooster = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("ConsumeBooster");
            return DirectEve.ThreadedCall(consumeBooster, new List<PyObject> { PyItem });
        }

        public bool PlugInImplant()
        {
            if (CategoryId != (int)CategoryID.Implant)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            PyObject plugInImplant = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("PlugInImplant");
            return DirectEve.ThreadedCall(plugInImplant, new List<PyObject> { PyItem });
        }

        public int FlagId
        {
            get
            {
                if (!_flagId.HasValue)
                    _flagId = (int)PyItem.Attribute("flagID");

                return _flagId.Value;
            }
            internal set => _flagId = value;
        }

        public string GivenName
        {
            get
            {
                if (_givenName == null)
                    _givenName = DirectEve.GetLocationName(ItemId);

                return _givenName;
            }
        }

        public bool IsCommonMissionItem => TypeId == 28260 || TypeId == 3814
                                                           || TypeId == 2076 || TypeId == 25373
                                                           || TypeId == 3810 || TypeId == 24576
                                                           || TypeId == 24766;

        public bool IsAbyssalLootItem => DirectEve.GetAbyssLootGroups().Contains(GroupId);

        public bool IsContraband
        {
            get
            {
                var result = false;
                result |= GroupId == (int)Group.Drugs;
                result |= GroupId == (int)Group.ToxicWaste;
                result |= TypeId == (int)TypeID.Slaves;
                result |= TypeId == (int)TypeID.Small_Arms;
                result |= TypeId == (int)TypeID.Ectoplasm;
                return result;
            }
        }

        public double? IskPerM3
        {
            get
            {
                if (AveragePrice() > 0 && Volume > 0)
                    return AveragePrice() / Volume;
                return 0;
            }
        }

        public bool IsSingleton
        {
            get
            {
                if (!_isSingleton.HasValue)
                    _isSingleton = (bool)PyItem.Attribute("singleton");

                if (Quantity < 0 && Stacksize == 1)
                    _isSingleton = true;

                return _isSingleton.Value;
            }
            internal set => _isSingleton = value;
        }

        public long ItemId
        {
            get
            {
                if (!_itemId.HasValue)
                    _itemId = (long)PyItem.Attribute("itemID");

                return _itemId.Value;
            }
            internal set => _itemId = value;
        }

        public long LocationId
        {
            get
            {
                if (!_locationId.HasValue)
                    _locationId = (long)PyItem.Attribute("locationID");

                return _locationId.Value;
            }
            internal set => _locationId = value;
        }

        public List<DirectItem> Materials
        {
            get
            {
                if (_materials == null)
                {
                    _materials = new List<DirectItem>();
                    foreach (var pyMaterial in PySharp.Import("__builtin__").Attribute("cfg").Attribute("invtypematerials").DictionaryItem(TypeId).ToList())
                    {
                        var material = new DirectItem(DirectEve);
                        material.ItemId = -1;
                        material.Stacksize = -1;
                        material.OwnerId = -1;
                        material.LocationId = -1;
                        material.FlagId = 0;
                        material.IsSingleton = false;
                        material.TypeId = (int)pyMaterial.Attribute("materialTypeID");
                        material.Quantity = (int)pyMaterial.Attribute("quantity");
                        _materials.Add(material);
                    }
                }

                return _materials;
            }
        }

        public int OwnerId
        {
            get
            {
                if (!_ownerId.HasValue)
                    _ownerId = (int)PyItem.Attribute("ownerID");

                return _ownerId.Value;
            }
            internal set => _ownerId = value;
        }

        private Vec3? _droneDamageState = null;

        /// <summary>
        /// // shield, armor, hull (x,y,z) range 0 ... 1.0
        /// </summary>
        /// <returns></returns>
        public Vec3? GetDroneInBayDamageState()
        {
            if (DirectEve._entityHealthPercOverrides.TryGetValue(this.ItemId, out var res))
            {
                return new Vec3(res.Item1, res.Item2, res.Item3);
            }

            if (_droneDamageState == null)
            {
                var state = DirectEve.GetLocalSvc("tactical")["inBayDroneDamageTracker"]["droneDamageStatesByDroneIDs"];
                if (state.IsValid)
                {
                    var dict = state.ToDictionary<long>();
                    if (dict.TryGetValue(this.ItemId, out var result))
                    {
                        var timestamp = result["timestamp"].ToDateTime();
                        var msSince = (DateTime.UtcNow - timestamp).TotalMilliseconds;
                        var shieldHealth = (double)result["shieldHealth"].ToDouble(); // 0 .. 1.0
                        var rechargeRate = TryGet<float>("shieldRechargeRate"); // milliseconds
                        //var sMax = TryGet<float>("shieldCapacity");
                        var sMax = TotalShield.Value;
                        var sCurrent = sMax * shieldHealth;
                        var timeDiffMilliSeconds = msSince;
                        var rechargeTimeMilliSeconds = rechargeRate;
                        var shieldAtOffset = sMax * Math.Pow((1 + Math.Exp(5 * (-timeDiffMilliSeconds / rechargeTimeMilliSeconds)) * (Math.Sqrt(sCurrent / sMax) - 1)), 2f);
                        var percAtOffset = (shieldAtOffset / sMax);

                        //DirectEve.Log($"sMax {sMax} shieldHealth {shieldHealth} sCurrent {sCurrent} timeDiffMilliSeconds {timeDiffMilliSeconds} rechargeTimeMilliSeconds {rechargeTimeMilliSeconds} shieldAtOffset {shieldAtOffset} percAtOffset {percAtOffset}");

                        shieldHealth = percAtOffset;
                        var armorHealth = result["armorHealth"].ToFloat();
                        var hullHealth = result["hullHealth"].ToFloat();
                        //DirectEve.Log($"Timestamp {timestamp} msSince {msSince} rechargeRate {rechargeRate}");
                        _droneDamageState = new Vec3(shieldHealth, armorHealth, hullHealth);
                    }
                }
            }
            return _droneDamageState;
        }

        public static HashSet<long> RequestedDynamicItems { get; set; } = new HashSet<long>();
        public static HashSet<long> FinishedRemoteCallDynamicItems { get; set; } = new HashSet<long>();

        public static bool AllDynamicItemsLoaded => RequestedDynamicItems.Count == 0;

        public PyObject DynamicItem
        {
            get
            {
                if (_pyDynamicItem != null)
                    return _pyDynamicItem;

                if (IsDynamicItem)
                {
                    var dynamicItemSvc = DirectEve.GetLocalSvc("dynamicItemSvc");
                    if (!dynamicItemSvc.IsValid)
                        return _pyDynamicItem;

                    _pyDynamicItem = dynamicItemSvc["dynamicItemCache"].DictionaryItem(this.ItemId);

                    if ((_pyDynamicItem == null || !_pyDynamicItem.IsValid) 
                        && !FinishedRemoteCallDynamicItems.Contains(this.ItemId) 
                        && RequestedDynamicItems.Add(this.ItemId))
                    {
                        //DirectEve.Log($"Retrieving dynamic item settings for ItemId [{this.ItemId}]");
                        //DirectEve.ThreadedCall(dynamicItemSvc["GetDynamicItem"], this.ItemId); 
                        return _pyDynamicItem;
                    }
                }
                return _pyDynamicItem;
            }
        }

        public bool IsDynamicInfoLoaded => IsDynamicItem && DynamicItem != null;

        public bool IsDynamicItem
        {
            get
            {
                var evetypes = PySharp.Import("evetypes");
                return evetypes.Call("IsDynamicType", this.TypeId).ToBool();

                //return this.TryGet<bool>("isDynamicType", true);
            }
        }

        public override T TryGet<T>(string keyname)
        {

            if (IsDynamicItem && DynamicItem != null)
            {
                var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                var value = DirectEve.GetInvType(sourceTypeID).TryGet<T>(keyname);
                return value;
            }

            return base.TryGet<T>(keyname);
        }


        public DirectInvType OrignalDynamicItem
        {
            get
            {
                if (IsDynamicItem && DynamicItem != null)
                {
                    var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                    return DirectEve.GetInvType(sourceTypeID);
                }
                return null;
            }
        }

        public int Quantity
        {
            get
            {

                if (!_quantity.HasValue)
                    _quantity = (int)PyItem.Attribute("quantity");

                return _quantity.Value;
            }
            internal set => _quantity = value;
        }

        public int Stacksize
        {
            get
            {
                if (!_stacksize.HasValue)
                    _stacksize = (int)PyItem.Attribute("stacksize");

                return _stacksize.Value;
            }
            internal set => _stacksize = value;
        }

        public double TotalVolume => Volume * Quantity;

        public bool IsBlueprintOriginal => Quantity == -1 && CategoryId == (int)CategoryID.Blueprint;

        public bool IsBlueprintCopy => Quantity == -2 && CategoryId == (int)CategoryID.Blueprint;

        public bool IsBlueprint => IsBlueprintOriginal || IsBlueprintCopy;

        private PyObject _itemChecker = null;

        public PyObject ItemChecker => _itemChecker ??= PySharp.Import("menucheckers").Call("ItemChecker", PyItem);

        public bool IsTrashable()
        {
            if (ItemChecker.IsValid)
            {
                return ItemChecker.Call("OfferTrashIt").ToBool();
            }
            return false;
        }


        public new double Volume
        {
            get
            {
                var vol = base.Volume;

                if (TypeId == 3468) // plastic wraps
                    return vol;

                if (!IsSingleton) // get packaged vol
                {
                    var group = GetPackagedVolOverrideGroup(GroupId, DirectEve);
                    if (group.HasValue)
                        return group.Value;

                    var type = GetPackagedVolOverrideType(TypeId, DirectEve);
                    if (type.HasValue)
                        return type.Value;

                }

                return vol;
            }
        }

        private static Dictionary<int, float> _packagedOverridePerGroupId;

        private static Dictionary<int, float> _packagedOverridePerTypeId;

        private static float? GetPackagedVolOverrideGroup(int groupId, DirectEve de)
        {
            if (_packagedOverridePerGroupId == null)
            {
                _packagedOverridePerGroupId = new Dictionary<int, float>();
                //inventorycommon.util.packagedVolumeOverridesPerGroup
                var invCommon = de.PySharp.Import("inventorycommon");
                var dict = invCommon.Attribute("util").Attribute("packagedVolumeOverridesPerGroup").ToDictionary<int>();
                foreach (var kv in dict)
                {
                    _packagedOverridePerGroupId.Add(kv.Key, kv.Value.ToFloat());
                }
            }

            if (_packagedOverridePerGroupId.TryGetValue(groupId, out var val))
            {
                return val;
            }

            return null;
        }

        private static float? GetPackagedVolOverrideType(int typeId, DirectEve de)
        {
            if (_packagedOverridePerTypeId == null)
            {
                _packagedOverridePerTypeId = new Dictionary<int, float>();
                //inventorycommon.util.packagedVolumeOverridesPerType
                var invCommon = de.PySharp.Import("inventorycommon");
                var dict = invCommon.Attribute("util").Attribute("packagedVolumeOverridesPerType").ToDictionary<int>();
                foreach (var kv in dict)
                {
                    _packagedOverridePerTypeId.Add(kv.Key, kv.Value.ToFloat());
                }
            }

            if (_packagedOverridePerTypeId.TryGetValue(typeId, out var val))
            {
                return val;
            }

            return null;
        }


        internal PyObject PyItem
        {
            get => _pyItem;
            set
            {
                _pyItem = value;

                if (_pyItem != null && _pyItem.IsValid)
                    TypeId = (int)_pyItem.Attribute("typeID");
            }
        }

        #endregion Properties

        #region Methods

        public bool ActivateAbyssalKey()
        {
            if (GroupId != (int)Group.AbyssalDeadspaceFilament)
                return false;

            PyObject pyActivateAbyssalKey = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("ActivateAbyssalKey");

            if (pyActivateAbyssalKey == null || !pyActivateAbyssalKey.IsValid)
                return false;

            return DirectEve.ThreadedCall(pyActivateAbyssalKey, PyItem);
        }

        /// <summary>
        ///     Activate this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip
        /// </remarks>
        public bool ActivateShip()
        {
            DirectSession.SetSessionNextSessionReady();

            if (LocationId != DirectEve.Session.StationId)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            return DirectEve.ThreadedLocalSvcCall("station", "TryActivateShip", PyItem);
        }

        /// <summary>
        ///     Assembles this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip and is not
        ///     allready assembled
        /// </remarks>
        public bool AssembleShip()
        {
            if (LocationId != DirectEve.Session.StationId)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            if (IsSingleton)
                return false;

            var AssembleShip = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("AssembleShip");
            return DirectEve.ThreadedCall(AssembleShip, new List<PyObject>() { PyItem });
        }

        /// <summary>
        ///     Board this ship from a ship maintanance bay!
        /// </summary>
        /// <returns>false if entity is player or out of range</returns>
        public bool BoardShipFromShipMaintBay()
        {
            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            if (IsSingleton)
                return false;

            var Board = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Board");
            return DirectEve.ThreadedCall(Board, ItemId);
        }

        /// <summary>
        ///     Fit this item to your ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the selected item is not of CategoryModule
        /// </remarks>
        public bool FitToActiveShip()
        {
            if (CategoryId != (int)DirectEve.Const.CategoryModule)
                return false;

            var data = new List<PyObject>();
            data.Add(PyItem);

            return DirectEve.ThreadedLocalSvcCall("menu", "TryFit", data);
        }

        /// <summary>
        ///     Inject the skill into your brain
        /// </summary>
        /// <returns></returns>
        public bool InjectSkill()
        {
            if (CategoryId != (int)DirectEve.Const.CategorySkill)
                return false;

            if (!DirectEve.Session.StationId.HasValue || LocationId != DirectEve.Session.StationId)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            var InjectSkillIntoBrain = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("InjectSkillIntoBrain");
            return DirectEve.ThreadedCall(InjectSkillIntoBrain, new List<PyObject> { PyItem });
        }

        /// <summary>
        ///     Leave this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip
        /// </remarks>
        public bool LeaveShip()
        {
            if (ItemId != DirectEve.Session.ShipId)
                return false;

            if (Quantity > 0)
                return false;

            //if (LocationId != DirectEve.Session.StationId)
            //    return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            return DirectEve.ThreadedLocalSvcCall("station", "TryLeaveShip", PyItem);
        }

        public bool MoveToPlexVault()
        {
            if (TypeId != 29668 && TypeId != 44992)
                return false;

            var redeemCurrency = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("RedeemCurrency");

            if (redeemCurrency == null || !redeemCurrency.IsValid)
                return false;

            return DirectEve.ThreadedCall(redeemCurrency, PyItem, Stacksize);
        }

        /// <summary>
        ///     Open up the quick-buy window to buy more of this item
        /// </summary>
        /// <returns></returns>
        public bool QuickBuy()
        {
            //if (!DirectEve.HasSupportInstances())
            //{
            //    DirectEve.Log("DirectEve: Error: This method requires a support instance.");
            //    return false;
            //}
            return DirectEve.ThreadedLocalSvcCall("marketutils", "Buy", TypeId, PyItem);
        }

        /// <summary>
        ///     Open up the quick-sell window to sell this item
        /// </summary>
        /// <returns></returns>
        public bool QuickSell()
        {
            //if (!DirectEve.HasSupportInstances())
            //{
            //    DirectEve.Log("DirectEve: Error: This method requires a support instance.");
            //    return false;
            //}
            return DirectEve.ThreadedLocalSvcCall("marketutils", "Sell", TypeId, PyItem);
        }

        /// <summary>
        ///     Set the name of an item.  Be sure to call DirectEve.ScatterEvent("OnItemNameChange") shortly after calling this
        ///     function.  Do not call ScatterEvent from the same frame!!
        /// </summary>
        /// <remarks>See menuSvc.SetName</remarks>
        /// <param name="name">The new name for this item.</param>
        /// <returns>true if successful.  false if not.</returns>
        public bool SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip && name.Length > 20)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryStructure && name.Length > 32)
                return false;

            if (name.Length > 100)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            var pyCall = DirectEve.GetLocalSvc("invCache").Call("GetInventoryMgr").Attribute("SetLabel");
            return DirectEve.ThreadedCall(pyCall, ItemId, name.Replace('\n', ' '));
        }

        /// <summary>
        ///     Drop items into People and Places
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="bookmarks"></param>
        /// <returns></returns>
        internal static bool DropInPlaces(DirectEve directEve, IEnumerable<DirectItem> bookmarks)
        {
            var data = new List<PyObject>();
            foreach (var bookmark in bookmarks)
                data.Add(directEve.PySharp.Import("eve.client.script.ui.util.uix").Call("GetItemData", bookmark.PyItem, "list"));

            return directEve.ThreadedLocalSvcCall("addressbook", "DropInPlaces", PySharp.PyNone, data);
        }

        internal static List<DirectItem> GetItems(DirectEve directEve, PyObject inventory, PyObject flag)
        {
            var items = new List<DirectItem>();
            var cachedItems = inventory.Attribute("cachedItems").ToDictionary();
            var pyItems = cachedItems.Values;

            foreach (var pyItem in pyItems)
            {
                var item = new DirectItem(directEve);
                item.PyItem = pyItem;

                // Do not add the item if the flags do not coincide
                if (flag.IsValid && (int)flag != item.FlagId)
                    continue;

                items.Add(item);
            }

            return items;
        }

        internal static bool RefreshItems(DirectEve directEve, PyObject inventory, PyObject flag)
        {
            return directEve.ThreadedCall(inventory.Attribute("InvalidateCache"));
        }

        #endregion Methods

        //        public bool ActivatePLEX()
        //        {
        //            if (TypeId != 29668)
        //                return false;
        //
        //            var ApplyPilotLicence = PySharp.Import("__builtin__").Attribute("sm").Call("RemoteSvc", "userSvc").Attribute("ApplyPilotLicence");
        //            return DirectEve.ThreadedCall(ApplyPilotLicence, ItemId);
        //        }
    }
}