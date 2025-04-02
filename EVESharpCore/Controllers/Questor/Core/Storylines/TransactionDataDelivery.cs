using System;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;

namespace EVESharpCore.Controllers.Questor.Core.Storylines
{
    public enum TransactionDataDeliveryState
    {
        GotoPickupLocation,
        PickupItem,
        GotoDropOffLocation,
        DropOffItem
    }

    public class TransactionDataDelivery : IStoryline
    {
        #region Fields

        private DateTime _nextAction;
        private TransactionDataDeliveryState _state;

        #endregion Fields

        #region Methods

        /// <summary>
        ///     Arm does nothing but get into a (assembled) shuttle
        /// </summary>
        /// <returns></returns>
        public StorylineState Arm(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.Arm;

            // Are we in a shuttle?  Yes, go to the agent
            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Shuttle)
                return StorylineState.GotoAgent;

            // Open the ship hangar
            if (ESCache.Instance.DirectEve.GetShipHangar() == null) return StorylineState.Arm;

            //  Look for a shuttle
            var item = ESCache.Instance.DirectEve.GetShipHangar().Items.FirstOrDefault(i => i.Quantity == -1 && i.GroupId == (int)Group.Shuttle);
            if (item != null)
            {
                Log.WriteLine("Switching to shuttle");

                _nextAction = DateTime.UtcNow.AddSeconds(10);

                item.ActivateShip();
                return StorylineState.Arm;
            }

            Log.WriteLine("No shuttle found, going in active ship");
            return StorylineState.GotoAgent;
        }

        /// <summary>
        ///     Goto the pickup location
        ///     Pickup the item
        ///     Goto drop off location
        ///     Drop the item
        ///     Goto Agent
        ///     Complete mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState ExecuteMission(Storyline storyline)
        {
            if (_nextAction > DateTime.UtcNow)
                return StorylineState.ExecuteMission;

            switch (_state)
            {
                case TransactionDataDeliveryState.GotoPickupLocation:
                    if (GotoMissionBookmark(ESCache.Instance.Agent, "Objective (Pick Up)"))
                        _state = TransactionDataDeliveryState.PickupItem;
                    break;

                case TransactionDataDeliveryState.PickupItem:
                    if (MoveItem(true))
                        _state = TransactionDataDeliveryState.GotoDropOffLocation;
                    break;

                case TransactionDataDeliveryState.GotoDropOffLocation:
                    if (GotoMissionBookmark(ESCache.Instance.Agent, "Objective (Drop Off)"))
                        _state = TransactionDataDeliveryState.DropOffItem;
                    break;

                case TransactionDataDeliveryState.DropOffItem:
                    if (MoveItem(false))
                        return StorylineState.ReturnToAgent;
                    break;
            }

            return StorylineState.ExecuteMission;
        }

        /// <summary>
        ///     There are no actions before you accept the mission
        /// </summary>
        /// <param name="storyline"></param>
        /// <returns></returns>
        public StorylineState PreAcceptMission(Storyline storyline)
        {
            _state = TransactionDataDeliveryState.GotoPickupLocation;

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

        private bool MoveItem(bool pickup)
        {
            // Open the item hangar (should still be open)
            if (ESCache.Instance.DirectEve.GetItemHangar() == null) return false;

            if (ESCache.Instance.CurrentShipsCargo == null) return false;

            // 314 == Transaction And Salary Logs (all different versions)
            const int groupId = 314;
            var from = pickup ? ESCache.Instance.DirectEve.GetItemHangar() : ESCache.Instance.CurrentShipsCargo;
            var to = pickup ? ESCache.Instance.CurrentShipsCargo : ESCache.Instance.DirectEve.GetItemHangar();

            // We moved the item
            if (to.Items.Any(i => i.GroupId == groupId))
                return true;

            if (!ESCache.Instance.DirectEve.NoLockedItemsOrWaitAndClearLocks())
                return false;

            var itemsToMove = from.Items.Where(i => i.GroupId == groupId);
            // Move items

            if (to.Add(itemsToMove))
            {
                foreach (var item in itemsToMove)
                {
                    Log.WriteLine("Moving [" + item.TypeName + "][" + item.ItemId + "] to " + (pickup ? "cargo" : "hangar"));
                    to.Add(item);
                }
                _nextAction = DateTime.UtcNow.AddSeconds(10);
            }

            return false;
        }

        #endregion Methods
    }
}