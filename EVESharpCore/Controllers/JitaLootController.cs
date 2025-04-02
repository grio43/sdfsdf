extern alias SC;
using EVESharpCore.Controllers.Base;
using SC::SharedComponents.EveMarshal;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using EVESharpCore.Framework;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Controllers
{
    public class JitaLootController : BaseController, IOnFrameController, IPacketHandlingController
    {

        private ConcurrentDictionary<long, object> _destroyedEntities = new ConcurrentDictionary<long, object>();

        private ConcurrentDictionary<long, object> _seenInvIds = new ConcurrentDictionary<long, object>();

        private ConcurrentDictionary<long, object> _loggedSeenInvIds = new ConcurrentDictionary<long, object>();

        private ConcurrentDictionary<long, object> _remoteCallInitiatedInvIds = new ConcurrentDictionary<long, object>();

        private ConcurrentDictionary<long, object> _lootedInvIds = new ConcurrentDictionary<long, object>();

        private ConcurrentDictionary<long, (DateTime, bool)> _openedInvIds = new ConcurrentDictionary<long, (DateTime, bool)>();

        private ConcurrentDictionary<long, (String, DirectWorldPosition)> _entities = new ConcurrentDictionary<long, (String, DirectWorldPosition)>();

        private DirectWorldPosition _myWorldPosition = null;

        private static Random _random = new Random();

        public JitaLootController()
        {
            Form = new JitaLootControllerForm(this);
        }

        public override void DoWork()
        {

        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public void HandleRecvPacket(byte[] packetBytes)
        {
            try
            {

                var unmarshal = new Unmarshal();
                var pyObjectMarshal = unmarshal.Process(packetBytes, null);

                var destructions = pyObjectMarshal.GetDestructedEntityIds();

                foreach (var entId in destructions)
                {
                    if (_destroyedEntities.ContainsKey(entId))
                    {
                        continue;
                    }

                    var entTypeName = _entities.ContainsKey(entId) ? _entities[entId].Item1 : "Unknown";
                    Log($"Destructed Entity Id: {entId} -- TypeName {entTypeName}");
                    _destroyedEntities.AddOrUpdate(entId, null);
                }

                var wreckInventoryIds = pyObjectMarshal.GetAllWreckLauncherIdsAndInventoryIds();

                foreach (var pair in wreckInventoryIds)
                {
                    var entId = pair.Item1;
                    var inventoryId = pair.Item2;

                    if (_destroyedEntities.ContainsKey(entId))
                    {
                        var entTypeName = _entities.ContainsKey(entId) ? _entities[entId].Item1 : "Unknown";
                        var distance = _myWorldPosition != null && _entities.ContainsKey(entId) ? _entities[entId].Item2.GetDistance(_myWorldPosition) : null;
                        Log($"Entity Id: {entId} --  TypeName {entTypeName} -- Wreck Inventory Id: {inventoryId} -- Distance {distance}");
                        _seenInvIds.AddOrUpdate(inventoryId, null);
                    }

                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        public void HandleSendPacket(byte[] packetBytes)
        {
            try
            {

            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        public void OnFrame()
        {
            try
            {
                if (Framework.ActiveShip.Entity == null)
                    return;

                _myWorldPosition = Framework.ActiveShip.Entity.DirectAbsolutePosition;


                foreach (var id in _remoteCallInitiatedInvIds)
                {

                    if (_lootedInvIds.ContainsKey(id.Key))
                        continue;

                    var inv = DirectContainer.GetContainer(Framework, id.Key, false);
                    if (inv.IsValid)
                    {
                        Log($"Inv is valid.");
                        if (!_openedInvIds.ContainsKey(id.Key))
                        {
                            _openedInvIds.AddOrUpdate(id.Key, (DateTime.UtcNow.AddMilliseconds(_random.Next(700,900)), false));
                            
                        }
                        else
                        {
                            if (_openedInvIds[id.Key].Item1 < DateTime.UtcNow && !_openedInvIds[id.Key].Item2)
                            {
                                _openedInvIds[id.Key] = (_openedInvIds[id.Key].Item1, true);
                                Log($"Opening the cargo container.");
                                Framework.ThreadedLocalSvcCall("menu", "OpenCargo", id.Key);
                            }
                        }

                        foreach (var item in inv.Items)
                        {
                            Log($"TypeName {item.TypeName}");
                        }

                        // if inv items are not empty or timeout assume looted

                        if (inv.Items.Any())
                        {
                            Log($"Looted.");
                            _lootedInvIds.AddOrUpdate(id.Key, null);
                        }

                    }
                    else
                    {
                        if (DirectEve.Interval((2000)))
                            Log($"Not valid {id.Key}");
                    }
                }

                foreach (var seenInvId in _seenInvIds)
                {
                    if (!_remoteCallInitiatedInvIds.ContainsKey(seenInvId.Key))
                    {
                        _remoteCallInitiatedInvIds.AddOrUpdate(seenInvId.Key, null);
                        var inv = DirectContainer.GetContainer(Framework, seenInvId.Key, true);
                        return;
                    }
                }

                var entities = Framework.Entities;
                foreach (var entity in entities)
                {

                    if (_seenInvIds.ContainsKey(entity.Id) && !_loggedSeenInvIds.ContainsKey(entity.Id))
                    {
                        _loggedSeenInvIds.AddOrUpdate(entity.Id, null);
                        Log($"Lootable inventory appeared within the client. Id {entity.Id}");
                    }

                    if (!_entities.ContainsKey(entity.Id) || (_entities.ContainsKey(entity.Id) && _entities[entity.Id].Item2 != entity.DirectAbsolutePosition))
                    {
                        _entities.AddOrUpdate(entity.Id, (entity.TypeName, entity.DirectAbsolutePosition));
                    }
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }
    }
}



