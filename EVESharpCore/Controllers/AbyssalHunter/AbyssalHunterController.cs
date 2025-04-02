extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework;
using EVESharpCore.Traveller;
using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.IPC;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack.OrmLite;
using EVESharpCore.Framework.Lookup;


namespace EVESharpCore.Controllers.AbyssalHunter
{
    public class AbyssalHunterController : BaseController
    {

        public enum AbyssalHunterState
        {
            Start,
            ChooseNextSystem,
            TravelToNextSystem,
            ProbeSystem,
            Error
        }

        public enum ProbeScanState
        {
            InitialMaxRangeScan,
            InitialSnapShot,
            PickNextSig,
            NextSigMaxRangeScan,
            ScanChosenSig,
            InvestigateTrace
        }

        private ProbeScanState _probeScanState;

        private AbyssalHunterState _state { get; set; }

        private DirectSolarSystem _nextDirectSolarSystem;

        private Dictionary<string, bool> _checkedSignatureIds;
        private Dictionary<string, bool> _visitedSignatureIds;

        private DirectUIModule _coverOpsClocks => ESCache.Instance.Modules.FirstOrDefault(i => i.TypeId == 11578);

        private string _currentlyVisitingSignatureId;

        private string _currentSignatureId;
        private int _minRangeProbeScanAttempts;
        public AbyssalHunterController()
        {
            Form = new AbyssalHunterControllerForm(this);
            _state = AbyssalHunterState.Start;
            ResetState();
        }

        private void ResetState()
        {
            _checkedSignatureIds = new Dictionary<string, bool>();
            _visitedSignatureIds = new Dictionary<string, bool>();
            _currentSignatureId = string.Empty;
            _minRangeProbeScanAttempts = 0;
            _currentlyVisitingSignatureId = string.Empty;
            _probeScanState = ProbeScanState.InitialMaxRangeScan;
        }

        public override void DoWork()
        {
            switch (_state)
            {
                case AbyssalHunterState.Start:
                    Start();
                    break;
                case AbyssalHunterState.TravelToNextSystem:
                    TravelToNextSystem();
                    break;
                case AbyssalHunterState.ChooseNextSystem:
                    ChooseNextSystem();
                    break;
                case AbyssalHunterState.ProbeSystem:
                    ProbeSystem();
                    break;
                case AbyssalHunterState.Error:
                    Error();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Start()
        {
            _state = AbyssalHunterState.ChooseNextSystem;
            //_state = AbyssalHunterState.ProbeSystem;

            _currentSignatureId = string.Empty;
            _minRangeProbeScanAttempts = 0;
        }

        private bool RecoverProbes()
        {
            var mapViewWindow = Framework.DirectMapViewWindow;

            if (mapViewWindow == null)
            {
                Log("Map view window not found.");
                return false;
            }

            if (mapViewWindow.GetProbes().Count > 0 && mapViewWindow.RecoverProbes())
            {
                Log($"Recovering proboes.");
                LocalPulse = UTCNowAddSeconds(3, 4);
                return true;
            }
            return false;
        }

        private void ChooseNextSystem()
        {
            using var rc = ReadConn.Open();
            using var wc = WriteConn.Open();
            var visited = rc.DB.Select<AbyssHunterVisited>().Where(e => e.Viewed >= DateTime.Now.AddHours(-6)).ToList();
            var excludedSystem = visited.Select(e => e.SolarSystemId).ToHashSet();
            //_nextDirectSolarSystem = GetClosestNonIslandSolarSystem(excludedSystem);
            _nextDirectSolarSystem = GetClosestNonIslandSolarSystem(excludedSystem, 0.45D, 0.65D);

            //if (_nextDirectSolarSystem == null)
            //{
            //    Log($"No more 0.5 systems, also checking 0.6 systems");
            //    _nextDirectSolarSystem = GetClosestNonIslandSolarSystem(excludedSystem, 0.45D, 0.65D);
            //}

            if (_nextDirectSolarSystem == null)
            {
                if (RecoverProbes())
                    return;

                Log("_nextDirectSolarSystem == null");
                LocalPulse = UTCNowAddSeconds(480, 600);
                return;
            }

            Log($"Next system: {_nextDirectSolarSystem.Name}");

            wc.DB.Insert(new AbyssHunterVisited
            {
                SolarSystemId = _nextDirectSolarSystem.Id,
                Viewed = DateTime.UtcNow,
                SolarSystemName = _nextDirectSolarSystem.Name,
                ViewedByCharacterName = Framework.Session.Character.Name,
            });

            _state = AbyssalHunterState.TravelToNextSystem;
            _checkedSignatureIds.Clear();
        }


        private void InvestigateTraces()
        {

            if (ESCache.Instance.InWarp)
            {
                Log("Waiting, in warp.");
                return;
            }

            if (String.IsNullOrEmpty(_currentlyVisitingSignatureId))
            {
                var kvp = _visitedSignatureIds.Select(e => new { e.Key, e.Value }).FirstOrDefault(e => !e.Value);

                if (kvp == null)
                {
                    return;
                }

                _currentlyVisitingSignatureId = kvp.Key;
            }

            // Check if a bookmark exists with our current visiting Signature
            var bookmark = Framework.Bookmarks.FirstOrDefault(e => e.Title == _currentlyVisitingSignatureId && e.IsInCurrentSystem);

            if (bookmark == null)
            {
                if (DirectEve.Interval(15000))
                    Log($"Bookmark with Id [{_currentlyVisitingSignatureId}] does not exist (yet?).");
                return;
            }

            if (bookmark.DistanceTo(Framework.ActiveShip.Entity) < 150_000)
            {
                // Already on grid with the bookmark
                // Update stats
                // Set current one to null
                // Check if abyss trace is still on the grid

                var trace = Framework.Entities.FirstOrDefault(e => e.GroupId == (int)Group.AbyssalTrace && bookmark.DistanceTo(e) < 150_000);

                if (trace == null)
                {
                    Log($"Abyss trace with Id [{_currentlyVisitingSignatureId}] does not exist (anymore?).");
                    _visitedSignatureIds[_currentlyVisitingSignatureId] = true;
                    _currentlyVisitingSignatureId = string.Empty;
                    _probeScanState = ProbeScanState.PickNextSig;
                    return;
                }

                var traceFilamenTypeName = Framework.GetInvType(trace.SlimFilamentTypeId)?.TypeName;
                var isHighTier = traceFilamenTypeName.Contains("Cataclysmic") || traceFilamenTypeName.Contains("Chaotic") || traceFilamenTypeName.Contains("Raging");

                if (DirectEve.Interval(10_000))
                    Log($"Trace with bookmarkId [{bookmark.Title}] HighTier [{isHighTier}] GameModeId [{trace.SlimFilamentGameModeId}]");

                var abyssalRunner = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.IsPlayer && trace.DirectAbsolutePosition.GetDistance(e.DirectEntity.DirectAbsolutePosition) < 150_000);

                if (abyssalRunner == null && isHighTier && trace.SlimFilamentGameModeId == 1)
                {
                    if (RecoverProbes())
                        return;

                    if (DirectEve.Interval(5_000))
                        Log($"Waiting for the AbyssalRunner to appear on grid.");
                    return;
                }

                using var wc = WriteConn.Open();
                wc.DB.Insert<AbyssHunterScans>(new AbyssHunterScans
                {
                    SignatureId = _currentlyVisitingSignatureId,
                    SolarSystemId = Framework.Me.CurrentSolarSystem.Id,
                    SolarSystemName = Framework.Me.CurrentSolarSystem.Name,
                    BookmarkId = bookmark.BookmarkId ?? 0,
                    BookmarkName = bookmark.Title,
                    FilamentTypeId = trace.SlimFilamentTypeId,
                    FilamentTypeName = Framework.GetInvType(trace.SlimFilamentTypeId)?.TypeName ?? "",
                    GameMode = trace.SlimFilamentGameModeId,
                    AbyssalRunnerCharacterName = abyssalRunner == null ? "" : Framework.GetOwner(abyssalRunner.OwnerID)?.Name ?? "",
                    AbyssalRunnerCharacterId = abyssalRunner?.OwnerID ?? 0,
                    AbyssalRunnerShipTypeId = abyssalRunner?.DirectEntity?.TypeId ?? 0,
                    AbyssalRunnerSigRadius = (int)(abyssalRunner?.DirectEntity?.SlimSignatureRadius ?? 0),
                    AbyssalRunnerMass = (int)(abyssalRunner?.DirectEntity?.BallMass ?? 0),
                    AreWeSeen = abyssalRunner != null && !ESCache.Instance.ActiveShip.Entity.IsCloaked,
                    Added = DateTime.UtcNow,
                    X = trace.DirectAbsolutePosition.X,
                    Y = trace.DirectAbsolutePosition.Y,
                    Z = trace.DirectAbsolutePosition.Z,
                });
                _probeScanState = ProbeScanState.PickNextSig;
                _visitedSignatureIds[_currentlyVisitingSignatureId] = true;
                _currentlyVisitingSignatureId = null;
                return;
            }
            else
            {
                // Recover probes before we warp to it
                if (RecoverProbes())
                    return;

                // Warp to it if we can
                if (Framework.Me.CanIWarp())
                {
                    Log($"Warping to bookmark [{_currentlyVisitingSignatureId}].");
                    bookmark.WarpTo(100_000);
                }
            }

        }

        private void TravelToNextSystem()
        {

            if (ESCache.Instance.Stargates.Any(s => s.Distance < 5000) && !Framework.ActiveShip.Entity.IsCloaked)
            {
                if (ReloadCombatProbes())
                    return;
            }

            if (ESCache.Instance.Traveler.Destination == null || ESCache.Instance.Traveler.Destination.SolarSystemId != _nextDirectSolarSystem.Id)
            {
                Log($"Set destination to {_nextDirectSolarSystem.Name} Id {_nextDirectSolarSystem.Id}");
                ESCache.Instance.Traveler.Destination = new SolarSystemDestination(_nextDirectSolarSystem.Id);
            }

            try
            {
                if (ESCache.Instance.State.CurrentTravelerState != TravelerState.AtDestination)
                {
                    ESCache.Instance.Traveler.ProcessState();
                    return;
                }
                else
                {
                    ESCache.Instance.State.CurrentTravelerState = TravelerState.Idle;
                    ESCache.Instance.Traveler.Destination = null;
                    _state = AbyssalHunterState.ProbeSystem;
                    return;
                }
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                _state = AbyssalHunterState.Error;
                return;
            }
        }

        private void GoNextSystem()
        {
            Log($"GoNextSystem");
            ResetState();
            _state = AbyssalHunterState.ChooseNextSystem;
        }

        private bool WarpToStar()
        {
            if (!IsAtStar)
            {
                if (DirectEve.Interval(5000, 10000))
                {
                    Log($"Warping to the star.");
                    ESCache.Instance.Star.WarpToAtRandomRange();
                    return true;
                }
            }
            return false;
        }

        private bool IsAtStar => ESCache.Instance.Star.Distance <= 500000000 && Framework.Me.CanIWarp();

        private bool ReloadCombatProbes()
        {
            var probeLauncher = ESCache.Instance.Modules.FirstOrDefault(m => m.GroupId == 481);
            if (probeLauncher == null)
            {
                Log("No probe launcher found.");
                return false;
            }

            if (probeLauncher.IsInLimboState || probeLauncher.IsActive)
            {
                Log("Probe launcher is active or reloading.");
                return false;
            }

            if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                return false;

            if (ESCache.Instance.CurrentShipsCargo == null)
                return false;

            if (ESCache.Instance.CurrentShipsCargo.CanBeStacked)
            {
                Log("Stacking current ship hangar.");
                ESCache.Instance.CurrentShipsCargo.StackAll();
                LocalPulse = UTCNowAddSeconds(2, 3);
                return true;
            }

            if (probeLauncher.Charge == null || probeLauncher.CurrentCharges < 8)
            {
                var probes = ESCache.Instance.CurrentShipsCargo.Items.Where(i => i.GroupId == 479).OrderBy(i => i.Stacksize);
                //var coreProbes = probes.Where(i => i.TypeName.Contains("Core")).OrderBy(i => i.Stacksize);
                var combatProbes = probes.Where(i => i.TypeName.Contains("Combat")).OrderBy(i => i.Stacksize);
                var charge = combatProbes.FirstOrDefault();
                if (charge == null)
                {
                    Log("No combat probes found in cargohold.");
                    return false;
                }

                if (charge.Stacksize < 8)
                {
                    Log("Probe stacksize was smaller than 8.");
                    return false;
                }

                probeLauncher.ChangeAmmo(charge);
                LocalPulse = UTCNowAddSeconds(4, 5);
                return true;
            }

            return false;
        }

        private void ProbeSystem()
        {
            try
            {
                if (Framework.Me.IsJumpCloakActive)
                {
                    Log("Gate cloak is active, warping to the star.");
                    WarpToStar();
                    return;
                }

                var mapViewWindow = Framework.DirectMapViewWindow;

                if (mapViewWindow == null)
                {
                    Log("Map view window not found.");
                    return;
                }

                if (!mapViewWindow.GetProbes().Any() && _probeScanState != ProbeScanState.InvestigateTrace)
                {
                    var probeLauncher = ESCache.Instance.Modules.FirstOrDefault(m => m.GroupId == 481);
                    if (probeLauncher == null)
                    {
                        Log("No probe launcher found.");
                        return;
                    }

                    if (probeLauncher.IsInLimboState || probeLauncher.IsActive)
                    {
                        Log("Probe launcher is active or reloading.");
                        return;
                    }

                    // TODO: decloak
                    if (_coverOpsClocks != null && _coverOpsClocks.IsActive && _coverOpsClocks.Click() && (IsAtStar || !Framework.Entities.Any(e => e.IsPlayer && e.Distance < 5_000_000)))
                    {
                        Log($"Deactivating cov ops cloak.");
                        LocalPulse = UTCNowAddMilliseconds(1500, 2500);
                        return;
                    }

                    if (_coverOpsClocks != null && _coverOpsClocks.IsActive)
                    {
                        Log($"Cov ops cloak is still active.");
                        return;
                    }

                    if (ReloadCombatProbes())
                        return;

                    if (!IsAtStar && !ESCache.Instance.Stargates.Any(s => s.Distance < 30_000))
                    {
                        Log("Warping to the star before lauching probes");
                        WarpToStar();
                        return;
                    }

                    if (!ESCache.Instance.InWarp)
                    {
                        Log("Launching probes.");
                        probeLauncher.Click();
                        LocalPulse = UTCNowAddSeconds(2, 3);
                    }
                    return;
                }

                if (mapViewWindow.GetProbes().Count != 8 && RecoverProbes())
                {
                    //TODO: check probe range, can't be retrieved if below CONST.MIN_PROBE_RECOVER_DISTANCE
                    Log("Probe amount is != 8, recovering probes.");
                    return;
                }

                // TODO: use cloaks
                if (_coverOpsClocks != null && !_coverOpsClocks.IsActive && _coverOpsClocks.ReactivationDelay <= 0 && _coverOpsClocks.Click(ESCache.Instance.RandomNumber(90, 150), true))
                {
                    Log($"Activating cov ops cloak.");
                    LocalPulse = UTCNowAddMilliseconds(1500, 2500);
                    return;
                }

                if (mapViewWindow.IsProbeScanning())
                {
                    Log("Probe scan active, waiting.");
                    LocalPulse = UTCNowAddSeconds(1, 2);
                    return;
                }

                // Leave state if all previously saved signatures have been checked
                if (_checkedSignatureIds.All(e => e.Value) && _checkedSignatureIds.Count > 0 && _visitedSignatureIds.All(e => e.Value))
                {
                    Log("All signatures have been checked. System scan finished. Moving to next system. Resetting the state.");
                    _state = AbyssalHunterState.ChooseNextSystem;
                    GoNextSystem();
                    LocalPulse = UTCNowAddSeconds(2, 3);
                    return;
                }

                switch (_probeScanState)
                {
                    case ProbeScanState.InvestigateTrace:
                        InvestigateTraces();
                        break;

                    case ProbeScanState.InitialMaxRangeScan:
                        Log("Doing the initial max range scan, probes centered on the star.");
                        mapViewWindow.SetMaxProbeRange();
                        mapViewWindow.MoveProbesTo(ESCache.Instance.Star.DirectEntity.DirectAbsolutePosition.GetVector());
                        mapViewWindow.ProbeScan();
                        LocalPulse = UTCNowAddSeconds(2, 3);
                        _probeScanState = ProbeScanState.InitialSnapShot;
                        break;

                    case ProbeScanState.InitialSnapShot:

                        Log($"Snapshotting signatures from the initial max range scan.");
                        mapViewWindow.SystemScanResults.ForEach(r =>
                        {
                            if (r.ScanGroup == ScanGroup.FilamentTrace)
                            {
                                if (!_checkedSignatureIds.ContainsKey(r.Id))
                                {
                                    _checkedSignatureIds.Add(r.Id, false);
                                    Log($"New signature added: [{r.Id}]");

                                }
                            }
                        });

                        if (_checkedSignatureIds.Count == 0)
                        {
                            Log("No signatures found in the initial scan.");
                            GoNextSystem();
                            return;
                        }

                        _probeScanState = ProbeScanState.PickNextSig;
                        break;

                    case ProbeScanState.PickNextSig:
                        // Pick next sig
                        var nextSig = _checkedSignatureIds.FirstOrDefault(e => !e.Value);
                        _currentSignatureId = nextSig.Key;

                        if (_currentSignatureId == null)
                        {
                            if (DirectEve.Interval(15000))
                                Log("No more signatures to check.");

                            if (_visitedSignatureIds.All(e => e.Value))
                            {
                                GoNextSystem();
                            }
                            return;
                        }

                        Log($"Picking the next signature from the list of unchecked signatures. Chosen signature [{_currentSignatureId}]");
                        _probeScanState = ProbeScanState.NextSigMaxRangeScan;
                        break;


                    case ProbeScanState.NextSigMaxRangeScan:
                        Log($"Initial max range scan for the next signature. [{_currentSignatureId}]");
                        mapViewWindow.SetMaxProbeRange();
                        mapViewWindow.MoveProbesTo(ESCache.Instance.Star.DirectEntity.DirectAbsolutePosition.GetVector());
                        mapViewWindow.ProbeScan();
                        LocalPulse = UTCNowAddSeconds(2, 3);
                        _probeScanState = ProbeScanState.ScanChosenSig;
                        break;

                    case ProbeScanState.ScanChosenSig:

                        if (String.IsNullOrEmpty(_currentSignatureId))
                        {
                            Log("Current signature id is null or empty.");
                            _probeScanState = ProbeScanState.PickNextSig;
                            return;
                        }

                        // If the sig is not part of the scan results anymore, remove it from the list and go back to pick next sig
                        if (mapViewWindow.SystemScanResults.Where(r => r.ScanGroup == ScanGroup.FilamentTrace).All(r => r.Id != _currentSignatureId))
                        {
                            Log($"Signature [{_currentSignatureId}] is not part of the scan results anymore. Removing it from list.");
                            _checkedSignatureIds.TryRemove(_currentSignatureId, out _);
                            _probeScanState = ProbeScanState.PickNextSig;
                            break;
                        }

                        var result = mapViewWindow.SystemScanResults.FirstOrDefault(r => r.ScanGroup == ScanGroup.FilamentTrace && r.Id == _currentSignatureId);

                        if (mapViewWindow.IsAnyProbeAtMinRange)
                        {
                            if (_minRangeProbeScanAttempts < 6 && result.SignalStrength < 1)
                            {
                                _minRangeProbeScanAttempts++;
                                Log($"Probe reached minimum range, but signal strength < 1. Trying again. Attempt [{_minRangeProbeScanAttempts}]");
                                mapViewWindow.RefreshUI();
                                mapViewWindow.MoveProbesTo(result.Pos);
                                mapViewWindow.ProbeScan();
                                LocalPulse = UTCNowAddSeconds(3, 4);
                                return;
                            }
                            else
                            {

                                // Mark as complete
                                _checkedSignatureIds[result.Id] = true;

                                if (result.SignalStrength >= 1.0d)
                                {
                                    Log($"Sig with Id [{result.Id}] was successfully probed.");
                                    // Create a bookmark with name of the sig
                                    if (!result.BookmarkScanResult(result.Id, "EVE"))
                                    {
                                        _state = AbyssalHunterState.Error;
                                        return;
                                    }
                                    else
                                    {
                                        Log($"Created a bookmark with name [{result.Id}]");
                                        _visitedSignatureIds[result.Id] = false;
                                        _probeScanState = ProbeScanState.InvestigateTrace;
                                        return;
                                    }
                                }

                                else
                                {
                                    Log($"Sig with Id [{result.Id}] was unsuccessfully probed. Skipping.");

                                }

                                _checkedSignatureIds[result.Id] = true;

                                _probeScanState = ProbeScanState.PickNextSig;
                                _minRangeProbeScanAttempts = 0;
                                return;
                            }
                        }
                        else
                        {
                            Log("Decreasing probe range and initiating scan again.");
                            mapViewWindow.DecreaseProbeRange();
                            _currentSignatureId = result.Id;
                            mapViewWindow.MoveProbesTo(result.Pos);
                            mapViewWindow.ProbeScan();
                            LocalPulse = UTCNowAddSeconds(2, 3);
                            return;
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
        }

        private void Error()
        {
            if (RecoverProbes())
                return;

            Log($"Error state pulse.");
            _state = AbyssalHunterState.Error;
        }

        private DirectSolarSystem GetClosestNonIslandSolarSystem(HashSet<long> excludedSystems, double minSec = 0.45, double maxSec = 0.55)
        {
            var t = Framework.SolarSystems.Values.Where(e => e.GetSecurity() >= minSec && e.GetSecurity() < maxSec).ToList();
            t = t.Where(s => !excludedSystems.Contains(s.Id)).ToList();
            t = t.Where(e => !e.IsHighsecIsleSystem()).ToList();
            t = t.Where(e => !Framework.GetInsurgencyInfestedSystems().Contains(e)).ToList();
            var closestSystem = t.OrderBy(e => e.CalculatePathTo(Framework.Me.CurrentSolarSystem).Item1.Count)
                .FirstOrDefault();
            return closestSystem;
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }
    }
}
