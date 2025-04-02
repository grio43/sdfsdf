extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{

    public enum SlotGroup
    {
        HiSlot = 0,
        MedSlot = 1,
        LoSlot = 2,
        SubsysSlot = 3,
        RigSlot = 4
    }

    public class DirectFittingModule : PyObject
    {
        private IntPtr _pyref;
        public bool SlotExists { get; private set; }
        public SlotGroup SlotGroup { get; private set; }

        public PyObject Module { get; private set; }

        private DirectEve DirectEve { get; set; }

        public int TypeId { get; private set; }

        public DirectInvType InvType { get; private set; }

        public bool HasItemFit { get; private set; }

        public DirectFittingModule(PySharp pySharp, IntPtr pyReference, bool newReference, DirectEve de, SlotGroup slotGroup,
            string attributeName = "") : base(pySharp, pyReference, newReference, attributeName)
        {
            _pyref = pyReference;
            SlotExists = this.Call("SlotExists").ToBool();
            SlotGroup = slotGroup;
            Module = this["dogmaModuleItem"];
            DirectEve = de;
            HasItemFit = Module.IsValid;
            if (Module.IsValid)
            {
                TypeId = Module["typeID"].ToInt();
                InvType = DirectEve.GetInvType(TypeId);
            }
        }

        public void OnlineModule()
        {
            DirectEve.ThreadedCall(this["OnlineModule"]);
        }

        public void OfflineModule()
        {
            DirectEve.ThreadedCall(this["OfflineModule"]);
        }

        // this might end up with additional modal messages
        // charges will be removed first then the module itself, so it must be called twice for modules with loaded charges
        public void Unfit()
        {
            DirectEve.ThreadedCall(this["Unfit"]);
        }

        public void FitModule(DirectItem item)
        {
            DirectEve.ThreadedCall(this["FitModule"], item.PyItem);
        }

        public bool IsOnline()
        {
            return this.Call("IsOnline").ToBool();
        }
    }
    public class DirectFittingWindow : DirectWindow
    {
        internal DirectFittingWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        public PyObject Controller => PyWindow["controller"];

        public PyObject SlotsByGroup => Controller["slotsByGroups"];

        public List<DirectFittingModule> GetModulesOfGroup(SlotGroup group, bool onlyExistingSlots = true)
        {
            var ret = new List<DirectFittingModule>();
            var slots = SlotsByGroupDictionary[(int)group].ToList<PyObject>();
            foreach (var slot in slots)
            {
                var item = new DirectFittingModule(PySharp, slot.PyRefPtr, false, DirectEve, group);
                if (onlyExistingSlots && !item.SlotExists)
                {
                    continue;
                }
                ret.Add(item);
            }
            return ret;
        }

        public List<DirectFittingModule> GetAllModules(bool onlyExistingSlots = true)
        {
            return GetModulesOfGroup(SlotGroup.HiSlot, onlyExistingSlots).Concat(GetModulesOfGroup(SlotGroup.MedSlot, onlyExistingSlots))
                .Concat(GetModulesOfGroup(SlotGroup.LoSlot, onlyExistingSlots)).Concat(GetModulesOfGroup(SlotGroup.RigSlot, onlyExistingSlots))
                .Concat(GetModulesOfGroup(SlotGroup.SubsysSlot, onlyExistingSlots)).ToList();
        }

        public Dictionary<int, PyObject> SlotsByGroupDictionary => SlotsByGroup.ToDictionary<int>();

        public PyObject DogmaLocation => Controller["dogmaLocation"];
        public Dictionary<String, PyObject> GetCurrentAttributeValues =>
            Controller.Call("GetCurrentAttributeValues").ToDictionary<string>();

        private float? _currentDroneControlRange;
        public float GetCurrentDroneControlRange => _currentDroneControlRange ??= Controller.Call("GetDroneControlRange")["value"].ToFloat();

        private float? _maxTargets;
        public float GetMaxTargets => _maxTargets ??= Controller.Call("GetMaxTargets")["value"].ToFloat();

        private float? _maxVelocity;
        public float GetMaxVelocity => _maxVelocity ??= Controller.Call("GetMaxVelocity")["value"].ToFloat();

        public bool IsFittingSimulated
        {
            get
            {
                var dogma = DirectEve.GetLocalSvc("ghostFittingSvc")["fittingDogmaLocation"];
                if (dogma.IsValid)
                {
                    var items = dogma["dogmaItems"].ToDictionary<string>(); // wait until there is at least one active module in the ghost fitting => (hopefully means) that we loaded the fitting and all values are up to date
                    foreach (var kv in items)
                    {
                        var py = kv.Value;
                        if (py.IsValid && py["IsActive"].IsCallable())
                        {
                            var ret = py.Call("IsActive").ToBool();

                            if (ret) {
                                return true && Controller["isShipSimulated"].ToBool(); ;
                            }
                        }
                    }

                    
                }

                return false;
            }
        }
        // look @ GetCurrentAttributeValues for all other attributes
    }
}
