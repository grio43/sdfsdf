extern alias SC;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace EVESharpCore.Framework
{

    //public class WindowQueueItem
    //{
    //    string Reference { get; }
    //    DateTime Added { get; }

    //    public WindowQueueItem(string reference, DateTime added)
    //    {
    //        this.Reference = reference;
    //        this.Added = added;
    //    }
    //}


    public class DirectWindowManager
    {
        private readonly DirectEve DirectEve;
        private int _timeOutMs = 1800;
        private string _windowActivationsUniqueName;
        private static Queue<string> _queue = new Queue<string>();
        private static DateTime _lastDequeue;
        private static string _currentQueueItem = null;
        private static bool _isDesktopActive;

        public DirectWindowManager(DirectEve de)
        {
            this.DirectEve = de;
        }

        private void ActivateDesktop()
        {
            var uicore = DirectEve.PySharp.Import("carbonui.uicore");
            uicore.Attribute("uicore").Attribute("registry").Call("SetFocus", uicore.Attribute("uicore").Attribute("desktop"));
        }

        private string GetCurrentQueueItem
        {
            get
            {
                var timeout = _lastDequeue.AddMilliseconds(_timeOutMs) < DateTime.UtcNow;
                if ((_currentQueueItem == null || timeout) && _queue.Count > 0)
                {
                    _lastDequeue = DateTime.UtcNow;
                    var next = _queue.Dequeue();
                    Log($"Dequeued next item. Previous [{_currentQueueItem ?? "null"}] Current [{next}]");
                    _currentQueueItem = next;
                }
                return _currentQueueItem;
            }
        }

        private static int? _nextFakeInput = null;
        private static Random _random = new Random();

        private void HandleInactivity()
        {
            try
            {
                if (!DirectEve.Interval(10000, 15000))
                    return;

                if (_nextFakeInput == null)
                    _nextFakeInput = _random.Next(25, 60);

                if (DirectEve.GetInactivitySecondsSinceLastInput() > _nextFakeInput)
                {
                    _nextFakeInput = _random.Next(25, 60);
                    DirectEve.SendFakeInputToPreventIdle();
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        // we need a total of two frame ticks to permit window usage [1.] activate [2.] permission granted
        // use canExecOnThisFrame == true for code which can happen on the same frame (e.g. lock/unlock target, or any selected item window option)
        public bool ActivateWindow(Type t, bool canExecOnThisFrame = false, bool clearQueue = false)
        {

            HandleInactivity();

            if (clearQueue)
            {
                _queue = new Queue<string>();
            }

            var caller = t.Name.ToString();
            var wnds = DirectEve.Windows.Where(w => w.GetType() == t);

            if (t == typeof(DirectContainerWindow))
            {
                var inventorySpace = wnds.FirstOrDefault(w => w.Name.Equals("InventorySpace"));
                if (inventorySpace != null)
                    wnds = new List<DirectWindow>() { inventorySpace };
            }

            if (!wnds.Any() && t != typeof(DirectDesktopWindow)) // FIX ME: we need to know if the desktop is active
            {
                Log($"Warning: There is no window of type [{t.Name}]");
                return false;
            }

            var current = GetCurrentQueueItem;
            if (!_queue.Concat(new List<string>() { current }).Contains(caller))
            {
                if (wnds.Any() && wnds.Any(w => w.IsWindowActive)) // we don't need to add it to the queue if the window is already active
                {
                    Log($"Window of type [{t.Name}] is already active.");
                    return true;
                }

                Log($"Adding to queue [{caller}] Size [{_queue.Count}]");
                _queue.Enqueue(caller);
            }
            current = GetCurrentQueueItem;

            if (current.Equals(caller)) // it's our turn?
            {
                if (wnds.Any() && wnds.Any(w => w.IsWindowActive) || (t == typeof(DirectDesktopWindow) && _isDesktopActive))
                {
                    Log($"Is active [{t.Name}] - Removing [{current}] from queue.");
                    _lastDequeue = DateTime.MinValue; // ok this item is done, go next
                    return true;
                }

                if (DirectEve.Interval(350, 420, nameof(_windowActivationsUniqueName)))
                {
                    Log($"Activating [{t.Name}]");
                    if (t == typeof(DirectDesktopWindow))
                    {
                        ActivateDesktop();
                        _isDesktopActive = true;  // FIX ME: temp fix, remove me
                    }
                    else
                    {
                        wnds.FirstOrDefault().SetActive();
                        _isDesktopActive = false;
                    }

                    if (canExecOnThisFrame)
                    {
                        _lastDequeue = DateTime.MinValue;
                        return true;
                    }
                }
            }

            return false;
        }

        public bool IsAnyWindowOfTypeActive(Type t)
        {

            var wnds = DirectEve.Windows.Where(w => w.GetType() == t);
            if (wnds.Any(w => w.IsWindowActive))
            {
                DirectEve.Log($"A window of type [{t.Name}] is active.");
                return true;
            }
            return false;
        }

        private void Log(string s)
        {
            DirectEve.Log(s);
        }

    }
}
