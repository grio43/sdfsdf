/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 23.10.2017
 * Time: 04:13
 *
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using ServiceStack.Text;
using DirectEvent = SC::SharedComponents.Events.DirectEvent;
using DirectEvents = SC::SharedComponents.Events.DirectEvents;

namespace EVESharpCore.Controllers.Pinata
{
    /// <summary>
    ///     Description of PinataController.
    /// </summary>
    public class PinataController : BaseController
    {
        #region Constructors

        public PinataController() : base()
        {
            _checkedSystems = new Dictionary<DirectSolarSystem, Tuple<bool, DateTime>>();
            _foundWorthyTargets = new List<Tuple<DateTime, string>>();
            IgnorePause = false;
            IgnoreModal = false;
            _previousSystemName = string.Empty;
            Form = new PinataControllerForm(this);

        }

        #endregion Constructors

        public static void SendDiscordWebHookMessage(string message)
        {
            string webhookUrl = "https://discord.com/api/webhooks/1134559873615540415/u0TN8T1fHfxBHDdIO9W3ChpWmjiTeDCpnqeiJQR6op0WrG-PSn2meeCSrEjn2Ufk6BHA";
            string usernameOverride = "CCP_BELT_P0LICE";

            try
            {
                WCFClient.Instance.GetPipeProxy.SendDiscordWebhookMessage(webhookUrl, message, usernameOverride);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in SendDiscordWebHookMessage: " + ex.Message);
            }
        }

        #region Routine for sorting asteroid belts

        private static int CompareAsteroidBelts(EntityCache e1, EntityCache e2)
        {
            int e1num = 0, e2num = 0;
            string planetE1 = e1.Name.Split(' ')[1],
                planetE2 = e2.Name.Split(' ')[1];

            string[] romans =
            {
                "N", "I", "II", "III", "IV", "V", "VI", "VII",
                "VIII", "IX", "X", "XI", "XII", "XIII", "XIV",
                "XV", "XVI", "XVII", "XVIII", "XIX", "XX"
            };

            for (var i = 0; i < romans.Length; i++)
            {
                if (romans[i] == planetE1)
                    e1num = i;
                if (romans[i] == planetE2)
                    e2num = i;
                if (e1num > 0 && e2num > 0)
                    break;
            }

            var i1 = int.Parse(e1.Name.Substring(e1.Name.LastIndexOf(' ') + 1));
            var i2 = int.Parse(e2.Name.Substring(e2.Name.LastIndexOf(' ') + 1));

            if (e1num > e2num)
            {
                return 1;
            }
            else if (e1num < e2num)
            {
                return -1;
            }
            else
            {
                if (i1 > i2)
                    return 1;
                else if (i1 < i2)
                    return -1;
                else
                    return 0;
            }
        }

        #endregion Routine for sorting asteroid belts

        #region Fields

        public static List<String> WORTHY_TARGETS = new List<string>()
        {
            "Unit D-34343", // min bounty for officers = 17,343,750
            "Unit F-435454",
            "Unit P-343554",
            "Unit W-634",
            "Gotan Kreiss",
            "Hakim Stormare",
            "Mizuro Cybon",
            "Tobias Kruzhor",
            "Brokara Ryver",
            "Chelm Soran",
            "Selynne Mardakar",
            "Vizan Ankonin",
            "Ahremen Arkah",
            "Draclira Merlonne",
            "Raysere Giant",
            "Tairei Namazoth",
            "Brynn Jerdola",
            "Cormack Vaaja",
            "Setele Schellan",
            "Tuvan Orth",
            "Estamel Tharchon",
            "Kaikka Peunato",
            "Thon Eney",
            "Vepas Minimala",
            "Carrier"
        };

        private static readonly int BELT_WARPIN_DISTANCE = 4500;

        private static readonly int COVERT_OPS_CLOAK_TYPE_ID = 11578;

        private static double TARGET_BOUNTY = 5_123_123;

        private int _beltId;

        private Dictionary<DirectSolarSystem, Tuple<bool, DateTime>> _checkedSystems;

        private Destination _currentDestination;
        private List<Tuple<DateTime, String>> _foundWorthyTargets;

        private DateTime _lastCloakModification = DateTime.MinValue;

        private List<DirectSolarSystem> _path;

        private PinataControllerStates _pinataControllerState;

        private Destination _prevDestination;
        private String _previousSystemName;

        private int _stargateSearchCount;

        private Destination CurrentDestination
        {
            get => _currentDestination;
            set
            {
                _prevDestination = _currentDestination;
                _currentDestination = value;
                //Log($"CurrentDestination {CurrentDestination} PreviousDestination {PreviousDestination}");
            }
        }

        private Destination PreviousDestination => _prevDestination;

        #endregion Fields

        #region Enums

        public enum Destination
        {
            Belt,
            Stargate,
            SolarSystem
        }

        public enum PinataControllerStates
        {
            CeckRegion,
            CalcPathClosestSystem,
            Travel,
            CheckBelts,
            CalcNextSystem,
            Finished,
            Reset,
            Error,
        }

        #endregion Enums

        #region Properties

        private string _destinationRegion => ESCache.Instance.EveAccount.CS.PinataMainSetting.Region.ToString().Replace("_", " ");

        #endregion Properties

        #region Methods

        private bool WaitForCloakReactivationDelay => ESCache.Instance.Modules.Any(m => m.TypeId == COVERT_OPS_CLOAK_TYPE_ID)
                                                      && ESCache.Instance.DirectEve.Me.IsJumpCloakActive
                                                      //&& QCache.Instance.EntitiesNotSelf.Any(e => e.Distance < 1400000)
                                                      && ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1400000)
                                                      && ESCache.Instance.Modules.FirstOrDefault(m => m.TypeId == COVERT_OPS_CLOAK_TYPE_ID).ReactivationDelay > 0;

        public void CheckWorthyTargets(EntityCache currentBelt)
        {
            var valuableEntities = ESCache.Instance.EntitiesOnGrid.Where(e => e.Id != ESCache.Instance.ActiveShip.ItemId
                                                                             && e.CategoryId !=
                                                                             (int)CategoryID.Asteroid
                                                                             && !e.IsPlayer
                                                                             &&
                                                                             (WORTHY_TARGETS.Contains(e.Name) || e.GetBounty > TARGET_BOUNTY));
            if (valuableEntities.Any())
            {
                Log($"----------------------------------------");
                foreach (var e in valuableEntities)
                {
                    var msg = $"-- A valuable Entity [{e.Name}] with bounty [{e.GetBounty}] was found on belt [{currentBelt.Name}]";
                    SendDiscordWebHookMessage(msg);
                    Log(msg);
                    Log($"{e.DirectEntity.GetResistInfo()}");
                    DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                    _foundWorthyTargets.Add(new Tuple<DateTime, string>(DateTime.UtcNow,
                        $"{e.Name} Bounty {e.GetBounty} Belt {currentBelt.Name}"));
                }
                Log($"----------------------------------------");
                UpdateFoundWorthyTarget();
            }
            else
            {
                Log($"No valuable entity was found.");
            }
        }

        public override void DoWork()
        {
            var de = ESCache.Instance.DirectEve;

            if (ESCache.Instance.InDockableLocation)
            {
                if (ESCache.Instance.ActiveShip == null)
                    return;

                Log($"Undock.");
                ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdExitStation);
                LocalPulse = UTCNowAddMilliseconds(8000, 10000);
                return;
            }

            if (!ESCache.Instance.InSpace)
                return;

            if (ESCache.Instance.ActiveShip.Entity == null)
                return;

            TARGET_BOUNTY = ESCache.Instance.EveAccount.ClientSetting.PinataMainSetting.MinBounty;

            if (!ESCache.Instance.DirectEve.Me.IsJumpCloakActive
                && ESCache.Instance.Stargates.Any(e => e.Name == _previousSystemName && e.Distance < 80000)
                && PreviousDestination == Destination.SolarSystem
                && (CurrentDestination == Destination.Belt || CurrentDestination == Destination.Stargate))
            {
                ActivateCloak(); // always activate cloak after a gate jump
            }

            if (PreviousDestination == CurrentDestination || CurrentDestination == Destination.Belt)
            {
                ActivateCloak(); // cloak belt -> belt or on start
            }

            if (ESCache.Instance.ActiveShip.Entity.Velocity > 50000
                && (PreviousDestination == Destination.Belt || PreviousDestination == Destination.SolarSystem) && CurrentDestination == Destination.Stargate)
            {
                DeactivateCloak(); // deactivate cloak belt -> stargate (the yacht has 30 sec cov ops cloak reactivation timer for example)
            }

            if (ESCache.Instance.InWarp) // do nothing while in warp
            {
                return;
            }

            if (WaitForCloakReactivationDelay)
            {
                Log($"Waiting for cloak reactivation delay.");
                return;
            }

            if (ESCache.Instance.ActiveShip.Entity.GroupId == (int)Group.Capsule)
            {
                Log($"Error: We are in a capsule.");
                _pinataControllerState = PinataControllerStates.Error;
            }

            var currentSystem = de.SolarSystems.FirstOrDefault(s => s.Key == de.Session.SolarSystemId);

            if (string.IsNullOrEmpty(_previousSystemName))
                _previousSystemName = currentSystem.Value.Name;

            switch (_pinataControllerState)
            {
                case PinataControllerStates.CeckRegion:

                    if (!de.Regions.Any(r => r.Value.Name.Contains(_destinationRegion)))
                    {
                        Log($"The destination region does not exist.");
                        _pinataControllerState = PinataControllerStates.Error;
                        return;
                    }

                    var region = de.Regions.FirstOrDefault(r => r.Value.Name.Contains(_destinationRegion));

                    // settings up checked systems dict
                    foreach (var sys in GetRegionSolarSystems(_destinationRegion))
                        SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(_checkedSystems, sys, new Tuple<bool, DateTime>(false, DateTime.MinValue));
                    UpdateDataGrid();

                    if (region.Value.Id == de.Session.RegionId)
                    {
                        Log($"We are in the correct region.");
                        _pinataControllerState = _pinataControllerState = PinataControllerStates.CheckBelts;
                        return;
                    }
                    else
                    {
                        Log($"We are NOT in the correct region.");
                        _pinataControllerState = _pinataControllerState = PinataControllerStates.CalcPathClosestSystem;
                        return;
                    }

                case PinataControllerStates.CalcPathClosestSystem:
                    Log($"Current system is {currentSystem.Value.Name}");
                    var destRegionSystem = GetRegionSolarSystems(_destinationRegion);
                    var closestRegionSolarSystem = destRegionSystem.Aggregate(
                        (i1, i2) => i1.GetDistance(currentSystem.Value) < i2.GetDistance(currentSystem.Value) ? i1 : i2);
                    Log($"The closest system of the destination region is {closestRegionSolarSystem.Name}");
                    _path = currentSystem.Value.CalculatePathTo(closestRegionSolarSystem, null, true, true).Item1;
                    Log($"Path to the closest system of the destination region is:");
                    var n = 0;
                    Log($"----------------------------------------", null);
                    foreach (var p in _path)
                    {
                        Log($"{n} {p.Name}");
                        n++;
                    }

                    Log($"----------------------------------------", null);
                    _pinataControllerState = PinataControllerStates.Travel;
                    break;

                case PinataControllerStates.Travel:

                    if (_path.Contains(currentSystem.Value))
                        _path.Remove(currentSystem.Value);

                    if (_path.Count > 0)
                    {
                        var next = _path[0];
                        if (ESCache.Instance.Stargates != null && ESCache.Instance.Stargates.Any(s => s.Name.Contains(next.Name)))
                        {
                            _stargateSearchCount = 0;
                            var gate = ESCache.Instance.Stargates.FirstOrDefault(s => s.Name.Contains(next.Name));

                            if (gate.Distance > 160000)
                            {
                                if (gate.WarpTo(0))
                                {
                                    Log($"Warping to {next.Name} stargate");
                                    CurrentDestination = Destination.Stargate;
                                    //LocalPulse = GetUTCNowDelayMilliseconds(3000, 4000);
                                    return;
                                }
                            }
                            else
                            {
                                if (gate.Distance < 2480)
                                {
                                    if (gate.Jump())
                                    {
                                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Keep alive."));
                                        Log($"Jumping to {next.Name}");
                                        _previousSystemName = currentSystem.Value.Name;
                                        LocalPulse = UTCNowAddMilliseconds(6000, 7000);
                                        CurrentDestination = Destination.SolarSystem;
                                        return;
                                    }
                                }
                                else
                                {
                                    if (gate.AlignTo())
                                    {
                                        Log($"Aligning to {next.Name} stargate");
                                        LocalPulse = UTCNowAddMilliseconds(6000, 7000);
                                        return;
                                    }
                                }
                            }
                        }
                        else
                        {
                            _stargateSearchCount++;
                            Log($"Stargate to the {next.Name} was not found.");
                            LocalPulse = UTCNowAddSeconds(800, 2000);
                            if (_stargateSearchCount > 20)
                                _pinataControllerState = PinataControllerStates.Error;

                            return;
                        }
                    }
                    else
                    {
                        Log($"Destination reached.");
                        _beltId = 0;
                        _pinataControllerState = PinataControllerStates.CheckBelts;
                    }

                    break;

                case PinataControllerStates.CheckBelts:
                    var belts = GetAsteroidBelts();
                    if (belts.Count != 0 && _beltId < belts.Count)
                    {
                        var currentBelt = belts[_beltId];
                        if (currentBelt.Distance < 155000)
                        {
                            CheckWorthyTargets(currentBelt);
                            var entitiesOnGrid = ESCache.Instance.EntitiesOnGrid.Where(e => e.Id != ESCache.Instance.ActiveShip.ItemId
                                                                                           && e.Id != currentBelt.Id
                                                                                           && e.CategoryId != (int)CategoryID.Asteroid);
                            if (entitiesOnGrid.Any()) // debug
                            {
                                Log($"----------------------------------------");
                                foreach (var e in entitiesOnGrid)
                                    Log($"-- Entity {e.Name} Typename {e.TypeName} GroupId {e.GroupId} CatId {e.CategoryId} in belt {currentBelt.Name}");
                                Log($"----------------------------------------");
                            }
                            _beltId++;
                            Log($"Amount of belts in current system [{belts.Count}] Checked [{_beltId}]");
                            return;
                        }
                        else
                        {
                            if (currentBelt.WarpTo(BELT_WARPIN_DISTANCE))
                            {
                                CheckWorthyTargets(currentBelt);
                                LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                                Log($"Warping to belt {currentBelt.Name}.");
                                CurrentDestination = Destination.Belt;
                                return;
                            }
                        }
                    }
                    else
                    {
                        Log($"No belts found or every one was checked, moving to next system.");
                        SC::SharedComponents.Extensions.DictionaryExtensions.AddOrUpdate(_checkedSystems, currentSystem.Value, new Tuple<bool, DateTime>(true, DateTime.Now));

                        UpdateDataGrid();
                        _pinataControllerState = PinataControllerStates.CalcNextSystem;
                    }

                    break;

                case PinataControllerStates.CalcNextSystem:

                    Log($"{_checkedSystems.Count(s => s.Value.Item1)} systems have been checked of a total of {_checkedSystems.Count} systems.");
                    if (_checkedSystems.All(s => s.Value.Item1))
                    {
                        _pinataControllerState = PinataControllerStates.Finished;
                        return;
                    }
                    else
                    {
                        var uncheckedSolarSystems = _checkedSystems.Where(s => !s.Value.Item1);
                        var uncheckedSolarSystem = uncheckedSolarSystems
                            .Aggregate((i1, i2) => i1.Key.GetDistance(currentSystem.Value) < i2.Key.GetDistance(currentSystem.Value) ? i1 : i2)
                            .Key;
                        Log($"The closest unchecked system (by x,y,z distance) is {uncheckedSolarSystem.Name}");
                        _path = currentSystem.Value.CalculatePathTo(uncheckedSolarSystem, null, true, true).Item1;

                        if (_path.Count > 2)
                        {
                            Log($"Looking for a system with less jumps required.");
                            var nearNeighbours = currentSystem.Value.Neighbours.SelectMany(s => s.Neighbours).Concat(currentSystem.Value.Neighbours);
                            if (nearNeighbours.Any(nn => uncheckedSolarSystems.Any(un => un.Key == nn)))
                            {
                                var possibleSystem = nearNeighbours.Where(nn => uncheckedSolarSystems.Any(un => un.Key == nn));
                                uncheckedSolarSystem = possibleSystem.Aggregate(
                                    (i1, i2) => i1.CalculatePathTo(currentSystem.Value, null, true, true).Item1.Count < i2.CalculatePathTo(currentSystem.Value, null, true, true).Item1.Count
                                        ? i1
                                        : i2);
                                Log($"A closer system is {uncheckedSolarSystem.Name}");
                                _path = currentSystem.Value.CalculatePathTo(uncheckedSolarSystem, null, true, true).Item1;
                            }
                        }

                        // const check
                        if (currentSystem.Value.ConstellationId != uncheckedSolarSystem.ConstellationId)
                            if (_checkedSystems.Where(s => s.Key.ConstellationId == currentSystem.Value.ConstellationId).Any(s => !s.Value.Item1))
                            {
                                uncheckedSolarSystem = _checkedSystems
                                    .Where(s => s.Key.ConstellationId == currentSystem.Value.ConstellationId && !s.Value.Item1).Aggregate(
                                        (i1, i2) => i1.Key.CalculatePathTo(currentSystem.Value, null, true, true).Item1.Count <
                                                    i2.Key.CalculatePathTo(currentSystem.Value, null, true, true).Item1.Count
                                            ? i1
                                            : i2).Key;
                                Log(
                                    $"Not all systems of the current constellation have been checked. Next system of current const is: {uncheckedSolarSystem.Name}");
                                _path = currentSystem.Value.CalculatePathTo(uncheckedSolarSystem, null, true, true).Item1;
                            }

                        Log($"Path to the next unchecked system is:");
                        Log($"----------------------------------------");
                        var k = 0;
                        foreach (var p in _path)
                        {
                            Log($"{k} {p.Name}");
                            k++;
                        }

                        if (_path.Count == 0)
                            throw new Exception("Path.Count == 0");

                        Log($"----------------------------------------");
                        _pinataControllerState = PinataControllerStates.Travel;
                    }

                    break;

                case PinataControllerStates.Finished:
                    Log($"Finished.");
                    _pinataControllerState = PinataControllerStates.Reset;
                    break;

                case PinataControllerStates.Reset:
                    Log($"Starting again.");
                    _pinataControllerState = PinataControllerStates.CeckRegion;
                    break;

                case PinataControllerStates.Error:
                    Log($"Error state.");
                    if (ESCache.Instance.InSpace && ESCache.Instance.Star != null)
                    {
                        if (ESCache.Instance.Star.Distance > 500000000)
                        {
                            Log($"Warping to star.");
                            LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                            ESCache.Instance.Star.WarpTo(4500);
                            return;
                        }
                        else
                        {
                            if (ESCache.Instance.ActiveShip.Entity.GroupId == (int)Group.Capsule || // capsule or no cloak
                                !ESCache.Instance.Modules.Any(m => m.TypeId == COVERT_OPS_CLOAK_TYPE_ID))
                            {
                                Log($"Disabling this instance.");
                                ESCache.Instance.DisableThisInstance();
                                ESCache.Instance.ExitEve("Error.");
                            }
                            else
                            {
                                ActivateCloak();
                                if (ESCache.Instance.ActiveShip.Entity.Velocity < 1)
                                {
                                    Log($"MoveTo (0,1,0)");
                                    ESCache.Instance.ActiveShip.MoveTo(0, 1, 0);
                                    LocalPulse = UTCNowAddMilliseconds(3000, 4000);
                                    return;
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public List<EntityCache> GetAsteroidBelts()
        {
            var list = ESCache.Instance.Entities.Where(e => e.GroupId == (int)Group.AsteroidBelt).ToList();
            list.Sort(CompareAsteroidBelts);
            list.Reverse();
            return list;
        }

        public List<DirectSolarSystem> GetRegionSolarSystems(string r)
        {
            if (ESCache.Instance.DirectEve.Regions.Any(n => n.Value.Name.Equals(r)))
            {
                var region = ESCache.Instance.DirectEve.Regions.FirstOrDefault(n => n.Value.Name.Equals(r));
                var solarsystems = region.Value.Constellations.Select(k => k.SolarSystems).ToList().SelectMany(k => k);
                return solarsystems.ToList();
            }
            return new List<DirectSolarSystem>();
        }

        public void UpdateDataGrid()
        {
            try
            {
                var list = _checkedSystems.OrderBy(d => d.Key.Constellation.Name)
                    .Select(d => new
                    {
                        d.Key.Name,
                        Checked = d.Value.Item1,
                        CheckedWhen = d.Value.Item2,
                        Const = d.Key.Constellation.Name,
                        Sec = Math.Round(d.Key.GetSecurity(), 2)
                    })
                    .ToList();

                Form.Invoke(new Action(() => { ((PinataControllerForm)Form).GetDataGridView1.DataSource = list; }));
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        public void UpdateFoundWorthyTarget()
        {
            try
            {
                var list = _foundWorthyTargets.OrderBy(d => d.Item1)
                    .Select(d => new
                    {
                        Log = d.Item2,
                    })
                    .ToList();

                Form.Invoke(new Action(() => { ((PinataControllerForm)Form).GetDataGridView2.DataSource = list; }));
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void ActivateCloak()
        {
            if (_lastCloakModification.AddMilliseconds(450) > DateTime.UtcNow)
                return;

            var cloak = ESCache.Instance.Modules.FirstOrDefault(m => m.TypeId == COVERT_OPS_CLOAK_TYPE_ID);

            if (cloak == null || !cloak.IsOnline) // no cloak
                return;

            if (cloak.IsActive) // active
                return;

            if (cloak.IsInLimboState) // limbo state, reactivation delay
                return;

            if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive) // jump cloak is active
                return;

            if (ESCache.Instance.EntitiesNotSelf.Any(e => e.Distance <= 2200)) // any entity near us
                return;

            if (CurrentDestination != Destination.Belt && !ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1400000) // if there is no player at the gate we don't need to cloak
                && ESCache.Instance.Stargates.Any(e => e.Distance < 80000))
                return;

            if (cloak.Click(400))
            {
                Log($"Activating cloak.");
                _lastCloakModification = DateTime.UtcNow;
            }
        }

        private void DeactivateCloak()
        {
            if (_lastCloakModification.AddSeconds(3) > DateTime.UtcNow)
                return;

            var cloak = ESCache.Instance.Modules.FirstOrDefault(m => m.TypeId == COVERT_OPS_CLOAK_TYPE_ID);
            if (cloak != null && cloak.IsActive)
            {
                if (cloak.Click())
                {
                    Log($"Deactivating cloak.");
                    _lastCloakModification = DateTime.UtcNow;
                }
            }
        }
        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}