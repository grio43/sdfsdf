/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.06.2016
 * Time: 09:05
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
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
using EveAccount = SC::SharedComponents.EVE.EveAccount;
using WCFClient = SC::SharedComponents.IPC.WCFClient;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Controllers.Questor
{
    /// <summary>
    ///     Description of BuyPlexController.
    /// </summary>
    public class BuyPlexController : BaseController
    {
        #region Fields

        private static int jumps;
        private static int maxStateIterations = 500;
        private static int orderIterations = 0;
        private static Dictionary<BuyPlexState, int> stateIterations = new Dictionary<BuyPlexState, int>();
        private static TravelerDestination travelerDestination;
        private int maxPlexPrice = 9000000;
        private int plexTypeId = 44992;

        private Action _doneAction;

        #endregion Fields

        #region Constructors

        public BuyPlexController(Action doneAction) : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            _doneAction = doneAction;
        }

        #endregion Constructors

        #region Properties

        public static bool ShouldBuyPlex => ESCache.Instance.EveAccount.CS.QMS.BuyPlex && ESCache.Instance.DirectEve.Me.IsOmegaClone.HasValue &&
                                            !ESCache.Instance.DirectEve.Me.IsOmegaClone.Value && ESCache.Instance.EveAccount.LastPlexBuy < DateTime.UtcNow.AddDays(-25);

        private static BuyPlexState _state { get; set; }

        private static bool StateCheckEveryPulse
        {
            get
            {
                if (stateIterations.ContainsKey(_state))
                    stateIterations[_state] = stateIterations[_state] + 1;
                else
                {
                    stateIterations.AddOrUpdate(_state, 1);
                }

                if (stateIterations[_state] >= maxStateIterations && _state != BuyPlexState.TravelToDestinationStation)
                {
                    Log("ERROR:  if (stateIterations[state] >= maxStateIterations)");
                    _state = BuyPlexState.Error;
                    return true;
                }

                return true;
            }
        }

        #endregion Properties

        #region Methods

        public override void DoWork()
        {
            if (!StateCheckEveryPulse)
                return;

            try
            {
                switch (_state)
                {
                    case BuyPlexState.Idle:
                        Log("BuyPlexState == idle");
                        _state = BuyPlexState.ActivateShuttle;
                        break;

                    case BuyPlexState.ActivateShuttle:

                        if (!ESCache.Instance.InDockableLocation)
                            return;

                        if (ESCache.Instance.DirectEve.GetShipHangar() == null)
                        {
                            Log("Shiphangar is null.");
                            return;
                        }

                        var ships = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton).ToList();

                        if (ESCache.Instance.ActiveShip == null)
                        {
                            Log("Active ship is null.");
                            return;
                        }

                        if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                        {
                            _state = BuyPlexState.TravelToDestinationStation;
                            travelerDestination = new DockableLocationDestination(ESCache.Instance.EveAccount.CS.QMS.BuyAmmoStationID == 0 ? 60003760 : ESCache.Instance.EveAccount.CS.QMS.BuyAmmoStationID);
                            Log("Already in a shuttle.");
                            return;
                        }

                        if (ships.Any(s => s.GroupId == (int)Group.Shuttle && s.IsSingleton && s.GivenName != null))
                        {
                            ships.FirstOrDefault(s => s.GivenName != null && s.GroupId == (int)Group.Shuttle && s.IsSingleton).ActivateShip();
                            Log("Found a shuttle. Making Shuttle active");
                            LocalPulse = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                            return;
                        }
                        else
                        {
                            Log("No shuttle found. Error.");
                            _state = BuyPlexState.Error;
                        }
                        break;

                    case BuyPlexState.TravelToDestinationStation:

                        if (ESCache.Instance.DirectEve.Session.IsInSpace && ESCache.Instance.ActiveShip.Entity != null &&
                            ESCache.Instance.ActiveShip.Entity.IsWarpingByMode)
                            return;

                        if (ESCache.Instance.Traveler.Destination != travelerDestination)
                            ESCache.Instance.Traveler.Destination = travelerDestination;

                        jumps = ESCache.Instance.DirectEve.Navigation.GetDestinationPath().Count;

                        ESCache.Instance.Traveler.ProcessState();

                        if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
                        {
                            Log("Arrived at destination");
                            _state = BuyPlexState.BuyPlex;
                            orderIterations = 0;
                            ESCache.Instance.Traveler.Destination = null;

                            return;
                        }

                        if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
                        {
                            if (ESCache.Instance.Traveler.Destination != null)
                                Log("Stopped traveling, traveller threw an error...");

                            ESCache.Instance.Traveler.Destination = null;
                            _state = BuyPlexState.Error;
                            return;
                        }

                        break;

                    case BuyPlexState.BuyPlex:

                        if (!ESCache.Instance.InDockableLocation)
                            return;

                        if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                            return;

                        var plexVault = ESCache.Instance.DirectEve.PlexVault;
                        if (!plexVault.IsPlexVaultOpen())
                        {
                            Log("Opening plex vault.");
                            plexVault.OpenPlexVault();
                            LocalPulse = UTCNowAddSeconds(2, 4);
                            return;
                        }

                        if (plexVault.GetPlexVaultBalance() == -1)
                        {
                            Log("Plex vault balance value is -1, retrying.");
                            LocalPulse = UTCNowAddSeconds(2, 4);
                            return;
                        }

                        var requiredPlexAmount = 500 - Convert.ToInt32(plexVault.GetPlexVaultBalance());
                        Log($"Vault balance: {plexVault.GetPlexVaultBalance()} Required amount of plex: {requiredPlexAmount}");

                        // Is there a market window?
                        var marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                        //// Do we have enough plex already in the item hangar?
                        //if (ESCache.Instance.DirectEve.GetItemHangar().Items.Any(i => i.TypeId == plexTypeId) &&
                        //    ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == plexTypeId).Sum(i => i.Stacksize) >= requiredPlexAmount)
                        //{
                        //    var ammoItemInHangar = ESCache.Instance.DirectEve.GetItemHangar().Items.FirstOrDefault(i => i.TypeId == plexTypeId);
                        //    if (ammoItemInHangar != null)
                        //        Log("We have [" +
                        //            ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == plexTypeId)
                        //                .Sum(i => i.Stacksize)
                        //                .ToString(CultureInfo.InvariantCulture) +
                        //            "] " + ammoItemInHangar.TypeName + " in the item hangar.");
                        //    _state = BuyPlexState.AddPlexToPlexVault;
                        //    return;
                        //}

                        if (plexVault.GetPlexVaultBalance() >= 500)
                        {
                            _state = BuyPlexState.AddOmegaCloneTime;
                            return;
                        }

                        // We do not have enough plex, open the market window
                        if (marketWindow == null)
                        {
                            LocalPulse = DateTime.UtcNow.AddSeconds(10);
                            Log("Opening market window");
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                            return;
                        }

                        // Wait for the window to become ready
                        if (!marketWindow.IsReady)
                            return;

                        // Are we currently viewing the correct orders?
                        if (marketWindow.DetailTypeId != plexTypeId)
                        {
                            // No, load the ammo orders
                            marketWindow.LoadTypeId(plexTypeId);

                            Log("Loading market window");

                            LocalPulse = DateTime.UtcNow.AddSeconds(10);
                            return;
                        }

                        // Are there any orders with an reasonable price?
                        IEnumerable<DirectOrder> orders =
                            marketWindow.SellOrders.Where(
                                    o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPlexPrice && o.TypeId == plexTypeId)
                                .ToList();

                        orderIterations++;

                        if (!orders.Any() && orderIterations < 5)
                        {
                            LocalPulse = DateTime.UtcNow.AddSeconds(5);
                            return;
                        }

                        // Are there any orders left?
                        if (!orders.Any())
                        {
                            Log($"No plex orders available, or just orders which would cost us more than [{maxPlexPrice}]");
                            LocalPulse = DateTime.UtcNow.AddSeconds(3);
                            _state = BuyPlexState.Error;
                            return;
                        }

                        var balance = plexVault.GetPlexVaultBalance();

                        // How much plex do we still need?                            
                        Log($"Remaining quantity to buy [{requiredPlexAmount}]");

                        if (requiredPlexAmount > 0)
                        {
                            // Get the first order
                            var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                            if (order != null)
                            {
                                // Calculate how many plex we still need
                                var remaining = Math.Min(requiredPlexAmount, order.VolumeRemaining);
                                var orderPrice = (long)(remaining * order.Price);

                                if (orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                                {
                                    Log("Buying [" + remaining + "] plex for [" + order.Price + "].");
                                    order.Buy(remaining, DirectOrderRange.Station);
                                    SetLastPlexBuy();
                                    // Wait for the order to go through
                                    LocalPulse = DateTime.UtcNow.AddSeconds(10);
                                }
                                else
                                {
                                    Log("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
                                    _state = BuyPlexState.Error;
                                    return;
                                }
                            }
                        }
                        break;

                    //case BuyPlexState.AddPlexToPlexVault:

                    //    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    //        return;

                    //    foreach (var item in ESCache.Instance.DirectEve.GetItemHangar().Items)
                    //    {
                    //        Log($"{item.TypeId} - {item.TypeName} - stacksize {item.Stacksize} qty {item.Quantity}");
                    //    }

                    //    var plexItem = ESCache.Instance.DirectEve.GetItemHangar().Items.FirstOrDefault(i => i.TypeId == 44992);

                    //    if (plexItem != null)
                    //    {
                    //        Log($"Moving {plexItem.Stacksize} plex to the vault.");
                    //        plexItem.MoveToPlexVault();
                    //        LocalPulse = GetUTCNowDelaySeconds(3, 5);
                    //        return;
                    //    }

                    //    LocalPulse = GetUTCNowDelaySeconds(3, 5);
                    //    _state = BuyPlexState.AddOmegaCloneTime;
                    //    break;

                    case BuyPlexState.AddOmegaCloneTime:

                        var vault = ESCache.Instance.DirectEve.PlexVault;

                        if (vault.IsPlexVaultOpen())
                        {
                            Log("Vault is already open.");
                            Log($"Vault balance: {vault.GetPlexVaultBalance()}");

                            if (vault.GetPlexVaultBalance() < 500)
                            {
                                Log($"Vault balance not sufficient. Error.");
                                _state = BuyPlexState.Error;
                                return;
                            }

                            var newEdenStore = ESCache.Instance.DirectEve.NewEdenStore;

                            if (!newEdenStore.IsStoreOpen)
                            {
                                Log($"Store isn't opened yet. Opening store.");
                                newEdenStore.OpenStore();
                                LocalPulse = UTCNowAddSeconds(3, 5);
                                return;
                            }

                            int offerId = 2293;
                            var offer = newEdenStore.Offer;

                            if (!newEdenStore.IsOfferOpen() || offer == null)
                            {
                                Log($"Offer detail view isn't opened. Selecting offer {offerId}");
                                newEdenStore.ShowOffer(offerId);
                                LocalPulse = UTCNowAddSeconds(3, 5);
                                return;
                            }

                            Log($"VGS offer window stats. Name {offer.OfferName} Price {offer.Price} Id {offer.OfferId}");

                            if (offer.OfferId == offerId && offer.OfferName.Equals("1 Month Omega") && (offer.Price <= 500))
                            {
                                offer.BuyOffer();
                                LocalPulse = UTCNowAddSeconds(18, 25);
                                _state = BuyPlexState.Done;
                                return;
                            }
                            else
                            {
                                Log($"Wrong offer. Error.");
                                _state = BuyPlexState.Error;
                                return;
                            }
                        }
                        else
                        {
                            Log("Vault is not open.");
                            Log("Opening vault.");
                            vault.OpenPlexVault();
                            LocalPulse = UTCNowAddSeconds(3, 5);
                        }
                        break;

                    case BuyPlexState.Done:
                    case BuyPlexState.DisabledForThisSession:


                        var store = ESCache.Instance.DirectEve.NewEdenStore;

                        if (store.IsStoreOpen)
                        {
                            Log($"Closing New Eden Store.");
                            store.CloseStore();
                            LocalPulse = UTCNowAddSeconds(8, 10);
                            return;
                        }

                        if (ESCache.Instance.DirectEve.Me.IsOmegaClone.HasValue && !ESCache.Instance.DirectEve.Me.IsOmegaClone.Value)
                        {
                            Log($"Account is still in alpha clone state. Error.");
                            _state = BuyPlexState.Error;
                            return;
                        }

                        Log("Removing BuyPlexController and executing done action.");
                        ControllerManager.Instance.RemoveController(typeof(BuyPlexController));
                        _doneAction();
                        break;

                    case BuyPlexState.Error:

                        Log("Error: Disabling this instance.");
                        ESCache.Instance.DisableThisInstance();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("BuyPlex exception: " + ex);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        private void SetLastPlexBuy()
        {
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.LastPlexBuy), DateTime.UtcNow);
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }


        #endregion Methods
    }
}