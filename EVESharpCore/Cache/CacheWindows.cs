extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Logging;
using System;
using System.Linq;

namespace EVESharpCore.Cache
{
    public partial class ESCache
    {
        #region Fields

        #endregion Fields

        #region Methods

        public bool RepairItems()
        {
            try
            {
                if (DateTime.UtcNow < Instance.Time.NextRepairItemsAction)
                    return false;

                var random = new Random();
                Instance.Time.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(random.Next(2, 4));

                if (Instance.InDockableLocation && !Instance.DirectEve.HasRepairFacility())
                {
                    Log.WriteLine("This station does not have repair facilities to use! aborting attempt to use non-existent repair facility.");
                    return true;
                }

                if (Instance.InDockableLocation)
                {
                    var repairWindow = Instance.DirectEve.Windows.OfType<DirectRepairShopWindow>().FirstOrDefault();

                    if (doneUsingRepairWindow)
                    {
                        doneUsingRepairWindow = false;
                        if (repairWindow != null) repairWindow.Close();
                        return true;
                    }

                    if (repairWindow == null)
                    {
                        Log.WriteLine("Opening repairshop window");
                        Instance.DirectEve.OpenRepairShop();
                        var random1 = new Random();
                        Instance.Time.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(random1.Next(1, 3));
                        return false;
                    }

                    if (Instance.DirectEve.GetItemHangar() == null)
                    {
                        Log.WriteLine("if (Cache.Instance.ItemHangar == null)");
                        return false;
                    }

                    if (Instance.DirectEve.GetShipHangar() == null)
                    {
                        Log.WriteLine("if (Cache.Instance.ShipHangar == null)");
                        return false;
                    }

                    if (Instance.Drones.UseDrones)
                        if (Instance.Drones.DroneBay == null)
                            return false;

                    if (Instance.DirectEve.GetShipHangar().Items == null)
                    {
                        Log.WriteLine("Cache.Instance.ShipHangar.Items == null");
                        return false;
                    }

                    var repairAllItems = Instance.DirectEve.GetShipHangar().Items;

                    repairAllItems.AddRange(Instance.DirectEve.GetItemHangar().Items);

                    if (Instance.Drones.UseDrones)
                        repairAllItems.AddRange(Instance.Drones.DroneBay.Items);

                    if (repairAllItems.Any())
                    {
                        if (String.IsNullOrEmpty(repairWindow.AvgDamage()))
                        {
                            Log.WriteLine("Add items to repair list");
                            repairWindow.RepairItems(repairAllItems);
                            var random1 = new Random();
                            Instance.Time.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(random1.Next(2, 4));
                            return false;
                        }

                        Log.WriteLine("Repairing Items: repairWindow.AvgDamage: " + repairWindow.AvgDamage());
                        if (repairWindow.AvgDamage().Equals("Avg: 0.0 % Damaged") || repairWindow.AvgDamage().Equals("Avg: 0,0 % Damaged"))
                        {
                            Log.WriteLine("Repairing Items: Zero Damage: skipping repair.");
                            repairWindow.Close();
                            return true;
                        }

                        repairWindow.RepairAll();
                        var random2 = new Random();
                        Instance.Time.NextRepairItemsAction = DateTime.UtcNow.AddSeconds(random2.Next(5, 6));
                        return false;
                    }

                    Log.WriteLine("No items available, nothing to repair.");
                    return true;
                }

                Log.WriteLine("Not in station.");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception:" + ex);
                return false;
            }
        }

        #endregion Methods
    }
}