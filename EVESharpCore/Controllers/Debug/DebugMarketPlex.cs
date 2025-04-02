using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Framework;
using System.Globalization;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugMarketPlex : Form
    {
        #region Constructors

        public DebugMarketPlex()
        {
            InitializeComponent();
            Rnd = new Random();
        }

        private int plexTypeId = 44992;
        private int maxPlexPrice = 6000000;
        protected Random Rnd { get; set; }
        private bool closeSellWnd;
        private int orderIterations = 0;

        #endregion Constructors

        #region Methods

        protected DateTime GetUTCNowDelaySeconds(int minDelayInSeconds, int maxDelayInSeconds)
        {
            return DateTime.UtcNow.AddMilliseconds(GetRandom(minDelayInSeconds * 1000, maxDelayInSeconds * 1000));
        }

        protected int GetRandom(int minValue, int maxValue)
        {
            return Rnd.Next(minValue, maxValue);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    if (!ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        waitUntil = GetUTCNowDelaySeconds(2, 4);
                        return;
                    }

                    var plexVault = ESCache.Instance.DirectEve.PlexVault;
                    if (!plexVault.IsPlexVaultOpen())
                    {
                        Log.WriteLine("Opening plex vault.");
                        plexVault.OpenPlexVault();
                        waitUntil = GetUTCNowDelaySeconds(2, 4);
                        action.QueueAction();
                        return;
                    }

                    if (plexVault.IsPlexVaultOpen())
                    {
                        Log.WriteLine("Plex vault is open.");
                    }

                    if (plexVault.GetPlexVaultBalance() == -1)
                    {
                        Log.WriteLine("Plex vault balance value is -1, retrying.");
                        waitUntil = GetUTCNowDelaySeconds(2, 4);
                        action.QueueAction();
                        return;
                    }

                    ModifyButtons(true);

                    var requiredPlexAmount = 1 - Convert.ToInt32(plexVault.GetPlexVaultBalance());
                    Log.WriteLine($"Vault balance: {plexVault.GetPlexVaultBalance()} Required amount of plex: {requiredPlexAmount}");


                    // Is there a market window?
                    var marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                    if (plexVault.GetPlexVaultBalance() < 1) // buy one plex
                    {

                        // We do not have enough plex, open the market window
                        if (marketWindow == null)
                        {
                            waitUntil = DateTime.UtcNow.AddSeconds(10);
                            Log.WriteLine("Opening market window");
                            ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                            action.QueueAction();
                            return;
                        }

                        // Wait for the window to become ready
                        if (!marketWindow.IsReady)
                        {
                            action.QueueAction();
                            return;
                        }

                        // Are we currently viewing the correct ammo orders?
                        if (marketWindow.DetailTypeId != plexTypeId)
                        {
                            // No, load the ammo orders
                            marketWindow.LoadTypeId(plexTypeId);

                            Log.WriteLine("Loading market window");

                            waitUntil = DateTime.UtcNow.AddSeconds(10);
                            action.QueueAction();
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
                            waitUntil = DateTime.UtcNow.AddSeconds(10);
                            action.QueueAction();
                            return;
                        }

                        // Are there any orders left?
                        if (!orders.Any())
                        {
                            Log.WriteLine($"No plex orders available, or just orders which would cost us more than [{maxPlexPrice}]");
                            return;
                        }

                        var balance = plexVault.GetPlexVaultBalance();

                        // How much plex do we still need?
                        int neededQuantity = requiredPlexAmount - (int)balance;

                        Log.WriteLine($"Remaining quantity to buy [{neededQuantity}]");

                        if (neededQuantity > 0)
                        {
                            // Get the first order
                            var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                            if (order != null)
                            {
                                // Calculate how many plex we still need
                                var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                                var orderPrice = (long)(remaining * order.Price);

                                if (orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                                {
                                    Log.WriteLine("Buying [" + remaining + "] plex for [" + order.Price + "].");
                                    order.Buy(remaining, DirectOrderRange.Station);
                                    // Wait for the order to go through
                                    waitUntil = DateTime.UtcNow.AddSeconds(5);
                                    action.QueueAction();
                                }
                                else
                                {
                                    Log.WriteLine("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
                                    return;
                                }
                            }
                        }

                    }
                    return;

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }


        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button)
                        button.Enabled = enabled;
            }));
        }

        #endregion Methods

        private void button1_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    var vault = ESCache.Instance.DirectEve.PlexVault;

                    if (vault.IsPlexVaultOpen())
                    {
                        Log.WriteLine("Vault is already open.");
                        Log.WriteLine($"Vault balance: {vault.GetPlexVaultBalance()}");

                        if (vault.GetPlexVaultBalance() < 1)
                        {
                            Log.WriteLine($"Vault balance not sufficient. Error.");
                            return;
                        }

                        var newEdenStore = ESCache.Instance.DirectEve.NewEdenStore;

                        if (!newEdenStore.IsStoreOpen)
                        {
                            Log.WriteLine($"Store isn't opened yet. Opening store.");
                            newEdenStore.OpenStore();
                            waitUntil = DateTime.UtcNow.AddSeconds(3);
                            action.QueueAction();
                            return;
                        }

                        int offerId = 2293;
                        var offer = newEdenStore.Offer;

                        if (!newEdenStore.IsOfferOpen() || offer == null)
                        {
                            Log.WriteLine($"Offer detail view isn't opened. Selecting offer {offerId}");
                            newEdenStore.ShowOffer(offerId);
                            waitUntil = DateTime.UtcNow.AddSeconds(3);
                            action.QueueAction();
                            return;
                        }

                        Log.WriteLine($"VGS offer window stats. Name {offer.OfferName} Price {offer.Price} Id {offer.OfferId}");

                        if (offer.OfferId == offerId && offer.OfferName.Equals("1 Month Omega") && (offer.Price <= 500))
                        {


                            Log.WriteLine($"Offer is correct. Here we would buy the offer.");
                            var buyButtonExist = offer.DoesBuyButtonExist();
                            if (buyButtonExist)
                            {
                                Log.WriteLine($"Buy button does exist.");
                                ModifyButtons(true);
                                return;
                            }
                            else
                            {
                                Log.WriteLine($"ERROR: Could not find the buy button.");
                            }
                            //offer.BuyOffer();
                            return;
                        }
                        else
                        {
                            Log.WriteLine($"Wrong offer. Error.");
                            return;
                        }
                    }
                    else
                    {
                        Log.WriteLine("Vault is not open.");
                        Log.WriteLine("Opening vault.");
                        vault.OpenPlexVault();
                        waitUntil = DateTime.UtcNow.AddSeconds(3);
                        action.QueueAction();
                        return;
                    }

                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    // Is there a market window?
                    var marketWindow = ESCache.Instance.DirectEve.Windows.OfType<DirectMarketWindow>().FirstOrDefault();

                    // We do not have enough plex, open the market window
                    if (marketWindow == null)
                    {
                        waitUntil = DateTime.UtcNow.AddSeconds(10);
                        Log.WriteLine("Opening market window");
                        ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.OpenMarket);
                        action.QueueAction();
                        return;
                    }

                    // Wait for the window to become ready
                    if (!marketWindow.IsReady)
                    {
                        action.QueueAction();
                        return;
                    }

                    var tritTypeId = 34;

                    // Are we currently viewing the correct orders?
                    if (marketWindow.DetailTypeId != tritTypeId)
                    {
                        // No, load orders
                        marketWindow.LoadTypeId(tritTypeId);

                        Log.WriteLine($"Loading market window with typeID: {tritTypeId}");

                        waitUntil = DateTime.UtcNow.AddSeconds(10);
                        action.QueueAction();
                        return;
                    }

                    var maxPrice = 200;


                    foreach (var order in marketWindow.SellOrders.Where(o => o.Jumps == 0))
                    {
                        Log.WriteLine(order.ToString());
                    }

                    // Are there any orders with an reasonable price?
                    IEnumerable<DirectOrder> orders =
                        marketWindow.SellOrders.Where(
                                o => o.StationId == ESCache.Instance.DirectEve.Session.StationId && o.Price < maxPrice && o.TypeId == tritTypeId)
                            .ToList();

                    orderIterations++;

                    if (!orders.Any() && orderIterations < 5)
                    {
                        waitUntil = DateTime.UtcNow.AddSeconds(10);
                        action.QueueAction();
                        return;
                    }

                    // Are there any orders left?
                    if (!orders.Any())
                    {
                        Log.WriteLine($"No orders available, or just orders which would cost us more than [{maxPrice}]");
                        return;
                    }

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        waitUntil = DateTime.UtcNow.AddSeconds(4);
                        action.QueueAction();
                        return;
                    }

                    //(ESCache.Instance.DirectEve.GetItemHangar().Items.Any(i => i.TypeId == plexTypeId)
                    var currentAmount = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == tritTypeId).Sum(i => i.Stacksize);
                    var amount = 1;

                    Log.WriteLine($"Current amount: {currentAmount}");
                    // How many do we still need?
                    var neededQuantity = amount - currentAmount;

                    Log.WriteLine($"Remaining quantity to buy [{neededQuantity}]");

                    if (neededQuantity > 0)
                    {
                        // Get the first order
                        var order = orders.OrderBy(o => o.Price).FirstOrDefault();
                        if (order != null)
                        {
                            // Calculate how many we still need
                            var remaining = Math.Min(neededQuantity, order.VolumeRemaining);
                            var orderPrice = (long)(remaining * order.Price);

                            if (orderPrice < ESCache.Instance.DirectEve.Me.Wealth)
                            {
                                Log.WriteLine("Buying [" + remaining + "] for [" + order.Price + "].");
                                order.Buy(remaining, DirectOrderRange.Station);
                                // Wait for the order to go through
                                waitUntil = DateTime.UtcNow.AddSeconds(5);
                                action.QueueAction();
                                return;
                            }
                            else
                            {
                                Log.WriteLine("ERROR: We don't have enough ISK on our wallet to finish that transaction.");
                                return;
                            }
                        }
                    }
                    ModifyButtons(true);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ModifyButtons();
            var waitUntil = DateTime.MinValue;
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (waitUntil > DateTime.UtcNow)
                    {
                        action.QueueAction();
                        return;
                    }

                    if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log.WriteLine("ItemHangar is null.");
                        waitUntil = DateTime.UtcNow.AddSeconds(3);
                        action.QueueAction();
                        return;
                    }

                    var loot2dump = ESCache.Instance.UnloadLoot.LootItemsInItemHangar().Where(i => !i.IsSingleton && i.TypeId == 34).ToList();

                    if (loot2dump.Any())
                    {
                        if (!ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().Any())
                        {
                            Log.WriteLine($"Opening MultiSellWindow with {loot2dump.Count} items.");
                            ESCache.Instance.DirectEve.MultiSell(loot2dump);
                            waitUntil = DateTime.UtcNow.AddSeconds(3);
                            action.QueueAction();
                            return;
                        }
                        else
                        {
                            var sellWnd = ESCache.Instance.DirectEve.Windows.OfType<DirectMultiSellWindow>().FirstOrDefault();
                            if (sellWnd.AddingItemsThreadRunning)
                            {
                                Log.WriteLine($"Waiting for items to be added.");
                                waitUntil = DateTime.UtcNow.AddSeconds(3);
                                action.QueueAction();
                                return;
                            }
                            else
                            {

                                if (sellWnd.GetDurationComboValue() != DurationComboValue.IMMEDIATE)
                                {
                                    Log.WriteLine($"Setting duration combo value to {DurationComboValue.IMMEDIATE}.");
                                    sellWnd.SetDurationCombovalue(DurationComboValue.IMMEDIATE);
                                    waitUntil = DateTime.UtcNow.AddSeconds(3);
                                    action.QueueAction();
                                    closeSellWnd = true;
                                    return;
                                }

                                if (closeSellWnd)
                                {
                                    closeSellWnd = false;
                                    sellWnd.Close(); // todo: fix me, changing duration combo via code does not save but it does if selected manually via the ui
                                    Log.WriteLine("Closing MultiSellWnd required after duration combo change. Closing window.");
                                    waitUntil = DateTime.UtcNow.AddSeconds(3);
                                    action.QueueAction();
                                    return;
                                }

                                if (sellWnd.GetSellItems().All(i => !i.HasBid))
                                {
                                    Log.WriteLine($"Only items without a bid are left. Done.");
                                    sellWnd.Cancel();
                                    waitUntil = DateTime.UtcNow.AddSeconds(3);
                                    action.QueueAction();
                                    return;
                                }
                                else
                                {

                                    Log.WriteLine($"Items added. Performing trade.");
                                    sellWnd.PerformTrade();
                                    waitUntil = DateTime.UtcNow.AddSeconds(3);
                                    action.QueueAction();
                                    return;
                                }
                            }
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Sold all items.");
                        ModifyButtons(true);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }
    }
}