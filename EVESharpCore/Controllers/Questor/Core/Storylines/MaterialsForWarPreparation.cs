using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    internal enum MaterialsForWarPreparationArmState
    {
        MakeTransportShipActive,
        CheckMarketForOre,
        TravelToOreSystem,
        BuyOre,
        MoveOreToShip,
    }

    public class MaterialsForWarPreparation : IStoryline
    {
        #region Fields

        private DateTime _nextAction;

        private bool _setDestinationStation = false;
        private MaterialsForWarPreparationArmState currentArmState = MaterialsForWarPreparationArmState.MakeTransportShipActive;

        #endregion Fields

        #region Properties

        public static int _materialsForWarOreQty;
        public static int _materialsForWarOreID;
        private int _buyOreSolarSystemId { get; set; }
        private int _buyOreStationId { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.Arm;

            switch (currentArmState)
            {
                case MaterialsForWarPreparationArmState.MakeTransportShipActive:

                    if (ESCache.Instance.DirectEve.GetShipHangar() == null) return StorylineState.Arm;

                    if (ESCache.Instance.MissionSettings.MissionsPath.ToLower().Contains("1"))
                    {
                        _materialsForWarOreID = 1230;
                        _materialsForWarOreQty = 1000;
                    }

                    if (ESCache.Instance.MissionSettings.MissionsPath.ToLower().Contains("2"))
                    {
                        _materialsForWarOreID = 1228;
                        _materialsForWarOreQty = 1665;
                    }

                    if (ESCache.Instance.MissionSettings.MissionsPath.ToLower().Contains("3"))
                    {
                        _materialsForWarOreID = 1227;
                        _materialsForWarOreQty = 10000;
                    }

                    if (ESCache.Instance.MissionSettings.MissionsPath.ToLower().Contains("4"))
                    {
                        _materialsForWarOreID = 20;
                        _materialsForWarOreQty = 8000;
                    }

                    if (ESCache.Instance.ActiveShip == null || ESCache.Instance.ActiveShip.GivenName == null)
                    {
                        if (DebugConfig.DebugArm) Log.WriteLine("if (Cache.Instance.ActiveShip == null)");
                        _nextAction = DateTime.UtcNow.AddSeconds(3);
                        return StorylineState.Arm;
                    }

                    var ships = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton).ToList();
                    var transportShipInCurrentHangar = ships.Any(ship => ship.GivenName != null &&
                                                                         ship.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower());

                    if (!transportShipInCurrentHangar)
                    {
                        Log.WriteLine("No transport ship found. Blacklisting agent and removing storyline mission.");
                        return StorylineState.BlacklistAgent;
                    }

                    if (ESCache.Instance.ActiveShip.GivenName.ToLower() != ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                    {
                        foreach (var ship in ships.Where(ship => ship.GivenName != null &&
                                                                 ship.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower()))
                        {
                            Log.WriteLine("Found a transport ship. Making transport ship active.");
                            ship.ActivateShip();
                            _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                            return StorylineState.Arm;
                        }
                    }
                    else
                    {
                        currentArmState = MaterialsForWarPreparationArmState.CheckMarketForOre;
                        return StorylineState.Arm;
                    }

                    break;

                case MaterialsForWarPreparationArmState.CheckMarketForOre:
                    {
                        var oreid = MaterialsForWarPreparation._materialsForWarOreID;
                        var orequantity = MaterialsForWarPreparation._materialsForWarOreQty;
                        var directEve = ESCache.Instance.DirectEve;
                        var marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();
                        // We do not have enough ore, open the market window
                        if (marketWindow == null)
                        {
                            _nextAction = DateTime.UtcNow.AddSeconds(10);

                            Log.WriteLine("Opening market window");

                            directEve.ExecuteCommand(DirectCmd.OpenMarket);
                            return StorylineState.Arm;
                        }

                        // Wait for the window to become ready (this includes loading the ore info)
                        if (!marketWindow.IsReady)
                            return StorylineState.Arm;

                        // Are we currently viewing ore orders?
                        if (marketWindow.DetailTypeId != oreid)
                        {
                            // No, load the ore orders
                            marketWindow.LoadTypeId(oreid);

                            Log.WriteLine("Loading market window with typeid:" + oreid);

                            _nextAction = DateTime.UtcNow.AddSeconds(5);
                            return StorylineState.Arm;
                        }

                        // Get the median sell price
                        var type = ESCache.Instance.DirectEve.GetInvType(oreid);

                        var OreTypeNeededForThisMission = type;
                        double maxPrice = 0;

                        if (OreTypeNeededForThisMission != null)
                        {
                            Log.WriteLine("OreTypeNeededForThisMission.BasePrice: " + OreTypeNeededForThisMission.BasePrice);
                            maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                            maxPrice = maxPrice * 10;
                        }
                        else
                        {
                            Log.WriteLine("OreTypeNeededForThisMission == null");
                        }

                        IEnumerable<DirectOrder> orders;

                        if (maxPrice != 0)
                        {
                            Log.WriteLine("Max price is: " + maxPrice);
                            orders = marketWindow.SellOrders.Where(o => o.StationId != -1 && o.StationId != 0 && ESCache.Instance.DirectEve.Stations.ContainsKey(o.StationId)
                                                                                          && o.Price < maxPrice &&
                                                                        o.VolumeRemaining > orequantity)
                                .ToList();
                        }
                        else
                        {
                            Log.WriteLine("Max price could not be found. Blacklisting agent.");
                            return StorylineState.BlacklistAgent;
                        }

                        if (!orders.Any())
                        {
                            if (marketWindow != null)
                                marketWindow.Close();
                            if (!ESCache.Instance.EveAccount.CS.QMS.BuyAmmo)
                            {
                                Log.WriteLine("There are no oders in the current region. Blacklisting agent.");
                                return StorylineState.BlacklistAgent;
                            }
                            else
                            {
                                _buyOreStationId = ESCache.Instance.EveAccount.CS.QMS.BuyAmmoStationID;
                                Log.WriteLine($"Using the BuyAmmoStationID {_buyOreStationId} as alternative source.");
                                if (!ESCache.Instance.DirectEve.Stations.ContainsKey(_buyOreStationId))
                                {
                                    Log.WriteLine($"Couldn't find station with ID {_buyOreStationId}. Error. Blacklisting this agent.");
                                    return StorylineState.BlacklistAgent;
                                }

                                _buyOreSolarSystemId = ESCache.Instance.DirectEve.Stations[_buyOreStationId].SolarSystemId;
                                currentArmState = MaterialsForWarPreparationArmState.TravelToOreSystem;
                                return StorylineState.Arm;
                            }
                        }
                        else
                        {
                            var order = orders.OrderBy(s => s.Jumps).FirstOrDefault();
                            Log.WriteLine("Using order from station: " + order.StationId.ToString() + " volume remaining: " + order.VolumeRemaining);
                            _buyOreStationId = order.StationId;
                            _buyOreSolarSystemId = order.SolarSystemId;
                            currentArmState = MaterialsForWarPreparationArmState.TravelToOreSystem;
                            if (marketWindow != null)
                                marketWindow.Close();
                            return StorylineState.Arm;
                        }
                    }

                case MaterialsForWarPreparationArmState.TravelToOreSystem:

                    if (_buyOreStationId == 0 || _buyOreSolarSystemId == 0)
                        return StorylineState.BlacklistAgent;

                    if (ESCache.Instance.Traveler.Destination == null || ESCache.Instance.Traveler.Destination.SolarSystemId != _buyOreSolarSystemId)
                    {
                        ESCache.Instance.Traveler.Destination = new DockableLocationDestination(_buyOreSolarSystemId, _buyOreStationId);
                        return StorylineState.Arm;
                    }

                    if (_buyOreSolarSystemId != ESCache.Instance.DirectEve.Session.SolarSystemId)
                    {
                        // if we haven't already done so, set Eve's autopilot
                        if (!_setDestinationStation)
                        {
                            if (!ESCache.Instance.Traveler.SetStationDestination(_buyOreStationId))
                            {
                                Log.WriteLine("GotoAgent: Unable to find route to storyline agent. Skipping.");
                                ESCache.Instance.State.CurrentStorylineState = StorylineState.Done;
                                return StorylineState.Arm;
                            }
                            _setDestinationStation = true;
                            _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(2, 4));
                            return StorylineState.Arm;
                        }
                    }

                    ESCache.Instance.Traveler.ProcessState();
                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        ESCache.Instance.Traveler.Destination = null;
                        _setDestinationStation = false;
                        currentArmState = MaterialsForWarPreparationArmState.BuyOre;
                        return StorylineState.Arm;
                    }

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                    {
                        Log.WriteLine("Traveller state = Error. Blacklisting this agent.");
                        return StorylineState.BlacklistAgent;
                    }

                    break;

                case MaterialsForWarPreparationArmState.BuyOre:
                    {
                        var oreid = MaterialsForWarPreparation._materialsForWarOreID;
                        var orequantity = MaterialsForWarPreparation._materialsForWarOreQty;
                        var directEve = ESCache.Instance.DirectEve;
                        var marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                        // We do not have enough ore, open the market window
                        if (marketWindow == null)
                        {
                            _nextAction = DateTime.UtcNow.AddSeconds(10);

                            Log.WriteLine("Opening market window");

                            directEve.ExecuteCommand(DirectCmd.OpenMarket);
                            return StorylineState.Arm;
                        }

                        // Wait for the window to become ready (this includes loading the ore info)
                        if (!marketWindow.IsReady)
                            return StorylineState.Arm;

                        // Are we currently viewing ore orders?
                        if (marketWindow.DetailTypeId != oreid)
                        {
                            // No, load the ore orders
                            marketWindow.LoadTypeId(oreid);

                            Log.WriteLine("Loading market window");

                            _nextAction = DateTime.UtcNow.AddSeconds(5);
                            return StorylineState.Arm;
                        }

                        // Get the median sell price
                        var type = ESCache.Instance.DirectEve.GetInvType(oreid);

                        var OreTypeNeededForThisMission = type;
                        double maxPrice = 0;

                        if (OreTypeNeededForThisMission != null)
                        {
                            maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                            maxPrice = maxPrice * 10;
                        }

                        IEnumerable<DirectOrder> orders;

                        if (maxPrice != 0)
                            orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId && o.Price < maxPrice).ToList();
                        else
                            orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();

                        // Do we have orders that sell enough ore for the mission?
                        if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < orequantity)
                        {
                            Log.WriteLine("Not enough (reasonably priced) ore available! Blacklisting agent for this Questor session! maxPrice [" + maxPrice + "]");

                            // Close the market window
                            marketWindow.Close();

                            // No, black list the agent in this Questor session (note we will never decline storylines!)
                            return StorylineState.BlacklistAgent;
                        }

                        // How much ore do we still need?
                        var neededQuantity = orequantity - ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity);
                        if (neededQuantity > 0)
                        {
                            // Get the first order
                            var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                            if (order != null)
                            {
                                // Calculate how much ore we still need
                                var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                                order.Buy(remaining, DirectOrderRange.Station);

                                Log.WriteLine("Buying [" + remaining + "] ore");

                                // Wait for the order to go through
                                _nextAction = DateTime.UtcNow.AddSeconds(10);
                            }
                        }
                        else
                        {
                            currentArmState = MaterialsForWarPreparationArmState.MoveOreToShip;

                            if (marketWindow != null)
                                marketWindow.Close();

                            return StorylineState.Arm;
                        }
                        break;
                    }

                case MaterialsForWarPreparationArmState.MoveOreToShip:
                    {
                        if (ESCache.Instance.DirectEve.GetItemHangar() == null) return StorylineState.Arm;
                        if (ESCache.Instance.CurrentShipsCargo == null) return StorylineState.Arm;

                        IEnumerable<DirectItem> items = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(k => k.TypeId == MaterialsForWarPreparation._materialsForWarOreID).ToList();
                        IEnumerable<DirectItem> itemsInShipCargo = ESCache.Instance.CurrentShipsCargo.Items
                            .Where(k => k.TypeId == MaterialsForWarPreparation._materialsForWarOreID)
                            .ToList();

                        if (itemsInShipCargo.Any() && itemsInShipCargo.Sum(i => i.Stacksize) >= MaterialsForWarPreparation._materialsForWarOreQty)
                        {
                            Log.WriteLine("We have enough ore in the ships cargo.");
                            return StorylineState.GotoAgent;
                        }

                        if (!items.Any() || items.Sum(k => k.Stacksize) < MaterialsForWarPreparation._materialsForWarOreQty)
                        {
                            Log.WriteLine("Ore for MaterialsForWar: typeID [" + MaterialsForWarPreparation._materialsForWarOreID + "] not found in ItemHangar");
                            return StorylineState.BlacklistAgent;
                        }

                        var oreIncargo = 0;
                        foreach (var cargoItem in ESCache.Instance.CurrentShipsCargo.Items.ToList())
                        {
                            if (cargoItem.TypeId != MaterialsForWarPreparation._materialsForWarOreID)
                                continue;

                            oreIncargo += cargoItem.Quantity;
                            continue;
                        }

                        var oreToLoad = MaterialsForWarPreparation._materialsForWarOreQty - oreIncargo;
                        if (oreToLoad <= 0)
                        {
                            Log.WriteLine("return StorylineState.GotoAgent");
                            return StorylineState.GotoAgent;
                        }

                        var item = items.FirstOrDefault();
                        if (item != null)
                        {
                            var moveOreQuantity = Math.Min(item.Stacksize, oreToLoad);
                            var volumeToMove = item.Volume * moveOreQuantity;
                            if ((ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity) < volumeToMove)
                            {
                                Log.WriteLine($"Transport ship has not enough free space. CurrentShipsCargo.Capacity {ESCache.Instance.CurrentShipsCargo.Capacity} CurrentShipsCargo.UsedCapacity {ESCache.Instance.CurrentShipsCargo.UsedCapacity} volumeToMove {volumeToMove}");
                                return StorylineState.BlacklistAgent;
                            }
                            if (ESCache.Instance.CurrentShipsCargo.Add(item, moveOreQuantity))
                            {
                                Log.WriteLine("Moving [" + moveOreQuantity + "] units of Ore [" + item.TypeName + "] Stack size: [" + item.Stacksize +
                                              "] from hangar to CargoHold");
                                _nextAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(3, 6));
                            }
                            return StorylineState.Arm; // you can only move one set of items per frame
                        }
                    }
                    break;
            }
            return StorylineState.Arm;
        }

        /// <summary>
        ///     We have no execute mission code
        /// </summary>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            return StorylineState.CompleteMission;
        }

        /// <summary>
        ///     We have no combat/delivery part in this mission, just accept it
        /// </summary>
        /// <returns></returns>
        public StorylineState PostAcceptMission(Storyline storyline)
        {
            // Close the market window (if its open)
            return StorylineState.CompleteMission;
        }

        /// <summary>
        ///     Check if we have kernite in station
        /// </summary>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            Reset();
            var directEve = ESCache.Instance.DirectEve;
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.PreAcceptMission;

            var oreid = MaterialsForWarPreparation._materialsForWarOreID;
            var orequantity = MaterialsForWarPreparation._materialsForWarOreQty;

            // Open the item hangar
            if (ESCache.Instance.DirectEve.GetItemHangar() == null) return StorylineState.PreAcceptMission;

            // Is there a market window?
            var marketWindow = directEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

            // Do we have the ore we need in the Item Hangar?.

            if (ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                var thisOreInhangar = ESCache.Instance.DirectEve.GetItemHangar().Items.FirstOrDefault(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                    Log.WriteLine("We have [" + ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == oreid)
                                      .Sum(i => i.Quantity)
                                      .ToString(CultureInfo.InvariantCulture) +
                                  "] " + thisOreInhangar.TypeName + " in the item hangar accepting mission");

                // Close the market window if there is one
                if (marketWindow != null)
                    marketWindow.Close();

                return StorylineState.AcceptMission;
            }

            if (ESCache.Instance.CurrentShipsCargo == null) return StorylineState.PreAcceptMission;

            if (ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity) >= orequantity)
            {
                var thisOreInhangar = ESCache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.TypeId == oreid);
                if (thisOreInhangar != null)
                    Log.WriteLine("We have [" +
                                  ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == oreid)
                                      .Sum(i => i.Quantity)
                                      .ToString(CultureInfo.InvariantCulture) + "] " +
                                  thisOreInhangar.TypeName + " in the CargoHold accepting mission");

                // Close the market window if there is one
                if (marketWindow != null)
                    marketWindow.Close();

                return StorylineState.AcceptMission;
            }

            // We do not have enough ore, open the market window
            if (marketWindow == null)
            {
                _nextAction = DateTime.UtcNow.AddSeconds(10);

                Log.WriteLine("Opening market window");

                directEve.ExecuteCommand(DirectCmd.OpenMarket);
                return StorylineState.PreAcceptMission;
            }

            // Wait for the window to become ready (this includes loading the ore info)
            if (!marketWindow.IsReady)
                return StorylineState.PreAcceptMission;

            // Are we currently viewing ore orders?
            if (marketWindow.DetailTypeId != oreid)
            {
                // No, load the ore orders
                marketWindow.LoadTypeId(oreid);

                Log.WriteLine("Loading market window");

                _nextAction = DateTime.UtcNow.AddSeconds(5);
                return StorylineState.PreAcceptMission;
            }

            // Get the median sell price
            var type = ESCache.Instance.DirectEve.GetInvType(oreid);

            var OreTypeNeededForThisMission = type;
            double maxPrice = 0;

            if (OreTypeNeededForThisMission != null)
            {
                maxPrice = OreTypeNeededForThisMission.BasePrice / OreTypeNeededForThisMission.PortionSize;
                maxPrice = maxPrice * 10;
            }

            IEnumerable<DirectOrder> orders;

            if (maxPrice != 0)
                orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId && o.Price < maxPrice).ToList();
            else
                orders = marketWindow.SellOrders.Where(o => o.StationId == directEve.Session.StationId).ToList();

            // Do we have orders that sell enough ore for the mission?

            orders = orders.Where(o => o.StationId == directEve.Session.StationId).ToList();
            if (!orders.Any() || orders.Sum(o => o.VolumeRemaining) < orequantity)
            {
                Log.WriteLine("Not enough (reasonably priced) ore available! Blacklisting agent for this Questor session! maxPrice [" + maxPrice + "]");

                // Close the market window
                marketWindow.Close();

                // No, black list the agent in this Questor session (note we will never decline storylines!)
                return StorylineState.BlacklistAgent;
            }

            // How much ore do we still need?
            var neededQuantity = orequantity - ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == oreid).Sum(i => i.Quantity);
            if (neededQuantity > 0)
            {
                // Get the first order
                var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                if (order != null)
                {
                    // Calculate how much ore we still need
                    var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                    order.Buy(remaining, DirectOrderRange.Station);

                    Log.WriteLine("Buying [" + remaining + "] ore");

                    // Wait for the order to go through
                    _nextAction = DateTime.UtcNow.AddSeconds(10);
                }
            }
            return StorylineState.PreAcceptMission;
        }

        public void Reset()
        {
            currentArmState = MaterialsForWarPreparationArmState.MakeTransportShipActive;
            _buyOreStationId = 0;
            _buyOreSolarSystemId = 0;
            _setDestinationStation = false;
        }

        #endregion Methods
    }
}