extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Questor.Core.Lookup;
using EVESharpCore.Controllers.Questor.Core.States;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using Action = EVESharpCore.Controllers.Questor.Core.Actions.Base.Action;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Fields

        private static int _currentAction;
        private static List<Action> _pocketActions;
        private DateTime? _clearPocketTimeout;
        private string _currentMissionInfo;

        private double _lastX;
        private double _lastY;
        private double _lastZ;
        private DateTime _moveToNextPocket = DateTime.UtcNow.AddHours(10);
        private DateTime _nextCombatMissionCtrlAction = DateTime.UtcNow;
        private bool _waiting;
        private DateTime _waitingSince;
        private int AttemptsToActivateGate;
        public static Action CurrentAction = null;

        #endregion Fields

        #region Constructors

        public ActionControl()
        {
            _pocketActions = new List<Action>();
            IgnoreTargets = new HashSet<string>();
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     List of targets to ignore
        /// </summary>
        public static HashSet<string> IgnoreTargets { get; private set; }

        //public string Mission { get; set; }
        public static int PocketNumber { get; set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Loads mission objectives from XML file
        /// </summary>
        public static IEnumerable<Action> LoadMissionActions(long agentId, int pocketId)
        {
            try
            {
                var missiondetails = ESCache.Instance.DirectEve.AgentMissions.Where(m => m.AgentId == agentId).FirstOrDefault();
                if (missiondetails == null)
                    return new Action[0];

                if (missiondetails != null)
                {
                    ESCache.Instance.MissionSettings.SetmissionXmlPath(Log.FilterPath(missiondetails.Name));
                    if (!File.Exists(ESCache.Instance.MissionSettings.MissionXmlPath))
                    {
                        ESCache.Instance.MissionSettings.MissionUseDrones = null;
                        return new Action[0];
                    }
                    try // this loads the settings from each pocket... but NOT any settings global to the mission
                    {
                        var xdoc = XDocument.Load(ESCache.Instance.MissionSettings.MissionXmlPath);
                        if (xdoc.Root != null)
                        {
                            var xElement = xdoc.Root.Element("pockets");
                            if (xElement != null)
                            {
                                var pockets = xElement.Elements("pocket");
                                foreach (var pocket in pockets)
                                {
                                    if ((int)pocket.Attribute("id") != pocketId)
                                        continue;

                                    if (pocket.Element("orbitentitynamed") != null)
                                        ESCache.Instance.OrbitEntityNamed = (string)pocket.Element("orbitentitynamed");


                                    if (pocket.Element("optimalrange") != null) //Load OrbitDistance from mission.xml, if present
                                    {
                                        Log.WriteLine("Using Mission OptimalRange [" + ESCache.Instance.EveAccount.CS.QMS.QS.OptimalRange + "]");
                                    }
                                    else //Otherwise, use value defined in charname.xml file
                                    {
                                        Log.WriteLine("Using Settings OptimalRange [" + ESCache.Instance.EveAccount.CS.QMS.QS.OptimalRange + "]");
                                    }

                                    if (pocket.Element("dronesKillHighValueTargets") != null) //Load afterMissionSalvaging setting from mission.xml, if present
                                        ESCache.Instance.MissionSettings.MissionDronesKillHighValueTargets = (bool)pocket.Element("dronesKillHighValueTargets");
                                    else //Otherwise, use value defined in charname.xml file
                                        ESCache.Instance.MissionSettings.MissionDronesKillHighValueTargets = null;

                                    var actions = new List<Action>();
                                    var elements = pocket.Element("actions");
                                    if (elements != null)
                                        foreach (var element in elements.Elements("action"))
                                        {
                                            var action = new Action
                                            {
                                                State = (ActionState)Enum.Parse(typeof(ActionState), (string)element.Attribute("name"), true)
                                            };
                                            var xAttribute = element.Attribute("name");
                                            if (xAttribute != null && xAttribute.Value == "ClearPocket")
                                                action.AddParameter("", "");
                                            else
                                                foreach (var parameter in element.Elements("parameter"))
                                                    action.AddParameter((string)parameter.Attribute("name"), (string)parameter.Attribute("value"));
                                            actions.Add(action);
                                        }

                                    return actions;
                                }

                                //actions.Add(action);
                            }
                            else
                            {
                                return new Action[0];
                            }
                        }
                        else
                        {
                            {
                                return new Action[0];
                            }
                        }

                        // if we reach this code there is no mission XML file, so we set some things -- Assail
                        return new Action[0];
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine("Error loading mission XML file [" + ex.Message + "]");
                        return new Action[0];
                    }
                }
                return new Action[0];
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return null;
            }
        }

        public static void ReplaceMissionsActions()
        {
            _pocketActions.Clear();
            // Clear the Pocket
            _pocketActions.Add(new Action { State = ActionState.ClearPocket });
            _pocketActions.Add(new Action { State = ActionState.ClearPocket });
            _pocketActions.AddRange(LoadMissionActions(ESCache.Instance.Agent.AgentId, PocketNumber));

            //we manually add 2 ClearPockets above, then we try to load other mission XMLs for this pocket, if we fail Count will be 2 and we know we need to add an activate and/or a done action.
            if (_pocketActions.Count() == 2)
                if (ESCache.Instance.AccelerationGates != null && ESCache.Instance.AccelerationGates.Any())
                {
                    // Activate it (Activate action also moves to the gate)
                    _pocketActions.Add(new Action { State = ActionState.Activate });
                    _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                }
                else // No, were done
                {
                    _pocketActions.Add(new Action { State = ActionState.Done });
                }
        }

        public void ProcessState()
        {
            // There is really no combat in stations (yet)
            if (ESCache.Instance.InDockableLocation)
                return;

            // if we are not in space yet, wait...
            if (!ESCache.Instance.InSpace)
                return;

            // What? No ship entity?
            if (ESCache.Instance.ActiveShip.Entity == null)
                return;

            // There is no combat when warping
            if (ESCache.Instance.InWarp)
                return;

            // There is no combat when cloaked
            if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                return;

            switch (ESCache.Instance.State.CurrentActionControlState)
            {
                case ActionControlState.Idle:
                    break;

                case ActionControlState.Done:
                    ESCache.Instance.Statistics.WritePocketStatistics();

                    if (!ESCache.Instance.NormalApproach)
                        ESCache.Instance.NormalApproach = true;

                    IgnoreTargets.Clear();
                    break;

                case ActionControlState.Error:
                    break;

                case ActionControlState.Start:
                    PocketNumber = 0;

                    // Update statistic values
                    ESCache.Instance.WealthatStartofPocket = ESCache.Instance.DirectEve.Me.Wealth;
                    ESCache.Instance.Statistics.StartedPocket = DateTime.UtcNow;

                    // Update UseDrones from settings (this can be overridden with a mission action named UseDrones)
                    ESCache.Instance.MissionSettings.MissionUseDrones = null;
                    ESCache.Instance.MissionSettings.PocketUseDrones = null;

                    // Reset notNormalNav and onlyKillAggro to false
                    ESCache.Instance.NormalNavigation = true;

                    // Update x/y/z so that NextPocket wont think we are there yet because its checking (very) old x/y/z cords
                    _lastX = ESCache.Instance.ActiveShip.Entity.X;
                    _lastY = ESCache.Instance.ActiveShip.Entity.Y;
                    _lastZ = ESCache.Instance.ActiveShip.Entity.Z;

                    ESCache.Instance.State.CurrentActionControlState = ActionControlState.LoadPocket;
                    break;

                case ActionControlState.LoadPocket:

                    _pocketActions.Clear();
                    _pocketActions.AddRange(LoadMissionActions(ESCache.Instance.Agent.AgentId, PocketNumber));
                    _currentMissionInfo = ESCache.Instance.Agent.GetAgentMissionInfo();
                    ESCache.Instance.Statistics.SaveMissionPocketObjectives(_currentMissionInfo, Log.FilterPath(ESCache.Instance.MissionSettings.Mission.Name), PocketNumber);
                    Log.WriteLine("Objectives for this pocket: [" + _currentMissionInfo + "]");

                    if (_pocketActions.Count == 0)
                    {
                        // No Pocket action, load default actions
                        Log.WriteLine("No mission actions specified, loading default actions");

                        // Wait for 30 seconds to be targeted
                        _pocketActions.Add(new Action { State = ActionState.WaitUntilTargeted });
                        _pocketActions[0].AddParameter("timeout", "15");

                        // Clear the Pocket
                        _pocketActions.Add(new Action { State = ActionState.ClearPocket });

                        _pocketActions.Add(new Action { State = ActionState.Activate });
                        _pocketActions[_pocketActions.Count - 1].AddParameter("target", "Acceleration Gate");
                        _pocketActions[_pocketActions.Count - 1].AddParameter("optional", "true");

                        if (!ESCache.Instance.NavigateOnGrid.SpeedTank)
                        {
                            var backgroundAction = new Action { State = ActionState.MoveToBackground };
                            backgroundAction.AddParameter("target", "Acceleration Gate");
                            backgroundAction.AddParameter("optional", "true");
                            _pocketActions.Insert(0, backgroundAction);
                        }
                    }
                    else
                    {
                        if (!ESCache.Instance.NavigateOnGrid.SpeedTank && !_pocketActions.Any(a => a.State == ActionState.MoveToBackground))
                        {
                            var backgroundAction = new Action { State = ActionState.MoveToBackground };
                            backgroundAction.AddParameter("target", "Acceleration Gate");
                            backgroundAction.AddParameter("optional", "true");
                            _pocketActions.Insert(0, backgroundAction);
                        }
                    }

                    Log.WriteLine("-----------------------------------------------------------------");
                    Log.WriteLine("-----------------------------------------------------------------");
                    Log.WriteLine("Mission Timer Currently At: [" + Math.Round(DateTime.UtcNow.Subtract(ESCache.Instance.Statistics.StartedMission).TotalMinutes, 0) +
                                  "] min");

                    Log.WriteLine("Max Range is currently: " + (ESCache.Instance.Combat.MaxRange / 1000).ToString(CultureInfo.InvariantCulture) + "k");
                    Log.WriteLine("-----------------------------------------------------------------");
                    Log.WriteLine("-----------------------------------------------------------------");
                    Log.WriteLine("Pocket [" + PocketNumber + "] loaded, executing the following actions");
                    var pocketActionCount = 1;
                    foreach (var a in _pocketActions)
                    {
                        Log.WriteLine("Action [ " + pocketActionCount + " ] " + a);
                        pocketActionCount++;
                    }
                    Log.WriteLine("-----------------------------------------------------------------");
                    Log.WriteLine("-----------------------------------------------------------------");

                    // Reset pocket information
                    _currentAction = 0;
                    ESCache.Instance.Drones.IsMissionPocketDone = false;

                    if (ESCache.Instance.NavigateOnGrid.SpeedTank && !ESCache.Instance.EveAccount.CS.QMS.QS.LootWhileSpeedTanking)
                    {
                        if (DebugConfig.DebugTargetWrecks)
                            Log.WriteLine("ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;");

                        ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;
                    }
                    else
                    {
                        ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = true;
                    }

                    IgnoreTargets.Clear();
                    ESCache.Instance.Statistics.PocketObjectStatistics(ESCache.Instance.Objects.ToList());
                    ESCache.Instance.State.CurrentActionControlState = ActionControlState.ExecutePocketActions;
                    break;

                case ActionControlState.ExecutePocketActions:
                    if (_currentAction >= _pocketActions.Count)
                    {
                        Log.WriteLine("We're out of actions but did not process a 'Done' or 'Activate' action"); // No more actions, but we're not done?!?!?!
                        ESCache.Instance.State.CurrentActionControlState = ActionControlState.Error;
                        break;
                    }

                    var action = _pocketActions[_currentAction];
                    if (action.ToString() != ESCache.Instance.CurrentPocketAction)
                        ESCache.Instance.CurrentPocketAction = action.ToString();
                    var currentAction = _currentAction;


                    CurrentAction = action;
                    PerformAction(action);

                    if (currentAction != _currentAction)
                    {
                        Log.WriteLine("Finished Action." + action);
                        if (_currentAction < _pocketActions.Count)
                        {
                            action = _pocketActions[_currentAction];
                            Log.WriteLine("Starting Action." + action);
                        }
                    }

                    break;

                case ActionControlState.NextPocket:
                    var distance = ESCache.Instance.DirectEve.Me.DistanceFromMe(_lastX, _lastY, _lastZ);
                    if (distance > (int)Distances.NextPocketDistance)
                    {
                        Log.WriteLine("We have moved to the next Pocket [" + Math.Round(distance / 1000, 0) + "k away]");
                        if (ControllerManager.Instance.TryGetController<QuestorController>(out var qc))
                        {
                            qc.CombatMissionsBehaviorInstance.MovedToNextPocket = true;
                        }
                        ESCache.Instance.MissionSettings.PocketUseDrones = null;

                        // If we moved more then 100km, assume next Pocket
                        PocketNumber++;
                        ESCache.Instance.State.CurrentActionControlState = ActionControlState.LoadPocket;
                        ESCache.Instance.Statistics.WritePocketStatistics();
                    }
                    else if (DateTime.UtcNow.Subtract(_moveToNextPocket).TotalMinutes > 2)
                    {
                        Log.WriteLine("We have timed out, retry last action");

                        // We have reached a timeout, revert to ExecutePocketActions (e.g. most likely Activate)
                        ESCache.Instance.State.CurrentActionControlState = ActionControlState.ExecutePocketActions;
                    }
                    break;
            }

            var newX = ESCache.Instance.ActiveShip.Entity.X;
            var newY = ESCache.Instance.ActiveShip.Entity.Y;
            var newZ = ESCache.Instance.ActiveShip.Entity.Z;

            if (newX != 0 && newY != 0 && newZ != 0) // For some reason x/y/z returned 0 sometimes
            {
                _lastX = newX; // Save X/Y/Z so that NextPocket can check if we actually went to the next Pocket :)
                _lastY = newY;
                _lastZ = newZ;
            }
        }

        private void Nextaction()
        {
            // make sure all approach / orbit / align timers are reset (why cant we wait them out in the next action!?)
            ESCache.Instance.Time.NextApproachAction = DateTime.UtcNow;

            // now that we have completed this action revert OpenWrecks to false

            if (ESCache.Instance.NavigateOnGrid.SpeedTank && !ESCache.Instance.EveAccount.CS.QMS.QS.LootWhileSpeedTanking)
            {
                if (DebugConfig.DebugTargetWrecks) Log.WriteLine("ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;");
                ControllerManager.Instance.GetController<SalvageController>().OpenWrecks = false;
            }

            ControllerManager.Instance.GetController<SalvageController>().MissionLoot = false;
            ESCache.Instance.NormalNavigation = true;
            ESCache.Instance.MissionSettings.MissionActivateRepairModulesAtThisPerc = null;
            //ESCache.Instance.MissionSettings.PocketUseDrones = null;
            _currentAction++;
            _waiting = false;
            return;
        }

        private void PerformAction(Action action)
        {

            switch (action.State)
            {
                case ActionState.Activate:
                    ActivateAction(action);
                    break;

                case ActionState.ClearPocket:
                    ClearPocketAction(action);
                    break;

                case ActionState.Done:
                    DoneAction();
                    break;

                case ActionState.Kill:
                    KillAction(action);
                    break;

                case ActionState.UseDrones:
                    UseDronesAction(action);
                    break;

                case ActionState.AggroOnly:
                    AggroOnlyAction(action);
                    break;

                case ActionState.MoveTo:
                    MoveToAction(action);
                    break;

                case ActionState.MoveToBackground:
                    MoveToBackgroundAction(action);
                    break;

                case ActionState.ClearWithinWeaponsRangeOnly:
                    ClearWithinWeaponsRangeOnlyAction(action);
                    break;

                case ActionState.Loot:
                    LootAction(action);
                    break;

                case ActionState.LootItem:
                    LootItemAction(action);
                    break;

                case ActionState.Ignore:
                    IgnoreAction(action);
                    break;

                case ActionState.WaitUntilTargeted:
                    WaitUntilTargetedAction(action);
                    break;

                case ActionState.WaitUntilAggressed:
                    WaitUntilAggressedAction(action);
                    break;
            }
        }

        #endregion Methods
    }
}