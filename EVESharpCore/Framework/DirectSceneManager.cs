extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using System.Windows.Media.Animation;
using EVESharpCore.Logging;
using SharpDX.Direct2D1.Effects;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using SC::SharedComponents.Extensions;

namespace EVESharpCore.Framework
{
    public class DirectSceneManager : DirectObject
    {
        public PyObject Camera { get; }
        public PyObject ProjectionMatrix { get; }

        public PyObject CamUtil { get; }

        public PyObject Geo2 { get; }

        public PyObject MatrixIdentity { get; }

        public PyObject DefaultScene { get; }

        private List<PyObject> _defaultSceneObjects;
        public List<PyObject> DefaultSceneObjects => _defaultSceneObjects ??= DefaultScene["objects"].ToList();

        private Dictionary<string, PyObject> _defaultSceneObjectsDict;
        public Dictionary<string, PyObject> DefaultSceneObjectsDict
        {
            get
            {
                _defaultSceneObjectsDict ??= DefaultSceneObjects.GroupBy(x => x["name"].ToUnicodeString()).Where(x => x.Key != null).ToDictionary(x => x.Key, x => x.First());
                return _defaultSceneObjectsDict;
            }
        }

        public PyObject ViewMatrix { get; }

        public Vec3 CameraDirection { get; }

        public Vec3 EyePosition { get; }

        internal DirectSceneManager(DirectEve directEve) : base(directEve)
        {
            var sm = directEve.GetLocalSvc("sceneManager");
            if (!sm.IsValid)
                return;

            DefaultScene = sm.Call("GetRegisteredScene", "default");

            var primaryJob = sm.Attribute("primaryJob");
            var secondaryJob = sm.Attribute("secondaryJob");
            var currentJob = secondaryJob.IsValid ? secondaryJob : primaryJob;

            if (currentJob.IsValid)
            {
                var cam = currentJob.Attribute("camera");
                this.Camera = cam;
                var eyePos = cam.Attribute("eyePosition").ToList();
                this.EyePosition = new Vec3(eyePos[0].ToFloat(), eyePos[1].ToFloat(), eyePos[2].ToFloat());
                this.CameraDirection = (new Vec3(0, 0, 0) - this.EyePosition).Normalize();
                //var world2Screen = cam.Attribute("ProjectWorldToCamera");
                var viewMatrixTransform = cam.Attribute("viewMatrix").Attribute("transform");
                var projectionMatrixTransform = cam.Attribute("projectionMatrix").Attribute("transform");
                this.ProjectionMatrix = projectionMatrixTransform;
                this.ViewMatrix = viewMatrixTransform;
            }

            var camUtil = DirectEve.PySharp.Import("eve.client.script.ui.camera.cameraUtil");
            this.CamUtil = camUtil;

            var geo2 = DirectEve.PySharp.Import("geo2");
            this.Geo2 = geo2;

            this.MatrixIdentity = Geo2.Call("MatrixIdentity");
        }

        public void SetMaxCameraZoom(float max)
        {
            if (Camera.IsValid)
            {
                Camera.SetAttribute<float>("maxZoom", max);
            }
        }

        public void SetMinCameraZoom(float min)
        {
            if (Camera.IsValid)
            {
                Camera.SetAttribute<float>("minZoom", min);
            }
        }

        public void EnableZoomHack()
        {
            if (Camera.IsValid && Camera.HasAttrString("minZoom") && Camera["minZoom"].ToFloat() != 10000000f)
            {
                SetMinCameraZoom(10000000);
                DirectEve.Log("Zoomhack enabled.");
            }
        }

        public bool IsSeenByCamera(PyObject vec3)
        {
            if (!Camera["IsInFrontOfCamera"].IsValid)
                DirectEve.Log("IsInFrontOfCamera not valid.");

            var r = Camera.Call("IsInFrontOfCamera", vec3);

            //DirectEve.Log(r.LogObject());

            return r.ToBool();
        }

        public static DateTime LastRedrawSceneColliders = DateTime.MinValue;

        public void RedrawSceneColliders(bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false)
        {

            LastRedrawSceneColliders = DateTime.UtcNow;
            // Sentry guns to debug
            var ents = ESCache.Instance.DirectEve.Entities.Where(ise => ise.Distance < 3000000 && (ise.GroupId == (int)Group.SentryGun || ise.HasAnyNonTraversableColliders
                || (!ignoreAbyssEntities &&
                    (ise.IsTrackingPylon && !ignoreTrackingPolyons
                    || ise.IsTrackingPylon && !ignoreTrackingPolyons
                    || ise.IsAutomataPylon && !ignoreAutomataPylon
                    || ise.IsWideAreaAutomataPylon && !ignoreWideAreaAutomataPylon
                    || ise.IsFilaCould && !ignoreFilaCouds
                    || ise.IsBioCloud && !ignoreBioClouds
                    || ise.IsTachCloud && !ignoreTachClouds))));

            //ESCache.Instance.DirectEve.SceneManager.RemoveAllDrawnObjects();

            var allCustomSceneObjects = ESCache.Instance.DirectEve.SceneManager.GetCustomDrawnObjectNames();
            List<string> activeCustomSceneObjects = new List<string>();

            var anyAbysEnts = ents.Any(e => e.IsAbyssSphereEntity);
            foreach (var ent in ents.Distinct())
            {
                if (ent.HasAnyNonTraversableColliders) // Draw colliders
                {
                    ent.ShowDestinyBalls(1);
                }

                if (!ignoreAbyssEntities && (ent.IsTrackingPylon && !ignoreTrackingPolyons
                                             || ent.IsTrackingPylon && !ignoreTrackingPolyons
                                             || ent.IsAutomataPylon && !ignoreAutomataPylon
                                             || ent.IsWideAreaAutomataPylon && !ignoreWideAreaAutomataPylon
                                             || ent.IsFilaCould && !ignoreFilaCouds
                                             || ent.IsBioCloud && !ignoreBioClouds
                                             || ent.IsTachCloud &&
                                             !ignoreTachClouds)) // Draw a sphere around those which don't have colliders
                {
                    ent.DrawSphere();
                    activeCustomSceneObjects.Add(ent.Id + "_CCPEndorsedRenderObject");
                }

                if (ent.GroupId == (int)Group.SentryGun)
                {
                    ent.DrawSphere();
                    activeCustomSceneObjects.Add(ent.Id + "_CCPEndorsedRenderObject");
                }
            }

            // Remove all custom scene objects which are not active anymore
            foreach (var obj in allCustomSceneObjects)
            {
                if (!activeCustomSceneObjects.Contains(obj))
                {
                    ESCache.Instance.DirectEve.SceneManager.RemoveDrawnObject(obj);
                }
            }
        }

        public static Vec3? GenerateSafeDirectionVector(Vec3 coneDir, Vec3 coneApex, Vec3 sphereCenter, float sphereRadius, double coneAngle = 45, int samples = 1000, double safeDistance = 25_000, bool drawResult = true
            , bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false, bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, bool ignoreSentryGuns = false, bool breakOnFirstHit = false)
        {

            var randomVectors = DirectSceneManager.FibonacciSphereWithinCone(samples: samples, coneAngle: coneAngle).RandomPermutation();
            var q = DirectSceneManager.GetRotationQuaternion(new Vec3(0, 0, 1), coneDir);
            var green = new Vector4(0, 255, 0, 255);
            var red = new Vector4(255, 0, 0, 255);
            var yellow = new Vector4(255, 255, 0, 255);
            Vec3? res = null;
            foreach (var r in randomVectors)
            {
                var randomDir = q * r;
                var intersect = DirectSceneManager.GetLineSphereIntersection(coneApex, coneApex + Vec3.Scale(randomDir, sphereRadius * 3), sphereCenter, sphereRadius);
                if (intersect != null)
                {
                    //Console.WriteLine(intersect);
                    var directionalVector = (intersect.Value - coneApex).Normalize();
                    var direc = intersect.Value + directionalVector.Scale(safeDistance);
                    var IsFree = DirectRayCasting.IsLineOfSightFree(intersect.Value, direc,
                        ignoreAbyssEntities, ignoreTrackingPolyons, ignoreAutomataPylon, ignoreWideAreaAutomataPylon, ignoreFilaCouds, ignoreBioClouds, ignoreTachClouds, ignoreSentryGuns);

                    if (drawResult)
                    {
                        if (IsFree.Item1)
                        {
                            ESCache.Instance.DirectEve.SceneManager.DrawLineGradientAbsolute(intersect.Value, direc, green, green);
                        }
                        else
                        {
                            ESCache.Instance.DirectEve.SceneManager.DrawLineGradientAbsolute(intersect.Value, direc, red, red);
                        }
                    }

                    if (IsFree.Item1)
                    {
                        res = (direc - coneApex).Normalize();
                        if (drawResult)
                        {
                            ESCache.Instance.DirectEve.SceneManager.DrawLineGradient(new Vec3(0, 0, 0), Vec3.Scale(res.Value, 100_000), yellow, yellow);
                        }
                    }

                    if (breakOnFirstHit && IsFree.Item1)
                    {
                        break;
                    }
                }
            }
            return res;
        }

        public enum SphereType { Jumprangebubble, Miniball, Scanbubblehitsphere, Scanconesphere };

        private Dictionary<SphereType, string> SpheretypeResPath = new Dictionary<SphereType, string>()
        {
            [SphereType.Jumprangebubble] = "res:/dx9/model/UI/JumpRangeBubble.red",
            [SphereType.Miniball] = "res:/Model/Global/Miniball.red",
            [SphereType.Scanbubblehitsphere] = "res:/dx9/model/UI/ScanBubbleHitSphere.red",
            [SphereType.Scanconesphere] = "res:/dx9/model/UI/ScanConeSphere.red",
        };

        public void DrawSphere(float radius, string name, float x, float y, float z, PyObject ball = null, SphereType sphereType = SphereType.Jumprangebubble)
        {
            radius *= 2;
            var resMan = PySharp.Import("blue")["resMan"];
            if (resMan.IsValid)
            {
                // 1: res:/dx9/model/ui/jumprangebubble.black - white X
                // 2: res:/Model/Global/Miniball.red - green checker X
                // 3: res:/dx9/model/ui/activitytrackersphere.black - very faint bubble
                // 4: res:/dx9/model/ui/jumprangebubble.black - white
                // 5: res:/dx9/model/ui/scanbubblehitsphere.black - red wire frame - size needs to be divied by 4 X
                // 6: res:/model/global/gridsphere.black - another faint grey bubble
                // 7: res:/dx9/model/ui/scanconesphere.black - dscan scan bubble X

                if (sphereType == SphereType.Scanbubblehitsphere)
                {
                    radius /= 2;
                    radius /= 4;
                }

                if (sphereType == SphereType.Scanconesphere)
                {
                    radius /= 2;
                }

                var resPath = SpheretypeResPath[sphereType];
                var sphere = resMan.Call("LoadObject", resPath).Call("CopyTo");
                var trinity = PySharp.Import("trinity");
                var eveRootTransform = DirectEve.PySharp.CreateInstance(trinity["EveRootTransform"]);

                var position = PyObject.CreateTuple(PySharp, x, y, z);
                var scaling = PyObject.CreateTuple(PySharp, radius, radius, radius);
                sphere.SetAttribute("translation", position);
                sphere.SetAttribute("scaling", scaling);
                eveRootTransform.SetAttribute("name", name + "_CCPEndorsedRenderObject");
                eveRootTransform["children"].Call("append", sphere);
                if (ball != null)
                {
                    eveRootTransform.SetAttribute("translationCurve", ball);
                    eveRootTransform.SetAttribute("rotationCurve", ball);
                }

                //Console.WriteLine("Adding " + name + "_CCPEndorsedRenderObject" + " to scene.");
                DirectEve.SceneManager.DefaultScene["objects"].Call("append", eveRootTransform);
            }
        }

        public void DrawModel(float scaling, string redPath, float x, float y, float z, PyObject ball = null)
        {

            var resMan = PySharp.Import("blue")["resMan"];
            if (resMan.IsValid)
            {
                var sphere = resMan.Call("LoadObject", redPath).Call("CopyTo");
                var trinity = PySharp.Import("trinity");
                var eveRootTransform = DirectEve.PySharp.CreateInstance(trinity["EveRootTransform"]);
                Console.WriteLine("DrawModel: " + redPath);
                var position = PyObject.CreateTuple(PySharp, x, y, z);
                var scalingTuple = PyObject.CreateTuple(PySharp, scaling, scaling, scaling);
                sphere.SetAttribute("translation", position);
                sphere.SetAttribute("scaling", scalingTuple);
                eveRootTransform.SetAttribute("name", redPath + "_CCPEndorsedRenderObject");
                eveRootTransform["children"].Call("append", sphere);
                if (ball != null)
                {
                    eveRootTransform.SetAttribute("translationCurve", ball);
                    eveRootTransform.SetAttribute("rotationCurve", ball);
                }

                DirectEve.SceneManager.DefaultScene["objects"].Call("append", eveRootTransform);
            }
        }

        public PyObject CreatePyTupleFromVec3(Vec3 v)
        {
            return PyObject.CreateTuple(DirectEve.PySharp, (float)v.X, (float)v.Y, (float)v.Z);
        }

        public void DrawBox(DirectMiniBox miniBox, PyObject ball = null)
        {
            var resMan = PySharp.Import("blue")["resMan"];
            if (resMan.IsValid)
            {
                var box = resMan.Call("LoadObject", "res:/Model/Global/Minibox.red").Call("CopyTo");
                var trinity = PySharp.Import("trinity");
                var eveRootTransform = DirectEve.PySharp.CreateInstance(trinity["EveRootTransform"]);


                var name = miniBox.XYZAxis + "_CCPEndorsedRenderObject";
                eveRootTransform.SetAttribute("name", name);

                if (ball != null)
                {
                    eveRootTransform.SetAttribute("translationCurve", ball);
                    eveRootTransform.SetAttribute("rotationCurve", ball);
                }

                box.SetAttribute("translation", CreatePyTupleFromVec3(miniBox.Center));


                box.SetAttribute("scaling",
                    CreatePyTupleFromVec3(new Vec3((float)miniBox.XAxis.Magnitude, (float)miniBox.YAxis.Magnitude,
                        (float)miniBox.ZAxis.Magnitude)));


                var rotation = PyObject.CreateTuple(PySharp, miniBox.RotationQuaternionNormalized.X,
                    miniBox.RotationQuaternionNormalized.Y,
                    miniBox.RotationQuaternionNormalized.Z, miniBox.RotationQuaternionNormalized.W);


                box.SetAttribute("rotation", rotation);

                eveRootTransform["children"].Call("append", box);
                DirectEve.SceneManager.DefaultScene["objects"].Call("append", eveRootTransform);
            }
        }


        public void DrawCapsule(DirectMiniCapsule capsule, PyObject ball = null)
        {
            var resMan = PySharp.Import("blue")["resMan"];
            if (resMan.IsValid)
            {
                var box = resMan.Call("LoadObject", "res:/Model/Global/Minicapsule.red").Call("CopyTo");
                var trinity = PySharp.Import("trinity");
                var eveRootTransform = DirectEve.PySharp.CreateInstance(trinity["EveRootTransform"]);

                var name = capsule.AX + capsule.AY + capsule.AZ + capsule.BX + capsule.BY + capsule.BZ +
                           "_CCPEndorsedRenderObject";

                eveRootTransform.SetAttribute("name", name);

                if (ball != null)
                {
                    eveRootTransform.SetAttribute("translationCurve", ball);
                    eveRootTransform.SetAttribute("rotationCurve", ball);
                }

                box.SetAttribute("translation", CreatePyTupleFromVec3(capsule.Center));

                var rotation = PyObject.CreateTuple(PySharp, capsule.RotationQuat.X, capsule.RotationQuat.Y,
                    capsule.RotationQuat.Z, capsule.RotationQuat.W);

                box.SetAttribute("rotation", rotation);

                var children = box["children"].ToList();

                foreach (var child in children)
                {
                    var cName = child["name"].ToUnicodeString();
                    var scaling = child["scaling"].ToList();

                    if (cName == "Cylinder")
                    {
                        var height = capsule.Length * scaling[1].ToDouble();
                        var rscaling = DirectEve.SceneManager.Geo2.Call("Vec3Scale",
                            PyObject.CreateTuple(DirectEve.PySharp, scaling[0].ToDouble(), scaling[2].ToDouble()),
                            capsule.Radius).ToList();
                        child.SetAttribute("scaling",
                            PyObject.CreateTuple(DirectEve.PySharp, rscaling[0].ToDouble(), height,
                                rscaling[1].ToDouble()));
                    }
                    else
                    {
                        var sc = DirectEve.SceneManager.Geo2.Call("Vec3Scale", child["scaling"], capsule.Radius);
                        var length =
                            DirectEve.SceneManager.Geo2.Call("Vec3Scale", child["translation"], capsule.Length);
                        child.SetAttribute("scaling", sc);
                        child.SetAttribute("translation", length);
                    }
                }

                eveRootTransform["children"].Call("append", box);
                DirectEve.SceneManager.DefaultScene["objects"].Call("append", eveRootTransform);
            }
        }

        public void RemoveDrawnObject(string name)
        {
            if (name.EndsWith("_CCPEndorsedRenderObject") == false)
                name += "_CCPEndorsedRenderObject";

            if (DefaultSceneObjectsDict.TryGetValue(name, out var obj))
            {
                DirectEve.SceneManager.DefaultScene["objects"].Call("fremove", obj);
                //Console.WriteLine("Removed " + name + " from scene.");
            }
        }


        public void RemoveAllDrawnObjects()
        {
            DefaultSceneObjectsDict.Keys.Where(e => e.EndsWith("_CCPEndorsedRenderObject")).ToList().ForEach(RemoveDrawnObject);
        }

        public List<string> GetCustomDrawnObjectNames() => DefaultSceneObjectsDict.Keys.Where(x => x.EndsWith("_CCPEndorsedRenderObject")).ToList();

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

            var quat = EulerToQuaternion(yaw, pitch, roll);

            // Rotate the vector using the quaternion
            Vec3 rotatedVec = quat * vec;
            return rotatedVec;
        }

        public static List<Vec3> FibonacciSphereWithinCone(int samples = 1000, double radius = 1, double coneAngle = 45)
        {
            List<Vec3> points = new List<Vec3>();
            double phi = Math.PI * (3.0 - Math.Sqrt(5.0)); // golden angle in radians
            double coneAngleRad = coneAngle * (Math.PI / 180.0); // cone angle in radians

            object lockObj = new object(); // Object for locking when adding points

            Parallel.For(0, samples, i =>
            {
                double y = 1 - (i / (double)(samples - 1)) * 2; // y goes from 1 to -1
                double radiusAtY = Math.Sqrt(1 - y * y); // radius at y

                double theta = phi * i; // golden angle increment

                double x = Math.Cos(theta) * radiusAtY;
                double z = Math.Sin(theta) * radiusAtY;

                Vec3 point = new Vec3(radius * x, radius * y, radius * z);

                // Check if the point is within the cone
                if (Math.Acos(point.Z / point.Magnitude) <= coneAngleRad)
                {
                    lock (lockObj)
                    {
                        points.Add(point.Normalize());
                    }
                }
            });

            return points;
        }

        public static Vec3? GetLineSphereIntersection(Vec3 start, Vec3 end, Vec3 sphereCenter, float sphereRadius)
        {
            Vec3 direction = end - start;
            Vec3 startToCenter = sphereCenter - start;

            var t = startToCenter.DotProduct(direction) / direction.DotProduct(direction);
            Vec3 closestPoint = start + direction * t;

            var distanceToCenter = (closestPoint - sphereCenter).Magnitude;

            if (distanceToCenter <= sphereRadius)
            {
                float offset = (float)Math.Sqrt(sphereRadius * sphereRadius - distanceToCenter * distanceToCenter);

                Vec3 intersection1 = closestPoint - direction.Normalize() * offset;
                Vec3 intersection2 = closestPoint + direction.Normalize() * offset;

                // Check which intersection point is in the forward direction of the line
                if (direction.DotProduct(intersection1 - start) > 0)
                {
                    return intersection1;
                }
                else
                {
                    return intersection2;
                }
            }

            return null; // No intersection
        }

        public static Quaternion GetRotationQuaternion(Vector3 vectorStart, Vector3 vectorEnd)
        {
            vectorStart = Vector3.Normalize(vectorStart);
            vectorEnd = Vector3.Normalize(vectorEnd);
            // Calculate the rotation axis using the cross product
            Vector3 axis = Vector3.Cross(vectorStart, vectorEnd);
            axis = Vector3.Normalize(axis);
            // Calculate the angle between the vectors
            float dotProduct = Vector3.Dot(vectorStart, vectorEnd);
            float angle = (float)Math.Acos(dotProduct);
            // Create the rotation quaternion
            float halfAngle = angle / 2;
            float sinHalfAngle = (float)Math.Sin(halfAngle);
            float cosHalfAngle = (float)Math.Cos(halfAngle);
            Quaternion rotationQuaternion = new Quaternion(axis * sinHalfAngle, cosHalfAngle);
            return rotationQuaternion;
        }


        public Tuple<float, float, float> ProjectWorldToCamera(PyObject vec3)
        {
            var ret = new Tuple<float, float, float>(0, 0, 0);
            var projectWorldToCamera = Camera.Attribute("ProjectWorldToCamera");
            if (projectWorldToCamera.IsValid)
            {
                var r = Camera.Call("ProjectWorldToCamera", vec3);
                // TODO: not yet finished
                DirectEve.Log(r.LogObject());
            }

            return ret;
        }

        /// <summary>
        /// Use relative positions!
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="colA"></param>
        /// <param name="colB"></param>
        /// <param name="colC"></param>
        /// <param name="colD"></param>
        public void DrawLine(Vec3 start, Vec3 end, float colA = 1, float colB = 1, float colC = 0, float colD = 1)
        {
            DrawLineGradient(start, end, new Vector4(colA, colB, colC, colD));
        }

        public void DrawLineGradient(Vec3 start, Vec3 end, Vector4? col1 = null, Vector4? col2 = null,
            float? width = null)
        {
            width ??= 2.0f;
            var uicore = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"];

            var color1 = col1 == null
                ? PySharp.PyNone
                : PyObject.CreateTuple(DirectEve.PySharp, col1.Value.X, col1.Value.Y, col1.Value.Z, col1.Value.W);
            var color2 = col2 == null
                ? PySharp.PyNone
                : PyObject.CreateTuple(DirectEve.PySharp, col2.Value.X, col2.Value.Y, col2.Value.Z, col2.Value.W);

            if (col1.HasValue && !col2.HasValue)
                color2 = color1;

            var pos1 = PyObject.CreateTuple(DirectEve.PySharp, (float)start.X, (float)start.Y, (float)start.Z);
            var pos2 = PyObject.CreateTuple(DirectEve.PySharp, (float)end.X, (float)end.Y, (float)end.Z);
            uicore.Call("DrawDebugLine", pos1, pos2, (float)width, color1, color2);
            //DirectEve.ThreadedCall(uicore["DrawDebugLine"], pos1, pos2, (float)width, color1, color2);
        }

        public void DrawLineGradientAbsolute(Vec3 start, Vec3 end, Vector4? col1 = null, Vector4? col2 = null,
            float? width = null)
        {
            if (DirectEve.ActiveShip.Entity == null)
                return;

            var pos = DirectEve.ActiveShip.Entity.DirectAbsolutePosition.GetVector();
            DrawLineGradient(start - pos, end - pos, col1, col2, width);
        }

        public void ClearDebugLines()
        {
            var uicore = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"];
            if (uicore["debugLineSet"].IsValid)

                uicore.Call("ClearDebugLines");
            //DirectEve.ThreadedCall(uicore["ClearDebugLines"]);
        }
    }

    //public PyObject World2Screen(PyObject vec)
    //{
    //    return Camera.Call("ProjectWorldToScreen", vec);
    //}

    //public Matrix4x4 ViewMatrixTransform { get; private set; }
}
