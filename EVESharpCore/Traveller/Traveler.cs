// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using SC::SharedComponents.IPC;
using ServiceStack.Text;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;
using ListExtensions = SC::SharedComponents.Extensions.ListExtensions;

namespace EVESharpCore.Traveller
{
    public class Traveler
    {
        #region Fields

        public DirectLocation _location;
        private TravelerDestination _destination;
        private List<long> _destinationRoute;
        private bool _instaBMUsed;
        private int _locationErrors;
        private string _locationName;
        private DateTime _nextGetLocation;
        private bool _startedInStation;
        private Dictionary<int, DateTime> _dynamicSystemsToAvoid = new Dictionary<int, DateTime>();
        private TimeSpan _dynamicSystemsToAvoidCacheTime = TimeSpan.FromMinutes(15);
        private bool _avoidGateCamps = true;
        private bool _avoidBubbles = true;
        private bool _avoidSmartbombs = true;
        public bool IgnoreDestinationChecks { get; set; } = true;

        public event EventHandler<EventArgs> OnSettingsChanged;

        private bool _allowLowSec;

        public bool AllowLowSec
        {
            get => _allowLowSec;
            set
            {
                var changed = _allowLowSec != value;
                _allowLowSec = value;

                if (changed)
                    OnSettingsChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool AllowNullSec { get; set; }

        public bool AvoidGateCamps
        {
            get => _avoidGateCamps;
            set
            {
                _avoidGateCamps = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }

        public bool AvoidBubbles
        {
            get => _avoidBubbles;
            set
            {
                _avoidBubbles = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }
        public bool AvoidSmartbombs
        {
            get => _avoidSmartbombs;
            set
            {
                _avoidSmartbombs = value;
                if (!value)
                {
                    _dynamicSystemsToAvoid.Clear();
                }
            }
        }

        #endregion Fields

        #region Properties

        public TravelerDestination Destination
        {
            get => _destination;
            set
            {
                _destination = value;
                ESCache.Instance.State.CurrentTravelerState = _destination == null ? TravelerState.AtDestination : TravelerState.Idle; // THIS is really code smelly // TODO: FIX ME
            }
        }

        public DirectBookmark UndockBookmark => ESCache.Instance.DirectEve.Bookmarks
            .Where(b => !string.IsNullOrEmpty(b.Title) && b.LocationId != null && b.Title.ToLower().StartsWith(ESCache.Instance.EveAccount.CS.QMS.QS.UndockBookmarkPrefix.ToLower())).ToList()
            .FirstOrDefault(b => b.IsInCurrentSystem && ESCache.Instance.DirectEve.Me.DistanceFromMe(b.X ?? 0, b.Y ?? 0, b.Z ?? 0) < (int)Distances.OnGridWithMe * 3);

        #endregion Properties

        #region Methods

        private ActionQueueAction _covertCloakActionQueueAction;
        private int _attemptsToRetrieveInsurgencySystem = 0;

        public void ProcessState(double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {

            // we could move to the station, but if we are outside of docking range, we are probably dead anyway if we can't dock up instantly
            if (ESCache.Instance.InSpace && ESCache.Instance.Stations.Any(s => s.Distance <= (int)Distances.DockingRange) && (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking) || ESCache.Instance.Entities.Count(e => e.IsTargetedBy && e.IsPlayer) > 1))
            {
                var station = ESCache.Instance.Stations.FirstOrDefault(s => s.Distance <= (int)Distances.DockingRange);
                if (ESCache.Instance.InWarp)
                {
                    Log.WriteLine($"We are outside of a station and being aggressed by another player or targeted by more than 2. Trying to stop the ship and dock.");
                    if (DirectEve.Interval(500, 1000))
                    {
                        ESCache.Instance.DirectEve.ExecuteCommand(EVESharpCore.Framework.DirectCmd.CmdStopShip);
                    }
                    return;
                }
                else
                {
                    Log.WriteLine("Docking attempt due the fact we are being agressed or targeted by more than 2 players.");
                    station.Dock();
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                    return;
                }
            }

            if (ESCache.Instance.ActiveShip.GroupId == (int)Group.BlockadeRunner || ESCache.Instance.ActiveShip.TypeId == 42685) // Blockade runner or sunesis
            {
                ESCache.Instance.Traveler.AllowLowSec = true;
            }

            if (ESCache.Instance.InSpace)
            {
                var scrambled = ESCache.Instance.Combat.TargetedBy.Any(t => t.IsWarpScramblingOrDisruptingMe);
                if (scrambled)
                {
                    Log.WriteLine($"We are warp scrambled. Processing combat/drones.");
                    ESCache.Instance.Combat.ProcessState();
                    ESCache.Instance.Drones.IsMissionPocketDone = false;
                    ESCache.Instance.Drones.ProcessState();
                    return;
                }

                var bastion = ESCache.Instance.DirectEve.Modules.FirstOrDefault(m => m.TypeId == 33400);
                if (bastion != null)
                {
                    if (bastion.IsActive)
                    {
                        // deactivate
                        if (!bastion.IsInLimboState && DirectEve.Interval(900, 1400))
                        {
                            Logging.Log.WriteLine($"Deactivating bastion module (travel).");
                            bastion.Click();
                        }
                    }
                }
            }

            switch (ESCache.Instance.State.CurrentTravelerState)
            {
                case TravelerState.Idle:
                    _startedInStation = false;
                    _instaBMUsed = false;
                    if (ESCache.Instance.InDockableLocation)
                        _startedInStation = true;
                    _attemptsToRetrieveInsurgencySystem = 0;
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.CalculatePath;
                    break;

                case TravelerState.CalculatePath:

                    Log.WriteLine($"Traveler calculating path. AllowLowSec [{AllowLowSec}] AllowNullSec [{AllowNullSec}]");
                    if (Destination == null)
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                        break;
                    }

                    var systemsToAvoid = new HashSet<DirectSolarSystem>();

                    var systemsToAvoidCommaSep = ESCache.Instance.EveAccount.CS.GlobalMainSetting.TravellerSystemsToAvoid.Replace(" ", "");

                    if (systemsToAvoidCommaSep.Length > 0 && !systemsToAvoidCommaSep.Contains(","))
                        systemsToAvoidCommaSep += ",";

                    foreach (var systemName in systemsToAvoidCommaSep.Split(','))
                    {
                        if (String.IsNullOrEmpty(systemName))
                            continue;

                        var systemId = ESCache.Instance.DirectEve.GetSolarSystemIdByName(systemName);
                        if (systemId > 0)
                        {

                            if ((int)Destination.SolarSystemId == systemId && IgnoreDestinationChecks)
                            {
                                Log.WriteLine($"Destination is in system to avoid [{systemName}]. Skipping.");
                                continue;
                            }

                            if (ESCache.Instance.DirectEve.SolarSystems.TryGetValue(systemId, out var ss))
                            {
                                Log.WriteLine($"Avoiding solarsystem [{ss.Name}]");
                                systemsToAvoid.Add(ss);
                            }
                        }
                    }

                    if (_attemptsToRetrieveInsurgencySystem <= 3)
                    {
                        // For now avoid all insurgency systems
                        var insurgencySystemList = ESCache.Instance.DirectEve.GetInsurgencyInfestedSystems();
                        if (insurgencySystemList.Count <= 0)
                        {
                            _attemptsToRetrieveInsurgencySystem++;
                            return;
                        }

                        foreach (var sys in insurgencySystemList)
                        {

                            if ((int)Destination.SolarSystemId == sys.Id && IgnoreDestinationChecks)
                            {
                                Log.WriteLine($"Destination is in system to avoid [{sys.Name}]. Skipping.");
                                continue;
                            }

                            Log.WriteLine($"Avoiding solarsystem [{sys.Name}]");
                            systemsToAvoid.Add(sys);
                        }
                    }
                    else
                    {
                        Log.WriteLine($"Failed to retrieve insurgency systems. Skipping.");
                    }

                    foreach (var dynamicSystemsToAvoidEntry in _dynamicSystemsToAvoid.ToList())
                    {
                        // Remove old entries based on _dynamicSystemsToAvoidCacheTime ago
                        if (DateTime.UtcNow.Subtract(dynamicSystemsToAvoidEntry.Value) > _dynamicSystemsToAvoidCacheTime)
                        {
                            _dynamicSystemsToAvoid.Remove(dynamicSystemsToAvoidEntry.Key);
                            continue;
                        }

                        if (ESCache.Instance.DirectEve.SolarSystems.TryGetValue(dynamicSystemsToAvoidEntry.Key, out var ss))
                        {

                            if ((int)Destination.SolarSystemId == ss.Id && IgnoreDestinationChecks)
                            {
                                Log.WriteLine($"Destination is in system to avoid [{ss.Name}]. Skipping.");
                                continue;
                            }

                            Log.WriteLine($"Avoiding solarsystem [{ss.Name}] due to dynamic avoidance settings cache.");
                            systemsToAvoid.Add(ss);
                        }
                    }

                    // Log all solar system names in systemsToAvoid
                    Log.WriteLine("-------------------");
                    int k = 0;
                    foreach (var ss in systemsToAvoid)
                    {
                        Log.WriteLine($"[{k}] Avoiding solarsystem [{ss.Name}]");
                        k++;
                    }
                    Log.WriteLine("-------------------");
                    // print all the flags
                    Log.WriteLine($"AllowLow [{AllowLowSec}] AllowNull [{AllowNullSec}] IGNORE [{IgnoreDestinationChecks}] GC [{AvoidGateCamps}] BUBB [{AvoidBubbles}] SMART [{AvoidSmartbombs}]");
                    Log.WriteLine("-------------------");

                    _destinationRoute = ESCache.Instance.DirectEve.Me.CurrentSolarSystem.CalculatePathTo(ESCache.Instance.DirectEve.SolarSystems[(int)Destination.SolarSystemId], systemsToAvoid, AllowLowSec, AllowNullSec).Item1.Select(s => (long)s.Id).ToList();

                    if (_destinationRoute.Count == 0)
                    {
                        // TODO: handle me
                        Log.WriteLine("Error: _destinationRoute.Count == 0");
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                        return;
                    }

                    if (AvoidBubbles || AvoidSmartbombs || AvoidGateCamps)
                    {
                        try
                        {
                            var pipeProxy = WCFClient.Instance.GetPipeProxy;
                            var gateCampInfo =
                                pipeProxy.GetGateCampInfo(_destinationRoute.Select(x => (int)x).ToArray(), _dynamicSystemsToAvoidCacheTime);

                            Log.WriteLine($"Got gatecamp info for [{gateCampInfo.Count}] systems.");

                            var reroute = false;
                            foreach (var destination in _destinationRoute)
                            {
                                if (!gateCampInfo.TryGetValue((int)destination, out var solarSystemEntry))
                                {
                                    // System has no information associated with it
                                    continue;
                                }

                                if (!ESCache.Instance.DirectEve.SolarSystems.TryGetValue((int)destination, out var ss))
                                {
                                    Log.WriteLine($"Failed to get solarsystem [{destination}]");
                                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                                    return;
                                }

                                if (ss.IsHighsecSystem)
                                    continue;


                                if ((int)Destination.SolarSystemId == ss.Id && IgnoreDestinationChecks)
                                {
                                    Log.WriteLine($"IgnoreDestinationChecks was set to true. Ignoring destinations checks for [{ss.Name}].");
                                    continue;
                                }

                                var hasBubbleShips = solarSystemEntry
                                    ?.Kills
                                    ?.GateKills
                                    ?.Values
                                    ?.Any(v => v.Checks.Dictors || v.Checks.Hictors) ?? false;
                                if (hasBubbleShips && AvoidBubbles)
                                {
                                    Log.WriteLine($"Avoiding solarsystem [{ss.Name}] due to bubbles.");
                                    _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                    reroute = true;
                                    continue;
                                }

                                var hasSmartbombShips = solarSystemEntry
                                    ?.Kills
                                    ?.GateKills
                                    ?.Values
                                    ?.Any(v => v.Checks.Smartbombs) ?? false;
                                if (hasSmartbombShips && AvoidSmartbombs)
                                {
                                    Log.WriteLine($"Avoiding solarsystem [{ss.Name}] due to smartbombs.");
                                    _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                    reroute = true;
                                    continue;
                                }

                                var hasGateCamp = solarSystemEntry
                                    ?.Kills
                                    ?.GateKillCountLastHour > 0;
                                if (hasGateCamp && AvoidGateCamps)
                                {
                                    Log.WriteLine($"Avoiding solarsystem [{ss.Name}] due to gatecamp.");
                                    _dynamicSystemsToAvoid[(int)destination] = DateTime.UtcNow;
                                    reroute = true;
                                    continue;
                                }

                                Log.WriteLine($"Solarsystem [{destination}] seems to be safe to travel through.");
                            }

                            if (reroute)
                            {
                                Log.WriteLine("Rerouting on next tick due to dynamic avoidance settings.");
                                ESCache.Instance.State.CurrentTravelerState = TravelerState.CalculatePath;
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("Error getting gatecamp info: " + e);
                            ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                            return;
                        }
                    }

                    Log.WriteLine("Calculated path:");
                    int i = 0;
                    foreach (var waypoint in _destinationRoute)
                    {
                        var wp = ESCache.Instance.DirectEve.SolarSystems[(int)waypoint];
                        Log.WriteLine($"[{i}] Name [{wp.Name}] Security [{wp.GetSecurity()}]");
                        i++;
                    }
                    Log.WriteLine("Calculated path end.");
                    //if (Logging.DebugTraveler) Logging.Log("Traveler", "Destination is set: processing...", Logging.Teal);

                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Traveling;
                    break;

                case TravelerState.Traveling:

                    _attemptsToRetrieveInsurgencySystem = 0;

                    if (!ESCache.Instance.InSpace && !ESCache.Instance.InDockableLocation)
                        return;

                    if (ESCache.Instance.ActiveShip != null)
                    {
                        ActivateCovertOpsCloak();
                    }

                    if (ESCache.Instance.InWarp)
                    {
                        return;
                    }

                    if (_startedInStation && ESCache.Instance.InSpace && !_instaBMUsed)
                    {
                        UseInstaBookmark();
                        _instaBMUsed = true;
                        return;
                    }

                    if (Destination == null)
                    {
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                        break;
                    }

                    if (Destination.SolarSystemId != ESCache.Instance.DirectEve.Session.SolarSystemId)
                    {
                        //Log.WriteLine("traveler: NavigateToBookmarkSystem(Destination.SolarSystemId);");
                        NavigateToBookmarkSystem(Destination.SolarSystemId);
                    }
                    else if (Destination.PerformFinalDestinationTask(finalWarpDistance, randomFinalWarpdDistance))
                    {
                        _destinationRoute = null;
                        _location = null;
                        _locationName = String.Empty;
                        _locationErrors = 0;

                        //Log.WriteLine("traveler: _States.CurrentTravelerState = TravelerState.AtDestination;");
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.AtDestination;
                    }

                    break;

                case TravelerState.AtDestination:
                    AllowLowSec = false;
                    //do nothing when at destination
                    //Traveler sits in AtDestination when it has nothing to do, NOT in idle.
                    break;

                case TravelerState.Error:
                    AllowLowSec = false;
                    break;

            }
        }

        /// <summary>
        ///     Set destination to a solar system
        /// </summary>
        public bool SetStationDestination(long stationId)
        {
            _location = ESCache.Instance.DirectEve.Navigation.GetLocation(stationId);
            if (DebugConfig.DebugTraveler)
                Log.WriteLine("Location = [" + _location + "]");
            if (_location != null && _location.IsValid)
            {
                _locationErrors = 0;
                if (DebugConfig.DebugTraveler)
                    Log.WriteLine("Setting destination to [" + _location.Name + "]");
                try
                {
                    _location.SetDestination();
                }
                catch (Exception)
                {
                    Log.WriteLine("Set destination to [" + _location.ToString() + "] failed ");
                }

                return true;
            }

            Log.WriteLine("Error setting station destination [" + stationId + "]");
            _locationErrors++;
            if (_locationErrors > 20)
                return false;
            return false;
        }

        public void TravelHome()
        {
            try
            {

                var destinationId = ESCache.Instance.Agent.StationId;

                if (_destination == null || (DockableLocationDestination)_destination != null && ((DockableLocationDestination)_destination).DockableLocationId != destinationId)
                {
                    Log.WriteLine("StationDestination: [" + destinationId + "]");
                    _destination = new DockableLocationDestination(destinationId);

                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                }

                if (DebugConfig.DebugGotobase)
                    if (Destination != null)
                        Log.WriteLine("Traveler.Destination.SolarSystemId [" + Destination.SolarSystemId + "]");
                ProcessState();
            }
            catch (Exception)
            {
            }
        }

        public void TravelToBookmark(DirectBookmark bookmark, double finalWarpDistance = 0, bool randomFinalWarpdDistance = false)
        {
            try
            {

                var bm = ESCache.Instance.DirectEve.Bookmarks.FirstOrDefault(b => b.BookmarkId == bookmark.BookmarkId.Value);

                if (_destination == null)
                {
                    Log.WriteLine("Bookmark title: [" + bm.Title + "]");
                    _destination = new BookmarkDestination(bm);
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                    return;
                }

                if (DebugConfig.DebugGotobase)
                    if (Destination != null)
                        Log.WriteLine("Traveler.Destination.SolarSystemId [" + Destination.SolarSystemId + "]");
                ProcessState(finalWarpDistance, randomFinalWarpdDistance);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void TravelToSetWaypoint()
        {
            try
            {
                var path = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
                path.RemoveAll(i => i == 0);
                if (path.Any())
                {
                    var dest = path.Last();
                    var location = ESCache.Instance.DirectEve.Navigation.GetLocation(dest);
                    var isStationLocation = location.ItemId.HasValue && ESCache.Instance.DirectEve.Stations.TryGetValue((int)location.ItemId.Value, out var _);

                    if (!location.SolarSystemId.HasValue)
                    {
                        Log.WriteLine("Location has no solarsystem id.");
                        return;
                    }


                    if (_destination == null)
                    {
                        if (isStationLocation)
                        {
                            _destination = new DockableLocationDestination(location.ItemId.Value);
                        }
                        else if (location.IsStructureLocation)
                        {
                            _destination = new DockableLocationDestination(location.LocationId);
                        }
                        else
                        {
                            _destination = new SolarSystemDestination(location.SolarSystemId.Value);
                        }
                        ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                        return;
                    }
                }

                if (_destination != null)
                {
                    ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public bool UseInstaBookmark()
        {
            try
            {
                if (ESCache.Instance.InWarp) return false;

                if (ESCache.Instance.InDockableLocation)
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                    return false;
                }

                if (ESCache.Instance.InSpace)
                {
                    if (UndockBookmark != null)
                    {
                        if (UndockBookmark.LocationId == ESCache.Instance.DirectEve.Session.LocationId)
                        {
                            var distance = ESCache.Instance.DirectEve.Me.DistanceFromMe(UndockBookmark.X ?? 0, UndockBookmark.Y ?? 0,
                                UndockBookmark.Z ?? 0);
                            if (distance < (int)Distances.WarptoDistance)
                            {
                                Log.WriteLine("Arrived at undock bookmark [" + UndockBookmark.Title +
                                              "]");
                                return true;
                            }

                            if (distance >= (int)Distances.WarptoDistance)
                            {
                                if (UndockBookmark.WarpTo())
                                {
                                    Log.WriteLine("Warping to undock bookmark [" + UndockBookmark.Title +
                                                  "][" + Math.Round(distance / 1000 / 149598000, 2) + " AU away]");
                                    //if (!Combat.ReloadAll(Cache.Instance.EntitiesNotSelf.OrderBy(t => t.Distance).FirstOrDefault(t => t.Distance < (double)Distance.OnGridWithMe))) return false;
                                    TravelerDestination._nextTravelerDestinationAction = DateTime.UtcNow.AddSeconds(10);
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        if (DebugConfig.DebugUndockBookmarks)
                            Log.WriteLine("Bookmark Named [" + UndockBookmark.Title +
                                          "] was somehow picked as an UndockBookmark but it is not in local with us! continuing without it.");
                        return true;
                    }

                    // if we just undocked from jita 4/4 warp to planet 5 to avoid bumping

                    if (ESCache.Instance.Stations.Any(s => s.Id == 60003760 && s.Distance < 1000))
                    {


                        var jita5Plnaet = ESCache.Instance.Entities.FirstOrDefault(e =>
                            e.GroupId == (int)Group.Planet && e.Name.Equals("Jita V"));

                        List<float> warpRanges = new List<float>() {
                            //10_000,
                            20_000,
                            30_000,
                            50_000,
                            70_000,
                            100_000,
                        };

                        if (jita5Plnaet != null)
                        {
                            var randomRange = ListExtensions.Random(warpRanges);
                            if (randomRange < 0 || randomRange > 100_000)
                                randomRange = 0;

                            Log.WriteLine($"We just undocked from Jita 4/4, warping to planet V at range [{randomRange}] to prevent bumps/ganks. (Instant warp)");
                            jita5Plnaet.WarpTo(ListExtensions.Random(warpRanges));

                        }
                        return true;
                    }

                    if (DebugConfig.DebugUndockBookmarks)
                        Log.WriteLine("No undock bookmarks in local matching our undockPrefix [" +
                                      ESCache.Instance.EveAccount.CS.QMS.QS.UndockBookmarkPrefix +
                                      "] continuing without it.");
                    return true;
                }

                if (DebugConfig.DebugUndockBookmarks)
                    Log.WriteLine("InSpace [" + ESCache.Instance.InSpace + "]: waiting until we have been undocked or in system a few seconds");
                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }


        private void ActivateCovertOpsCloak([CallerMemberName] string caller = "")
        {
            if (ESCache.Instance.InDockableLocation || !ESCache.Instance.InSpace)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"ESCache.Instance.InDockableLocation || !ESCache.Instance.InSpace)");
                return;
            }

            var cloak = ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);
            if (cloak == null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"Cloak == null");
                return;
            }

            if (!caller.Equals(nameof(AddActivateCovertOpsCloakAfterJumpAction)) && _covertCloakActionQueueAction != null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine($"Blocked. Callermember: {caller}");
                return;
            }

            if (cloak.IsLimboStateWithoutEffectActivating || cloak.IsActive)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("(cloak.IsInLimboState || cloak.IsActive)");
                return;
            }

            if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("QCache.Instance.DirectEve.Me.IsJumpCloakActive");
                return;
            }

            if (ESCache.Instance.ActiveShip.Entity == null)
            {
                if (DebugConfig.DebugTraveler) Log.WriteLine("QCache.Instance.ActiveShip.Entity == null");
                return;
            }

            if (ESCache.Instance.EntitiesNotSelf.Any(e => e.GroupId != 227 && e.Distance <= (int)Distances.SafeToCloakDistance)) // 227 = Inventory Groups.Celestial.Cloud
            {
                var ent = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.Distance <= (int)Distances.SafeToCloakDistance);
                if (ent != null && ent.IsValid)
                {
                    Log.WriteLine($"Can't activate cloak because there is another entity within [{(int)Distances.SafeToCloakDistance}]m. Entity {ent.TypeName}");
                    return;
                }
            }

            if (cloak.Click(ESCache.Instance.RandomNumber(90, 150), true))
            {
                Log.WriteLine("Activating covert ops cloak.");
            }
        }

        private void AddActivateCovertOpsCloakAfterJumpAction()
        {
            var cloakActionTimeout = DateTime.UtcNow.AddSeconds(45);
            var _rnd = new Random();
            var nextPulse = DateTime.UtcNow;
            ActionQueueAction covOpsAction = new ActionQueueAction(() => // create new action to delay the click for 40ms
            {
                ActivateCovertOpsCloak(nameof(AddActivateCovertOpsCloakAfterJumpAction));
            }, true).Initialize();
            _covertCloakActionQueueAction = new ActionQueueAction(() =>
            {

                //Log.WriteLine("Pulse");
                if (nextPulse > DateTime.UtcNow)
                {
                    _covertCloakActionQueueAction.QueueAction();
                    return;
                }

                nextPulse = DateTime.UtcNow.AddMilliseconds(_rnd.Next(90, 150));

                if (cloakActionTimeout < DateTime.UtcNow)
                {
                    _covertCloakActionQueueAction = null;
                    if (DebugConfig.DebugTraveler) Log.WriteLine("Cov ops cloak action timed out.");
                    return;
                }

                var covOpsCloak = ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);

                if (covOpsCloak.IsActive && ESCache.Instance.ActiveShip.Entity != null && ESCache.Instance.ActiveShip.Entity.Velocity > 80000)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == 34590)
                    {
                        DeactivateCloak();
                    }
                    _covertCloakActionQueueAction = null;
                    Log.WriteLine("Stopping action, cloak is active.");
                    return;
                }

                if (ESCache.Instance.ActiveShip.Entity == null || ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                {
                    if (DebugConfig.DebugTraveler) Log.WriteLine("ESCache.Instance.ActiveShip.Entity == null || ESCache.Instance.DirectEve.Me.IsJumpCloakActive");
                    _covertCloakActionQueueAction.QueueAction();
                    return;
                }

                if ((!ESCache.Instance.DirectEve.Me.IsJumpCloakActive || ESCache.Instance.ActiveShip.Entity.Velocity > 0) && !covOpsCloak.IsLimboStateWithoutEffectActivating && !covOpsCloak.IsActive)
                {
                    Log.WriteLine($"Adding AddActivateCovertOpsCloakAfterJumpAction. JumpCloakActive [{ESCache.Instance.DirectEve.Me.IsJumpCloakActive}] Velocity [{ESCache.Instance.ActiveShip.Entity.Velocity}] covOpsCloak.IsLimboStateWithoutEffectActivating [{covOpsCloak.IsLimboStateWithoutEffectActivating}] [{covOpsCloak.IsActive}]");
                    covOpsAction.QueueAction(_rnd.Next(90, 100));
                    //covOpsAction.QueueAction();
                }
                else
                {
                    if (DebugConfig.DebugTraveler)
                        Log.WriteLine($"ESCache.Instance.DirectEve.Me.IsJumpCloakActive [{ESCache.Instance.DirectEve.Me.IsJumpCloakActive}] ActiveShip.Entity.Velocity [{ESCache.Instance.ActiveShip.Entity.Velocity > 0}] CovOpsLimbo [{covOpsCloak.IsInLimboState}] CovOpsActive [{covOpsCloak.IsActive}] CovOpsReactivationDelay [{covOpsCloak.ReactivationDelay}] EffectActivating [{covOpsCloak.EffectActivating}] DirectEve.IsEffectActivating(covOpsCloak) [{ESCache.Instance.DirectEve.IsEffectActivating(covOpsCloak)}]");
                }

                _covertCloakActionQueueAction.QueueAction();
            }, true);
            _covertCloakActionQueueAction.Initialize().QueueAction(5000);
        }

        private void DeactivateCloak()
        {
            var cloak = ESCache.Instance.Modules.FirstOrDefault(m => m.TypeId == 11578);
            if (cloak != null && cloak.IsActive)
            {
                if (cloak.Click())
                {
                    Log.WriteLine($"Deactivating cloak.");
                }
            }
        }

        /// <summary>
        ///     Navigate to a solar system
        /// </summary>
        /// <param name="solarSystemId"></param>
        private void NavigateToBookmarkSystem(long solarSystemId)
        {
            if (ESCache.Instance.Time.NextTravelerAction > DateTime.UtcNow)
            {
                if (DebugConfig.DebugTraveler)
                    Log.WriteLine("will continue in [ " + Math.Round(ESCache.Instance.Time.NextTravelerAction.Subtract(DateTime.UtcNow).TotalSeconds, 0) + " ]sec");
                return;
            }

            ESCache.Instance.Time.NextTravelerAction = DateTime.UtcNow.AddSeconds(2);
            if (DebugConfig.DebugTraveler) Log.WriteLine("Iterating- next iteration should be in no less than [1] second ");


            //_destinationRoute = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();
            var evePath = ESCache.Instance.DirectEve.Navigation.GetDestinationPath();


            // [PLACEBO] just set a path within eve.. may look more legit // do we really need that tho? kinda wasted resources
            if (evePath == null || evePath.Count == 0 || evePath.All(d => d != solarSystemId))
            {
                if (DateTime.UtcNow < _nextGetLocation)
                    if (evePath.Count == 0)
                        Log.WriteLine("We have no destination");
                    else if (evePath.All(d => d != solarSystemId))
                        Log.WriteLine("The destination is not currently set to solarsystemId [" + solarSystemId + "]");

                // We do not have the destination set
                if (DateTime.UtcNow > _nextGetLocation || _location == null)
                {
                    Log.WriteLine("Getting Location of solarSystemId [" + solarSystemId + "]");
                    _nextGetLocation = DateTime.UtcNow.AddSeconds(10);
                    _location = ESCache.Instance.DirectEve.Navigation.GetLocation(solarSystemId);
                    ESCache.Instance.Time.NextTravelerAction = DateTime.UtcNow.AddSeconds(2);
                    return;
                }

                if (_location != null && _location.IsValid)
                {
                    _locationErrors = 0;
                    Log.WriteLine("Setting destination to [" + _location.Name + "] [The real used path may differ]");
                    try
                    {
                        _location.SetDestination();
                    }
                    catch (Exception)
                    {
                        Log.WriteLine("Set destination to [" + _location.ToString() + "] failed ");
                    }

                    ESCache.Instance.Time.NextTravelerAction = DateTime.UtcNow.AddSeconds(3);
                    return;
                }

                Log.WriteLine("Error setting solar system destination [" + solarSystemId + "]");
                _locationErrors++;
                if (_locationErrors > 20)
                {
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Error;
                    return;
                }

                return;
            }

            _locationErrors = 0;
            if (!ESCache.Instance.InSpace)
            {
                if (ESCache.Instance.InDockableLocation)
                {
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);

                    ESCache.Instance.Time.LastUndockAction = DateTime.UtcNow;
                }

                ESCache.Instance.Time.NextActivateModules = DateTime.UtcNow.AddSeconds(ESCache.Instance.RandomNumber(2, 3));

                // We are not yet in space, wait for it
                return;
            }

            // We are apparently not really in space yet...
            if (ESCache.Instance.ActiveShip == null || ESCache.Instance.ActiveShip.Entity == null)
                return;


            // Find the next waypoint

            var currentIndex = _destinationRoute.IndexOf(ESCache.Instance.DirectEve.Me.CurrentSolarSystem.Id);
            var waypoint = _destinationRoute[currentIndex + 1];


            //if (Logging.DebugTraveler) Logging.Log("Traveler", "NavigateToBookmarkSystem: getting next way-points locationName", Logging.Teal);
            _locationName = ESCache.Instance.DirectEve.Navigation.GetLocationName(waypoint);
            if (DebugConfig.DebugTraveler)
                Log.WriteLine("Next Waypoint is: [" + _locationName + "]");

            var solarSystemInRoute = ESCache.Instance.DirectEve.SolarSystems[(int)waypoint];

            if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                if (solarSystemInRoute != null && solarSystemInRoute.GetSecurity() < 0.45 &&
                    (ESCache.Instance.ActiveShip.GroupId != (int)Group.Shuttle &&
                     ESCache.Instance.ActiveShip.GroupId != (int)Group.Frigate &&
                     ESCache.Instance.ActiveShip.GroupId != (int)Group.Interceptor &&
                     ESCache.Instance.ActiveShip.GroupId != (int)Group.BlockadeRunner &&
                     ESCache.Instance.ActiveShip.GroupId != (int)Group.ForceReconShip &&
                     ESCache.Instance.ActiveShip.GroupId != (int)Group.StealthBomber))
                {
                    Log.WriteLine("Next Waypoint is: [" + _locationName +
                                  "] which is LOW SEC! This should never happen. Turning off AutoStart and going home.");
                    if (ESCache.Instance.State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                        ESCache.Instance.State.CurrentCombatMissionBehaviorState = CombatMissionsBehaviorState.GotoBase;
                    return;
                }
            // Find the stargate associated with it

            if (!ESCache.Instance.Stargates.Any())
            {
                // not found, that cant be true?!?!?!?!
                Log.WriteLine("Error [" + _locationName + "] not found, most likely lag waiting [" + ESCache.Instance.Time.TravelerNoStargatesFoundRetryDelay_seconds +
                              "] seconds.");
                ESCache.Instance.Time.NextTravelerAction = DateTime.UtcNow.AddSeconds(ESCache.Instance.Time.TravelerNoStargatesFoundRetryDelay_seconds);
                return;
            }

            // Warp to, approach or jump the stargate
            var nextStargate = ESCache.Instance.Entities.Where(i => i.GroupId == (int)Group.Stargate).Where(e => e.Name.ToLower() == _locationName.ToLower()).ToList()
                .FirstOrDefault();
            if (nextStargate != null)
            {
                if (!ESCache.Instance.ActiveShip.Entity.IsCloaked && nextStargate.Distance < (int)Distances.JumpRange)
                {
                    if (ESCache.Instance.InWarp) return;
                    if (nextStargate.Jump())
                    {
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                        Log.WriteLine("Jumping to [" + _locationName + "]");
                        if (ESCache.Instance.Modules.Any(i => i.TypeId == 11578) && ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578).IsOnline)
                        {
                            Log.WriteLine("Covert ops cloak found, adding cloak action.");
                            AddActivateCovertOpsCloakAfterJumpAction();
                        }
                        return;
                    }
                    return;
                }

                if (nextStargate.Distance != 0)
                    if (ESCache.Instance.NavigateOnGrid.NavigateToTarget(nextStargate, "Traveler", false, 0))
                        return;
            }
        }

        #endregion Methods
    }
}