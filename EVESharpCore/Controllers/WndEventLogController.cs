/*
 * User: duketwo
 * Date: 09.07.2019
 * Time: 01:46
 *
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    extern alias SC;

    public class WndEventLogController : BaseController
    {
        private SC::SharedComponents.Utility.AsyncLogQueue.AsyncLogQueue _asyncLogQueue;
        private int _prevTotalEventCount;
        private WndEventLogControllerForm _wndEventLogControllerForm;
        private HashSet<String> _loggedEventsHashSet;

        public WndEventLogController() : base()
        {
            IgnorePause = true;
            IgnoreModal = true;
            _asyncLogQueue = new SC::SharedComponents.Utility.AsyncLogQueue.AsyncLogQueue();
            _asyncLogQueue.File = EVESharpCore.Logging.Log.WindowEventLogFile;
            Form = new WndEventLogControllerForm(this);
            _loggedEventsHashSet = new HashSet<string>();
            _wndEventLogControllerForm = (WndEventLogControllerForm)Form;
            _asyncLogQueue.OnMessage += _wndEventLogControllerForm.AddLogInvoker;
            _asyncLogQueue.StartWorker();
        }

        public override void DoWork()
        {

            using (var pySharp = ESCache.Instance.DirectEve.PySharp)
            {

                var services = pySharp.Import("__builtin__")["sm"]["services"];
                if (services.IsValid)
                {
                    var serviceDict = ((SC::SharedComponents.Py.PyObject)services).ToDictionary<string>();
                    if (serviceDict.ContainsKey("infoGatheringSvc") && serviceDict["infoGatheringSvc"].Attribute("state").ToInt() == 4)
                    {
                        var loggedEvents = serviceDict["infoGatheringSvc"].Attribute("loggedEvents");
                        if (loggedEvents.IsValid)
                        {
                            var loggedEventsDict = loggedEvents.ToDictionary<int>();
                            if (loggedEventsDict.Count > 0)
                            {
                                var totalEventCount = 0;
                                foreach (var kv in loggedEventsDict)
                                {
                                    totalEventCount += kv.Value.Size();
                                }

                                if (_prevTotalEventCount > 0 && _prevTotalEventCount > totalEventCount)
                                {
                                    LocalLog("Events have been transmitted to the server.");
                                    _loggedEventsHashSet = new HashSet<string>();
                                }
                                _prevTotalEventCount = totalEventCount;

                                if (loggedEventsDict.ContainsKey((int)InfoManagerConst.infoEventWndOpenedCounters))
                                {
                                    var openedWndList = loggedEventsDict[(int)InfoManagerConst.infoEventWndOpenedCounters].ToList<SC::SharedComponents.Py.PyObject>();
                                    foreach (var i in openedWndList)
                                    {
                                        var currItem = i.ToList<SC::SharedComponents.Py.PyObject>();
                                        var item = new InfoEventWndOpenedCounters();
                                        item.Date = currItem[0].ToDateTimeExact();
                                        item.Title = currItem[6].ToUnicodeString();
                                        item.Count = currItem[3].ToInt();
                                        if (!_loggedEventsHashSet.Contains(item.Hash))
                                        {
                                            LocalLog($"Window [{item.Title}] has been OPENED for [{item.Count}] times. Event created on [{item.Date.ToString("s")}]");
                                            _loggedEventsHashSet.Add(item.Hash);
                                        }
                                    }
                                }

                                if (loggedEventsDict.ContainsKey((int)InfoManagerConst.infoEventWndSecondsInFocus))
                                {
                                    var wndFocusList = loggedEventsDict[(int)InfoManagerConst.infoEventWndSecondsInFocus].ToList<SC::SharedComponents.Py.PyObject>();
                                    foreach (var i in wndFocusList)
                                    {
                                        var currItem = i.ToList<SC::SharedComponents.Py.PyObject>();
                                        var item = new InfoEventWndSecondsInFocus();
                                        item.Date = currItem[0].ToDateTimeExact();
                                        item.Title = currItem[6].ToUnicodeString();
                                        item.FocusDuration = currItem[9].ToFloat();
                                        if (!_loggedEventsHashSet.Contains(item.Hash))
                                        {
                                            LocalLog($"Window [{item.Title}] has been ACTIVE for [{item.FocusDuration}] seconds. Event created on [{item.Date.ToString("s")}]");
                                            _loggedEventsHashSet.Add(item.Hash);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }


        public void LocalLog(string text, Color? col = null, [CallerMemberName] string memberName = "")
        {
            try
            {
                _asyncLogQueue.Enqueue(new SC::SharedComponents.Utility.AsyncLogQueue.LogEntry(text, memberName, col));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

    }

    public class InfoEventWndSecondsInFocus
    {
        public DateTime Date;
        public string Title;
        public float FocusDuration;
        private string _hash;
        public string Hash
        {
            get
            {
                if (String.IsNullOrEmpty(_hash))
                {
                    _hash = $"{Date.ToBinary()} {Title} {FocusDuration}";
                }
                return _hash;
            }
        }
    }

    public class InfoEventWndOpenedCounters
    {
        public DateTime Date;
        public string Title;
        public int Count;
        private string _hash;
        public string Hash
        {
            get
            {
                if (String.IsNullOrEmpty(_hash))
                {
                    _hash = $"{Date.ToBinary()} {Title} {Count}";
                }
                return _hash;
            }
        }

    }



    enum InfoManagerConst // #Embedded file name: eve\release\TRANQUILITY\eve\common\lib\infoEventConst.py
    {
        infoEventOreMined = 2,
        infoEventSalvagingAttempts = 3,
        infoEventHackingAttempts = 4,
        infoEventArcheologyAttempts = 5,
        infoEventScanningAttempts = 6,
        infoEventFleet = 7,
        infoEventFleetCreated = 8,
        infoEventFleetBroadcast = 9,
        infoEventNPCKilled = 12,
        infoEventRefinedTypesAmount = 13,
        infoEventRefiningYieldTypesAmount = 14,
        infoEventPlanetResourceDepletion = 15,
        infoEventPlanetResourceScanning = 16,
        infoEventPlanetUserAccess = 17,
        infoEventPlanetInstallProgramQuery = 18,
        infoEventPlanetUpdateNetwork = 19,
        infoEventPlanetAbandonPlanet = 20,
        infoEventPlanetEstablishColony = 21,
        infoEventEntityKillWithoutBounty = 22,
        infoEventRecruitmentAdSearch = 23,
        infoEventNexCloseNex = 24,
        infoEventViewStateUsage = 25,
        infoEventDoubleclickToMove = 26,
        infoEventCCDuration = 27,
        infoEvenTrackingCameraEnabled = 28,
        infoEventRadialMenuAction = 29,
        infoEventISISCounters = 30,
        infoEventInfoWindowTabs = 31,
        infoEventCareerFunnel = 32,
        infoEventWndOpenedFirstTime = 33,
        infoEventWndOpenedCounters = 34,
        infoEventTaskCompleted = 35,
        infoEventCharacterCreationStep = 36,
        infoEventCharacterFinalStep = 37,
        infoEventVideoPlayed = 38,
        infoEventOperationsResetCheckpoint = 39,
        infoEventAgencyCardClicked = 40,
        infoEventAgencyIconClicked = 41,
        infoEventAgencyPrimaryButtonClick = 42,
        infoEventAgencyDistanceFilter = 43,
        infoEventAgencyContentTypeFilter = 44,
        infoEventActivityTrackerNodeClick = 45,
        infoEventNewFeatureSeen = 46,
        infoEventNewFeatureCallToAction = 47,
        infoEventHelperPointerLinkClicked = 48,
        infoEventHelperPointerLinkCreated = 49,
        infoEventAgencyGroupCardClicked = 50,
        infoEventAgencyFilterChanged = 51,
        infoEventAgencyBookmarkAdded = 52,
        infoEventWndSecondsInFocus = 53
    }
}