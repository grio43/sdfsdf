/*
 * User: duketwo
 * Date: 03.10.2018
 * Time: 19:52
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Traveller;
using SC::SharedComponents.IPC;
using WCFClient = SC::SharedComponents.IPC.WCFClient;

namespace EVESharpCore.Controllers.Questor
{

    public enum DumpLootControllerState
    {
        TravelToHomeStation,
        CreateJitaUndockSpot,
        ActivateCloakyHauler,
        MoveLootToShip,
        TravelToJita,
        MoveLootToItemHangar,
        DumpLoot,
        Error,
        Done,
    }

    /// <summary>
    ///     Description of DumpLootController.
    /// </summary>
    public class DumpLootController : BaseController
    {
        private DumpLootControllerState _state;
        private TravelerDestination _travelerDestination;
        private bool _createUndockBookmark;
        private const int MAX_DUMP_LOOT_ITERATIONS = 10;
        private const int MIN_LOOT_TO_DUMP = 30000;
        private static bool _disabledForThisSession;
        private readonly Action _doneAction;
        private int errorCnt;

        #region Constructors

        public DumpLootController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }
        public DumpLootController(Action doneAction) : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            _doneAction = doneAction;
        }

        #endregion Constructors

        #region Methods

        private bool AnyJitaUndockBookmark
        {
            get
            {
                var jita = ESCache.Instance.DirectEve.Stations[60003760];
                return ESCache.Instance.DirectEve.Bookmarks.Any(b => b.DistanceTo(jita) < 150000);
            }
        }

        public static bool ShouldDumpLoot
        {
            get
            {
                if (ESCache.Instance.ForceDumpLoop)
                    return true;

                if (_disabledForThisSession)
                    return false;
                var loot2Dump = ESCache.Instance.UnloadLoot.LootItemsInItemHangar();
                var totalLoot2DumpVolume = loot2Dump.Sum(i => i.Quantity * i.Volume);
                return totalLoot2DumpVolume > MIN_LOOT_TO_DUMP && DumpLootIterations < MAX_DUMP_LOOT_ITERATIONS;
            }
        }

        private static int DumpLootIterations => WCFClient.Instance.GetPipeProxy.GetDumpLootIterations(ESCache.Instance.EveAccount.CharacterName);

        private static void IncreaseDumpLootIterations() => WCFClient.Instance.GetPipeProxy.IncreaseDumpLootIterations(ESCache.Instance.EveAccount.CharacterName);

        private bool DockedInJita => ESCache.Instance.InDockableLocation && (ESCache.Instance.DirectEve.Session.StationId == 60003760);

        private bool _sellPerformed;
        private DateTime _sellPerformedDateTime;

        public override void DoWork()
        {

            if (ESCache.Instance.InSpace &&
                ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
            {
                Log($"We are in a capsule. Error.");
                _state = DumpLootControllerState.Error;
            }

            switch (_state)
            {
                case DumpLootControllerState.TravelToHomeStation:

                    if (ESCache.Instance.Agent.Name.ToLower() != ESCache.Instance.EveAccount.CS.QMS.AgentName.ToLower())
                    {
                        Log($"The agent does not match the one defined within the settings. Reset the agent.");
                        ESCache.Instance.Agent = null;
                    }

                    var homeStationId = ESCache.Instance.Agent.StationId;
                    if (ESCache.Instance.DirectEve.Session.StationId == homeStationId)
                    {
                        Log($"We are in the right station. Changing to next state ({nameof(DumpLootControllerState.ActivateCloakyHauler)}).");
                        _state = DumpLootControllerState.ActivateCloakyHauler;
                        return;
                    }
                    else
                    {
                        if (DirectEve.Interval(2000, 2500))
                        {
                            Log($"TravelHome pulse.");
                        }
                        ESCache.Instance.Traveler.TravelHome();
                    }

                    break;

                case DumpLootControllerState.CreateJitaUndockSpot:

                    if (AnyJitaUndockBookmark)
                    {
                        Log($"Jita undock bookmark exists, starting again.");
                        _state = DumpLootControllerState.TravelToHomeStation;
                        return;
                    }
                    else
                    {

                        if (DockedInJita)
                        {
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            return;
                        }

                        else if ((ESCache.Instance.InSpace
                             && ESCache.Instance.Stations.Any(e => e.Id == 60003760 && e.Distance < 150000)))
                        {
                            ESCache.Instance.DirectEve.BookmarkCurrentLocation(new Random().Next(10, 99).ToString(), null);
                            Log($"Adding Jita undock boomkark.");
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            return;
                        }

                        else
                        {
                            _createUndockBookmark = true;
                            Log($"Travelling to Jita to create the undock bookmark.");
                            Log($"Changing state ({nameof(DumpLootControllerState.TravelToJita)}).");
                            _travelerDestination = new DockableLocationDestination(60003760);
                            _state = DumpLootControllerState.TravelToJita;
                        }
                    }

                    break;

                case DumpLootControllerState.ActivateCloakyHauler:

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetShipHangar() == null)
                    {
                        Log("Shiphangar is null.");
                        return;
                    }

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log("ItemHangar is null.");
                        return;
                    }

                    if (!ShouldDumpLoot)
                    {
                        Log($"ShouldDumpLoot == false, changing state to done.");
                        _state = DumpLootControllerState.Done;
                        return;
                    }

                    var ships = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                                                            && i.GroupId == (int)Group.BlockadeRunner
                                                                                            && i.GivenName != null).ToList();

                    if (ESCache.Instance.ActiveShip == null)
                    {
                        Log("Active ship is null.");
                        return;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.BlockadeRunner)
                    {
                        _state = DumpLootControllerState.MoveLootToShip;
                        Log("Already in a transport ship.");
                        return;
                    }

                    if (ships.Any())
                    {
                        ships.FirstOrDefault().ActivateShip();
                        Log("Found a transport ship. Making it active.");
                        LocalPulse = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                        _state = DumpLootControllerState.MoveLootToShip;
                        return;
                    }
                    else
                    {
                        Log("No transport ship found. Error.");
                        _state = DumpLootControllerState.Error;
                    }

                    break;
                case DumpLootControllerState.MoveLootToShip:

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log("ItemHangar is null.");
                        return;
                    }

                    if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Capacity == 0)
                        return;

                    if (!AnyJitaUndockBookmark)
                    {
                        Log($"No Jita undock bookmark found. Creating the bookmark.");
                        _state = DumpLootControllerState.CreateJitaUndockSpot;
                        return;
                    }

                    var freeCargo = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                    Log("Current [" + ESCache.Instance.ActiveShip.GivenName + "] Cargo [" + ESCache.Instance.CurrentShipsCargo.Capacity +
                        "] Used Capacity [" +
                        ESCache.Instance.CurrentShipsCargo.UsedCapacity + "] Free Capacity [" + freeCargo + "]");

                    var loot2Dump = ESCache.Instance.UnloadLoot.LootItemsInItemHangar().OrderByDescending(i => i.IskPerM3);
                    var cargoPerc = (ESCache.Instance.CurrentShipsCargo.UsedCapacity / (ESCache.Instance.CurrentShipsCargo.Capacity / 100));

                    Log($"CargoPerc {cargoPerc}%");

                    if (!loot2Dump.Any() || cargoPerc >= 95)
                    {
                        if (ESCache.Instance.CurrentShipsCargo.Items.Any())
                        {
                            Log($"Changing state ({nameof(DumpLootControllerState.TravelToJita)}).");
                            ESCache.Instance.Traveler.AllowLowSec = true;
                            _travelerDestination = new DockableLocationDestination(60003760);
                            _state = DumpLootControllerState.TravelToJita;
                        }
                        else
                        {
                            Log($"Changing state (Done).");
                            _state = DumpLootControllerState.Done;
                        }
                        return;
                    }

                    var itemsToMove = new List<DirectItem>();

                    foreach (var item in loot2Dump)
                    {
                        var totalVolume = item.Quantity * item.Volume;
                        if (totalVolume > freeCargo)
                        {
                            if (itemsToMove.Any()) // items to move first
                                break;
                            // try to move it partially or skip if the volume of one is more than left free cargo
                            if (item.Volume > freeCargo)
                            {
                                Log($"Item {item.TypeName} Volume {item.Volume} exceeds remaining freecargo {freeCargo}.");
                                continue; // pick the next item in that case
                            }
                            else
                            {
                                var quantityToMove = Convert.ToInt32(Math.Floor(freeCargo / item.Volume));
                                Log($"Adding {item.TypeName} partially. Total quantity {item.Quantity} Moving quantity {quantityToMove}");
                                if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                                    return;
                                if (ESCache.Instance.CurrentShipsCargo.Add(item, quantityToMove))
                                {
                                    LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                                }
                                return;
                            }
                        }
                        else
                        {
                            // add item to the list
                            itemsToMove.Add(item);
                            freeCargo -= totalVolume;
                            Log($"Added {item.TypeName} TotalVol {totalVolume} Freecargo {freeCargo}");
                        }
                        //Log($"TypeName {item.TypeName} Quantity {item.Quantity} ISKPerM3 {item.IskPerM3} Stacksize {item.Stacksize} Vol {item.Volume} TotalVol {item.Volume * item.Stacksize}");
                    }

                    if (itemsToMove.Any())
                    {
                        // move items
                        Log($"Moving items.");
                        if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                            return;
                        if (ESCache.Instance.CurrentShipsCargo.Add(itemsToMove))
                        {
                            LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                        }
                        return;
                    }

                    break;
                case DumpLootControllerState.TravelToJita:

                    if (ESCache.Instance.DirectEve.Session.IsInSpace && ESCache.Instance.ActiveShip.Entity != null &&
                        ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                        return;

                    if (ESCache.Instance.Traveler.Destination != _travelerDestination)
                        ESCache.Instance.Traveler.Destination = _travelerDestination;

                    ESCache.Instance.Traveler.ProcessState();

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        if (_createUndockBookmark)
                        {
                            Log($"Arrived at Jita 4/4. Changing to next state {nameof(DumpLootControllerState.CreateJitaUndockSpot)}.");
                            _state = DumpLootControllerState.CreateJitaUndockSpot;
                            _createUndockBookmark = false;
                        }
                        else
                        {
                            Log($"Arrived at Jita 4/4. Changing to next state {nameof(DumpLootControllerState.MoveLootToItemHangar)}.");
                            _state = DumpLootControllerState.MoveLootToItemHangar;
                        }
                        ESCache.Instance.Traveler.Destination = null;
                        return;
                    }

                    if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                    {
                        if (ESCache.Instance.Traveler.Destination != null)
                            Log("Stopped traveling, traveller threw an error.");

                        ESCache.Instance.Traveler.Destination = null;
                        _state = DumpLootControllerState.Error;
                        return;
                    }

                    break;
                case DumpLootControllerState.MoveLootToItemHangar:

                    if (ESCache.Instance.CurrentShipsCargo == null || ESCache.Instance.CurrentShipsCargo.Capacity == 0)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log("ItemHangar is null.");
                        return;
                    }

                    if (ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        Log($"Moving items.");

                        if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                            return;
                        if (ESCache.Instance.DirectEve.GetItemHangar().Add(ESCache.Instance.CurrentShipsCargo.Items))
                        {
                            LocalPulse = UTCNowAddMilliseconds(3000, 3500);
                        }
                        return;
                    }
                    else
                    {
                        Log($"Changing state to {nameof(DumpLootControllerState.DumpLoot)}.");
                        _state = DumpLootControllerState.DumpLoot;
                    }

                    break;
                case DumpLootControllerState.DumpLoot:
                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log("ItemHangar is null.");
                        return;
                    }

                    var loot2dump = ESCache.Instance.UnloadLoot.LootItemsInItemHangar().Where(i => !i.IsSingleton).ToList();

                    if (loot2dump.Any())
                    {


                        var anyMultiSellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().Any();

                        if (ESCache.Instance.SellError && anyMultiSellWnd)
                        {
                            Log("Sell error, closing window and trying again.");
                            var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                            sellWnd.Cancel();
                            errorCnt++;
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            ESCache.Instance.SellError = false;

                            if (errorCnt > 20)
                            {
                                Log($"Too many errors while dumping loot, disabled for this session.");
                                _state = DumpLootControllerState.Error;
                                return;
                            }

                            return;
                        }

                        if (!anyMultiSellWnd)
                        {
                            Log($"Opening MultiSellWindow with {loot2dump.Count} items.");
                            ESCache.Instance.DirectEve.MultiSell(loot2dump);
                            LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                            _sellPerformed = false;
                            return;
                        }
                        else
                        {
                            var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                            if (sellWnd.AddingItemsThreadRunning)
                            {
                                Log($"Waiting for items to be added.");
                                LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                                return;
                            }
                            else
                            {

                                if (sellWnd.GetDurationComboValue() != DurationComboValue.IMMEDIATE)
                                {
                                    Log($"Setting duration combo value to {DurationComboValue.IMMEDIATE}.");
                                    //Log($"Currently not working correctly, you need to select IMMEDIATE manually.");
                                    sellWnd.SetDurationCombovalue(DurationComboValue.IMMEDIATE);
                                    LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                                    return;
                                }

                                if (sellWnd.GetSellItems().All(i => !i.HasBid))
                                {
                                    Log($"Only items without a bid are left. Done. " +
                                        $"Changing to next state ({nameof(DumpLootControllerState.TravelToHomeStation)}).");
                                    sellWnd.Cancel();
                                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                                    _state = DumpLootControllerState.TravelToHomeStation;
                                    IncreaseDumpLootIterations();
                                    return;
                                }

                                if (_sellPerformed)
                                {
                                    var secondsSince =
                                        Math.Abs((DateTime.UtcNow - _sellPerformedDateTime).TotalSeconds);
                                    Log($"We just performed a sell [{secondsSince}] seconds ago. Waiting for timeout.");
                                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);

                                    if (secondsSince <= 16) return;

                                    Log($"Timeout reached. Canceling the trade and changing to next state.");
                                    sellWnd.Cancel();
                                    LocalPulse = UTCNowAddMilliseconds(1500, 2000);
                                    _state = DumpLootControllerState.TravelToHomeStation;
                                    IncreaseDumpLootIterations();
                                    return;
                                }


                                Log($"Items added. Performing trade.");
                                sellWnd.PerformTrade();
                                _sellPerformed = true;
                                _sellPerformedDateTime = DateTime.UtcNow;
                                LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                                return;

                            }
                        }
                    }
                    else
                    {
                        Log($"Sold all items. Changing to next state ({nameof(DumpLootControllerState.TravelToHomeStation)}).");
                        _state = DumpLootControllerState.TravelToHomeStation;
                        IncreaseDumpLootIterations();
                        return;
                    }

                    break;
                case DumpLootControllerState.Error:
                    _disabledForThisSession = true;
                    _state = DumpLootControllerState.Done;
                    break;
                case DumpLootControllerState.Done:
                    ControllerManager.Instance.RemoveController(typeof(DumpLootController));
                    _doneAction?.Invoke();
                    break;
            }
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