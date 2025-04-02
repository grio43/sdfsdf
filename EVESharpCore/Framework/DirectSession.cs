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

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace EVESharpCore.Framework
{

    extern alias SC;


    public class DirectSession : DirectObject
    {
        #region Fields

        private static DateTime _nextSession;
        private static Random _rnd = new Random();

        public static event EventHandler<EventArgs> OnSessionReadyEvent = delegate { };

        //def InSpace():
        //    return bool(session.solarsystemid) and bool(session.shipid) and session.structureid in (session.shipid, None)
        //def InShip():
        //    return bool(session.shipid) and bool(session.shipid != session.structureid)
        //def InShipInSpace():
        //    return bool(session.solarsystemid) and bool(session.shipid) and not bool(session.structureid)
        //def IsDocked():
        //    return bool(session.stationid2) or IsDockedInStructure()
        //def InStructure():
        //    return bool(session.structureid)
        //def IsDockedInStructure():
        //    return bool(session.structureid) and bool(session.structureid != session.shipid)

        private bool? _inDockableLocation;
        private bool? _inSpace;
        private static bool _IsReady;

        #endregion Fields

        #region Constructors

        internal DirectSession(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long? AllianceId => (long?)Session.Attribute("allianceid");
        public DirectOwner Character => DirectEve.GetOwner(CharacterId ?? -1);
        public long? CharacterId => (long?)Session.Attribute("charid");
        public long? ConstellationId => (long?)Session.Attribute("constellationid");
        public long? CorporationId => (long?)Session.Attribute("corpid");
        public long? FleetId => (long?)Session.Attribute("fleetid");
        public bool IsInDockableLocation => IsReady && (_inDockableLocation ?? (_inDockableLocation = InDockableLocation).Value);
        public bool IsInSpace => IsReady && (_inSpace ?? (_inSpace = InSpace).Value);

        // holds the value of session ready, will be updated once a frame
        public bool IsReady
        {
            get
            {
                if (_nextSession > DateTime.UtcNow)
                    return false;

                return _IsReady;
            }

            // will be set once a frame
            private set => _IsReady = value;
        }

        public int? LocationId => (int?)Session.Attribute("locationid");

        public long? RegionId => (long?)Session.Attribute("regionid");
        public long? ShipId => (long?)Session.Attribute("shipid");
        public int? SolarSystemId => (int?)Session.Attribute("solarsystemid2");
        public int? StationId => (int?)Session.Attribute("stationid");

        public long? Structureid => (long?)Session.Attribute("structureid");

        public bool HasStructureId => Session.Attribute("structureid").IsValid;

        public bool HasStationId => Session.Attribute("stationid").IsValid;

        public int UserType => (int)Session.Attribute("userType");

        private bool __inDockableLocation => (LocationId.HasValue && LocationId == StationId) || Structureid.HasValue;
        private bool __inSpace => LocationId.HasValue && LocationId == SolarSystemId && !Structureid.HasValue;

        private bool InDockableLocation
        {
            get
            {
                try
                {
                    if (!IsReady)
                        return false;

                    if (__inSpace)
                        return false;

                    if (!__inDockableLocation)
                        return false;

                    if (DirectEve.AnyEntities())
                        return false;

                    if (DirectEve.ActiveShip.Entity != null)
                        return false;

                    if (DirectEve.Interval(2000))
                        Task.Run(() =>
                        {
                            try
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.IsDocked), true);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }

                        });

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        private bool InSpace
        {
            get
            {
                try
                {
                    if (!IsReady)
                        return false;

                    if (!__inSpace)
                        return false;

                    if (__inDockableLocation)
                        return false;

                    if (!DirectEve.AnyEntities())
                        return false;

                    if (DirectEve.ActiveShip.Entity == null)
                        return false;

                    if (DirectEve.Interval(2000))
                        Task.Run(() =>
                        {
                            try
                            {
                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName, nameof(EveAccount.IsDocked), false);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                            }
                        });

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                    return false;
                }
            }
        }

        private PyObject Session => PySharp.Import("__builtin__").Attribute("eve").Attribute("session");

        #endregion Properties

        #region Methods

        // makes the session invalid until GetNextSessionTimer value. make sure to call this on for example (undock / dock / jump / switch ship)
        // and ensure the execution flow returns ( stop the current frame execution ) at this point.
        // else the client might hang if there are any methods called between the session change
        public static void SetSessionNextSessionReady(int min = 10000, int max = 10100)
        {
            _nextSession = DateTime.UtcNow.AddMilliseconds(_rnd.Next(min, max));
        }

        public static DateTime LastSessionChange = DateTime.UtcNow;

        // being called once a frame by the controller manager, should not be called elsewhere
        public void SetSessionReady()
        {
            var prevValue = _IsReady;
            _IsReady = CheckSessionReady();

            if (prevValue != _IsReady)
            {
                LastSessionChange = DateTime.UtcNow;
                DirectEve.Log($"Session value changed. Previous session value [{prevValue}]. Current value [{_IsReady}].");

                if (_IsReady)
                {
                    // TODO: fire session change event here  [done]
                    OnSessionReadyEvent(this, EventArgs.Empty);
                    DirectEntity.OnSessionChange();
                    DirectUIModule.OnSessionChange();
                    DirectWorldPosition.OnSessionChange();

                    // Reduce working memory size if the option was set
                    if (ESCache.Instance.EveAccount.ClientSetting.GlobalMainSetting.ClearMemoryDuringSessionChange)
                    {
                        DirectEve.Log("Optimized memory.");
                        SC::SharedComponents.Utility.MemoryOptimizer.OptimizeMemory();
                    }
                }
            }
        }

        // being called once a frame by the controller manager, should not be called elsewhere
        private bool CheckSessionReady()
        {
            if (!DirectEve.HasFrameChanged())
                return _IsReady;

            var debug = false;

            var inSpace = __inSpace;
            var inDockableLocation = __inDockableLocation;
            var michelle = DirectEve.GetLocalSvc("michelle", false, false);
            var undockingSvc = DirectEve.GetLocalSvc("undocking", false, false);
            var godma = DirectEve.GetLocalSvc("godma");
            var dockingHeroNotification = DirectEve.GetLocalSvc("dockingHeroNotification");


            if (!michelle.IsValid)
                return false;


            if (debug)
            {
                //Console.WriteLine($"InSpace [{inSpace}] InDockableLocation [{inDockableLocation}]");
            }




            if (inSpace)
            {
                var ballparkReady = michelle["bpReady"].ToBool();

                if (!ballparkReady)
                {
                    if (debug)
                    {
                        Console.WriteLine("Ballpark is invalid.");
                    }
                    return false;
                }

                var michelleBallpark  = michelle.Attribute("_Michelle__bp");

                if (!michelleBallpark.IsValid)
                {
                    if (debug)
                    {
                        Console.WriteLine("_Michelle__bpis invalid.");
                    }

                    return false;
                }

                var remoteBallpark = michelleBallpark["remoteBallpark"];

                if (!remoteBallpark.IsValid)
                {
                    if (debug)
                    {
                        Console.WriteLine("remoteBallpark invalid.");
                    }

                    return false;
                }

                var bindParams = remoteBallpark.Attribute("_Moniker__bindParams");
                if (!bindParams.IsValid)
                {
                    if (debug)
                    {
                        Console.WriteLine("_Moniker__bindParams is invalid.");
                    }

                    return false;
                }

                if (bindParams.ToInt() != SolarSystemId)
                {
                    if (debug)
                    {
                        Console.WriteLine("bindParams.ToInt() != SolarSystemId");
                    }

                    return false;
                }

                var bpSolarSystemId = michelleBallpark.Attribute("solarsystemID");
                if (bpSolarSystemId.IsValid && bpSolarSystemId.ToInt() != SolarSystemId)
                {
                    if (debug)
                    {
                        Console.WriteLine("bpSolarSystemId.IsValid && bpSolarSystemId.ToInt() != SolarSystemId");
                    }
                    return false;
                }
            }

            if (inDockableLocation)
                if (undockingSvc.Attribute("exitingDockableLocation").ToBool())
                {
                    if (debug)
                    {
                        Console.WriteLine("undockingSvc.Attribute('exitingDockableLocation').ToBool()");
                    }
                    return false;
                }


            if (godma.IsValid)
            {
                var priming = godma["stateManager"]["priming"];
                if (priming.IsValid && priming.ToBool())
                {
                    if (debug)
                    {
                        Console.WriteLine("godma['stateManager']['priming'].ToBool()");
                    }
                    return false;
                }
            }

            if (dockingHeroNotification.IsValid)
            {
                var data = dockingHeroNotification["_active_notification_cancellation_tokens"]["data"];
                if (data.IsValid && data.ToList().Count > 0)
                {
                    if (debug)
                    {
                        Console.WriteLine("dockingHeroNotification[\"_active_notification_cancellation_tokens\"][\"data\"] is valid and has more than 0 items.");
                    }
                    return false;
                }
            }

            if (ShipId == null)
            {
                if (debug)
                {
                    Console.WriteLine("ShipId == null");
                }
                return false;
            }

            if (!Session.IsValid)
            {
                if (debug)
                {
                    Console.WriteLine("!Session.IsValid");
                }
                return false;
            }

            if (!Session.Attribute("locationid").IsValid)
            {
                if (debug)
                {
                    Console.WriteLine("Session.Attribute('locationid').IsValid");
                }
                return false;
            }

            if (!Session.Attribute("solarsystemid2").IsValid)
            {
                if (debug)
                {
                    Console.WriteLine("!Session.Attribute('solarsystemid2').IsValid");
                }
                return false;
            }

            if (Session.Attribute("changing").IsValid)
            {
                if (debug)
                {
                    Console.WriteLine("Session.Attribute('changing').IsValid");
                }
                return false;
            }

            if ((bool)Session.Attribute("mutating"))
            {
                if (debug)
                {
                    Console.WriteLine("Session mutating.");
                }
                return false;
            }

            if (!(bool)Session.Attribute("rwlock").Call("IsCool"))
            {
                if (debug)
                {
                    Console.WriteLine("Session IsCool == false.");
                }
                return false;
            }

            if (DirectEve.GetLocalSvc("jumpQueue", false, false).Attribute("jumpQueue").IsValid)
            {
                if (debug)
                {
                    Console.WriteLine("Svc jumpQueu.jumpQueue is valid.");
                }
                return false;
            }

            if (Session.Attribute("nextSessionChange").IsValid) // next session change is always +10 sec after a session change
            {
                var nextSessionChange = Session.Attribute("nextSessionChange").ToDateTime();
                nextSessionChange = nextSessionChange.AddSeconds(-5);
                if (nextSessionChange >= DateTime.UtcNow)
                {
                    if (debug)
                    {
                        Console.WriteLine("nextSessionChange >= DateTime.UtcNow");
                    }
                    return false;
                }
            }

            var station = DirectEve.GetLocalSvc("station", false, false);
            if (station.IsValid)
            {
                if ((bool)station.Attribute("activatingShip"))
                {
                    if (debug)
                    {
                        Console.WriteLine("station.activatingShip");
                    }
                    return false;
                }

                if ((bool)station.Attribute("loading"))
                {
                    if (debug)
                    {
                        Console.WriteLine("station.loading");
                    }
                    return false;
                }

                if ((bool)station.Attribute("leavingShip"))
                {
                    if (debug)
                    {
                        Console.WriteLine("station.leavingShip");
                    }
                    return false;
                }
            }

            var loading = (bool)DirectEve.PySharp.Import("carbonui")
                .Attribute("uicore")
                .Attribute("uicore")
                .Attribute("layer")
                .Attribute("loading")
                .Attribute("display");
            if (loading)
            {
                if (debug)
                {
                    Console.WriteLine("carbonui.loading.display");
                }
                return false;
            }

            var anyEnt = DirectEve.AnyEntities();

            if (inDockableLocation && anyEnt)
            {
                if (debug)
                {
                    Console.WriteLine("inDockableLocation && anyEnt");
                }
                return false;
            }

            if (inSpace)
                if (!anyEnt)
                {
                    if (debug)
                    {
                        Console.WriteLine("inSpace && no entities");
                    }
                    return false;
                }

            if (DirectEve.Me.IsJumpCloakActive && DirectEve.Me.JumpCloakRemainingSeconds >= 57)
            {
                if (debug)
                {
                    Console.WriteLine("DirectEve.Me.IsJumpCloakActive && DirectEve.Me.JumpCloakRemainingSeconds >= XX");
                }
                return false;
            }

            if (DirectEve.Me.IsInvuln && DirectEve.Me.InvulnRemainingSeconds() >= 27)
            {
                if (debug)
                {
                    Console.WriteLine(
                        "DirectEve.Me.IsInvulnUndock && DirectEve.Me.IsInvulnUndockRemainingSeconds >= XX)");
                }
                return false;
            }

            if (!DirectEve.Windows.Any())
            {
                if (debug)
                {
                    Console.WriteLine("!DirectEve.Windows.Any()");
                }
                return false;
            }

            if (DirectEve.GetLocalSvc("sceneManager")["primaryJob"]["scene"]["objects"].ToList().Count < 3)
            {
                if (debug)
                {
                    Console.WriteLine("sceneManager.primaryJob.scene.object amount is below 3.");
                }
                return false;
            }

            var wnd = DirectEve.Windows.FirstOrDefault(w => w.Guid == "form.LobbyWnd" || w.WindowId == "overview"); // both can't be active at a time

            if (wnd == null)
            {
                if (debug)
                {
                    Console.WriteLine("wnd == null");
                }
                return false;
            }

            var display = wnd.PyWindow.Attribute("_display").ToBool();
            //var alignmentDirty = wnd.PyWindow.Attribute("_alignmentDirty").ToBool();
            //var childAlignmentDirty = wnd.PyWindow.Attribute("_childrenAlignmentDirty").ToBool();
            //var displayDirty = wnd.PyWindow.Attribute("_displayDirty").ToBool();

            if (!DirectEve.NewEdenStore.IsStoreOpen)
            {
                //if (alignmentDirty || childAlignmentDirty || !display)
                //{
                //    if (debug)
                //    {
                //        Console.WriteLine($"alignmentDirty {alignmentDirty} || !display {!display} childAlignmentDirty {childAlignmentDirty}");
                //    }
                //    return false;
                //}

                if (!display)
                {
                    if (debug)
                    {
                        Console.WriteLine($"display {display}");
                    }
                    return false;
                }
            }

            return true;
        }

        #endregion Methods
    }
}