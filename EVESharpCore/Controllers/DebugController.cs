/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 26.06.2016
 * Time: 18:31
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Windows.Controls;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Abyssal;
using EVESharpCore.Controllers.ActionQueue.Actions;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Hooks;
using EVESharpCore.Framework.Lookup;
using SC::EasyHook;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings.Abyssal.Main;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Py;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack;
using ServiceStack.OrmLite;
using ServiceStack.Text;
using SC::SharedComponents.EveMarshal;
using SharpDX.Direct2D1;
using SC::SharedComponents.EVE.ClientSettings.Abyssal.Main;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of DebugController.
    /// </summary>
    public class DebugController : BaseController, IOnFrameController, IPacketHandlingController
    {
        #region Constructors

        public DebugController() : base()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new DebugControllerForm(this);
            //RunBeforeLoggedIn = true;
            //IgnoreValidSession = true;
        }

        private DirectEntity _midGate =>
            ESCache.Instance.DirectEve.Entities.FirstOrDefault(e =>
                e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);


        private DirectEntity _endGate =>
            ESCache.Instance.DirectEve.Entities.FirstOrDefault(e =>
                e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);

        private DirectEntity _nextGate => _midGate ?? _endGate;

        #endregion Constructors

        #region Methods

        private void PrintDroneDamageState()
        {
            foreach (var drone in ESCache.Instance.DirectEve?.GetShipsDroneBay()?.Items
                         .Where(e => e.TypeName.Contains("Gecko")))
            {
                Log($"TypeName {drone.TypeName} DmgState {drone.GetDroneInBayDamageState()}");
            }

            Log("--------------");
        }

        private void PrintInspaceDroneStates()
        {
            foreach (var drone in ESCache.Instance.DirectEve?.ActiveDrones)
            {
                Log($"TypeName {drone.TypeName} DroneState {drone.DroneState}");
            }
        }

        //internal List<DirectEntity> _targetsOnGrid => ESCache.Instance.DirectEve.Entities.Where(e => e.IsNPCByBracketType && e.BracketType != BracketType.NPC_Drone || IsEntityWeWantToLoot(e)).OrderBy(e => e.AbyssalTargetPriority).ToList();
        private static Random random = new Random();

        private String rndHex()
        {
            int num = random.Next(0, 0xffffff);
            string hexString = num.ToString("X").PadLeft(6, '0');
            return "FF" + hexString;
        }

        private string HtmlTextColorize(string text, Vector4 color)
        {
            return
                $"<color=0x{((int)color.W).ToString("X")}{(int)color.X:X}{(int)color.Y:X}{(int)color.Z:X}>{text}</color>";
        }

        internal List<DirectItem> _getDronesInBay(MarketGroup marketGroup) =>
            ESCache.Instance.DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.MarketGroupId == (int)marketGroup)
                ?.ToList() ?? new List<DirectItem>();

        internal List<DirectItem> _getDronesInBayByTypeId(int typeId) =>
            ESCache.Instance.DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.TypeId == typeId)?.ToList() ??
            new List<DirectItem>();

        internal List<DirectItem> smallDronesInBay => _getDronesInBay(MarketGroup.LightScoutDrone).Concat(_getDronesInBayByTypeId(60478)).ToList();
        internal List<DirectItem> mediumDronesInBay => _getDronesInBay(MarketGroup.MediumScoutDrone).Concat(_getDronesInBayByTypeId(60479)).ToList();

        internal List<DirectItem> largeDronesInBay => _getDronesInBay(MarketGroup.HeavyAttackDrone)
            .Concat(_getDronesInBayByTypeId(60480)).ToList();

        internal List<DirectItem> alldronesInBay =>
            smallDronesInBay.Concat(mediumDronesInBay).Concat(largeDronesInBay).ToList();

        private static double _droneRecoverShieldPerc = 50;
        private static double _droneLaunchShieldPerc = _droneRecoverShieldPerc + 15;

        private Vec3 CalculateRandomConePointOnNormalizedDirectionVector(Vec3 D)
        {
            var p = new Vec3(-D.Z, 0, D.X).Normalize();
            var q = p.CrossProduct(D);
            var phi = 0.01d;
            var rMax = Math.Tan(phi);
            var theta = (0.5d * Math.PI);
            var r = rMax * Math.Sqrt(0.5d);
            var v = r * (p * Math.Cos(theta) + q * Math.Sin(theta));
            return v.Normalize();
        }

        public static System.Numerics.Quaternion ToQ(float pitch, float yaw, float roll)
        {
            yaw *= 0.017453292f;
            pitch *= 0.017453292f;
            roll *= 0.017453292f;
            float rollOver2 = roll * 0.5f;
            float sinRollOver2 = (float)Math.Sin((double)rollOver2);
            float cosRollOver2 = (float)Math.Cos((double)rollOver2);
            float pitchOver2 = pitch * 0.5f;
            float sinPitchOver2 = (float)Math.Sin((double)pitchOver2);
            float cosPitchOver2 = (float)Math.Cos((double)pitchOver2);
            float yawOver2 = yaw * 0.5f;
            float sinYawOver2 = (float)Math.Sin((double)yawOver2);
            float cosYawOver2 = (float)Math.Cos((double)yawOver2);
            System.Numerics.Quaternion result;
            result.W = cosYawOver2 * cosPitchOver2 * cosRollOver2 + sinYawOver2 * sinPitchOver2 * sinRollOver2;
            result.X = cosYawOver2 * sinPitchOver2 * cosRollOver2 + sinYawOver2 * cosPitchOver2 * sinRollOver2;
            result.Y = sinYawOver2 * cosPitchOver2 * cosRollOver2 - cosYawOver2 * sinPitchOver2 * sinRollOver2;
            result.Z = cosYawOver2 * cosPitchOver2 * sinRollOver2 - sinYawOver2 * sinPitchOver2 * cosRollOver2;
            return result;
        }

        public static System.Numerics.Quaternion EulerToQuaternion(float pitch, float yaw, float roll)
        {
            // Convert Euler angles to radians
            pitch = pitch * (float)Math.PI / 180f;
            yaw = yaw * (float)Math.PI / 180f;
            roll = roll * (float)Math.PI / 180f;

            // Calculate sin and cosine for all angles
            float sinPitch = (float)Math.Sin(pitch);
            float cosPitch = (float)Math.Cos(pitch);
            float sinYaw = (float)Math.Sin(yaw);
            float cosYaw = (float)Math.Cos(yaw);
            float sinRoll = (float)Math.Sin(roll);
            float cosRoll = (float)Math.Cos(roll);

            // Calculate the quaternion elements
            float w = cosRoll * cosPitch * cosYaw + sinRoll * sinPitch * sinYaw;
            float x = sinRoll * cosPitch * cosYaw - cosRoll * sinPitch * sinYaw;
            float y = cosRoll * sinPitch * cosYaw + sinRoll * cosPitch * sinYaw;
            float z = cosRoll * cosPitch * sinYaw - sinRoll * sinPitch * cosYaw;

            // Return the quaternion
            return new System.Numerics.Quaternion(x, y, z, w);
        }

        static Random rand = new Random();

        public static Vec3 RotateVector(Vec3 vec, float maxAngle)
        {
            // Normalize the vector
            vec.Normalize();

            // Generate a random unit quaternion

            float pitch = (float)(rand.NextDouble() * 2 - 1) * maxAngle;
            float yaw = (float)(rand.NextDouble() * 2 - 1) * maxAngle;
            float roll = (float)(rand.NextDouble() * 2 - 1) * maxAngle;

            // float pitch = maxAngle;
            // float yaw = maxAngle;
            // float roll = maxAngle;

            var quat = ToQ(yaw, pitch, roll);

            // Rotate the vector using the quaternion
            Vec3 rotatedVec = quat * vec;
            return rotatedVec;
        }

        private void OverwriteAbyssalSettings(AbyssalMainSetting setting)
        {
            var cs = ESCache.Instance.EveAccount.ClientSetting;
            cs.AbyssalMainSetting = setting;
            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                nameof(ESCache.Instance.EveAccount.ClientSetting), cs);
        }

        internal DirectEntity _getMTUInSpace => ESCache.Instance.DirectEve.Entities.FirstOrDefault(i => i.GroupId == 1250);

        public override void DoWork()
        {
            try
            {

                //Log($"???????????????");
                //var mtu = _getMTUInSpace;

                //if (mtu != null)
                //{

                //    var cont = ESCache.Instance.DirectEve.GetContainer(_getMTUInSpace.Id);
                //    if (cont == null)
                //    {
                //        Log($"Error: Cont == null!");
                //        return;
                //    }

                //    if (cont.Window == null)
                //    {
                //        if (DirectEve.Interval(2500, 3000) && _getMTUInSpace.OpenCargo())
                //        {
                //            Log($"Opening container cargo.");
                //            return;
                //        }

                     
                //    }
                //    else
                //    {
                //        Log($"Window != null");

                //        if (cont.Window.CurrInvIdItem != cont.ItemId)
                //        {
                //            Log($"Selecting inv tree item with id [{cont.ItemId}]");
                //            cont.Window.SelectTreeEntryByID(cont.ItemId);
                //            return;
                //        }
                //        else
                //        {
                //            Log("adsasdaß");
                //        }
                //    }
                //}
                //else
                //{
                //    Log($"MTU null???");
                //}

                //var friendlyOrca = Framework.Entities.Where(e =>
                //    Framework.FleetMembers.Any(f =>
                //        f.CharacterId == e.OwnerId) && e.TypeId == 28606
                //);

                //Log($"[{friendlyOrca.Count()}] [{friendlyOrca.FirstOrDefault().TypeName}]");

                //foreach (var fm in Framework.FleetMembers)
                //{
                //    Log($"{fm.Name} CharId [{fm.CharacterId}]");
                //}

                //Log($"Framework.ActiveShip.OwnerId [{Framework.ActiveShip.OwnerId}] Framework.Session.CharacterId [{Framework.Session.CharacterId}]");

                //var threshgolds = ESCache.Instance.EveAccount.ClientSetting.AutoBotMainSetting.Thresholds;

                //Log($"{threshgolds.Count}");




                //foreach (var th in threshgolds)
                //{
                //    Log($"{th.GetType()} {th.ControllerSettings.Settings.GetType()}");
                //    if (th.ControllerSettings.Settings is AbyssalMainSetting a)
                //    {
                //        Log($"XD");
                //        var cs = ESCache.Instance.EveAccount.ClientSetting;
                //        cs.AbyssalMainSetting = a;
                //        WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(ESCache.Instance.CharName,
                //            nameof(ESCache.Instance.EveAccount.ClientSetting), cs);
                //    }
                //}

                //var seconds = Framework.GetInactivitySecondsSinceLastInput();
                //Log($"InactiveSecs: [{seconds}]");

                //if (seconds > 30)
                //{
                //    Framework.SendFakeInputToPreventIdle();
                //}
                //_last_ui_interaction_timestamp
                //var lastInt = Framework.GetLocalSvc("gameui")["_last_ui_interaction_timestamp"]
                //    .ToDateTimeFromPythonDateTime();
                //var gametime = Framework.PySharp.Import("gametime");
                //var now = gametime.Call("now").ToDateTimeFromPythonDateTime();
                //Log($"_last_ui_interaction_timestamp [{lastInt}] now [{now}] diffSeconds {Math.Abs((lastInt-now).TotalSeconds)}");
                //var itemHangar = Framework.GetItemHangar();
                //var tritItems = itemHangar.Items.Where(e => e.TypeId == 34).Take(2).ToList();
                //Framework.TrashItems(tritItems);

                //var t = Framework.SolarSystems.Values.Where(e => e.GetSecurity() >= 0.45 && e.GetSecurity() < 0.55).ToList();
                //Log($"Amount of 0.5 Systems [{t.Count}]");
                //t = t.Where(e => !e.IsHighsecIsleSystem()).ToList();
                //Log($"Amount of 0.5 Systems excluding HS Islands [{t.Count}]");

                //var closestSystem = t.OrderBy(e => e.CalculatePathTo(Framework.Me.CurrentSolarSystem).Item1.Count);

                //Log($"Closest 0.5 System [{closestSystem.First().Name}]");

                //Framework.FormFleetWithSelf();


                //var playersNotSelf = Framework.Entities.Where(e => e.Name != Framework.Me.Name && e.IsPlayer).ToList();

                //foreach (var p in playersNotSelf)
                //{
                //    Log($"Id [{Framework.GetOwner(p.OwnerId).Name}] IsMassive [{p.IsMassive}]");
                //}


                //var systems = Framework.GetInsurgencyInfestedSystem();

                //// print all system names
                //foreach (var system in systems)
                //{
                //    Log($"System: {system.Name}");
                //}

                //if (Framework.Me.GetSafety() == DirectMe.SafetyLevel.ShipSafetyLevelNone)
                //{
                //    Framework.Me.SetSafety(DirectMe.SafetyLevel.ShipSafetyLevelFull);
                //}

                //var bcs = Framework.GetTargetBroadcasts();

                //foreach (var kv in bcs)
                //{
                //    Log($"[{kv.Key}] [{string.Join(",", kv.Value)}]");
                //}

            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            finally
            {
                IsPaused = true;
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }


        public void OnFrame()
        {
            try
            {
                return;

                if (!DirectEve.Interval(500))
                    return;

                //var nextTick = Framework.PySharp.NextServerTick;
                //var lastTick = Framework.PySharp.LastServerTick;

                var gametime = Framework.PySharp.Import("gametime");

                var blueOs = Framework.PySharp.Import("blue")["os"];

                if (!gametime.IsValid)
                    Log($"!gametime.IsValid");

                if (!blueOs.IsValid)
                    Log($"!blueOs.IsValid");

                //self.GetSimTime = blue.os.GetSimTime
                //self.GetWallclockTime = blue.os.GetWallclockTime
                //self.GetWallclockTimeNow = blue.os.GetWallclockTimeNow

                var simTime = blueOs.Call("GetSimTime").ToLong();
                var wallclockTime = blueOs.Call("GetWallclockTime").ToLong();
                var wallclockTimeNow = blueOs.Call("GetWallclockTimeNow").ToLong();


                var simTimeDtExact = blueOs.Call("GetSimTime").ToDateTimeExact();
                var wallclockTimeDtExact = blueOs.Call("GetWallclockTime").ToDateTimeExact();
                var wallclockTimeNowDtExact = blueOs.Call("GetWallclockTimeNow").ToDateTimeExact();

                var simTimeDt = blueOs.Call("GetSimTime").ToDateTime();
                var wallclockTimeDt = blueOs.Call("GetWallclockTime").ToDateTime();
                var wallclockTimeNowDt = blueOs.Call("GetWallclockTimeNow").ToDateTime();


                Log($"simTime [{simTime}] wallclockTime [{wallclockTime}] wallclockTimeNow [{wallclockTimeNow}]");
                Log($"simTimeDtExact [{simTimeDtExact:O}] wallclockTimeDtExact [{wallclockTimeDtExact:O}] wallclockTimeNowDtExact [{wallclockTimeNowDtExact:O}]");
                Log($"simTimeDt [{simTimeDt:O}] wallclockTimeDt [{wallclockTimeDt:O}] wallclockTimeNowDt [{wallclockTimeNowDt:O}]");
                //Log($"Next tick will be at [{nextTick}] in [{(DateTime.UtcNow - nextTick).TotalMilliseconds}] milliseconds.");
                //Log($"Last tick was at [{lastTick}] in [{Math.Abs((DateTime.UtcNow - lastTick).TotalMilliseconds)}] milliseconds.");
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }

        private void DebugRender()
        {
            //Log($"Ping!");
            ///Framework.SceneManager.ClearDebugLines();
            var tr = Framework.PySharp.Import("trinity");
            var pySharp = Framework.PySharp;
            var dgbr = tr.Call("GetDebugRenderer");

            if (dgbr.IsValid)
            {
                Vec3 PosA = new Vec3(0, 0, 0);
                Vec3 PosB = new Vec3(10000, 10000, 10000);
                var activeShip = Framework.ActiveShip.Entity.DirectAbsolutePosition;
                Vec3 PosC = new Vec3(activeShip.X, activeShip.Y, activeShip.Z);
                long color = 0xBB_ff_00_ff;
                var pyPosA = PyObject.CreateTuple(pySharp, PosA.X, PosA.Y, PosA.Z);
                var pyPosB = PyObject.CreateTuple(pySharp, PosB.X, PosB.Y, PosB.Z);
                var pyPosC = PyObject.CreateTuple(pySharp, PosC.X, PosC.Y, PosC.Z);
                float radius = 10_000.0f;
                dgbr.Call("DrawBox", pyPosA, pyPosB, color);
                //dgbr.Call("DrawSphere", pyPosC, radius, 6, color);
                dgbr.Call("DrawCylinder", pyPosA, pyPosB, radius, ~0, color);
                dgbr.Call("Print3D", pyPosA, color, "HELLO WORLD \n yEP coCK");
                //Log($"{dgbr.LogObject()}");
            }
            else
            {
                Log($"dgbr is not valid");
                var renderJob = tr.Call("CreateRenderJob");
                var dr = renderJob.Call("RenderDebug");
                tr.Call("SetDebugRenderer", dr);
                renderJob.Call("ScheduleRecurring");
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }


        public void HandleRecvPacket(byte[] packetBytes)
        {
            try
            {


            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public void HandleSendPacket(byte[] packetBytes)
        {

        }

        #endregion Methods
    }
}