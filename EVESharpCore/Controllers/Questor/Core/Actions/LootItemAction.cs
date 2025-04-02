using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void LootItemAction(Action action)
        {
            try
            {
                ControllerManager.Instance.GetController<SalvageController>().MissionLoot = true;
                List<string> targetContainerNames = null;
                if (action.GetParameterValues("target") != null)
                    targetContainerNames = action.GetParameterValues("target");

                if ((targetContainerNames == null || !targetContainerNames.Any()) && ESCache.Instance.EveAccount.CS.QMS.QS.LootItemRequiresTarget)
                    Log.WriteLine(" *** No Target Was Specified In the LootItem Action! ***");

                List<string> itemsToLoot = null;
                if (action.GetParameterValues("item") != null)
                    itemsToLoot = action.GetParameterValues("item");

                if (itemsToLoot == null)
                {
                    Log.WriteLine(" *** No Item Was Specified In the LootItem Action! ***");
                    Nextaction();
                }

                // if we are not generally looting we need to re-enable the opening of wrecks to
                // find this LootItems we are looking for
                ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = true;

                int quantity;
                if (!int.TryParse(action.GetParameterValue("quantity"), out quantity))
                    quantity = 1;

                if (ESCache.Instance.CurrentShipsCargo != null &&
                    ESCache.Instance.CurrentShipsCargo.Items.Where(i => itemsToLoot != null && itemsToLoot.Contains(i.TypeName)).Sum(j => j.Stacksize) >= quantity)
                {
                    Log.WriteLine("We are done - we have the item(s)");

                    // now that we have completed this action revert OpenWrecks to false
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
                // we re-sot by distance on every pulse. The order will be potentially different on each pulse as we move around the field. this is ok and desirable.
                //
                var containers =
                    ESCache.Instance.Containers.Where(e => !ESCache.Instance.LootedContainers.Contains(e.Id))
                        .OrderByDescending(e => e.GroupId == (int)Group.CargoContainer)
                        .ThenBy(e => e.IsWreckEmpty)
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
                    foreach (var _targetContainer in targetContainerNames)
                    {
                        i++;
                        Log.WriteLine("TargetContainerName [" + i + "][ " + _targetContainer + " ]");
                    }
                }

                if (!containers.Any())
                {
                    Log.WriteLine("no containers left to loot, next action");

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
                if (targetContainerNames != null && targetContainerNames.Any())
                    foreach (var continerToLoot in containers)
                    {
                        if (targetContainerNames.Any())
                            foreach (var targetContainerName in targetContainerNames)
                            {
                                if (continerToLoot.Name == targetContainerName)
                                    if (!ESCache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                        ESCache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);

                                continue;
                            }
                        else
                            foreach (var _unlootedcontainer in ESCache.Instance.UnlootedContainers)
                            {
                                if (continerToLoot.Name == _unlootedcontainer.Name)
                                    if (!ESCache.Instance.ListofContainersToLoot.Contains(continerToLoot.Id))
                                        ESCache.Instance.ListofContainersToLoot.Add(continerToLoot.Id);

                                continue;
                            }

                        continue;
                    }

                if (itemsToLoot != null && itemsToLoot.Any())
                    foreach (var _itemToLoot in itemsToLoot)
                        if (!ESCache.Instance.ListofMissionCompletionItemsToLoot.Contains(_itemToLoot))
                            ESCache.Instance.ListofMissionCompletionItemsToLoot.Add(_itemToLoot);

                EntityCache container;
                if (targetContainerNames != null && targetContainerNames.Any())
                    container = containers.FirstOrDefault(c => targetContainerNames.Contains(c.Name));
                else
                    container = containers.FirstOrDefault();

                if (container != null)
                    if (container.Distance > (int)Distances.SafeScoopRange)
                        ESCache.Instance.NavigateOnGrid.NavigateToTarget(container, "CombatMissionCtrl", false, 0);
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