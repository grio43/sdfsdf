extern alias SC;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using AmmoType = SC::SharedComponents.EVE.ClientSettings.AmmoType;

namespace EVESharpCore.Controllers.Questor.Core.Activities
{
    public class Arm
    {
        #region Fields

        public bool SwitchShipsOnly;
        private DirectInvType _droneInvTypeItem;
        private int _itemsLeftToMoveQuantity;
        private DateTime _lastArmAction;
        private DateTime _lastFitAction = DateTime.UtcNow;
        private int AncillaryShieldBoosterScripts = 0;
        private bool bWaitingonScripts = false;
        private int CapacitorInjectorScripts = 0;
        private IEnumerable<DirectItem> cargoItems;
        private bool CustomFittingFound;
        private bool DefaultFittingChecked;

        //false; //flag to check for the correct default fitting before using the fitting manager
        private bool DefaultFittingFound;

        private int DroneBayRetries = 0;
        private DirectItem ItemHangarItem;
        private IEnumerable<DirectItem> ItemHangarItems;
        private bool ItemsAreBeingMoved;
        private DateTime NextSwitchShipsRetry = DateTime.MinValue;

        private int SensorBoosterScripts = 0;

        private int SensorDampenerScripts = 0;

        private int SwitchingShipRetries = 0;

        private int TrackingComputerScripts = 0;

        // Chant - 05/03/2016 - globals for moving scripts to cargo
        private int TrackingDisruptorScripts = 0;

        private int TrackingLinkScripts = 0;

        //Did we find the default fitting?

        private int WeHaveThisManyOfThoseItemsInAmmoHangar;
        private int WeHaveThisManyOfThoseItemsInCargo;
        private int WeHaveThisManyOfThoseItemsInItemHangar;
        private int WeHaveThisManyOfThoseItemsInLootHangar;

        #endregion Fields

        #region Properties

        public bool ArmLoadCapBoosters => ESCache.Instance.EveAccount.CS.QMS.QS.ArmLoadCapBoosters;

        public bool NeedRepair { get; set; }

        private DirectInvType DroneInvTypeItem
        {
            get
            {
                try
                {
                    if (_droneInvTypeItem == null)
                    {
                        if (DebugConfig.DebugArm)
                            Log.WriteLine(" Drones.DroneTypeID: " + ESCache.Instance.MissionSettings.CurrentDroneTypeId);
                        _droneInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.MissionSettings.CurrentDroneTypeId);
                    }

                    return _droneInvTypeItem;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private DateTime LastRepairDateTime { get; set; }

        #endregion Properties

        #region Methods

        public bool ChangeArmState(ArmState state, bool wait = false)
        {
            try
            {
                switch (state)
                {
                    case ArmState.OpenShipHangar:
                        ESCache.Instance.State.CurrentCombatState = CombatState.Idle;
                        break;

                    case ArmState.NotEnoughAmmo:
                        ControllerManager.Instance.SetPause(true);
                        ESCache.Instance.State.CurrentCombatState = CombatState.Idle;
                        break;
                }

                if (ESCache.Instance.State.CurrentArmState != state)
                {
                    ClearDataBetweenStates();
                    ESCache.Instance.State.CurrentArmState = state;
                    if (wait)
                        _lastArmAction = DateTime.UtcNow;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public void ClearDataBetweenStates()
        {
            _itemsLeftToMoveQuantity = 0;
        } // check

        public void InvalidateCache()
        {
            cargoItems = null;
            ItemHangarItem = null;
            ItemHangarItems = null;
        } // check

        public void ProcessState()
        {
            try
            {
                if (!ESCache.Instance.InDockableLocation)
                    return;

                if (ESCache.Instance.InSpace)
                    return;

                if (ESCache.Instance.Time.NextArmAction > DateTime.UtcNow)
                    return;

                switch (ESCache.Instance.State.CurrentArmState)
                {
                    case ArmState.Idle:
                        break;

                    case ArmState.Begin:
                        if (!BeginArm()) break;
                        break;

                    case ArmState.ActivateCombatShip:
                        if (!ActivateCombatShip()) return;
                        break;

                    case ArmState.RepairShop:
                        if (!RepairShop()) return;
                        break;

                    case ArmState.StripFitting:
                        if (!StripFitting()) return;
                        break;

                    case ArmState.LoadSavedFitting:
                        if (!LoadSavedFitting()) return;
                        break;

                    case ArmState.MoveDrones:
                        if (!MoveDrones()) return;
                        break;

                    case ArmState.MoveMissionItems:
                        if (!MoveMissionItems()) return;
                        break;

                    case ArmState.MoveOptionalItems:
                        if (!MoveOptionalItems()) return;
                        break;

                    case ArmState.MoveScripts:
                        if (!MoveScripts()) return;
                        break;

                    case ArmState.MoveCapBoosters:
                        if (!MoveCapBoosters()) return;
                        break;

                    case ArmState.MoveAmmo:
                        if (!MoveAmmo()) return;
                        break;

                    case ArmState.StackAmmoHangar:
                        if (!StackAmmoHangar()) return;
                        break;

                    case ArmState.Cleanup:
                        if (!Cleanup()) return;
                        break;

                    case ArmState.Done:
                        break;

                    case ArmState.ActivateTransportShip:
                        if (!ActivateTransportShip()) return;
                        break;

                    case ArmState.NotEnoughDrones: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;

                    case ArmState.NotEnoughAmmo: //This is logged in questor.cs - do not double log, stay in this state until dislodged elsewhere
                        break;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return;
            }
        }

        public void RefreshMissionItems(long agentId)
        {
            if (ESCache.Instance.State.CurrentQuestorState != QuestorState.CombatMissionsBehavior)
            {
                return;
            }

            var missionDetailsForMissionItems = ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == agentId).FirstOrDefault();
            if (missionDetailsForMissionItems == null)
                return;

            ESCache.Instance.MissionSettings.MissionItems.Clear();
            ESCache.Instance.MissionSettings.MoveMissionItems = string.Empty;
            ESCache.Instance.MissionSettings.MoveOptionalMissionItems = string.Empty;

            var missionName = Log.FilterPath(missionDetailsForMissionItems.Name);
            ESCache.Instance.MissionSettings.MissionXmlPath = Path.Combine(ESCache.Instance.MissionSettings.MissionsPath, missionName + ".xml");
            if (!File.Exists(ESCache.Instance.MissionSettings.MissionXmlPath))
                return;

            try
            {
                var xdoc = XDocument.Load(ESCache.Instance.MissionSettings.MissionXmlPath);
                var items =
                    ((IEnumerable)
                        xdoc.XPathEvaluate(
                            "//action[(translate(@name, 'LOT', 'lot')='loot') or (translate(@name, 'LOTIEM', 'lotiem')='lootitem')]/parameter[translate(@name, 'TIEM', 'tiem')='item']/@value")
                    )
                    .Cast<XAttribute>()
                    .Select(a => ((string)a ?? string.Empty).ToLower());
                ESCache.Instance.MissionSettings.MissionItems.AddRange(items);

                if (xdoc.Root != null)
                {
                    ESCache.Instance.MissionSettings.MoveMissionItems = (string)xdoc.Root.Element("bring") ?? string.Empty;
                    ESCache.Instance.MissionSettings.MoveMissionItems = ESCache.Instance.MissionSettings.MoveMissionItems.ToLower();
                    if (DebugConfig.DebugArm)
                        Log.WriteLine("bring XML [" + xdoc.Root.Element("bring") + "] BringMissionItem [" + ESCache.Instance.MissionSettings.MoveMissionItems + "]");
                    ESCache.Instance.MissionSettings.MoveMissionItemsQuantity = (int?)xdoc.Root.Element("bringquantity") ?? 1;
                    if (DebugConfig.DebugArm)
                        Log.WriteLine("bringquantity XML [" + xdoc.Root.Element("bringquantity") + "] BringMissionItemQuantity [" +
                                      ESCache.Instance.MissionSettings.MoveMissionItemsQuantity + "]");

                    ESCache.Instance.MissionSettings.MoveOptionalMissionItems = (string)xdoc.Root.Element("trytobring") ?? string.Empty;
                    ESCache.Instance.MissionSettings.MoveOptionalMissionItems = ESCache.Instance.MissionSettings.MoveOptionalMissionItems.ToLower();
                    if (DebugConfig.DebugArm)
                        Log.WriteLine("trytobring XML [" + xdoc.Root.Element("trytobring") + "] BringOptionalMissionItem [" +
                                      ESCache.Instance.MissionSettings.MoveOptionalMissionItems +
                                      "]");
                    ESCache.Instance.MissionSettings.MoveOptionalMissionItemQuantity = (int?)xdoc.Root.Element("trytobringquantity") ?? 1;
                    if (DebugConfig.DebugArm)
                        Log.WriteLine("trytobringquantity XML [" + xdoc.Root.Element("trytobringquantity") + "] BringOptionalMissionItemQuantity [" +
                                      ESCache.Instance.MissionSettings.MoveOptionalMissionItemQuantity + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Error loading mission XML file [" + ex.Message + "]");
            }
        }

        private bool ActivateCombatShip() // -> ArmState.RepairShop
        {
            try
            {
                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CS.QMS.CombatShipName))
                {
                    Log.WriteLine("Could not find CombatShipName: " + ESCache.Instance.EveAccount.CS.QMS.CombatShipName + " in settings!");
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                if (!ActivateShip(ESCache.Instance.EveAccount.CS.QMS.CombatShipName))
                    return false;

                if (SwitchShipsOnly)
                {
                    ChangeArmState(ArmState.Done, true);
                    SwitchShipsOnly = false;
                    return true;
                }

                ChangeArmState(ArmState.RepairShop, true);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool ActivateShip(string shipName)
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(4000, 5000))) return false;

                //
                // is the ShipName is already the current ship? (we may have started in the right ship!)
                //

                if (ESCache.Instance.DirectEve.ActiveShip == null)
                {
                    Log.WriteLine("Activeship is null.");
                    return false;
                }

                if (shipName == null)
                {
                    Log.WriteLine("shipName == null.");
                    return false;
                }

                if (ESCache.Instance.DirectEve.ActiveShip.GivenName == null)
                {
                    Log.WriteLine("QCache.Instance.DirectEve.ActiveShip.GivenName == null.");
                    return false;
                }

                if (ESCache.Instance.DirectEve.ActiveShip.GivenName.ToLower() == shipName.ToLower())
                    return true;

                if (NextSwitchShipsRetry > DateTime.UtcNow) return false;

                //
                // Check and warn the use if their config is hosed.
                //
                if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CS.QMS.CombatShipName))
                {
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                if (SwitchingShipRetries > 4)
                {
                    Log.WriteLine("Could not switch ship after 4 retries. Error.");
                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                //
                // we have the shipname configured but it is not the current ship
                //
                if (!string.IsNullOrEmpty(shipName))
                {
                    if (ESCache.Instance.DirectEve.GetShipHangar() == null) return false;

                    var shipsInShipHangar = ESCache.Instance.DirectEve.GetShipHangar().Items;
                    if (shipsInShipHangar.Any(s => s.GivenName != null && s.GivenName.ToLower() == shipName.ToLower()))
                    {
                        var ship = shipsInShipHangar.FirstOrDefault(s => s.GivenName != null && s.GivenName.ToLower() == shipName.ToLower());
                        if (ship != null)
                        {
                            Log.WriteLine("Making [" + ship.GivenName + "] active");
                            ship.ActivateShip();
                            SwitchingShipRetries++;
                            NextSwitchShipsRetry = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(4, 6));
                            _lastArmAction = DateTime.UtcNow;
                            return false;
                        }

                        return false;
                    }

                    if (ESCache.Instance.DirectEve.GetShipHangar().Items.Any())
                    {
                        Log.WriteLine("Unable to find a ship named [" + shipName.ToLower() + "] in this station. Found the following ships:");
                        foreach (var shipInShipHangar in ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.GivenName != null))
                            Log.WriteLine("GivenName [" + shipInShipHangar.GivenName.ToLower() + "] TypeName[" + shipInShipHangar.TypeName + "]");

                        if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.GroupId == (int)Group.Capsule)
                        {
                            Log.WriteLine("Capsule detected... this shouldn't happen, disabling this instance.");

                            ESCache.Instance.DisableThisInstance();
                        }

                        if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                        return false;
                    }

                    if (!ChangeArmState(ArmState.NotEnoughAmmo, false)) return false;
                    return false;
                }

                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool ActivateTransportShip() // check
        {
            if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CS.QMS.TransportShipName))
            {
                Log.WriteLine("Could not find transportshipName in settings!");
                ChangeArmState(ArmState.NotEnoughAmmo);
                return false;
            }

            if (!ActivateShip(ESCache.Instance.EveAccount.CS.QMS.TransportShipName)) return false;

            Log.WriteLine("Done");
            ChangeArmState(ArmState.Cleanup);
            return true;
        }

        private bool BeginArm() // --> ArmState.ActivateCombatShip
        {
            try
            {
                DefaultFittingChecked = false; //flag to check for the correct default fitting before using the fitting manager
                DefaultFittingFound = false; //Did we find the default fitting?
                CustomFittingFound = false;
                ItemsAreBeingMoved = false;
                SwitchShipsOnly = false;
                if (DebugConfig.DebugArm)
                    Log.WriteLine("Cache.Instance.BringOptionalMissionItemQuantity is [" + ESCache.Instance.MissionSettings.MoveOptionalMissionItemQuantity + "]");
                DroneBayRetries = 0;
                SwitchingShipRetries = 0;
                RefreshMissionItems(ESCache.Instance.Agent.AgentId);
                ESCache.Instance.State.CurrentCombatState = CombatState.Idle;

                ChangeArmState(ArmState.ActivateCombatShip);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool Cleanup()
        {
            if (ESCache.Instance.DirectEve.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
            {
                ESCache.Instance.DirectEve.FittingManagerWindow.Close();
                return true;
            }
            ESCache.Instance.State.CurrentArmState = ArmState.Done;
            return false;
        }

        private bool DoesDefaultFittingExist()
        {
            try
            {
                DefaultFittingFound = false;
                if (!DefaultFittingChecked)
                {
                    if (DebugConfig.DebugFittingMgr)
                        Log.WriteLine("Character Settings XML says Default Fitting is [" + ESCache.Instance.MissionSettings.DefaultFitting + "]");

                    if (ESCache.Instance.DirectEve.FittingManagerWindow == null)
                    {
                        Log.WriteLine("FittingManagerWindow is null");
                        return false;
                    }

                    if (DebugConfig.DebugFittingMgr)
                        Log.WriteLine("Character Settings XML says Default Fitting is [" + ESCache.Instance.MissionSettings.DefaultFitting + "]");

                    if (ESCache.Instance.DirectEve.FittingManagerWindow.Fittings.Any())
                    {
                        if (DebugConfig.DebugFittingMgr)
                            Log.WriteLine("if (Cache.Instance.FittingManagerWindow.Fittings.Any())");
                        var i = 1;
                        foreach (var fitting in ESCache.Instance.DirectEve.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            if (DebugConfig.DebugFittingMgr)
                                Log.WriteLine("[" + i + "] Found a Fitting Named: [" + fitting.Name + "]");

                            if (fitting.Name.ToLower().Equals(ESCache.Instance.MissionSettings.DefaultFitting.FittingName.ToLower()))
                            {
                                DefaultFittingChecked = true;
                                DefaultFittingFound = true;
                                Log.WriteLine("[" + i + "] Found Default Fitting [" + fitting.Name + "]");
                                return true;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Log.WriteLine("No Fittings found in the Fitting Manager at all!  Disabling fitting manager.");
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;
                        return true;
                    }

                    if (!DefaultFittingFound)
                    {
                        Log.WriteLine("Error! Could not find Default Fitting [" + ESCache.Instance.MissionSettings.DefaultFitting.FittingName.ToLower() +
                                      "].  Disabling fitting manager.");
                        DefaultFittingChecked = true;
                        DefaultFittingFound = false;

                        if (ESCache.Instance.DirectEve.Windows.OfType<DirectFittingManagerWindow>().FirstOrDefault() != null)
                        {
                            Log.WriteLine("Closing Fitting Manager");
                            ESCache.Instance.DirectEve.FittingManagerWindow.Close();
                        }

                        ChangeArmState(ArmState.MoveMissionItems);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool LoadSavedFitting() // --> ArmState.MoveDrones
        {
            if (DateTime.UtcNow < _lastFitAction.AddMilliseconds(ESCache.Instance.RandomNumber(700, 1200)))
                return false;

            try
            {
                var agent = ESCache.Instance.Agent;

                if (agent == null)
                {
                    ChangeArmState(ArmState.MoveDrones, true);
                    return true;
                }

                try
                {
                    if (ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == agent.AgentId).FirstOrDefault().State != (int)MissionState.Accepted)
                    {
                        ChangeArmState(ArmState.MoveDrones, true);
                        return true;
                    }
                }
                catch (Exception)
                {
                    ChangeArmState(ArmState.MoveDrones, true);
                    return true;
                }

                if (ESCache.Instance.EveAccount.CS.QMS.QS.UseFittingManager && ESCache.Instance.MissionSettings.Mission != null)
                {
                    //If we are already loading a fitting...
                    if (ItemsAreBeingMoved)
                    {
                        if (!WaitForLockedItems(ArmState.MoveDrones)) return false;
                        return true;
                    }

                    if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                    {
                        //let's check first if we need to change fitting at all
                        if (!string.IsNullOrEmpty(ESCache.Instance.MissionSettings.FittingToLoad) && !string.IsNullOrEmpty(ESCache.Instance.MissionSettings.CurrentFittingName) &&
                            ESCache.Instance.MissionSettings.FittingToLoad.Equals(ESCache.Instance.MissionSettings.CurrentFittingName))
                        {
                            Log.WriteLine("Correct fitting is already loaded.");
                            ChangeArmState(ArmState.MoveDrones, false);
                            return true;
                        }

                        //let's check first if we need to change fitting at all
                        if (string.IsNullOrEmpty(ESCache.Instance.MissionSettings.FittingToLoad))
                        {
                            Log.WriteLine("No fitting to load.");
                            ChangeArmState(ArmState.MoveDrones, true);
                            return true;
                        }

                        if (!DoesDefaultFittingExist()) return false;

                        if (!DefaultFittingFound)
                        {
                            ChangeArmState(ArmState.MoveDrones, true);
                            return false;
                        }

                        if (ESCache.Instance.DirectEve.FittingManagerWindow == null) return false;

                        Log.WriteLine("Looking for saved fitting named: [" + ESCache.Instance.MissionSettings.FittingToLoad + " ]");

                        foreach (var fitting in ESCache.Instance.DirectEve.FittingManagerWindow.Fittings)
                        {
                            //ok found it
                            var currentShip = ESCache.Instance.ActiveShip;
                            if (ESCache.Instance.MissionSettings.FittingToLoad.ToLower().Equals(fitting.Name.ToLower()) && fitting.ShipTypeId == currentShip.TypeId)
                            {
                                ESCache.Instance.Time.NextArmAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                                Log.WriteLine("Found saved fitting named: [ " + fitting.Name + " ][" +
                                              Math.Round(ESCache.Instance.Time.NextArmAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + "sec]");

                                //switch to the requested fitting for the current mission
                                fitting.Fit();
                                _lastArmAction = DateTime.UtcNow;
                                _lastFitAction = DateTime.UtcNow;
                                ItemsAreBeingMoved = true;
                                ESCache.Instance.MissionSettings.CurrentFittingName = fitting.Name;
                                ESCache.Instance.MissionSettings.OfflineModulesFound = false;
                                CustomFittingFound = true;
                                return false;
                            }

                            continue;
                        }

                        //if we did not find it, we'll set currentfit to default
                        //this should provide backwards compatibility without trying to fit always
                        if (!CustomFittingFound)
                        {
                            Log.WriteLine("Could not find fitting - switching to default");
                            ChangeArmState(ArmState.MoveDrones, true);
                            return false;
                        }
                    }
                }

                ChangeArmState(ArmState.MoveDrones, true);
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool LookForItem(string itemToFind, DirectContainer hangarToCheckForItemsdWeAlreadyMoved)
        {
            try
            {
                WeHaveThisManyOfThoseItemsInCargo = 0;
                WeHaveThisManyOfThoseItemsInItemHangar = 0;
                WeHaveThisManyOfThoseItemsInAmmoHangar = 0;
                WeHaveThisManyOfThoseItemsInLootHangar = 0;
                cargoItems = new List<DirectItem>();

                ItemHangarItems = new List<DirectItem>();
                ItemHangarItem = null;

                // check the local cargo for items and subtract the items in the cargo from the quantity we still need to move to our cargohold
                //
                if (hangarToCheckForItemsdWeAlreadyMoved != null && hangarToCheckForItemsdWeAlreadyMoved.Items.Any())
                {
                    cargoItems =
                        hangarToCheckForItemsdWeAlreadyMoved.Items.Where(i => (i.TypeName ?? string.Empty).ToLower().Equals(itemToFind.ToLower())).ToList();
                    WeHaveThisManyOfThoseItemsInCargo = cargoItems.Sum(i => i.Stacksize);
                    //do not return here
                }

                //
                // check itemhangar for the item
                //
                try
                {
                    if (ESCache.Instance.DirectEve.GetItemHangar() == null) return false;
                    if (ESCache.Instance.DirectEve.GetItemHangar().Items.Any())
                        if (ESCache.Instance.DirectEve.GetItemHangar().Items.Any(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()))
                        {
                            ItemHangarItems =
                                ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => (i.TypeName ?? string.Empty).ToLower() == itemToFind.ToLower()).ToList();
                            ItemHangarItem = ItemHangarItems.OrderBy(s => s.Stacksize).FirstOrDefault();
                            WeHaveThisManyOfThoseItemsInItemHangar = ItemHangarItems.Sum(i => i.Stacksize);
                            if (DebugConfig.DebugArm)
                                Log.WriteLine("We have [" + WeHaveThisManyOfThoseItemsInItemHangar + "] [" + itemToFind + "] in ItemHangar");
                            return true;
                        }
                }
                catch (Exception ex)
                {
                    if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                }

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        private bool MoveAmmo() // --> ArmState.StackAmmoHangar
        {
            try
            {
                if (DateTime.UtcNow < _lastArmAction.AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)))
                    return false;

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int)Group.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int)Group.BlockadeRunner ||
                    ESCache.Instance.ActiveShip.GroupId == (int)Group.RookieShip ||
                    ESCache.Instance.ActiveShip.GivenName != ESCache.Instance.EveAccount.CS.QMS.CombatShipName)
                {
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                if (ESCache.Instance.DirectEve.Weapons.Any(i => i.TypeId == (int)TypeID.CivilianGatlingAutocannon
                                                     || i.TypeId == (int)TypeID.CivilianGatlingPulseLaser
                                                     || i.TypeId == (int)TypeID.CivilianGatlingRailgun
                                                     || i.TypeId == (int)TypeID.CivilianLightElectronBlaster))
                {
                    Log.WriteLine("No ammo needed for civilian guns: done");
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(ArmState.MoveAmmo)) return false;
                    return true; // this might make trouble
                }

                foreach (var ammo in ESCache.Instance.Combat.Ammo)
                {
                    if (ESCache.Instance.DirectEve.GetItemHangar().Items.Any(i => i.TypeId == ammo.TypeId && !i.IsSingleton && i.Quantity >= ammo.Quantity))
                    {
                        if (DebugConfig.DebugArm)
                            Log.WriteLine($"We have enough [{ammo.DamageType}] ammo in the hangar.");
                        continue;
                    }

                    Log.WriteLine($"Error: We don't have enough [{ammo.DamageType}] ammo in the hangar.");
                    ChangeArmState(ArmState.NotEnoughAmmo);
                    return false;
                }

                AmmoType missing = null;
                foreach (var ammo in ESCache.Instance.Combat.Ammo)
                {
                    if (ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ammo.TypeId && !i.IsSingleton).Sum(i => i.Stacksize) < ammo.Quantity)
                    {
                        missing = ammo;
                        break;
                    }
                }

                var CurrentAmmoToLoad = missing;

                if (CurrentAmmoToLoad == null)
                {
                    Log.WriteLine("We have no more ammo types to be loaded. We have to be finished with arm.");
                    ChangeArmState(ArmState.StackAmmoHangar);
                    return false;
                }

                try
                {
                    List<DirectItem> AmmoHangarItems = null;
                    IEnumerable<DirectItem> AmmoItems = null;
                    if (ESCache.Instance.DirectEve.GetItemHangar() != null && ESCache.Instance.DirectEve.GetItemHangar().Items != null)
                    {
                        if (DebugConfig.DebugArm) Log.WriteLine("if (Cache.Instance.AmmoHangar != null && Cache.Instance.AmmoHangar.Items != null)");
                        AmmoHangarItems =
                            ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId)
                                .OrderBy(i => !i.IsSingleton)
                                .ThenByDescending(i => i.Quantity)
                                .ToList();
                        AmmoItems = AmmoHangarItems.ToList();
                    }

                    if (AmmoHangarItems == null)
                    {
                        _lastArmAction = DateTime.UtcNow;
                        Log.WriteLine("if(AmmoHangarItems == null)");
                        return false;
                    }

                    if (DebugConfig.DebugArm)
                        Log.WriteLine("Ammohangar has [" + AmmoHangarItems.Count() + "] items with the right typeID [" + CurrentAmmoToLoad.TypeId +
                                      "] for this ammoType. MoveAmmo will use AmmoHangar");

                    try
                    {
                        var itemnum = 0;

                        if (AmmoItems != null)
                        {
                            AmmoItems = AmmoItems.ToList();
                            if (AmmoItems.Any())
                                foreach (var item in AmmoItems)
                                {
                                    itemnum++;
                                    var quant = CurrentAmmoToLoad.Quantity;

                                    if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.TypeId == CurrentAmmoToLoad.TypeId))
                                    {
                                        var amountInCargo = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == CurrentAmmoToLoad.TypeId).Sum(i => i.Stacksize);
                                        if (amountInCargo < quant)
                                            quant -= amountInCargo;
                                    }

                                    var moveAmmoQuantity = Math.Min(item.Stacksize, quant);

                                    moveAmmoQuantity = Math.Max(moveAmmoQuantity, 1);

                                    if (DebugConfig.DebugArm)
                                        Log.WriteLine("In Hangar we have: [" + itemnum + "] TypeName [" + item.TypeName + "] StackSize [" +
                                                      item.Stacksize +
                                                      "] - CurrentAmmoToLoad.Quantity [" + CurrentAmmoToLoad.Quantity + "] Actual moveAmmoQuantity [" +
                                                      moveAmmoQuantity +
                                                      "]");

                                    if (moveAmmoQuantity <= item.Stacksize && moveAmmoQuantity >= 1)
                                    {
                                        if (ESCache.Instance.CurrentShipsCargo.Add(item, moveAmmoQuantity))
                                        {
                                            Log.WriteLine("Moving [" + moveAmmoQuantity + "] units of Ammo  [" + item.TypeName +
                                                          "] from [ AmmoHangar ] to CargoHold");
                                            //
                                            // move items to cargo
                                            //

                                            ItemsAreBeingMoved = true;
                                            _lastArmAction = DateTime.UtcNow;

                                            //
                                            // subtract the moved items from the items that need to be moved
                                            //

                                            CurrentAmmoToLoad.Quantity -= moveAmmoQuantity;
                                        }
                                    }
                                    else
                                    {
                                        Log.WriteLine("While calculating what to move we wanted to move [" + moveAmmoQuantity + "] units of Ammo  [" +
                                                      item.TypeName +
                                                      "] from [ AmmoHangar ] to CargoHold, but somehow the current Item Stacksize is only [" +
                                                      item.Stacksize + "]");
                                        continue;
                                    }

                                    return false; //you can only move one set of items per frame.
                                }
                        }
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("AmmoItems Exception [" + exception + "]");
                    }
                }
                catch (Exception exception)
                {
                    Log.WriteLine("Error while processing Itemhangar Items exception was: [" + exception + "]");
                }

                _lastArmAction = DateTime.UtcNow;
                ChangeArmState(ArmState.StackAmmoHangar);
                return false;
            }
            catch (Exception ex)
            {
                if (DebugConfig.DebugArm) Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool MoveCapBoosters() // --> ArmState.MoveAmmo
        {
            if (ESCache.Instance.ActiveShip.GivenName != ESCache.Instance.EveAccount.CS.QMS.CombatShipName)
            {
                Log.WriteLine("if (Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)");
                ChangeArmState(ArmState.MoveAmmo);
                return false;
            }

            var _CapBoosterInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript);

            if (ArmLoadCapBoosters && _CapBoosterInvTypeItem != null)
            {
                Log.WriteLine("Calling MoveItemsToCargo");
                if (!MoveItemsToCargo(_CapBoosterInvTypeItem.TypeName, ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorToLoad, ArmState.MoveAmmo,
                    ArmState.MoveCapBoosters))
                    return false;
            }

            ChangeArmState(ArmState.MoveAmmo, true);
            return false;
        }

        private bool MoveDrones() // --> ArmState.MoveMissionItems
        {
            try
            {
                if (DateTime.UtcNow < _lastFitAction.AddMilliseconds(ESCache.Instance.RandomNumber(400, 600)))
                    return false;

                if (!ESCache.Instance.Drones.UseDrones)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "UseDrones is [" + Drones.UseDrones + "] Changing ArmState to MoveBringItems",Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle ||
                    ESCache.Instance.ActiveShip.GroupId == (int)Group.Industrial ||
                    ESCache.Instance.ActiveShip.GroupId == (int)Group.BlockadeRunner)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "ActiveShip GroupID is [" + Cache.Instance.ActiveShip.GroupId + "] Which we assume is a Shuttle, Industrial, TransportShip: Changing ArmState to MoveBringItems", Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (ESCache.Instance.ActiveShip.GivenName != ESCache.Instance.EveAccount.CS.QMS.CombatShipName)
                {
                    //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", "ActiveShip Name is [" + Cache.Instance.ActiveShip.GivenName + "] Which is not the CombatShipname [" + Combat.CombatShipName + "]: Changing ArmState to MoveBringItems", Logging.Debug);
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (DroneInvTypeItem == null)
                {
                    Log.WriteLine("(DroneInvTypeItem == null)");
                    return false;
                }

                //if (Logging.DebugArm) Logging.Log("Arm.MoveDrones", " DroneInvTypeItem.TypeName: " + DroneInvTypeItem.TypeName, Logging.Orange);

                if (!MoveDronesToDroneBay(DroneInvTypeItem.TypeName, ArmState.MoveMissionItems, ArmState.MoveDrones))
                    return false;

                //Logging.Log("Arm.MoveDrones", "MoveDronesToDroneBay returned true! CurrentArmState is [" + _States.CurrentArmState + "]: this should NOT still be MoveDrones!", Logging.Orange);
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool MoveDronesToDroneBay(string itemName, ArmState nextState, ArmState fromState)
        {
            try
            {
                if (DebugConfig.DebugArm) Log.WriteLine("(re)Entering MoveDronesToDroneBay");

                if (string.IsNullOrEmpty(itemName))
                {
                    Log.WriteLine("if (string.IsNullOrEmpty(MoveItemTypeName))");
                    ChangeArmState(nextState);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    Log.WriteLine("if (ItemsAreBeingMoved)");
                    if (!WaitForLockedItems(fromState)) return false;
                    return false;
                }

                if (ESCache.Instance.DirectEve.GetItemHangar() == null)
                {
                    Log.WriteLine("if (Cache.Instance.ItemHangar == null)");
                    return false;
                }

                if (ESCache.Instance.Drones.DroneBay == null)
                {
                    Log.WriteLine("if (Drones.DroneBay == null)");
                    return false;
                }

                if (DroneBayRetries > 10)
                {
                    ChangeArmState(ArmState.MoveMissionItems);
                    return false;
                }

                if (ESCache.Instance.Drones.DroneBay.Capacity == 0 && DroneBayRetries <= 10)
                {
                    DroneBayRetries++;
                    Log.WriteLine("Dronebay: not yet ready.");
                    ESCache.Instance.Time.NextArmAction = DateTime.UtcNow.AddSeconds(2);
                    return false;
                }

                if (!LookForItem(itemName, ESCache.Instance.Drones.DroneBay))
                {
                    Log.WriteLine("if (!LookForItem(MoveItemTypeName, Drones.DroneBay))");
                    return false;
                }

                if (ESCache.Instance.Drones.DroneBay != null && DroneInvTypeItem != null && ESCache.Instance.Drones.DroneBay.Items != null && ESCache.Instance.DirectEve.GetItemHangar() != null &&
                    ESCache.Instance.DirectEve.GetItemHangar().Items != null)
                    if (ESCache.Instance.Drones.DroneBay.Items.Any(d => d.TypeId != DroneInvTypeItem.TypeId))
                    {
                        Log.WriteLine("We have other drones in the bay, moving them to the ammo hangar.");
                        var other_drones = ESCache.Instance.Drones.DroneBay.Items.Where(d => d.TypeId != DroneInvTypeItem.TypeId);
                        ESCache.Instance.DirectEve.GetItemHangar().Add(other_drones);
                        ESCache.Instance.Time.NextArmAction = DateTime.UtcNow.AddMilliseconds(300);
                        return false;
                    }

                Log.WriteLine("Dronebay details: Capacity [" + ESCache.Instance.Drones.DroneBay.Capacity + "] UsedCapacity [" + ESCache.Instance.Drones.DroneBay.UsedCapacity + "]");

                if ((int)ESCache.Instance.Drones.DroneBay.Capacity == (int)ESCache.Instance.Drones.DroneBay.UsedCapacity)
                {
                    Log.WriteLine("if ((int)Drones.DroneBay.Capacity == (int)Drones.DroneBay.UsedCapacity)");
                    Log.WriteLine("Dronebay is Full. No need to move any more drones.");
                    ChangeArmState(nextState);
                    return false;
                }

                if (ESCache.Instance.Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                {
                    var neededDrones = (int)Math.Floor((ESCache.Instance.Drones.DroneBay.Capacity - ESCache.Instance.Drones.DroneBay.UsedCapacity) / DroneInvTypeItem.Volume);
                    _itemsLeftToMoveQuantity = neededDrones;

                    Log.WriteLine("neededDrones: [" + neededDrones + "]");

                    if ((int)neededDrones == 0)
                    {
                        Log.WriteLine("MoveItems");
                        ChangeArmState(ArmState.MoveMissionItems);
                        return false;
                    }

                    if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar < neededDrones)
                    {
                        Log.WriteLine("ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" +
                                      WeHaveThisManyOfThoseItemsInAmmoHangar +
                                      "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemName + "] we need [" + neededDrones +
                                      "] drones to fill the DroneBay)");
                        ItemsAreBeingMoved = false;
                        ControllerManager.Instance.SetPause(true);
                        ChangeArmState(ArmState.NotEnoughDrones);
                        return true;
                    }

                    //  here we check if we have enough free m3 in our drone hangar

                    if (ESCache.Instance.Drones.DroneBay != null && DroneInvTypeItem != null && DroneInvTypeItem.Volume != 0)
                    {
                        var freeCapacity = ESCache.Instance.Drones.DroneBay.Capacity - ESCache.Instance.Drones.DroneBay.UsedCapacity;

                        Log.WriteLine("freeCapacity [" + freeCapacity + "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]" +
                                      " DroneInvTypeItem.Volume [" +
                                      DroneInvTypeItem.Volume + "]");

                        var amount = Convert.ToInt32(freeCapacity / DroneInvTypeItem.Volume);
                        _itemsLeftToMoveQuantity = Math.Min(amount, _itemsLeftToMoveQuantity);

                        Log.WriteLine("freeCapacity [" + freeCapacity + "] amount [" + amount + "] _itemsLeftToMoveQuantity [" +
                                      _itemsLeftToMoveQuantity + "]");
                    }
                    else
                    {
                        Log.WriteLine("Drones.DroneBay || ItemHangarItem != null");
                        ChangeArmState(nextState);
                        return false;
                    }

                    if (_itemsLeftToMoveQuantity <= 0)
                    {
                        Log.WriteLine("if (_itemsLeftToMoveQuantity <= 0)");
                        ChangeArmState(nextState);
                        return false;
                    }

                    if (ItemHangarItem != null && !string.IsNullOrEmpty(ItemHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                    {
                        if (ItemHangarItem.ItemId <= 0 || ItemHangarItem.Volume == 0.00 || ItemHangarItem.Quantity == 0)
                            return false;

                        var dronesItemsInItemHangar = ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => i.TypeId == DroneInvTypeItem.TypeId);
                        var amountOfDrones = dronesItemsInItemHangar.Sum(i => i.Stacksize);

                        Log.WriteLine("There is a total of [" + amountOfDrones + "] of " + DroneInvTypeItem.TypeName + " in Itemhangar.");

                        if (dronesItemsInItemHangar.Any() && amountOfDrones > _itemsLeftToMoveQuantity)
                        {
                            //							dronesItemsInItemHangar.OrderBy(a => a.Stacksize).ToList().ForEach(d => Logging.Logging.Log("(DronesInHangar) Dronename: " + d.TypeName + " Stacksize: " + d.Stacksize));

                            var dronesToMove = new List<DirectItem>();
                            foreach (var droneItem in dronesItemsInItemHangar.OrderBy(a => a.Stacksize))
                            {
                                var qtyToMove = droneItem.Stacksize;
                                if (qtyToMove <= _itemsLeftToMoveQuantity && _itemsLeftToMoveQuantity - qtyToMove >= 0)
                                {
                                    dronesToMove.Add(droneItem);
                                    _itemsLeftToMoveQuantity -= qtyToMove;
                                }

                                if (_itemsLeftToMoveQuantity == 0) break;
                            }

                            if (dronesToMove.Any())
                            {
                                dronesToMove.ForEach(d => Log.WriteLine("(Multi) Dronename: " + d.TypeName + " Stacksize: " + d.Stacksize));

                                if (ESCache.Instance.Drones.DroneBay.Add(dronesToMove))
                                {
                                    ItemsAreBeingMoved = true;
                                    _lastArmAction = DateTime.UtcNow;
                                }
                                return false;
                            }

                            if (dronesItemsInItemHangar.Any(i => i.Stacksize >= _itemsLeftToMoveQuantity))
                            {
                                var stackToMove = dronesItemsInItemHangar.FirstOrDefault(i => i.Stacksize >= _itemsLeftToMoveQuantity);
                                var qtyToMove = Math.Min(stackToMove.Stacksize, _itemsLeftToMoveQuantity);
                                if (ESCache.Instance.Drones.DroneBay.Add(stackToMove, qtyToMove))
                                {
                                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - qtyToMove;
                                    dronesToMove.ForEach(d => Log.WriteLine("(Single) Dronename: " + stackToMove.TypeName + " Stacksize: " + qtyToMove));
                                    ItemsAreBeingMoved = true;
                                    _lastArmAction = DateTime.UtcNow;
                                }
                                return false;
                            }
                        }
                        else
                        {
                            // this should not be called anymore.
                            var moveDroneQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                            moveDroneQuantity = Math.Max(moveDroneQuantity, 1);

                            if (ESCache.Instance.Drones.DroneBay.Add(ItemHangarItem, moveDroneQuantity))
                            {
                                _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveDroneQuantity;
                                Log.WriteLine("Moving Item(5) [" + ItemHangarItem.TypeName + "] from ItemHangar to DroneBay: We have [" +
                                              _itemsLeftToMoveQuantity +
                                              "] more item(s) to move after this");

                                ItemsAreBeingMoved = true;
                                _lastArmAction = DateTime.UtcNow;
                            }
                            return false;
                        }
                    }

                    return true;
                }

                Log.WriteLine("droneTypeId is highly likely to be incorrect in your settings xml");
                return false;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        // check
        private bool MoveItemsToCargo(string itemName, int quantity, ArmState nextState, ArmState fromState,
            bool moveToNextStateIfQuantityIsBelowAsk = false)
        {
            try
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    ChangeArmState(nextState);
                    return false;
                }

                if (ItemsAreBeingMoved)
                {
                    if (!WaitForLockedItems(fromState)) return false;
                    return false;
                }

                if (!LookForItem(itemName, ESCache.Instance.CurrentShipsCargo)) return false;

                if (WeHaveThisManyOfThoseItemsInCargo + WeHaveThisManyOfThoseItemsInItemHangar < quantity)
                    if (!moveToNextStateIfQuantityIsBelowAsk)
                    {
                        Log.WriteLine("ItemHangar has: [" + WeHaveThisManyOfThoseItemsInItemHangar + "] AmmoHangar has: [" +
                                      WeHaveThisManyOfThoseItemsInAmmoHangar +
                                      "] LootHangar has: [" + WeHaveThisManyOfThoseItemsInLootHangar + "] [" + itemName + "] we need [" + quantity +
                                      "] units)");
                        ItemsAreBeingMoved = false;
                        ControllerManager.Instance.SetPause(true);
                        ChangeArmState(ArmState.NotEnoughAmmo);
                        return true;
                    }

                _itemsLeftToMoveQuantity = quantity - WeHaveThisManyOfThoseItemsInCargo > 0 ? quantity - WeHaveThisManyOfThoseItemsInCargo : 0;

                //  here we check if we have enough free m3 in our ship hangar

                if (ESCache.Instance.CurrentShipsCargo == null)
                    return false;

                if (ESCache.Instance.CurrentShipsCargo != null && ItemHangarItem != null)
                {
                    var amount = 0;
                    var freeCapacity = ESCache.Instance.CurrentShipsCargo.Capacity - ESCache.Instance.CurrentShipsCargo.UsedCapacity;
                    var freeCapacityReduced = freeCapacity * 0.7; // keep some free space for ammo
                    if (ItemHangarItem != null)
                        amount = Convert.ToInt32(freeCapacityReduced / ItemHangarItem.Volume);

                    _itemsLeftToMoveQuantity = Math.Min(amount, _itemsLeftToMoveQuantity);

                    Log.WriteLine("freeCapacity [" + freeCapacity + "] freeCapacityReduced [" + freeCapacityReduced + "] amount [" + amount +
                                  "] _itemsLeftToMoveQuantity [" + _itemsLeftToMoveQuantity + "]");
                }
                else // we've got none of the item in our hangars, return true to move on
                {
                    Log.WriteLine("Cache.Instance.CurrentShipsCargo == null || ItemHangarItem == null");
                    ChangeArmState(nextState);
                    ItemsAreBeingMoved = false;
                    return true;
                }

                if (_itemsLeftToMoveQuantity <= 0)
                {
                    Log.WriteLine("if (_itemsLeftToMoveQuantity <= 0)");
                    ChangeArmState(nextState);
                    return false;
                }

                Log.WriteLine("_itemsLeftToMoveQuantity: " + _itemsLeftToMoveQuantity);

                if (ItemHangarItem != null && !string.IsNullOrEmpty(ItemHangarItem.TypeName.ToString(CultureInfo.InvariantCulture)))
                {
                    if (ItemHangarItem.ItemId <= 0 || ItemHangarItem.Volume == 0.00 || ItemHangarItem.Quantity == 0)
                        return false;

                    var moveItemQuantity = Math.Min(ItemHangarItem.Stacksize, _itemsLeftToMoveQuantity);
                    moveItemQuantity = Math.Max(moveItemQuantity, 1);
                    _itemsLeftToMoveQuantity = _itemsLeftToMoveQuantity - moveItemQuantity;
                    if (ESCache.Instance.CurrentShipsCargo.Add(ItemHangarItem, moveItemQuantity))
                    {
                        Log.WriteLine("Moving(2) Item [" + ItemHangarItem.TypeName + "] from ItemHangar to CargoHold: We have [" + _itemsLeftToMoveQuantity +
                                      "] more item(s) to move after this");

                        ItemsAreBeingMoved = true;
                        _lastArmAction = DateTime.UtcNow;
                    }
                    return false;
                }

                ItemsAreBeingMoved = false;
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool MoveMissionItems() // --> MoveOptionalItems
        {
            if (
                !MoveItemsToCargo(ESCache.Instance.MissionSettings.MoveMissionItems, ESCache.Instance.MissionSettings.MoveMissionItemsQuantity, ArmState.MoveOptionalItems,
                    ArmState.MoveMissionItems, false)) return false;
            return false;
        }

        private bool MoveOptionalItems() // --> ArmState.MoveScripts
        {
            if (
                !MoveItemsToCargo(ESCache.Instance.MissionSettings.MoveOptionalMissionItems, ESCache.Instance.MissionSettings.MoveOptionalMissionItemQuantity, ArmState.MoveScripts,
                    ArmState.MoveOptionalItems, true)) return false;
            return false;
        }

        // Chant - 05/02/2016 - need to load sensor manipulation scripts if specified
        private bool MoveScripts() // --> ArmState.MoveCapBoosters
        {
            if (ESCache.Instance.ActiveShip.GivenName != ESCache.Instance.EveAccount.CS.QMS.CombatShipName)
            {
                Log.WriteLine("if (Cache.Instance.ActiveShip.GivenName != Combat.CombatShipName)");
                ChangeArmState(ArmState.MoveCapBoosters);
                return false;
            }

            var TrackingDisruptorScriptsLeft = 0;
            var TrackingComputerScriptsLeft = 0;
            var TrackingLinkScriptsLeft = 0;
            var SensorBoosterScriptsLeft = 0;
            var SensorDampenerScriptsLeft = 0;
            var CapacitorInjectorScriptsLeft = 0;
            var AncillaryShieldBoosterScriptsLeft = 0;

            if (!bWaitingonScripts)
            {
                TrackingDisruptorScriptsLeft =
                    TrackingDisruptorScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.TrackingDisruptor).Sum(i => i.Quantity));
                TrackingComputerScriptsLeft =
                    TrackingComputerScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.TrackingComputer).Sum(i => i.Quantity));
                TrackingLinkScriptsLeft =
                    TrackingLinkScripts = Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.TrackingLink).Sum(i => i.Quantity));
                SensorBoosterScriptsLeft =
                    SensorBoosterScripts = Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.SensorBooster)
                        .Sum(i => i.Quantity));
                SensorDampenerScriptsLeft =
                    SensorDampenerScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.SensorDampener).Sum(i => i.Quantity));
                CapacitorInjectorScriptsLeft =
                    CapacitorInjectorScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.CapacitorInjector).Sum(i => i.Quantity));
                AncillaryShieldBoosterScriptsLeft =
                    AncillaryShieldBoosterScripts =
                        Math.Abs(ESCache.Instance.FittedModules.Items.Where(i => i.GroupId == (int)Group.AncillaryShieldBooster).Sum(i => i.Quantity));

                bWaitingonScripts = true;
            }
            else
            {
                TrackingDisruptorScriptsLeft = Math.Max(0,
                    TrackingDisruptorScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.TrackingDisruptorScript).Sum(i => i.Quantity)));
                TrackingComputerScriptsLeft = Math.Max(0,
                    TrackingComputerScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.TrackingComputerScript).Sum(i => i.Quantity)));
                TrackingLinkScriptsLeft = Math.Max(0,
                    TrackingLinkScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.TrackingLinkScript).Sum(i => i.Quantity)));
                SensorBoosterScriptsLeft = Math.Max(0,
                    SensorBoosterScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.SensorBoosterScript).Sum(i => i.Quantity)));
                SensorDampenerScriptsLeft = Math.Max(0,
                    SensorDampenerScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.SensorDampenerScript).Sum(i => i.Quantity)));
                CapacitorInjectorScriptsLeft = Math.Max(0,
                    CapacitorInjectorScripts -
                    Math.Abs(ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript).Sum(i => i.Quantity)));
                AncillaryShieldBoosterScriptsLeft = Math.Max(0,
                    AncillaryShieldBoosterScripts -
                    Math.Abs(
                        ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.TypeId == ESCache.Instance.EveAccount.CS.QMS.QS.AncillaryShieldBoosterScript).Sum(i => i.Quantity)));
            }

            var _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingDisruptorScript);
            if (TrackingDisruptorScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingDisruptorScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Tracking Disruptor scripts in hangar");
                    TrackingDisruptorScriptsLeft = 0;
                    TrackingDisruptorScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingComputerScript);
            if (TrackingComputerScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingComputerScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Tracking Computer scripts in hangar");
                    TrackingComputerScriptsLeft = 0;
                    TrackingComputerScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.TrackingLinkScript);
            if (TrackingLinkScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, TrackingLinkScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Tracking Link scripts in hangar");
                    TrackingLinkScriptsLeft = 0;
                    TrackingLinkScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.SensorBoosterScript);
            if (SensorBoosterScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                Log.WriteLine("[" + SensorBoosterScriptsLeft + "] SensorBoosterScriptsLeft");
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, SensorBoosterScripts, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Sensor Booster scripts in hangar");
                    SensorBoosterScriptsLeft = 0;
                    SensorBoosterScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.SensorDampenerScript);
            if (SensorDampenerScriptsLeft >= 1 && _ScriptInvTypeItem != null)

            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, SensorDampenerScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Sensor Dampener scripts in hangar");
                    SensorDampenerScriptsLeft = 0;
                    SensorDampenerScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.CapacitorInjectorScript);
            if (CapacitorInjectorScriptsLeft >= 1 && _ScriptInvTypeItem != null)
            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, CapacitorInjectorScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Capacitor Injector scripts in hangar");
                    CapacitorInjectorScriptsLeft = 0;
                    CapacitorInjectorScripts = 0;
                }
                return false;
            }

            _ScriptInvTypeItem = ESCache.Instance.DirectEve.GetInvType(ESCache.Instance.EveAccount.CS.QMS.QS.AncillaryShieldBoosterScript);
            if (AncillaryShieldBoosterScriptsLeft >= 1 && _ScriptInvTypeItem != null)

            {
                if (MoveItemsToCargo(_ScriptInvTypeItem.TypeName, AncillaryShieldBoosterScriptsLeft, ArmState.MoveScripts, ArmState.MoveScripts, true) &&
                    !ItemsAreBeingMoved)
                {
                    Log.WriteLine("Not enough Ancillary Shield Booster scripts in hangar");
                    AncillaryShieldBoosterScriptsLeft = 0;
                    AncillaryShieldBoosterScripts = 0;
                }
                return false;
            }

            Log.WriteLine("Finished moving scripts");
            bWaitingonScripts = false;
            ChangeArmState(ArmState.MoveCapBoosters, true);
            //return successfullyMovedScripts;
            return false;
        }

        private bool RepairShop() // --> ArmState.LoadSavedFitting
        {
            try
            {
                //				Arm.NeedRepair = true;  // enable repair by default

                if (ESCache.Instance.EveAccount.CS.QMS.QS.UseStationRepair && NeedRepair)
                    if (!ESCache.Instance.RepairItems()) return false; //attempt to use repair facilities if avail in station

                NeedRepair = false;

                LastRepairDateTime = DateTime.UtcNow;
                ChangeArmState(ArmState.StripFitting, true);

                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        private bool StackAmmoHangar() // --> ArmState.Done
        {
            if (!(ESCache.Instance.DirectEve.GetItemHangar() != null
                  && ESCache.Instance.DirectEve.GetItemHangar().StackAll())) return false;
            Cleanup();
            ChangeArmState(ArmState.Done);
            return true;
        }

        private bool StripFitting()
        {
            // if we have offline modules and fittingmanage is disabled pause and disable this instance so it can be fixed manually
            if (ESCache.Instance.MissionSettings.OfflineModulesFound && !ESCache.Instance.EveAccount.CS.QMS.QS.UseFittingManager)
            {
                Log.WriteLine("We have offline modules but fitting manager is disabled! Pausing and disabling so this can be fixed manually.");
                ESCache.Instance.DisableThisInstance();
                ESCache.Instance.PauseAfterNextDock = true;
                return true;
            }

            if (!ESCache.Instance.EveAccount.CS.QMS.QS.UseFittingManager)
            {
                ChangeArmState(ArmState.MoveDrones, true);
                return true;
            }

            // if there are no offline modules we do not need to strip the fitting
            if (!ESCache.Instance.MissionSettings.OfflineModulesFound)
            {
                Log.WriteLine("Not stripping fitting as there are no offline modules.");
                ChangeArmState(ArmState.LoadSavedFitting, true);
                return true;
            }

            if (ESCache.Instance.DirectEve.FittingManagerWindow == null) return false;

            ESCache.Instance.MissionSettings.CurrentFittingName = String.Empty; // force to acutally select the correct mission fitting
            var currentShip = ESCache.Instance.ActiveShip;
            Log.WriteLine("Stripping fitting as there are offline modules.");
            currentShip.StripFitting();
            _lastFitAction = DateTime.UtcNow;

            ChangeArmState(ArmState.LoadSavedFitting, true);
            return true;
        }

        private bool WaitForLockedItems(ArmState _armStateToSwitchTo)
        {

            if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                return false;

            _lastArmAction = DateTime.UtcNow.AddSeconds(-1);
            Log.WriteLine("Done");
            ItemsAreBeingMoved = false;
            ChangeArmState(_armStateToSwitchTo);
            return true;
        }

        #endregion Methods
    }
}