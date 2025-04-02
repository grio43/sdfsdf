/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.05.2016
 * Time: 17:38
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using EVESharpCore.Controllers.Questor;

namespace EVESharpCore
{
    /// <summary>
    ///     Description of ControllerManager.
    /// </summary>
    public sealed partial class ControllerManager : IDisposable
    {
        #region Fields

        public static readonly List<Type> DEFAULT_CONTROLLERS =
            new List<Type>() // controllers which are enabled by default
            {
                typeof(LoginController),
                typeof(CleanupController),
                typeof(ActionQueueController),
                typeof(BackgroundWorkerController),
                typeof(DebugController),
                typeof(UITravellerController),
                //typeof(WndEventLogController)
            };

        public static readonly List<Type> HIDDEN_CONTROLLERS = new List<Type>()
        {
            typeof(BuyPlexController),
            typeof(BuyAmmoController),
            //typeof(DefenseController),
            typeof(SalvageController),
            typeof(PanicController),
            typeof(AbyssalBaseController),
        };

        private static readonly ControllerManager _instance = new ControllerManager();

        #endregion Fields

        #region Constructors

        private ControllerManager(int pulseDelayMilliseconds = 350)
        {
            PulseDelayMilliseconds = pulseDelayMilliseconds;
            Pulse = DateTime.MinValue;
            Rnd = new Random();
            ControllerList = new ConcurrentBindingList<BaseController>();
            ControllerDict = new ConcurrentDictionary<Type, IController>();
        }

        #endregion Constructors

        #region Properties

        public static ControllerManager Instance => _instance;
        public ConcurrentDictionary<Type, IController> ControllerDict { get; private set; }
        public ConcurrentBindingList<BaseController> ControllerList { get; private set; }
        public BaseController CurrentController => Enumerator != null ? Enumerator.Current : null;

        private IEnumerator<BaseController> Enumerator { get; set; }

        private DateTime Pulse { get; set; }

        private int PulseDelayMilliseconds { get; set; }

        private Random Rnd { get; set; }

        #endregion Properties

        #region Methods

        private static Stopwatch _getNextControllerStopwatch = new Stopwatch();

        public void AddController(IController controller) // main add method
        {
            try
            {
                if (controller == null)
                    return;

                if (ControllerList.All(c => c.GetType() != controller.GetType()))
                {
                    Log.WriteLine("AddController [" + controller.GetType() + "]");
                    ControllerList.Add(controller);
                    ControllerDict.AddOrUpdate(controller.GetType(), controller, (key, oldValue) => controller);
                }

                foreach (var depControllerType in controller.DependsOn) // add dependant controllers
                {
                    if (ControllerList.All(c => c.GetType() != depControllerType))
                    {
                        AddController(depControllerType);
                    }
                }

                Program.EveSharpCoreFormInstance.AddControllerTab(controller);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        private Queue<Action> _controllerAddQueue = new Queue<Action>();

        private static HashSet<Type> _controllerTypes = typeof(CleanupController).Assembly.GetTypes()
            .Where(e => !e.IsAbstract)
            .Where(e => typeof(IController).IsAssignableFrom(e)).ToHashSet();

        private Dictionary<string, Type> _controllerTypeByString = null;

        public IController GetController(string type)
        {
            if (_controllerTypeByString == null)
            {
                _controllerTypeByString= new Dictionary<string, Type>();
                foreach (var controllerType in _controllerTypes)
                {
                    _controllerTypeByString[controllerType.Name] = controllerType;
                }
            }

            if (_controllerTypeByString.TryGetValue(type, out var r))
                if (ControllerDict.TryGetValue(r, out var contr))
                    return contr;

            return null;
        }

        public void AddController(Type t)
        {
            var action = new Action(() =>
            {
                var inst = Activator.CreateInstance(t);
                AddController((IController)inst);
            });
            _controllerAddQueue.Enqueue(action);
        }

        public void AddController(string n)
        {
            var action = new Action(() =>
            {
                try
                {
                    if (n.Equals("None"))
                        return;
                    var t = _controllerTypes.FirstOrDefault(t => t.Name.Equals(n));
                    var inst = Activator.CreateInstance(t);
                    AddController((IController)inst);
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });
            _controllerAddQueue.Enqueue(action);
        }

        public T GetController<T>()
        {
            if (ControllerDict.TryGetValue(typeof(T), out var c))
            {
                return (T)c;
            }

            return default(T);
        }

        public void Initialize()
        {
            try
            {
                // add global default controllers
                foreach (var c in DEFAULT_CONTROLLERS)
                    AddController(c);

                // add selected controller from EVESharpLauncher
                var selectedController = ESCache.Instance.EveAccount.SelectedController;
                Log.WriteLine($"Adding [{selectedController}]");
                Instance.AddController(selectedController);

                // set callback
                WCFClient.Instance.SetCallBackService(new LauncherCallback(), ESCache.Instance.CharName);
                if (!ESCache.LoadDirectEVEInstance()) return;

                ESCache.Instance.DirectEve.OnFrame += EVEOnFrame;
            }
            catch (Exception e)
            {
                Log.WriteLine(e.ToString());
            }
        }

        public void RemoveAllControllers()
        {
            try
            {
                ControllerList.Clear();
                ControllerDict.Clear();
                Log.WriteLine("Removed all controllers.");
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void RemoveController(Type t)
        {
            try
            {
                if (ControllerList.Any(c => c.GetType().Equals(t)))
                    RemoveController(ControllerList.FirstOrDefault(c => c.GetType().Equals(t)));
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void RemoveController(IController controller) // main remove method
        {
            try
            {
                if (controller == null)
                    return;

                if (DEFAULT_CONTROLLERS.Any(t => t == controller.GetType()))
                {
                    Log.WriteLine("Default type controllers can't be removed.");
                    return;
                }

                if (ControllerList.Any(c => c.GetType().Equals(controller.GetType())))
                {
                    // remove dependant controllers
                    var controllerToBeRemoved =
                        ControllerList.FirstOrDefault(c => c.GetType().Equals(controller.GetType()));
                    foreach (var depControllerType in controllerToBeRemoved.DependsOn)
                    {
                        // check if any other controller depends on that we are trying to remove before removing it completely
                        var allDepsExceptSelf = ControllerList.ToList().Where(c => c != controllerToBeRemoved)
                            .SelectMany(k => k.DependsOn);
                        if (!allDepsExceptSelf.Contains(depControllerType))
                            RemoveController(depControllerType);
                    }

                    controller.Dispose();
                    ControllerList.Remove(controller);
                    ControllerDict.TryRemove(controller.GetType(), out _);
                    Log.WriteLine("RemoveController [" + controller.GetType() + "]");
                }

                Program.EveSharpCoreFormInstance.RemoveControllerTab(controller);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        public void SetPause(bool val = true)
        {
            foreach (var controller in ControllerList.ToList())
            {
                if (controller.IgnorePause)
                    continue;

                controller.IsPaused = val;
            }
        }

        public bool TryGetController<T>(out T controller)
        {
            controller = default(T);
            if (ControllerDict.TryGetValue(typeof(T), out var c))
            {
                controller = (T)c;
                return true;
            }

            return false;
        }

        private void EVEOnFrame(object sender, DirectEveEventArgs e)
        {
            // pop actions from the add queue and execute within eve context
            try
            {
                if (_controllerAddQueue.Count > 0)
                {
                    var action = _controllerAddQueue.Dequeue();
                    action();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("EVEOnFrame Exception: " + ex);
            }

            try
            {
                TryGetController<LoginController>(out var loginController);
                if (TryGetController<ActionQueueController>(out var aQC))
                {
                    if (loginController != null && loginController.IsWorkDone && !aQC.IsPaused)
                        if (!aQC.IsFastActionQueueEmpty)
                        {
                            SessionCheck();
                            aQC.DoWorkEveryFrame();
                        }
                }

                var isLoggedIn = loginController != null && loginController.IsWorkDone;

                // handle all controllers here which run every frame
                HandleOnFrameControllers(isLoggedIn, e);
                HandleNextPulseController(isLoggedIn, e);
            }
            catch (Exception ex)
            {
                Log.WriteLine("EVEOnFrame Exception: " + ex);
            }
        }

        private void HandleOnFrameControllers(bool isLoggedIn, DirectEveEventArgs e)
        {
            foreach (var controller in ControllerList)
            {
                if (controller is IOnFrameController frameController)
                {
                    //don't run any paused controller
                    if (controller.IsPaused)
                        continue;

                    //check if the controller should only run after we successfully logged in
                    if (!controller.RunBeforeLoggedIn)
                    {
                        if (!isLoggedIn)
                            continue;
                    }

                    SessionCheck();

                    // check if the session is valid
                    if (!controller.IgnoreValidSession)
                    {
                        if (!controller.CheckSessionValid())
                            continue;
                    }

                    // if the work is done, do nothing
                    if (controller.IsWorkDone)
                        continue;

                    // if there is a modal and not ignoring modal windows
                    if (ESCache.Instance.DirectEve.AnyModalWindowExceptFleetInvite() && !controller.IgnoreModal)
                        continue;

                    // evaluate dependencies
                    if (!controller.EvaluateDependencies(this))
                        continue;

                    // Handle the OnFrame
                    frameController.OnFrame();
                }
            }
        }

        private bool? _debugControllerManagerControllerManager = null;

        private bool DebugControllerManagerControllerManager => _debugControllerManagerControllerManager ??
                                                                (_debugControllerManagerControllerManager =
                                                                    Cache.ESCache.Instance.EveAccount.ClientSetting
                                                                        .GlobalMainSetting.DebugControllerManager)
                                                                .Value;

        private void HandleNextPulseController(bool isLoggedIn, DirectEveEventArgs e)
        {
            if (DateTime.UtcNow < Pulse)
                return;

            var controller = GetNextPulseController();
            if (controller == null)
                return;

            if (controller.LastControllerExecTimestamp != DateTime.MinValue)
            {
                var diff = DateTime.UtcNow - controller.LastControllerExecTimestamp;
                controller.Interval = (ulong)diff.TotalMilliseconds;
            }

            controller.LastControllerExecTimestamp = DateTime.UtcNow;

            // start stopwatch
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            //Log.WriteLine($"Pulse. Current controller: {controller.GetType()} Frame count: {DirectEve.FrameCount}");

            if (controller.LocalPulse > DateTime.UtcNow)
                return;

            //don't run any paused controller
            if (controller.IsPaused)
                return;

            //check if the controller should only run after we successfully logged in
            if (!controller.RunBeforeLoggedIn)
            {
                if (!isLoggedIn)
                    return;
            }

            SessionCheck();

            // check if the session is valid
            if (!controller.IgnoreValidSession)
            {
                if (!controller.CheckSessionValid())
                {
                    if (DebugControllerManagerControllerManager &&
                        DirectEve.Interval(5000, uniqueName: controller.Name))
                    {
                        Log.WriteLine(
                            $"ControllerName [{controller.Name}] did not run because of the session was not ready.");
                    }

                    return;
                }
            }

            // if the work is done, do nothing
            if (controller.IsWorkDone)
                return;

            // if there is a modal and not ignoring modal windows
            if (ESCache.Instance.DirectEve.AnyModalWindowExceptFleetInvite() && !controller.IgnoreModal)
            {
                if (DebugControllerManagerControllerManager && DirectEve.Interval(5000, uniqueName: controller.Name))
                {
                    Log.WriteLine($"ControllerName [{controller.Name}] did not run because of any modal window.");
                }

                return;
            }

            // evaluate dependencies
            if (!controller.EvaluateDependencies(this))
            {
                if (DebugControllerManagerControllerManager && DirectEve.Interval(5000, uniqueName: controller.Name))
                {
                    Log.WriteLine(
                        $"ControllerName [{controller.Name}] did not run because of a dependency was not met.");
                }

                return;
            }

            // execute controller
            controller.DoWork();
            //Log.WriteLine($"Running controller {controller.Name}");
            stopwatch.Stop();
            controller.SetDuration((ulong)stopwatch.ElapsedMilliseconds + (ulong)e.LastFrameTook);
        }

        public bool ResponsiveMode { get; set; } = false;

        private BaseController GetNextPulseController()
        {
            try
            {
                if (Enumerator == null)
                {
                    if (_getNextControllerStopwatch.IsRunning)
                        _getNextControllerStopwatch.Stop();

                    _getNextControllerStopwatch.Restart();
                    Enumerator = ControllerList.GetEnumerator();
                }

                while (Enumerator.MoveNext())
                {
                    if (Enumerator.Current.IsWorkDone)
                        continue;
                    if (Enumerator.Current.IsPaused)
                        continue;

                    return Enumerator.Current;
                }
            }
            catch (Exception)
            {
                //Log.WriteLine($"ControllerList was changed during processing.");
                // thrown if list was changed during iteration
            }

            var rnd = _rnd.Next(1, 600);
            var duration = _getNextControllerStopwatch.ElapsedMilliseconds; // The duration all controllers took.
            var nextPulseDuration =
                PulseDelayMilliseconds -
                duration; // Reduce that duration from the next pulse delay to ensure we run the controllers at the given interval.
            nextPulseDuration = Math.Max(1, nextPulseDuration);

            var nextDura = nextPulseDuration + rnd;

            if (ResponsiveMode && nextDura > 200)
                nextDura = nextDura / 2;

            Pulse = DateTime.UtcNow.AddMilliseconds(nextDura);
            Enumerator = null; // reset at the end of the iterator or if an exception was thrown
            return null;
        }

        private void SessionCheck()
        {
            ESCache.Instance.DirectEve.Session.SetSessionReady();
            ESCache.Instance.InvalidateCache();
        }

        #endregion Methods

        #region IDisposable implementation

        private bool m_Disposed = false;

        private static Random _rnd = new Random();

        ~ControllerManager()
        {
            Dispose();
        }

        public void Dispose()
        {
            try
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        private void Dispose(bool disposing)
        {
            try
            {
                if (!m_Disposed)
                {
                    if (disposing)
                        if (ESCache.Instance.DirectEve != null)
                        {
                            ESCache.Instance.DirectEve.OnFrame -= EVEOnFrame;
                            ESCache.Instance.DirectEve.Dispose();
                        }

                    m_Disposed = true;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine(ex.ToString());
            }
        }

        #endregion IDisposable implementation
    }
}