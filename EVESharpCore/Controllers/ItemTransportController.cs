extern alias SC;


using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Traveller;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Events;
using System;
using System.Collections.Generic;
using System.Linq;
//using System.Xml.Linq; // No longer needed for settings
using SC::SharedComponents.EVE.ClientSettings.ItemTransport;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;

namespace EVESharpCore.Controllers
{
    public class ItemTransportController : BaseController
    {
        #region Enums

        private enum TransportState
        {
            Idle,
            InitializeRun,
            SelectNextPickupStation,
            TravelingToPickupStation,
            PickingUpItems,
            TravelingToDeliveryStation,
            DeliveringItems,
            WaitOnErrorCooldown,
            Done,
            Error
        }

        #endregion

        #region Settings Accessor Property

        // Convenience property to access the specific settings section
        private ItemTransportMainSetting Settings => ESCache.Instance.EveAccount.ClientSetting.ItemTransportMainSetting;

        #endregion

        #region Fields

        private TransportState _currentState = TransportState.Idle;
        private TransportState _previousState = TransportState.Idle;
        private TravelerDestination _destination; // Keep this for the Traveler component

        // State for managing multiple pickup locations
        private Queue<long> _pickupStationsQueue = new Queue<long>();
        private long? _currentPickupTargetID = null;
        private bool _needsDeliveryRun = false; // Flag to indicate cargo needs delivering

        // Error handling
        private int _errorCounter = 0;
        private const int MaxErrorCount = 5; // Increased slightly
        private DateTime _errorCooldownUntil = DateTime.UtcNow;
        private TimeSpan _errorCooldownDuration = TimeSpan.FromSeconds(45); // Increased cooldown

        private static DateTime _lastMoveItemsAction = DateTime.UtcNow.AddDays(-1); // Keep track of last move action

        #endregion

        #region Overrides

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            // Example: Ensure Traveler is ready if needed by specific states
            // if (_currentState == TransportState.TravelingToPickupStation || _currentState == TransportState.TravelingToDeliveryStation)
            // {
            //     // Could check if Traveler controller exists and is not paused, etc.
            // }
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {
            // No broadcast messages handled here
        }

        private static void StartTraveler() // Kept static as it only uses static Traveler/State properties
        {
            Log("StartTraveler: Resetting Traveler state.");
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
        }

        public void EveryPulse() // Instance-level pulse logic
        {
            // Basic checks - maybe expand later if needed
            if (ESCache.Instance.InStation && _currentState != TransportState.Idle && _currentState != TransportState.InitializeRun && _currentState != TransportState.ActivateTransportShip && _currentState != TransportState.PickingUpItems && _currentState != TransportState.DeliveringItems && _currentState != TransportState.WaitOnErrorCooldown && _currentState != TransportState.Done && _currentState != TransportState.Error)
            {
                // If docked unexpectedly, might need error handling or state reset
                // Log($"Unexpectedly docked in state: {_currentState}. Resetting?");
                // _currentState = TransportState.Idle; // Example reset
            }
        }

        public override void DoWork()
        {
            try
            {
                // Ensure settings are loaded (this check might be redundant if ControllerManager ensures it)
                if (Settings == null)
                {
                    Log("ItemTransportMainSetting is null. Cannot proceed.");
                    _currentState = TransportState.Error; // Transition to error state
                    // Immediately process the error state to halt
                    StateError();
                    return;
                }

                EveryPulse(); // Call instance-specific pulse logic

                if (DebugConfig.DebugItemTransportController) Log($"Current State: {_currentState}");

                switch (_currentState)
                {
                    case TransportState.Idle:
                        StateIdle();
                        break;
                    case TransportState.InitializeRun:
                        StateInitializeRun();
                        break;
                    case TransportState.ActivateTransportShip: // Added this state
                        StateActivateTransportShip();
                        break;
                    case TransportState.SelectNextPickupStation:
                        StateSelectNextPickupStation();
                        break;
                    case TransportState.TravelingToPickupStation:
                        StateTravelToPickupStation();
                        break;
                    case TransportState.PickingUpItems:
                        StatePickupItems();
                        break;
                    case TransportState.TravelingToDeliveryStation:
                        StateTravelToDeliveryStation();
                        break;
                    case TransportState.DeliveringItems:
                        StateDeliverItems();
                        break;
                    case TransportState.WaitOnErrorCooldown:
                        StateWaitOnErrorCooldown();
                        break;
                    case TransportState.Done:
                        StateDone();
                        break;
                    case TransportState.Error:
                        StateError();
                        break;
                }
            }
            catch (Exception ex)
            {
                Log("Exception in DoWork: " + ex);
                TriggerErrorCooldown($"Unhandled exception in DoWork: {ex.Message}");
            }
        }

        #endregion

        #region States Implementation

        private void StateIdle()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: Idle");
            // Check basic prerequisites like being in station before starting.
            if (!ESCache.Instance.InStation)
            {
                Log("Not starting Idle - must be in station first.");
                // Optionally, add logic to go home first?
                // For now, just wait.
                LocalPulse = UTCNowAddSeconds(5, 10); // Wait before checking again
                return;
            }
            StartTraveler(); // Reset traveler state
            _currentState = TransportState.InitializeRun;
            _errorCounter = 0; // Reset error count for a new run
        }

        private void StateInitializeRun()
        {
            if (DebugConfig.DebugItemTransportController) Log("State: InitializeRun");
            _pickupStationsQueue.Clear();
            _currentPickupTargetID = null;
            _needsDeliveryRun = false;

            string pickupIDsString = Settings?.PickupStationIDs;
            if (string.IsNullOrWhiteSpace(pickupIDsString))
            {
                Log("Error: PickupStationIDs setting is empty.");
                _currentState = TransportState.Error;
                return;
            }

            var ids = pickupIDsString.Split(',');
            int parsedCount = 0;
            foreach (var idStr in ids)
            {
                if (long.TryParse(idStr.Trim(), out long id) && id > 0)
                {
                    _pickupStationsQueue.Enqueue(id);
                    parsedCount++;
                }
                else
                {
                    Log($"Warning: Could not parse '{idStr.Trim()}' as a valid Station/Structure ID. Skipping.");
                }
            }

            if (parsedCount == 0)
            {
                Log("Error: No valid Pickup Station IDs were parsed from the settings.");
                _currentState = TransportState.Error;
                return;
            }

            if (Settings.DeliveryStationID <= 0)
            {
                Log("Error: DeliveryStationID is not set or invalid.");
                _currentState = TransportState.Error;
                return;
            }

            Log($"Initialized pickup queue with {parsedCount} stations.");
            _currentState = TransportState.ActivateTransportShip;
        }

        // Added StateActivateTransportShip
        private void StateActivateTransportShip()
        {
            if (DebugConfig.DebugItemTransportController) Log("State: ActivateTransportShip");

            if (!ESCache.Instance.InDockableLocation)
            {
                Log("Not in station, cannot activate transport ship. Attempting to go home or erroring.");
                // Decide whether to travel home or just error out
                TriggerErrorCooldown("Tried to activate ship while not in station.");
                // OR: _currentState = TransportState.GoHome; // Need a GoHome state
                return;
            }

            string transportShipName = Settings?.TransportShipName;
            if (string.IsNullOrEmpty(transportShipName))
            {
                Log("TransportShipName not set in settings. Assuming current ship is correct.");
                _currentState = TransportState.SelectNextPickupStation;
                return;
            }

            if (ESCache.Instance.ActiveShip?.GivenName == transportShipName)
            {
                Log("Already in the correct transport ship.");
                _currentState = TransportState.SelectNextPickupStation;
                return;
            }

            // Logic to find and activate the ship (similar to previous implementation)
            var shipHangar = ESCache.Instance.DirectEve.GetShipHangar();
            if (shipHangar == null || !shipHangar.IsReady)
            {
                Log("Waiting for Ship Hangar.");
                LocalPulse = UTCNowAddSeconds(2, 3);
                return;
            }

            var targetShipItem = shipHangar.Items.FirstOrDefault(i => i.IsSingleton && i.GivenName == transportShipName);

            if (targetShipItem != null)
            {
                Log($"Activating transport ship: {transportShipName}");
                if (targetShipItem.ActivateShip())
                {
                    LocalPulse = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds); // Wait for activation
                    // Stay in this state until ship is active
                }
                else
                {
                    TriggerErrorCooldown($"Failed to activate ship {transportShipName}.");
                }
            }
            else
            {
                Log($"Transport ship '{transportShipName}' not found in hangar.");
                TriggerErrorCooldown($"Transport ship '{transportShipName}' not found.");
            }
        }


        private void StateSelectNextPickupStation()
        {
            if (DebugConfig.DebugItemTransportController) Log("State: SelectNextPickupStation");

            // If we have items, we must deliver first
            bool? cargoCheck = DoWeHaveAnyCargo;
            if (cargoCheck == null) return; // Wait
            if (cargoCheck.Value)
            {
                Log("Cargo hold is not empty. Proceeding to delivery.");
                _currentState = TransportState.TravelingToDeliveryStation;
                return;
            }

            // Try to get the next station from the queue
            if (_pickupStationsQueue.Count > 0)
            {
                _currentPickupTargetID = _pickupStationsQueue.Dequeue();
                Log($"Selected next pickup station ID: {_currentPickupTargetID}");
                StartTraveler(); // Reset traveler for new destination
                _currentState = TransportState.TravelingToPickupStation;
            }
            else
            {
                Log("All pickup stations visited and cargo is empty. Task finished.");
                _currentState = TransportState.Done;
            }
        }


        private void StateTravelToPickupStation()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: TravelingToPickupStation");

            if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity?.HasInitiatedWarp == true)
                return;

            if (_currentPickupTargetID == null)
            {
                Log("Error: No current pickup target ID set. Returning to selection.");
                _currentState = TransportState.SelectNextPickupStation;
                return;
            }

            // Check if already docked at the target station
            if (ESCache.Instance.InDockableLocation && ESCache.Instance.DirectEve.Session.LocationId == _currentPickupTargetID.Value)
            {
                Log($"Already docked at pickup station: {_currentPickupTargetID.Value}");
                _currentState = TransportState.PickingUpItems;
                return;
            }

            // Set destination if not already set or incorrect
            if (_destination == null || !(_destination is DockableLocationDestination dDest) || dDest.DockableLocationId != _currentPickupTargetID.Value)
            {
                Log($"Setting Destination to pickup station ID: {_currentPickupTargetID.Value}");
                _destination = new DockableLocationDestination(_currentPickupTargetID.Value);
                Traveler.Destination = _destination; // Update the static Traveler destination
                State.CurrentTravelerState = TravelerState.Idle; // Reset traveler state
                return; // Allow traveler to initialize
            }


            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log($"Arrived at Pickup Station ID: {_currentPickupTargetID.Value} (via Traveler)");
                Traveler.Destination = null;
                _destination = null;
                _currentState = TransportState.PickingUpItems;
            }
            else if (State.CurrentTravelerState == TravelerState.Error)
            {
                TriggerErrorCooldown($"Error while traveling to pickup station ID: {_currentPickupTargetID.Value}.");
                _currentPickupTargetID = null; // Clear target so we re-select on retry
                _currentState = TransportState.SelectNextPickupStation; // Go back to selection after cooldown
            }
        }

        private void StatePickupItems()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: PickingUpItems");

            if (ESCache.Instance.InDockableLocation)
            {
                Log("Not in station during pickup phase. Returning to pickup station.");
                _currentState = TransportState.TravelingToPickupStation; // Or trigger error
                return;
            }

            // Ensure we are at the *correct* pickup station
            if (_currentPickupTargetID == null || ESCache.Instance.DirectEve.Session.LocationId != _currentPickupTargetID.Value)
            {
                Log($"Error: Docked at wrong station ({ESCache.Instance.DirectEve.Session.LocationId}) while trying to pick up from ({_currentPickupTargetID}).");
                _currentState = TransportState.TravelingToPickupStation; // Go back to the correct one
                _currentPickupTargetID = null; // Force re-selection next time
                return;
            }


            // Check special holds first
            bool? ammoPickup = PickupItemsFromItemHangarToAmmoHold();
            if (ammoPickup == null) return; // Wait
            if (!ammoPickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to AmmoHold."); return; }

            bool? mineralPickup = PickupItemsFromItemHangarToMineralHold();
            if (mineralPickup == null) return;
            if (!mineralPickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to MineralHold."); return; }

            bool? generalMiningPickup = PickupItemsFromItemHangarToGeneralMiningHold();
            if (generalMiningPickup == null) return;
            if (!generalMiningPickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to GeneralMiningHold."); return; }

            bool? orePickup = PickupItemsFromItemHangarToOreHold();
            if (orePickup == null) return;
            if (!orePickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to OreHold."); return; }

            // Then main cargo
            bool? cargoPickup = PickupItemsFromItemHangarToCargoHold();
            if (cargoPickup == null) return;
            if (!cargoPickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to CargoHold."); return; }

            // Finally fleet hangar
            bool? fleetPickup = PickupItemsFromItemHangarToFleetHangar();
            if (fleetPickup == null) return;
            if (!fleetPickup.Value) { TriggerErrorCooldown("Failed pickup from ItemHangar to FleetHangar."); return; }

            // Check if cargo was actually added (or if source was empty)
            bool? cargoCheck = DoWeHaveAnyCargo;
            if (cargoCheck == null) return; // Wait if unsure

            Log($"Finished pickup attempt at station {_currentPickupTargetID}. Cargo present: {cargoCheck.Value}");
            _currentPickupTargetID = null; // Mark this station as visited for this cycle
            _needsDeliveryRun = cargoCheck.Value || _needsDeliveryRun; // Set flag if we picked anything up now or previously

            // Decide next step: Go deliver, or go to next pickup?
            _currentState = TransportState.SelectNextPickupStation; // Let selection logic decide
        }

        private void StateTravelToDeliveryStation()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: TravelingToDeliveryStation");

            if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity?.HasInitiatedWarp == true)
                return; // Don't interfere with warp

            long deliveryId = Settings.DeliveryStationID;
            if (deliveryId <= 0)
            {
                Log("Error: DeliveryStationID is invalid.");
                _currentState = TransportState.Error;
                return;
            }

            // Check if already docked at the target station
            if (ESCache.Instance.InDockableLocation && ESCache.Instance.DirectEve.Session.LocationId == deliveryId)
            {
                Log($"Already docked at delivery station: {deliveryId}");
                _currentState = TransportState.DeliveringItems;
                return;
            }

            // Set destination if not already set or incorrect
            if (_destination == null || !(_destination is DockableLocationDestination dDest) || dDest.DockableLocationId != deliveryId)
            {
                Log($"Setting Destination to delivery station ID: {deliveryId}");
                _destination = new DockableLocationDestination(deliveryId);
                Traveler.Destination = _destination; // Update static Traveler
                State.CurrentTravelerState = TravelerState.Idle; // Reset traveler state
                return; // Allow traveler to initialize
            }

            Traveler.ProcessState();

            if (State.CurrentTravelerState == TravelerState.AtDestination)
            {
                Log($"Arrived at Delivery Station ID: {deliveryId} (via Traveler)");
                Traveler.Destination = null; // Clear static traveler destination
                _destination = null; // Clear instance destination
                _currentState = TransportState.DeliveringItems;
            }
            else if (State.CurrentTravelerState == TravelerState.Error)
            {
                TriggerErrorCooldown($"Error while traveling to delivery station ID: {deliveryId}.");
                // Keep state, retry will happen after cooldown
            }
        }

        private void StateDeliverItems()
        {
            try
            {
                if (DebugConfig.DebugItemTransportController && DirectEve.Interval(10000)) Log("State: DeliveringItems");

                if (!ESCache.Instance.InDockableLocation)
                {
                    Log("Not in station during delivery phase. Returning to delivery station.");
                    _currentState = TransportState.TravelingToDeliveryStation; // Or trigger error
                    return;
                }

                // Ensure we are at the correct delivery station
                if (ESCache.Instance.DirectEve.Session.LocationId != Settings.DeliveryStationID)
                {
                    Log($"Error: Docked at wrong station ({ESCache.Instance.DirectEve.Session.LocationId}) while trying to deliver to ({Settings.DeliveryStationID}).");
                    _currentState = TransportState.TravelingToDeliveryStation; // Go back to the correct one
                    return;
                }

                // Unload from each hold type sequentially
                bool? ammoUnload = MoveItemsFromAmmoHoldToItemHangar();
                if (ammoUnload == null) return; // Wait
                if (!ammoUnload.Value) { TriggerErrorCooldown("Failed delivery from AmmoHold to ItemHangar."); return; }

                bool? mineralUnload = MoveItemsFromMineralHoldToItemHangar();
                if (mineralUnload == null) return;
                if (!mineralUnload.Value) { TriggerErrorCooldown("Failed delivery from MineralHold to ItemHangar."); return; }

                bool? generalMiningUnload = MoveItemsFromGeneralMiningHoldToItemHangar();
                if (generalMiningUnload == null) return;
                if (!generalMiningUnload.Value) { TriggerErrorCooldown("Failed delivery from GeneralMiningHold to ItemHangar."); return; }

                bool? oreUnload = MoveItemsFromOreHoldToItemHangar(); // Assuming logic exists
                if (oreUnload == null) return;
                if (!oreUnload.Value) { TriggerErrorCooldown("Failed delivery from OreHold to ItemHangar."); return; }

                bool? cargoUnload = MoveItemsFromCargoHoldToItemHangar();
                if (cargoUnload == null) return;
                if (!cargoUnload.Value) { TriggerErrorCooldown("Failed delivery from CargoHold to ItemHangar."); return; }

                bool? fleetUnload = MoveItemsFromFleetHangarToItemHangar();
                if (fleetUnload == null) return;
                if (!fleetUnload.Value) { TriggerErrorCooldown("Failed delivery from FleetHangar to ItemHangar."); return; }

                // Check if all cargo holds are now empty
                bool? cargoCheck = DoWeHaveAnyCargo;
                if (cargoCheck == null) return; // Wait if unsure

                if (cargoCheck.Value)
                {
                    Log("Still have items in cargo after attempting delivery. Retrying or erroring.");
                    TriggerErrorCooldown("Failed to completely empty cargo holds during delivery.");
                    return;
                }

                Log("Delivered items successfully.");
                _needsDeliveryRun = false; // Reset delivery flag
                _currentState = TransportState.SelectNextPickupStation; // Go back to see if more pickups are needed
            }
            catch (Exception ex)
            {
                Log("Exception in StateDeliverItems: " + ex);
                TriggerErrorCooldown($"Exception during item delivery: {ex.Message}");
            }
        }

        private void StateWaitOnErrorCooldown()
        {
            if (DebugConfig.DebugItemTransportController && DirectEve.Interval(5000)) Log($"State: WaitOnErrorCooldown. Waiting until {(_errorCooldownUntil - DateTime.UtcNow).TotalSeconds:F1}s.");
            if (DateTime.UtcNow < _errorCooldownUntil)
                return; // Wait out the cooldown

            if (_errorCounter >= MaxErrorCount)
            {
                Log("WaitOnErrorCooldown: Reached max errors.");
                _currentState = TransportState.Error;
                return;
            }

            Log($"Error cooldown finished; retrying. Error attempts: {_errorCounter}/{MaxErrorCount}. Returning to state: {_previousState}");
            _currentState = _previousState; // Go back to the state before the error
        }

        private void StateDone()
        {
            Log("State: Done. Transport task completed successfully.");
            IsWorkDone = true; // Signal controller completion
        }

        private void StateError()
        {
            Log("State: Error - ItemTransportController encountered a critical error and is stopping.");
            Log("Please review logs for details.");

            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.ERROR, "ItemTransportController encountered an error and stopped."));
            IsWorkDone = true; // Stop processing
            ControllerManager.Instance.SetPause(true); // Pause the bot for manual intervention
        }

        #endregion

        #region Error Handling

        private void TriggerErrorCooldown(string errorMessage = "")
        {
            _errorCounter++;
            Log($"Error in state {_currentState}: {errorMessage}. Attempt {_errorCounter}/{MaxErrorCount}. Entering cooldown.");
            if (_errorCounter >= MaxErrorCount)
            {
                Log("Reached max errors. Transitioning to Error state.");
                _currentState = TransportState.Error;
                return;
            }

            _previousState = _currentState; // Store the state where the error occurred
            _errorCooldownUntil = DateTime.UtcNow + _errorCooldownDuration;
            _currentState = TransportState.WaitOnErrorCooldown;
        }

        #endregion

        #region Helpers

        private bool? DoWeHaveAnyCargo // Checks if *any* hold has items
        {
            get
            {
                try
                {
                    // Check standard cargo first
                    if (ESCache.Instance.CurrentShipsCargo?.UsedCapacity == null) return null; // Wait if cargo not ready
                    if (ESCache.Instance.CurrentShipsCargo.UsedCapacity > 0) return true;

                    // Check special holds if they exist and are ready
                    if (ESCache.Instance.ActiveShip.HasOreHold)
                    {
                        if (ESCache.Instance.CurrentShipsOreHold?.UsedCapacity == null) return null;
                        if (ESCache.Instance.CurrentShipsOreHold.UsedCapacity > 0) return true;
                    }
                    if (ESCache.Instance.ActiveShip.HasFleetHangar)
                    {
                        if (ESCache.Instance.CurrentShipsFleetHangar?.UsedCapacity == null) return null;
                        if (ESCache.Instance.CurrentShipsFleetHangar.UsedCapacity > 0) return true;
                    }
                    if (ESCache.Instance.ActiveShip.HasMineralHold)
                    {
                        if (ESCache.Instance.CurrentShipsMineralHold?.UsedCapacity == null) return null;
                        if (ESCache.Instance.CurrentShipsMineralHold.UsedCapacity > 0) return true;
                    }
                    if (ESCache.Instance.ActiveShip.HasGeneralMiningHold)
                    {
                        if (ESCache.Instance.CurrentShipsGeneralMiningHold?.UsedCapacity == null) return null;
                        if (ESCache.Instance.CurrentShipsGeneralMiningHold.UsedCapacity > 0) return true;
                    }
                    if (ESCache.Instance.ActiveShip.HasAmmoHold)
                    {
                        if (ESCache.Instance.CurrentShipsAmmoHold?.UsedCapacity == null) return null;
                        if (ESCache.Instance.CurrentShipsAmmoHold.UsedCapacity > 0) return true;
                    }

                    return false; // No cargo found in any checked & ready hold
                }
                catch (Exception ex)
                {
                    Log($"Exception checking cargo status: {ex.Message}");
                    return null; // Treat exceptions as needing to wait/recheck
                }
            }
        }


        private bool IsValidTransportItem(DirectItem item, ItemTransportMainSetting settings) // Pass settings
        {
            if (item == null || (item.IsSingleton && !item.IsBlueprintCopy)) return false;

            // Check Exclusions first
            if (settings.ExcludedTypeIDs?.Contains(item.TypeId) ?? false) return false;
            if (settings.ExcludedGroupIDs?.Contains(item.GroupId) ?? false) return false;
            if (settings.ExcludedCategoryIDs?.Contains(item.CategoryId) ?? false) return false;

            // Check Inclusions
            bool typeListHasItems = settings.TypeIDsToTransport?.Any() ?? false;
            bool groupListHasItems = settings.GroupIDsToTransport?.Any() ?? false;
            bool categoryListHasItems = settings.CategoryIDsToTransport?.Any() ?? false;

            // If NO include lists are specified, include everything not excluded
            if (!typeListHasItems && !groupListHasItems && !categoryListHasItems)
            {
                return true;
            }

            // If ANY include list has items, the item MUST match at least one include criteria
            if (typeListHasItems && settings.TypeIDsToTransport.Contains(item.TypeId)) return true;
            if (groupListHasItems && settings.GroupIDsToTransport.Contains(item.GroupId)) return true;
            if (categoryListHasItems && settings.CategoryIDsToTransport.Contains(item.CategoryId)) return true;

            // If include lists exist but the item didn't match any, exclude it
            return false;
        }

        // --- Updated Pickup Method ---
        private bool? Pickup(DirectContainer fromContainer, DirectContainer toContainer, string fromContainerName, string toContainerName)
        {
            // Use a local temporary exclusion list for this specific pickup operation
            var tempExcludedTypeIDs = new List<long>();

            if (toContainer == null || fromContainer == null || !fromContainer.IsReady || !toContainer.IsReady)
            {
                // Log($"Pickup: Waiting for containers. From: {fromContainerName} ({fromContainer?.IsReady}), To: {toContainerName} ({toContainer?.IsReady})");
                return null; // Wait for containers to be ready
            }

            if (toContainer.UsedCapacityPercentage >= 99.9) // Use a high threshold
            {
                // Log($"[{toContainerName}] is effectively full ({toContainer.UsedCapacityPercentage}%).");
                return true; // Done with this container type for now
            }

            var currentSettings = this.Settings; // Use instance settings property
            if (currentSettings == null) { /*...*/ return false; }

            var itemsToPotentiallyMove = fromContainer.Items
                .Where(x => IsValidTransportItem(x, currentSettings) && !tempExcludedTypeIDs.Contains(x.TypeId))
                .OrderBy(i => i.TotalVolume) // Smallest items first
                .ToList(); // Snapshot

            if (!itemsToPotentiallyMove.Any())
            {
                // Log($"No items matching criteria found in [{fromContainerName}] to move to [{toContainerName}].");
                return true; // Nothing to move
            }

            bool actionTakenThisPulse = false;
            foreach (var item in itemsToPotentiallyMove)
            {
                if (toContainer.FreeCapacity < 1) break; // Stop if completely full

                // Pass settings to MoveItems
                bool? moveResult = MoveItems(fromContainer, toContainer, item, item.Quantity);

                if (moveResult == null) return null; // Waiting inside MoveItems

                if (!moveResult.Value)
                {
                    // Log($"MoveItems indicated failure for {item.TypeName}."); // Logged in MoveItems
                    // Decide if this item should be temporarily excluded for this cycle
                    if (item.Volume > toContainer.FreeCapacity) // Check if failure was due to space
                    {
                        tempExcludedTypeIDs.Add(item.TypeId); // Exclude for this cycle only
                        Log($"Temporarily excluding {item.TypeName} (ID: {item.TypeId}) due to insufficient space in {toContainerName}.");
                    }
                    // If failure wasn't due to space, might be another issue, could log/retry later
                }
                else
                {
                    actionTakenThisPulse = true; // An action (move or decision not to move due to space) happened
                                                 // If MoveItems succeeded, we want to re-evaluate immediately for the next item or state change
                    return false; // Indicate action occurred
                }
            }

            // If we looped through all items and took no definitive "move" action this pulse
            return true; // Indicate this pickup target (fromContainer -> toContainer) is done for now
        }

        // --- MoveItems Method (Refined) ---
        private bool? MoveItems(DirectContainer fromContainer, DirectContainer toContainer, DirectItem itemToMove, double quantityToMove)
        {
            try
            {
                // Add a slightly longer cooldown to prevent spamming move commands if something is stuck
                if (_lastMoveItemsAction.AddSeconds(1.5) > DateTime.UtcNow)
                    return null; // Indicate wait

                if (fromContainer == null || toContainer == null || itemToMove == null || quantityToMove <= 0)
                {
                    Log($"MoveItems: Invalid parameters. From: {fromContainer?.ItemId}, To: {toContainer?.ItemId}, Item: {itemToMove?.ItemId}, Qty: {quantityToMove}");
                    return false; // Indicate failure (don't retry this specific call immediately)
                }

                // Ensure containers are ready *before* checking locks
                if (!fromContainer.IsReady || !toContainer.IsReady) return null; // Wait

                if (toContainer.WaitingForLockedItems() || fromContainer.WaitingForLockedItems())
                {
                    // Log("Waiting for locked items before moving."); // Can be spammy
                    return null; // Wait
                }

                // Re-verify item exists in source container right before moving
                var currentSourceItem = fromContainer.Items.FirstOrDefault(i => i.ItemId == itemToMove.ItemId);
                if (currentSourceItem == null || currentSourceItem.Quantity <= 0)
                {
                    Log($"Item {itemToMove.TypeName} (ID: {itemToMove.ItemId}) no longer exists or has 0 quantity in {fromContainer.HangarName}.");
                    return true; // Treat as success (nothing to move)
                }
                double actualQuantityInSource = currentSourceItem.Quantity;


                // Use instance setting for partial loads
                bool allowPartials = Settings?.AllowPartialLoads ?? false;

                // Calculate available space and required volume *again*
                double freeCapacity = toContainer.FreeCapacity;
                double volumePerUnit = itemToMove.Volume; // Should be accurate from the item object

                if (freeCapacity < volumePerUnit && volumePerUnit > 0) // Check if even one unit fits
                {
                    // Log($"Not enough space in [{toContainer.HangarName}] for even one unit of [{itemToMove.TypeName}]. Free: {freeCapacity:F2}, Needed: {volumePerUnit:F2}.");
                    return true; // Cannot move this item now, move to next item/state
                }

                double quantityToAttempt = Math.Min(quantityToMove, actualQuantityInSource); // Don't try to move more than exists
                double volumeNeeded = volumePerUnit * quantityToAttempt;

                if (freeCapacity < volumeNeeded)
                {
                    if (allowPartials && volumePerUnit > 0)
                    {
                        quantityToAttempt = Math.Floor(freeCapacity / volumePerUnit);
                        if (quantityToAttempt <= 0)
                        {
                            // Log($"Calculation error or no space for [{itemToMove.TypeName}] even after adjustment.");
                            return true; // Cannot move even one partial unit
                        }
                        // Log($"Adjusting quantity for [{itemToMove.TypeName}] due to space limit. Attempting to move {quantityToAttempt} of {actualQuantityInSource}.");
                    }
                    else
                    {
                        // Log($"Not enough space for full quantity of [{itemToMove.TypeName}] and partial loads disabled. Free: {freeCapacity:F2}, Needed: {volumeNeeded:F2}.");
                        return true; // Cannot move this stack now
                    }
                }

                // Final check on quantity
                if (quantityToAttempt <= 0) return true; // Nothing to move


                // Perform the move
                if (DebugConfig.DebugItemTransportControllerDontMoveItems)
                {
                    Log($"Debug: Would move {(int)quantityToAttempt} of [{itemToMove.TypeName}] from [{fromContainer.HangarName}] to [{toContainer.HangarName}].");
                    _lastMoveItemsAction = DateTime.UtcNow;
                    return true; // Simulate success
                }

                Log($"Moving {(int)quantityToAttempt} of [{itemToMove.TypeName}] from [{fromContainer.HangarName}] to [{toContainer.HangarName}].");
                if (toContainer.Add(itemToMove, (int)quantityToAttempt))
                {
                    _lastMoveItemsAction = DateTime.UtcNow;
                    return true; // Indicate success
                }
                else
                {
                    Log($"Failed to execute move command for [{itemToMove.TypeName}].");
                    return false; // Indicate failure
                }
            }
            catch (Exception ex)
            {
                Log($"Exception in MoveItems for {itemToMove?.TypeName}: {ex.Message}");
                return null; // Indicate error/wait
            }
        }


        #endregion
    }
}