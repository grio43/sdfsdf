using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    public enum GenericCourierStorylineState
    {
        GotoPickupLocation,
        PickupItem,
        GotoDropOffLocation,
        DropOffItem
    }

    public class GenericCourier : IStoryline
    {
        #region Fields

        private DateTime _nextGenericCourierStorylineAction;
        private GenericCourierStorylineState _state;

        #endregion Fields

        #region Methods

        public StorylineState Arm(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow) return StorylineState.Arm;

            if (ESCache.Instance.DirectEve.GetShipHangar() == null) return StorylineState.Arm;

            // Open the ship hangar
            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
            {
                _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(5);
                return StorylineState.Arm;
            }

            if (string.IsNullOrEmpty(ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower()))
            {
                ESCache.Instance.State.CurrentArmState = ArmState.NotEnoughAmmo;
                Log.WriteLine("Could not find transportshipName in settings!");
                return StorylineState.BlacklistAgent;
            }

            try
            {
                if (DebugConfig.DebugArm) Log.WriteLine("try");
                if (ESCache.Instance.ActiveShip.GivenName.ToLower() != ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                {
                    if (DebugConfig.DebugArm)
                        Log.WriteLine("if (Cache.Instance.ActiveShip.GivenName.ToLower() != transportshipName.ToLower())");
                    if (!ESCache.Instance.DirectEve.GetShipHangar().Items.Any()) return StorylineState.Arm; //no ships?!?

                    if (DebugConfig.DebugArm)
                        Log.WriteLine("if (!Cache.Instance.ShipHangar.Items.Any()) return StorylineState.Arm; done");

                    var ships = ESCache.Instance.DirectEve.GetShipHangar().Items;
                    if (DebugConfig.DebugArm) Log.WriteLine("List<DirectItem> ships = Cache.Instance.ShipHangar.Items;");

                    foreach (
                        var ship in ships.Where(ship => ship.GivenName != null && ship.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower()))
                    {
                        Log.WriteLine("Making [" + ship.GivenName + "] active");
                        ship.ActivateShip();
                        _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.SwitchShipsDelay_seconds);
                        return StorylineState.Arm;
                    }

                    return StorylineState.Arm;
                }
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception thrown while attempting to switch to transport ship: [" + exception + "]");
                Log.WriteLine("blacklisting this storyline agent for this session because we could not switch to the configured TransportShip named [" +
                              ESCache.Instance.EveAccount.CS.QMS.TransportShipName + "]");
                return StorylineState.BlacklistAgent;
            }

            if (DateTime.UtcNow > ESCache.Instance.Time.NextArmAction) //default 7 seconds
                if (ESCache.Instance.ActiveShip.GivenName.ToLower() == ESCache.Instance.EveAccount.CS.QMS.TransportShipName.ToLower())
                {
                    Log.WriteLine("Done");
                    ESCache.Instance.State.CurrentArmState = ArmState.Done;
                    return StorylineState.GotoAgent;
                }

            return StorylineState.Arm;
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextGenericCourierStorylineAction > DateTime.UtcNow)
                return StorylineState.ExecuteMission;

            switch (_state)
            {
                case GenericCourierStorylineState.GotoPickupLocation:
                    if (GotoMissionBookmark(ESCache.Instance.Agent, "Objective (Pick Up)"))
                        _state = GenericCourierStorylineState.PickupItem;
                    break;

                case GenericCourierStorylineState.PickupItem:
                    if (MoveItem(true))
                        _state = GenericCourierStorylineState.GotoDropOffLocation;
                    break;

                case GenericCourierStorylineState.GotoDropOffLocation:
                    if (GotoMissionBookmark(ESCache.Instance.Agent, "Objective (Drop Off)"))
                        _state = GenericCourierStorylineState.DropOffItem;
                    break;

                case GenericCourierStorylineState.DropOffItem:
                    if (MoveItem(false))
                        return StorylineState.CompleteMission;
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     There are no pre-accept actions
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            _state = GenericCourierStorylineState.GotoPickupLocation;

            ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
            ESCache.Instance.Traveler.Destination = null;

            return StorylineState.AcceptMission;
        }

        private bool GotoMissionBookmark(DirectAgent agent, string title)
        {
            var destination = ESCache.Instance.Traveler.Destination as MissionBookmarkDestination;
            if (destination == null || destination.AgentId != agent.AgentId || !destination.Title.ToLower().StartsWith(title.ToLower()))
                ESCache.Instance.Traveler.Destination = new MissionBookmarkDestination(agent.GetMissionBookmark(title));

            ESCache.Instance.Traveler.ProcessState();

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.AtDestination)
            {
                ESCache.Instance.Traveler.Destination = null;
                return true;
            }

            if (ESCache.Instance.State.CurrentTravelerState == TravelerState.Error)
            {
                Log.WriteLine("Traveller state = Error. Blacklisting this agent.");
                ESCache.Instance.State.CurrentStorylineState = StorylineState.BlacklistAgent;
            }

            return false;
        }

        private int _moveTries;
        private bool MoveItem(bool pickup)
        {
            var directEve = ESCache.Instance.DirectEve;

            // Open the item hangar (should still be open)
            if (ESCache.Instance.DirectEve.GetItemHangar() == null) return false;

            if (ESCache.Instance.CurrentShipsCargo == null) return false;

            var from = pickup ? ESCache.Instance.DirectEve.GetItemHangar() : ESCache.Instance.CurrentShipsCargo;
            var to = pickup ? ESCache.Instance.CurrentShipsCargo : ESCache.Instance.DirectEve.GetItemHangar();

            // We moved the item
            if ((pickup && _moveTries > 5) || to.Items.Any(i => i.GroupId == (int)Group.MiscSpecialMissionItems || i.GroupId == (int)Group.Livestock))
                return true;

            if (!directEve.NoLockedItemsOrWaitAndClearLocks())
                return false;

            // Move items
            var itemsToMove = from.Items.Where(i => i.GroupId == (int)Group.MiscSpecialMissionItems || i.GroupId == (int)Group.Livestock);

            if (to.Add(itemsToMove))
            {
                foreach (var item in itemsToMove)
                {
                    Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                }
                _nextGenericCourierStorylineAction = DateTime.UtcNow.AddSeconds(10);
                _moveTries++;
            }
            return false;
        }

        #endregion Methods
    }
}