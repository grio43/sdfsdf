using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void LootAction(Action action)
        {
            try
            {
                var items = action.GetParameterValues("item");
                var targetNames = action.GetParameterValues("target");

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = true;

                if (ESCache.Instance.CurrentShipsCargo != null && ESCache.Instance.CurrentShipsCargo.Items.Any(i => items.Contains(i.TypeName)))
                {
                    Log.WriteLine("LootEverything:  We are done looting");

                    // now that we are done with this action revert OpenWrecks to false

                    if (ESCache.Instance.NavigateOnGrid.SpeedTank && !ESCache.Instance.EveAccount.CS.QMS.QS.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks) Log.WriteLine("ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;");
                        ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;
                    }

                    ControllerManager.Instance.GetController<SalvageController>().MissionLoot = false;
                    Nextaction();
                    return;
                }

                // unlock targets count
                ControllerManager.Instance.GetController<SalvageController>().MissionLoot = true;

                //
                // sorting by distance is bad if we are moving (we'd change targets unpredictably)... sorting by ID should be better and be nearly the same(?!)
                //
                var containers = ESCache.Instance.Containers.Where(e => !ESCache.Instance.LootedContainers.Contains(e.Id))
                    .OrderBy(i => i.IsWreck)
                    .ThenBy(e => e.Distance);

                if (DebugConfig.DebugLootWrecks)
                {
                    var i = 0;
                    foreach (var _container in containers)
                    {
                        i++;
                        Log.WriteLine("[" + i + "] " + _container.Name + "[" + Math.Round(_container.Distance / 1000, 0) + "k] isWreckEmpty [" +
                                      _container.IsWreckEmpty +
                                      "] IsTarget [" + _container.IsTarget + "]");
                    }

                    i = 0;
                    foreach (var _containerToLoot in ESCache.Instance.ListofContainersToLoot)
                    {
                        i++;
                        Log.WriteLine("_containerToLoot [" + i + "] ID[ " + _containerToLoot + " ]");
                    }
                }

                if (!containers.Any())
                {
                    // lock targets count
                    Log.WriteLine("We are done looting");

                    // now that we are done with this action revert OpenWrecks to false

                    if (ESCache.Instance.NavigateOnGrid.SpeedTank && !ESCache.Instance.EveAccount.CS.QMS.QS.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks) Log.WriteLine("ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;");
                        ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;
                    }

                    ControllerManager.Instance.GetController<SalvageController>().MissionLoot = false;
                    Nextaction();
                    return;
                }

                //
                // add containers that we were told to loot into the ListofContainersToLoot so that they are prioritized by the background salvage routine
                //
                if (targetNames != null && targetNames.Any())
                    foreach (var continerToLoot in containers)
                        if (continerToLoot.Name == targetNames.FirstOrDefault())
                            if (!ESCache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                ESCache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);

                var container = containers.FirstOrDefault(c => targetNames != null && targetNames.Contains(c.Name)) ?? containers.FirstOrDefault();
                if (container != null)
                    if (container.Distance > (int)Distances.SafeScoopRange)
                        ESCache.Instance.NavigateOnGrid.NavigateToTarget(container, "CombatMissionCtrl.LootAction", false, 0);
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception logged was [" + exception + "]");
                return;
            }
        }

        #endregion Methods
    }
}