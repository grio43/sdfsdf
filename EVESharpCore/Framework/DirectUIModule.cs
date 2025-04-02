extern alias SC;

using EVESharpCore.Logging;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework.Lookup;
using System.Security.Cryptography;
using SC::SharedComponents.Events;
using EVESharpCore.Cache;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectUIModule : DirectItem
    {
        #region Fields

        public bool _isDeactivating;
        private static Dictionary<long, DateTime> _lastModuleAction = new Dictionary<long, DateTime>();

        // CCP bug, sometimes repairing doesn't update the clients game state, but it does on the servers side.
        // So we keep track of the repair amounts until the module is in the state of "isBeingRepaired", then we clear that dict.
        // We return that the module has no module damage until isBeingRepaired == true or next session change.
        private static Dictionary<long, long> _repairAttempts = new Dictionary<long, long>();

        private bool _isActive;
        //public bool IsChangingAmmo { get; internal set; }

        private bool
            _isReloadingAmmo; // this works because it's created every frame again, by setting is directly within the reloading routine other code can't reload twice per frame

        //private List<DirectItem> _matchingAmmo;
        private PyObject _pyModule;

        private long? _targetId = null;

        #endregion Fields

        #region Constructors

        internal DirectUIModule(DirectEve directEve, PyObject pyModule) : base(directEve)
        {
            _pyModule = pyModule;
        }

        #endregion Constructors

        #region Properties

        private const int MODULE_REACTIVATION_DELAY = 400;

        public bool AutoReload { get; internal set; }
        public double? CapacitorNeed => Attributes.TryGet<double>("capacitorNeed");
        public DirectItem Charge { get; internal set; }
        public int CurrentCharges => Charge != null ? Charge.Quantity : 0;
        public double HeatDamage { get; internal set; }
        /// <summary>
        /// 0 ... 100 range
        /// </summary>
        public double HeatDamagePercent { get; internal set; }
        public bool DisableAutoReload => !IsInLimboState && AutoReload && SetAutoReload(false);
        public double? Duration => Attributes.TryGet<double>("duration");
        public bool EffectActivating => _pyModule.Attribute("effect_activating").ToBool();
        public int? EffectCategory => this?.DefEffect.Attribute("effectCategory").ToInt() ?? null;
        public int? EffectId => this?.DefEffect.Attribute("effectID").ToInt() ?? null;
        public string EffectName => this?.DefEffect.Attribute("effectName").ToUnicodeString() ?? null;
        public double? FallOff => Attributes.TryGet<double>("falloff");
        public PyObject GetPyModule => _pyModule;

        public double Hp { get; internal set; }
        public bool IsActivatable => DefEffect != null && DefEffect.IsValid;
        public bool IsPassiveModule => !IsActivatable;
        public bool IsActive => IsActivatable && (DefEffect.Attribute("isActive").ToBool() || _isActive);
        public bool IsBeingRepaired { get; internal set; }


        private bool? _isMaster;
        public bool IsMaster => _isMaster ??= _pyModule.Attribute("isMaster").ToBool();

        public int SlaveCount => _pyModule.Attribute("slaves").ToList().Count;

        public bool IsDeactivating => IsActivatable && DefEffect.Attribute("isDeactivating").ToBool() || _isDeactivating;
        public bool? IsEffectOffensive => this?.DefEffect.Attribute("isOffensive").ToBool() ?? null;
        public bool IsEnergyWeapon => GroupId == (int)Group.EnergyWeapon;

        public bool IsEwarModule => GroupId == (int)Group.WarpDisruptor
                                    || GroupId == (int)Group.StasisWeb
                                    || GroupId == (int)Group.TargetPainter
                                    || GroupId == (int)Group.TrackingDisruptor
                                    || GroupId == (int)Group.Neutralizer;

        //public bool EffectActivating2 => DirectEve.GetLocalSvc("godma").Attribute("stateManager").Attribute("activatingEffects").ToList().Any(e => e.Item(0).ToLong() == ItemId);
        public bool IsInLimboState => !IsActivatable
                                      || !IsOnline
                                      || IsDeactivating
                                      || IsReloadingAmmo
                                      || IsBeingRepaired
                                      || !DirectEve.Session.IsInSpace
                                      || DirectEve.Session.IsInDockableLocation
                                      || EffectActivating
                                      || DirectEve.IsEffectActivating(this)
                                      || ReactivationDelay > 0;

        // use for time critical module operations
        public bool IsLimboStateWithoutEffectActivating => !IsActivatable
                                      || !IsOnline
                                      || IsDeactivating
                                      || IsReloadingAmmo
                                      || IsBeingRepaired
                                      || !DirectEve.Session.IsInSpace
                                      || DirectEve.Session.IsInDockableLocation
                                      || EffectActivating
                                      || ReactivationDelay > 0;

        public bool IsInLimboStatePassiveModules => !IsOnline
                                 || IsBeingRepaired
                                 || !DirectEve.Session.IsInSpace
                                 || DirectEve.Session.IsInDockableLocation
                                 || ReactivationDelay > 0;

        public bool IsOnline { get; internal set; }
        public bool IsOverloaded { get; internal set; }
        public bool IsPendingOverloading { get; internal set; }
        public bool IsPendingStopOverloading { get; internal set; }
        public bool IsOverloadLimboState => IsPendingOverloading || IsPendingStopOverloading;
        public bool IsReloadingAmmo => _pyModule.IsValid && _pyModule.Attribute("reloadingAmmo").ToBool() || _isReloadingAmmo;

        public bool IsTurret => GroupId == (int)Group.EnergyWeapon
                                || GroupId == (int)Group.ProjectileWeapon
                                || GroupId == (int)Group.HybridWeapon;

        public int MaxCharges
        {
            get
            {
                if (Capacity == 0)
                    return 0;

                if (Charge != null && Charge.Volume > 0)
                    return Convert.ToInt32(Capacity / Charge.Volume);

                /*if (MatchingAmmo.Count > 0)
                    return (int) (Capacity/MatchingAmmo[0].Volume);*/

                return 0;
            }
        }

        public static void OnSessionChange()
        {
            _repairAttempts = new Dictionary<long, long>();
        }

        public double? OptimalRange => Attributes.TryGet<double>("maxRange");
        public bool RampActive => _pyModule.Attribute("ramp_active").ToBool();

        // always use the targetId we have set while activate, else the target id of a previous target would've been used
        public long? TargetId => _targetId.HasValue ? _targetId : (_targetId = DefEffect.Attribute("targetID").ToLong());

        private PyObject DefEffect { get; set; }

        public int OverloadState { get; set; }

        #endregion Properties

        /*public List<DirectItem> MatchingAmmo
        {
            get
            {
                if (_matchingAmmo == null)
                {
                    _matchingAmmo = new List<DirectItem>();

                    var pyCharges = _pyModule.Call("GetMatchingAmmo", TypeId).ToList();
                    foreach (var pyCharge in pyCharges)
                    {
                        var charge = new DirectItem(DirectEve);
                        charge.PyItem = pyCharge;
                        _matchingAmmo.Add(charge);
                    }
                }

                return _matchingAmmo;
            }
        }*/

        #region Methods

        public double ReactivationDelay
        {
            get
            {
                var dictEntry = _pyModule.Attribute("stateManager").Attribute("lastStopTimesByItemID").DictionaryItem(this.ItemId);
                if (dictEntry.IsValid)
                {
                    var delayedUntil = dictEntry.GetItemAt(0).ToDateTime().AddMilliseconds(dictEntry.GetItemAt(1).ToFloat());
                    if (delayedUntil > DateTime.UtcNow)
                    {
                        return (delayedUntil - DateTime.UtcNow).TotalMilliseconds;
                    }
                }
                return 0d;
            }
        }

        public bool Activate(long targetId)
        {
            try
            {
                if (IsActive)
                    return false;

                if (IsInLimboState)
                    return false;

                if (DisableAutoReload)
                    return false;

                if (this.CurrentCharges > this.MaxCharges)
                {
                    DirectEve.Log("this.CurrentCharges > this.MaxCharges");
                    return false;
                }

                DirectEve.EntitiesById.TryGetValue(targetId, out var ent);
                if (ent != null)
                {
                    if (!ent.IsValid)
                        return false;

                    if (!DirectEve.IsTargetStillValid(targetId))
                        return false;

                    if (DirectEve.IsTargetBeingRemoved(targetId))
                        return false;

                    if (ent.IsEwarImmune && IsEwarModule)
                        return false;

                    if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Activate)))
                        return false;

                    if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                        return false;

                    _isActive = true;
                    _lastModuleAction[ItemId] = DateTime.UtcNow;
                    _targetId = targetId;
                    return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"), targetId);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
                return false;
            }
        }

        /// <summary>
        ///     Cancels the repairing of DirectModule in space
        /// </summary>
        /// <returns></returns>
        public bool CancelRepair()
        {
            if (!DirectEve.Interval(1500, 2200))
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;
            return DirectEve.ThreadedCall(_pyModule.Attribute("CancelRepair"));
        }

        public bool ChangeAmmo(DirectItem charge)
        {
            if (!CanBeReloaded)
                return true;

            if (IsMaster && charge.Stacksize < SlaveCount)
            {
                if (DirectEve.Interval(15000))
                {
                    DirectEve.Log($"Stacksize [{charge.Stacksize}] of charge is too small for the amount of grouped weapons. SlaveCount [{SlaveCount}].");
                }
                return false;
            }

            if (charge.ItemId <= 0)
                return false;

            if (IsInLimboState)
                return false;

            if (charge.TypeId <= 0)
                return false;

            if (charge.Stacksize <= 0)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(ChangeAmmo)))
                return false;

            if (!charge.PyItem.IsValid)
            {
                DirectEve.Log("Charge.pyItem is not valid!");
                return false;
            }

            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;

            var reloadInfo = _pyModule.Call("GetChargeReloadInfo");

            if (!reloadInfo.IsValid)
            {
                DirectEve.Log("GetChargeReloadInfo is not valid! Error.");
                return false;
            }

            var reloadInfoList = reloadInfo.ToList();

            if (!reloadInfoList.Any())
            {
                DirectEve.Log("ReloadInfoList is empty! Error.");
                return false;
            }

            if (charge.TypeId == (int)reloadInfoList[0])
            {
                return ReloadAmmo(charge, true);
            }
            else
            {

                //self.icon.LoadIconByTypeID(charge.typeID)
                //self.charge = charge
                //self.stateManager.ChangeAmmoTypeForModule(self.moduleinfo.itemID, charge.typeID)
                //self.id = charge.itemID

                _pyModule["icon"].Call("LoadIconByTypeID", charge.TypeId);
                _pyModule.SetAttribute("charge", charge.PyItem);
                _pyModule.Attribute("stateManager").Call("ChangeAmmoTypeForModule", ItemId, charge.TypeId);
                _pyModule.SetAttribute("id", charge.ItemId);
                _pyModule.Call("UpdateChargeQuantity", charge.PyItem);
                //var setCharge = _pyModule.Attribute("SetCharge");
                //if (!setCharge.IsValid)
                //{
                //    DirectEve.Log($"SetCharge is not valid!");
                //    return false;
                //}

                //_pyModule.Call("SetCharge", charge.PyItem);
                //DirectEve.Log("Calling SetCharge()");

                return ReloadAmmo(charge, true);
            }
        }

        public bool Click(int moduleReactivationDelay = MODULE_REACTIVATION_DELAY, bool ignoreLimbo = false)
        {
            // limbo state
            if (IsInLimboState && !ignoreLimbo)
            {
                //DirectEve.Log("IsInLimbo");
                return false;
            }

            // allow only one action per frame
            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Click)))
            {
                //DirectEve.Log("FrameHasNotChanged");
                return false;
            }

            // module interval
            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
            {
                //DirectEve.Log("Mod reactivation");
                return false;
            }

            // prevent any action if the target is in the death blink animation
            if (TargetId.HasValue && DirectEve.IsTargetBeingRemoved(TargetId.Value))
                return false;

            // temp fix, deactivating modules after being jammed causes exceptions
            if (IsActive && (IsEffectOffensive.HasValue && IsEffectOffensive.Value || EffectCategory.HasValue && EffectCategory.Value == 2))
            {
                if (Math.Min(DirectEve.Me.MaxLockedTargets, DirectEve.ActiveShip.MaxLockedTargets) == 0)
                {
                    DirectEve.Log($"Blocked module [{this.TypeId}] deactivation, we are jammed.");
                    return false;
                }
            }

            // don't allow prop mods with bastion
            if (!DirectEve.ActiveShip.CanWeMove && (int)Group.Afterburner == this.GroupId && !this.IsActive)
            {
                return false;
            }

            // set is deactivating instantly for this frame
            if (IsActive)
                _isDeactivating = true;
            else
                _isActive = true;

            if (this.GroupId == 330)
            {
                    if (ESCache.Instance.InDockableLocation || !ESCache.Instance.InSpace)
                    {
                        return false;
                    }

                    if (ESCache.Instance.DirectEve.Me.IsJumpCloakActive)
                    {
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.Entity == null)
                    {
                        return false;
                    }

                    if (ESCache.Instance.EntitiesNotSelf.Any(e => e.GroupId != 227 && e.Distance <= (int)Distances.SafeToCloakDistance)) // 227 = Inventory Groups.Celestial.Cloud
                    {
                        var ent = ESCache.Instance.EntitiesNotSelf.FirstOrDefault(e => e.Distance <= (int)Distances.SafeToCloakDistance);
                        if (ent != null && ent.IsValid)
                        {
                            DirectEve.Log($"Can't activate cloak because there is another entity within [{(int)Distances.SafeToCloakDistance}]m. Entity {ent.TypeName}");
                            return false;
                        }
                    }

                    // cloak avoid storm 56050 56049
                    if (DirectEve.Entities.Any(e => e.TypeId == 56050 || e.TypeId == 56049))
                    {
                        DirectEve.IntervalLog(10000, message: "Can't cloak because of electrical storm.");
                        return false;
                    }
            }

            //DirectEve.Log("Calling click!");
            _lastModuleAction[ItemId] = DateTime.UtcNow;
            return DirectEve.ThreadedCall(_pyModule.Attribute("Click"));
        }

        public bool Deactivate()
        {
            if (IsInLimboState)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(Deactivate))) return false;

            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;
            DirectEve.AddEffectTimer(this);

            return DirectEve.ThreadedCall(_pyModule.Attribute("DeactivateEffect"), _pyModule.Attribute("def_effect"));
        }

        public void OfflineModule()
        {
            DirectEve.ThreadedCall(_pyModule.Attribute("ChangeOnline"), 0);
        }

        public void OnlineModule()
        {
            DirectEve.ThreadedCall(_pyModule.Attribute("ChangeOnline"), 1);
        }

        /// <summary>
        ///     Repairs a DirectModule in space with nanite paste
        /// </summary>
        /// <returns></returns>
        public bool Repair()
        {
            if (!DirectEve.Interval(1700, 2400))
                return false;

            if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(950) > DateTime.UtcNow)
                return false;

            if (IsInLimboStatePassiveModules)
                return false;

            _lastModuleAction[ItemId] = DateTime.UtcNow;

            if (_repairAttempts.ContainsKey(ItemId))
            {
                _repairAttempts[ItemId] = _repairAttempts[ItemId] + 1;
            }
            else
            {
                _repairAttempts[ItemId] = 1;
            }

            return DirectEve.ThreadedCall(_pyModule.Attribute("RepairModule"));
        }

        public bool SetAutoReload(bool on)
        {
            return DirectEve.ThreadedCall(_pyModule.Attribute("SetAutoReload"), on);
        }

        /// <summary>
        ///     Toggles overload of the DirectModule. If it's not allowed it will fail silently.
        /// </summary>
        /// <returns></returns>
        public bool ToggleOverload()
        {
            if (IsOverloadLimboState)
                return false;

            if (!DirectEve.Interval(900, 1500))
                return false;

            return DirectEve.ThreadedCall(_pyModule.Attribute("ToggleOverload"));
        }

        private static HashSet<int> _chargeCompatibleGroups;

        public bool WaitingForActiveTarget => _pyModule["waitingForActiveTarget"].ToBool();

        public HashSet<int> ChargeCompatibleGroups
        {
            get
            {
                if (_chargeCompatibleGroups != null)
                {
                    return _chargeCompatibleGroups;
                }

                _chargeCompatibleGroups = new HashSet<int>();

                var pyObj = PySharp.Import("__builtin__").Attribute("cfg").Attribute("__chargecompatiblegroups__");
                if (pyObj.IsValid)
                {
                    var size = pyObj.Size();
                    for (var i = 0; i < size; i++)
                    {
                        _chargeCompatibleGroups.Add(pyObj.GetItemAt(i).ToInt());
                    }
                }
                return _chargeCompatibleGroups;
            }
        }

        public bool CanBeReloaded => ChargeCompatibleGroups.Contains(GroupId);

        //public void UnloadToCargo()
        //{
        //    if (IsReloadingAmmo)
        //        return;

        //    if (Charge == null)
        //        return;

        //    if (Charge.ItemId <= 0)
        //        return;

        //    if (Charge.TypeId <= 0)
        //        return;

        //    DirectEve.ThreadedCall(_pyModule.Attribute("UnloadToCargo"), Charge.ItemId);
        //}

        internal static List<DirectUIModule> GetModules(DirectEve directEve)
        {
            var modules = new List<DirectUIModule>();

            var pySharp = directEve.PySharp;
            var carbonui = pySharp.Import("carbonui");

            var pyModules = carbonui.Attribute("uicore")
                .Attribute("uicore")
                .Attribute("layer")
                .Attribute("shipui")
                .Attribute("slotsContainer")
                .Attribute("modulesByID")
                .ToDictionary<long>();
            foreach (var pyModule in pyModules)
            {
                var module = new DirectUIModule(directEve, pyModule.Value);
                module.PyItem = pyModule.Value.Attribute("moduleinfo");
                module.ItemId = pyModule.Key;
                module.IsOnline = (bool)pyModule.Value.Attribute("online");
                //module.IsReloadingAmmo = pyModule.Value.Attribute("reloadingAmmo").ToBool();
                module.HeatDamage = _repairAttempts.ContainsKey(module.ItemId) && _repairAttempts[module.ItemId] > 4 ? 0 : (double)pyModule.Value.Attribute("moduleinfo").Attribute("damage");
                module.Hp = (double)pyModule.Value.Attribute("moduleinfo").Attribute("hp");
                module.HeatDamagePercent = module.Hp != 0 ? module.HeatDamage / module.Hp * 100 : 0;
                module.OverloadState = pyModule.Value.Attribute("stateManager").Call("GetOverloadState", module.ItemId).ToInt();
                module.IsOverloaded = module.OverloadState == 1;
                module.IsPendingOverloading = module.OverloadState == 2;
                module.IsPendingStopOverloading = module.OverloadState == 3;
                module.IsBeingRepaired = (bool)pyModule.Value.Attribute("isBeingRepaired");
                if (module.IsBeingRepaired && _repairAttempts.ContainsKey(module.ItemId))
                {
                    _repairAttempts.Remove(module.ItemId);
                }
                module.AutoReload = (bool)pyModule.Value.Attribute("autoreload");
                module.DefEffect = pyModule.Value.Attribute("def_effect");
                //module.IsActivatable = effect.IsValid;
                //module.IsActive = (bool)effect.Attribute("isActive");
                //module.IsDeactivating = (bool)effect.Attribute("isDeactivating");
                //module.TargetId = (long?)effect.Attribute("targetID");

                var pyCharge = pyModule.Value.Attribute("charge");
                if (pyCharge.IsValid)
                {
                    module.Charge = new DirectItem(directEve);
                    module.Charge.PyItem = pyCharge;
                }

                modules.Add(module);
            }
            return modules;
        }
        /// <summary>
        /// (IsValid, ActivationTime, ModuleCycleDurationMilliseconds, MillisecondsLeftUntilNextCycle)
        /// </summary>
        /// <returns></returns>
        public (bool, DateTime, int, int) GetEffectTiming()
        {
            var res = this._pyModule.Call("GetEffectTiming");
            if (res != null && res.GetPyType() == PyType.TupleType)
            {
                var outerTuple = res.ToList();
                if (outerTuple.Count > 0 && outerTuple[0].GetPyType() == PyType.TupleType)
                {
                    var innterTuple = outerTuple[0].ToList();
                    if (innterTuple.Count > 1)
                    {
                        var activationTime = innterTuple[1].ToDateTime();
                        var moduleDurationMilliseconds = outerTuple[1].ToInt();
                        var isValid = true;
                        if (moduleDurationMilliseconds <= 0)
                            isValid = false;

                        var millisecondsLeftUntilNextCycle = (activationTime.AddMilliseconds(moduleDurationMilliseconds) - DateTime.UtcNow).TotalMilliseconds;
                        if (millisecondsLeftUntilNextCycle <= 0)
                            isValid = false;

                        //DirectEve.Log($"isValid {isValid} activationTime {activationTime} moduleDurationMilliseconds {moduleDurationMilliseconds} millisecondsLeftUntilNextCycle {millisecondsLeftUntilNextCycle}");

                        return (isValid, activationTime, moduleDurationMilliseconds, (int)millisecondsLeftUntilNextCycle);
                    }
                }
                //DirectEve.Log($"{res.LogObject()}");
            }
            return (false, DateTime.UtcNow, 0, 0);
        }

        public int? EffectDurationMilliseconds
        {
            get
            {
                var dura = DefEffect.Attribute("duration");
                if (dura.IsValid)
                {
                    return dura.ToInt();
                }
                return null;
            }
        }

        public DateTime? EffectStartedWhen
        {
            get
            {
                var pySharp = DirectEve.PySharp;
                var carbonui = pySharp.Import("carbonui");
                var rampTimers = carbonui["uicore"]["uicore"]["layer"]["shipui"]["sr"]["rampTimers"];

                if (!rampTimers.IsValid)
                    return null;

                var rampTimersDict = rampTimers.ToDictionary<long>();
                if (!rampTimersDict.ContainsKey(this.ItemId))
                    return null;

                var rampTimer = rampTimersDict[this.ItemId];
                if (!rampTimer.IsValid)
                    return null;

                var tuple = rampTimer.ToList();

                if (tuple.Count <= 1)
                    return null;

                return tuple[1].ToDateTime();
            }
        }

        public int? MillisecondsUntilNextCycle
        {
            get
            {
                if (!EffectStartedWhen.HasValue)
                    return null;

                if (!EffectDurationMilliseconds.HasValue)
                    return null;

                var millisecondsLeftUntilNextCycle = (EffectStartedWhen.Value.AddMilliseconds(EffectDurationMilliseconds.Value) - DateTime.UtcNow).TotalMilliseconds;

                if (millisecondsLeftUntilNextCycle <= 0)
                {
                    return null;
                }

                return (int)millisecondsLeftUntilNextCycle;
            }
        }


        private bool ReloadAmmo(DirectItem charge, bool ignoreModuleAction = false)
        {
            if (IsInLimboState)
                return false;

            if (charge.ItemId <= 0)
                return false;

            if (charge.TypeId <= 0)
                return false;

            if (ItemId <= 0)
                return false;

            if (!DirectEve.HasFrameChanged(ItemId.ToString() + nameof(ReloadAmmo)))
                return false;


            if (!DirectEve.DWM.ActivateWindow(typeof(DirectDesktopWindow), true))
                return false;

            if (!ignoreModuleAction)
            {
                if (_lastModuleAction.ContainsKey(ItemId) && _lastModuleAction[ItemId].AddMilliseconds(MODULE_REACTIVATION_DELAY) > DateTime.UtcNow)
                    return false;

                _lastModuleAction[ItemId] = DateTime.UtcNow;
            }



            _isReloadingAmmo = true;
            Log.WriteLine("Reloading/Changing [" + ItemId + "] [" + TypeName + "] with [" + charge.TypeName + "]");
            DirectEve.AddEffectTimer(this);
            DirectEve.ThreadedCall(DirectEve.GetLocalSvc("clientDogmaIM").Attribute("dogmaLocation").Attribute("LoadAmmoTypeToModule"), ItemId, charge.TypeId);
            return true;
        }

        #endregion Methods
    }
}

//		public bool Activate()
//		{
//			try
//			{
//
//				if (LastActivatedModule.ContainsKey(this.ItemId) && LastActivatedModule[this.ItemId].AddMilliseconds(500) > DateTime.UtcNow)
//				{
//					return false;
//				}
//
//				LastActivatedModule[this.ItemId] = DateTime.UtcNow;
//				return DirectEve.ThreadedCall(_pyModule.Attribute("ActivateEffect"), _pyModule.Attribute("def_effect"));
//
//			}
//			catch (Exception ex)
//			{
//
//				Console.WriteLine("DirectEve.Activate Exception [" + ex + "]");
//				return false;
//			}
//
//		}