/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Traveller;
using SC::SharedComponents.IPC;
using EveAccount = SC::SharedComponents.EVE.EveAccount;
using WCFClient = SC::SharedComponents.IPC.WCFClient;

namespace EVESharpCore.Controllers.Questor
{
    /// <summary>
    ///     Description of ExampleController
    /// </summary>
    public class BuyAmmoController : BaseController
    {
        #region Constructors

        public BuyAmmoController(Action doneAction) : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            _doneAction = doneAction;
            DependsOn = new List<Type>()
            {
                typeof(DefenseController)
            };
            buyList = new Dictionary<int, int>();
            moveToCargoList = new Dictionary<int, int>();
        }

        #endregion Constructors

        #region Fields

        private Dictionary<int, int> buyList = new Dictionary<int, int>();
        private readonly static int _hoursBetweenAmmoBuy = 10;
        private int _jumps;
        private int _maxAmmoMultiplier = 100;
        private int _maxAvgPriceMultiplier = 4;
        private int _maxBasePriceMultiplier = 16;
        private int _maxStateIterations = 500;
        private readonly static int _minAmmoMultiplier = 20;
        private readonly static int _minimumDroneAmount = 200;
        private Dictionary<int, int> moveToCargoList = new Dictionary<int, int>();
        private DateTime nextAction = DateTime.MinValue;
        private int orderIterations = 0;
        private Dictionary<BuyAmmoState, int> stateIterations = new Dictionary<BuyAmmoState, int>();
        private TravelerDestination _travelerDestination;
        private readonly Action _doneAction;

        #endregion Fields

        #region Properties

        public static BuyAmmoState _state { get; set; } // idle == default

        private bool StateCheckEveryPulse
        {
            get
            {
                if (stateIterations.ContainsKey(_state))
                    stateIterations[_state] = stateIterations[_state] + 1;
                else
                    SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(stateIterations, _state, 1);

                if (stateIterations[_state] >= _maxStateIterations && _state != BuyAmmoState.TravelToDestinationStation &&
                    _state != BuyAmmoState.TravelToHomeSystem)
                {
                    Log("ERROR:  if (stateIterations[state] >= maxStateIterations)");
                    _state = BuyAmmoState.Error;
                    return false;
                }

                return true;
            }
        }

        #endregion Properties

        #region Methods


        public static bool ShouldBuyAmmo()
        {

            if (_state == BuyAmmoState.DisabledForThisSession)
                return false;

            if (!ESCache.Instance.InDockableLocation)
                return false;

            if (ESCache.Instance.EveAccount.LastAmmoBuy.AddHours(_hoursBetweenAmmoBuy) > DateTime.UtcNow)
            {
                Log("We were buying ammo already in the past [" + _hoursBetweenAmmoBuy + "] hours.");
                return false;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                return false;

            if (!ESCache.Instance.Combat.Ammo.Any())
                return false;

            var buy = false;

            foreach (var ammo in ESCache.Instance.Combat.Ammo)
            {
                var totalQuantity = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);
                var minQty = ammo.Quantity * _minAmmoMultiplier;
                if (totalQuantity < minQty)
                {
                    Log("Total ammo amount in hangar type [" + ammo.TypeId + "] [" + totalQuantity + "] Minimum amount [" + minQty +
                        "] We're going to buy ammo.");
                    buy = true;
                    break;
                }
            }

            if (ESCache.Instance.EveAccount.CS.QMS.QS.UseDrones)
            {
                var droneTypeIds = new List<int>
                        {
                            ESCache.Instance.MissionSettings.CurrentDroneTypeId
                        };

                foreach (var factionFtting in ESCache.Instance.MissionSettings.FactionFittings)
                {
                    if (!factionFtting.DronetypeId.HasValue)
                        continue;
                    if (!droneTypeIds.Contains(factionFtting.DronetypeId.Value))
                        droneTypeIds.Add(factionFtting.DronetypeId.Value);
                }

                foreach (var missionFitting in ESCache.Instance.MissionSettings.MissionFittings)
                {
                    if (!missionFitting.DronetypeId.HasValue)
                        continue;
                    if (!droneTypeIds.Contains(missionFitting.DronetypeId.Value))
                        droneTypeIds.Add(missionFitting.DronetypeId.Value);
                }

                foreach (var droneTypeId in droneTypeIds)
                {
                    var totalQuantityDrones = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);
                    if (totalQuantityDrones < _minimumDroneAmount)
                    {
                        Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + _minimumDroneAmount +
                            "] We're going to buy drones of type [" + droneTypeId + "]");
                        buy = true;
                    }
                }
            }

            Log("LastAmmoBuy was on [" + ESCache.Instance.EveAccount.LastAmmoBuy + "]");

            if (buy)
            {
                return true;
            }
            else
            {
                Log("There is still enough ammo avaiable in the itemhangar. No reason to buy ammo.");
                return false;
            }

        }

        public void ProcessState()
        {
            if (nextAction > DateTime.UtcNow)
                return;

            if (!StateCheckEveryPulse)
                return;

            switch (_state)
            {
                case BuyAmmoState.Idle:
                    stateIterations = new Dictionary<BuyAmmoState, int>();
                    _state = BuyAmmoState.ActivateTransportShip;
                    break;

                case BuyAmmoState.ActivateTransportShip:

                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.LastAmmoBuy), DateTime.UtcNow);

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    if (!ESCache.Instance.Combat.Ammo.Any())
                        return;

                    if (ESCache.Instance.DirectEve.GetShipHangar() == null)
                        return;

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName != null &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                    {
                        _state = BuyAmmoState.CreateBuyList;
                        return;
                    }

                    if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GivenName != null &&
                        ESCache.Instance.ActiveShip.GivenName.ToLower() != ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                    {
                        var ships = ESCache.Instance.DirectEve.GetShipHangar().Items;
                        foreach (
                            var ship in ships.Where(ship =>
                                ship.GivenName != null && ship.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                        )
                        {
                            Log("Making [" + ship.GivenName + "] active");
                            ship.ActivateShip();
                            nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                        }
                    }

                    break;

                case BuyAmmoState.CreateBuyList:

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    if (!ESCache.Instance.Combat.Ammo.Any())
                        return;

                    //var invtypes = Cache.Instance.DirectEve.InvTypes;

                    var freeCargo = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;

                    if (ESCache.Instance.CurrentShipsCargo.Capacity == 0)
                    {
                        Log("if(Cache.Instance.CurrentShipsCargo.Capacity == 0)");
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }

                    Log("Current [" + ESCache.Instance.ActiveShip.GivenName + "] Cargo [" + ESCache.Instance.CurrentShipsCargo.Capacity +
                        "] Used Capacity [" +
                        ESCache.Instance.CurrentShipsCargo.UsedCapacity + "] Free Capacity [" + freeCargo + "]");

                    if (ESCache.Instance.EveAccount.CS.QMS.QS.UseDrones)
                    {
                        var droneTypeIds = new List<int>();
                        droneTypeIds.Add(ESCache.Instance.MissionSettings.CurrentDroneTypeId);

                        foreach (var factionFtting in ESCache.Instance.MissionSettings.FactionFittings)
                        {
                            if (!factionFtting.DronetypeId.HasValue)
                                continue;
                            if (!droneTypeIds.Contains(factionFtting.DronetypeId.Value))
                                droneTypeIds.Add(factionFtting.DronetypeId.Value);
                        }

                        foreach (var missionFitting in ESCache.Instance.MissionSettings.MissionFittings)
                        {
                            if (!missionFitting.DronetypeId.HasValue)
                                continue;
                            if (!droneTypeIds.Contains(missionFitting.DronetypeId.Value))
                                droneTypeIds.Add(missionFitting.DronetypeId.Value);
                        }

                        foreach (var droneTypeId in droneTypeIds.Distinct())
                        {
                            var totalQuantityDrones = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == droneTypeId).Sum(i => i.Stacksize);

                            if (totalQuantityDrones < _minimumDroneAmount)
                            {
                                Log("Total drone amount in hangar [" + totalQuantityDrones + "]  Minimum amount [" + _minimumDroneAmount +
                                    "]");
                                SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(buyList, droneTypeId, ESCache.Instance.Drones.BuyAmmoDroneAmmount);
                            }
                        }
                    }

                    if (ESCache.Instance.Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)
                    {
                        Log("ERROR: if (Combat.Ammo.Select(a => a.DamageType).Distinct().Count() != 4)");
                        _state = BuyAmmoState.Error;
                        return;
                    }

                    foreach (var buyListKeyValuePair in buyList.ToList())
                    {
                        if (ESCache.Instance.DirectEve.GetInvType(buyListKeyValuePair.Key) == null)
                        {
                            Log("TypeId [" + buyListKeyValuePair.Key + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                            buyList.Remove(buyListKeyValuePair.Key);
                            continue;
                        }

                        var droneInvType = ESCache.Instance.DirectEve.GetInvType(buyListKeyValuePair.Key);
                        var cargoBefore = freeCargo;
                        freeCargo = freeCargo - buyListKeyValuePair.Value * droneInvType.Volume;
                        Log("Drones, Reducing freeCargo from [" + cargoBefore + "] to [" + freeCargo + "]");
                    }

                    freeCargo = freeCargo * 0.995; // leave 0.5% free space
                    var majorBuySlotUsed = false;
                    foreach (var ammo in ESCache.Instance.Combat.Ammo)
                        try
                        {
                            var totalQuantity = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == ammo.TypeId).Sum(i => i.Stacksize);
                            var minQty = ammo.Quantity * _minAmmoMultiplier;
                            var maxQty = ammo.Quantity * _maxAmmoMultiplier;

                            if (ESCache.Instance.DirectEve.GetInvType(ammo.TypeId) == null)
                            {
                                Log("TypeId [" + ammo.TypeId + "] does not exist in eve invtypes. THIS SHOULD NOT HAPPEN AT ALL.");
                                continue;
                            }

                            var ammoInvType = ESCache.Instance.DirectEve.GetInvType(ammo.TypeId);
                            if (totalQuantity < minQty && !majorBuySlotUsed)
                            {
                                majorBuySlotUsed = true;
                                var ammoBuyAmount = (int)(freeCargo * 0.4 / ammoInvType.Volume); // 40% of the volume for the first missing ammo

                                if (buyList.TryGetValue(ammo.TypeId, out var amountBefore)) SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(buyList, ammo.TypeId, ammoBuyAmount + amountBefore);
                                else SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(buyList, ammo.TypeId, ammoBuyAmount);
                            }
                            else
                            {
                                if (totalQuantity <= maxQty)
                                {
                                    var ammoBuyAmount = (int)(freeCargo * (0.6 / (ESCache.Instance.Combat.Ammo.Count - 1)) / ammoInvType.Volume); // 60% for the rest

                                    if (buyList.TryGetValue(ammo.TypeId, out var amountBefore)) SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(buyList, ammo.TypeId, ammoBuyAmount + amountBefore);
                                    else SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(buyList, ammo.TypeId, ammoBuyAmount);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log("ERROR: foreach(var ammo in Combat.Ammo)");
                            Log("Stacktrace [" + e.StackTrace + "]");
                            _state = BuyAmmoState.Error;
                            return;
                        }

                    Log("Done building the ammoToBuy list:");
                    var z = 0;
                    double totalVolumeBuyList = 0;
                    foreach (var entry in buyList)
                    {
                        var buyInvType = ESCache.Instance.DirectEve.GetInvType(entry.Key);
                        var buyTotalVolume = buyInvType.Volume * entry.Value;
                        z++;

                        Log("[" + z + "] typeID [" + entry.Key + "] amount [" + entry.Value + "] volume [" + buyTotalVolume + "]");
                        totalVolumeBuyList += buyTotalVolume;
                    }

                    var currentShipFreeCargo = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                    Log("CurrentShipFreeCargo [" + currentShipFreeCargo + "] BuyListTotalVolume [" + totalVolumeBuyList + "]");

                    if (currentShipFreeCargo < totalVolumeBuyList)
                    {
                        Log("if(currentShipFreeCargo < totalVolumeBuyList)");
                        _state = BuyAmmoState.Error;
                        return;
                    }

                    _state = BuyAmmoState.TravelToDestinationStation;

                    foreach (var entry in buyList)
                        moveToCargoList.Add(entry.Key, entry.Value);

                    _travelerDestination = new DockableLocationDestination(ESCache.Instance.EveAccount.CS.QMS.BuyAmmoStationID);

                    break;

                case BuyAmmoState.TravelToDestinationStation:

                    if (ESCache.Instance.DirectEve.Session.IsInSpace && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                        return;

                    if (ESCache.Instance.Traveler.Destination != _travelerDestination)
                        ESCache.Instance.Traveler.Destination = _travelerDestination;

                    _jumps = ESCache.Instance.DirectEve.Navigation.GetDestinationPath().Count;

                    ESCache.Instance.Traveler.ProcessState();

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Log("Arrived at destination");
                        _state = BuyAmmoState.BuyAmmo;
                        orderIterations = 0;
                        ESCache.Instance.Traveler.Destination = null;

                        return;
                    }

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                    {
                        if (ESCache.Instance.Traveler.Destination != null)
                            Log("Stopped traveling, traveller threw an error...");

                        ESCache.Instance.Traveler.Destination = null;
                        _state = BuyAmmoState.Error;
                        return;
                    }

                    break;

                case BuyAmmoState.BuyAmmo:

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    // Is there a market window?
                    var marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                    if (!buyList.Any())
                    {
                        // Close the market window if there is one
                        if (marketWindow != null)
                            marketWindow.Close();

                        Log("Finished buying changing state to MoveItemsToCargo");
                        _state = BuyAmmoState.MoveItemsToCargo;
                        return;
                    }

                    var currentBuyListItem = buyList.FirstOrDefault();

                    var ammoTypeId = currentBuyListItem.Key;
                    var ammoQuantity = currentBuyListItem.Value;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    // Do we have the ammo we need in the Item Hangar?

                    if (ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize) >= ammoQuantity)
                    {
                        var ammoItemInHangar = ESCache.Instance.DirectEve.GetItemHangar().Items.FirstOrDefault(i => i.TypeId == ammoTypeId);
                        if (ammoItemInHangar != null)
                            Log("We have [" +
                                ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == ammoTypeId)
                                    .Sum(i => i.Stacksize)
                                    .ToString(CultureInfo.InvariantCulture) +
                                "] " + ammoItemInHangar.TypeName + " in the item hangar.");

                        buyList.Remove(ammoTypeId);
                        return;
                    }

                    // We do not have enough ammo, open the market window
                    if (marketWindow == null)
                    {
                        nextAction = DateTime.UtcNow.AddSeconds(10);

                        Log("Opening market window");

                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        return;
                    }

                    // Wait for the window to become ready
                    if (!marketWindow.IsReady)
                        return;

                    // Are we currently viewing the correct ammo orders?
                    if (marketWindow.DetailTypeId != ammoTypeId)
                    {
                        // No, load the ammo orders
                        marketWindow.LoadTypeId(ammoTypeId);

                        Log("Loading market window");

                        nextAction = DateTime.UtcNow.AddSeconds(10);
                        return;
                    }

                    // Get the median sell price
                    var type = ESCache.Instance.DirectEve.GetInvType(ammoTypeId);

                    var currentAmmoDirectItem = type;
                    double maxPrice = 0;

                    if (currentAmmoDirectItem != null)
                    {
                        var avgPrice = currentAmmoDirectItem.AveragePrice();
                        var basePrice = currentAmmoDirectItem.BasePrice / currentAmmoDirectItem.PortionSize;

                        Log("Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice +
                            "] groupID [" +
                            currentAmmoDirectItem.GroupId + "] groupName [" + currentAmmoDirectItem.GroupId + "]");

                        if (avgPrice != 0)
                        {
                            maxPrice = avgPrice * _maxAvgPriceMultiplier; // 3 times the avg price
                        }
                        else
                        {
                            if (basePrice != 0)
                                maxPrice = basePrice * _maxBasePriceMultiplier; // 6 times the base price
                            else
                                maxPrice = 1000;
                        }

                        Log("Item [" + currentAmmoDirectItem.TypeName + "] avgPrice [" + avgPrice + "] basePrice [" + basePrice + "]");
                    }

                    // Are there any orders with an reasonable price?
                    IEnumerable<DirectOrder> orders;
                    if (maxPrice == 0)
                    {
                        Log("if(maxPrice == 0)");
                        orders =
                            marketWindow.SellOrders.Where(o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.TypeId == ammoTypeId).ToList();
                    }
                    else
                    {
                        Log("if(maxPrice != 0) max price [" + maxPrice + "]");
                        orders =
                            marketWindow.SellOrders.Where(
                                    o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == ammoTypeId)
                                .ToList();
                    }

                    orderIterations++;

                    if (!orders.Any() && orderIterations < 5)
                    {
                        nextAction = DateTime.UtcNow.AddSeconds(5);
                        return;
                    }

                    // Is there any order left?
                    if (!orders.Any())
                    {
                        Log("No reasonably priced ammo available! Removing this item from the buyList");
                        buyList.Remove(ammoTypeId);
                        nextAction = DateTime.UtcNow.AddSeconds(3);
                        return;
                    }

                    // How much ammo do we still need?
                    var neededQuantity = ammoQuantity - ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == ammoTypeId).Sum(i => i.Stacksize);
                    if (neededQuantity > 0)
                    {
                        // Get the first order
                        var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {
                            // Calculate how much ammo we still need
                            var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                            var orderPrice = (long)(remaining * order.Price);

                            if (orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                            {
                                Log("Buying [" + remaining + "] ammo price [" + order.Price + "]");
                                order.Buy(remaining, DirectOrderRange.Station);

                                // Wait for the order to go through
                                nextAction = DateTime.UtcNow.AddSeconds(10);
                            }
                            else
                            {
                                Log("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
                                _state = BuyAmmoState.Error;
                                return;
                            }
                        }
                    }

                    break;

                case BuyAmmoState.MoveItemsToCargo:

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                        return;

                    if (!ESCache.Instance.Combat.Ammo.Any())
                        return;

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    IEnumerable<DirectItem> ammoItems = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => moveToCargoList.ContainsKey(i.TypeId)).ToList();
                    if (ammoItems.Any())
                    {
                        if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                        {
                            Log($"Waiting on locked items.");
                            return;
                        }

                        var ammoItem = ammoItems.FirstOrDefault();

                        var maxAmountToMove = Math.Min(ammoItem.Stacksize, moveToCargoList[ammoItem.TypeId]);
                        maxAmountToMove = Math.Max(1, maxAmountToMove);
                        var volumeToMove = ammoItem.Volume * maxAmountToMove;

                        var remainingCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;

                        if (volumeToMove > remainingCapacity)
                        {
                            Log($"Error. Not enough cargo space left in the current active ship.");
                            _state = BuyAmmoState.Error;
                            return;
                        }

                        if (ESCache.Instance.CurrentShipsCargo.Add(ammoItem, maxAmountToMove))
                        {
                            nextAction = DateTime.UtcNow.AddSeconds(5);
                            Log("Moving ammo to cargohold");
                        }
                        return;
                    }

                    Log("Done moving ammo to cargohold");
                    _state = BuyAmmoState.Done;
                    break;

                case BuyAmmoState.Done:

                    if (ESCache.Instance.DirectEve.Session.StationId != null && ESCache.Instance.DirectEve.Session.StationId > 0 &&
                        ESCache.Instance.DirectEve.Session.StationId == ESCache.Instance.EveAccount.CS.QMS.BuyAmmoStationID)
                    {
                        Log($"Executing done action.");
                        _doneAction();
                    }

                    Log($"Removing BuyAmmoController and setting state to idle.");
                    _state = BuyAmmoState.Idle;
                    ControllerManager.Instance.RemoveController(typeof(BuyAmmoController));

                    break;

                case BuyAmmoState.Error:
                    _state = BuyAmmoState.DisabledForThisSession;
                    Log($"Executing done action.");
                    _doneAction();
                    Log("ERROR. BuyAmmo should stay disabled while this session is still active.");
                    ControllerManager.Instance.RemoveController(typeof(BuyAmmoController));
                    break;

                case BuyAmmoState.DisabledForThisSession:
                    break;

                default:
                    throw new Exception("Invalid value for BuyAmmoState");
            }
        }

        public override void DoWork()
        {
            ProcessState();
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}