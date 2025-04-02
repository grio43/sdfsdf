extern alias SC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Media.Animation;
using EVESharpCore.Cache;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using SharpDX;

namespace EVESharpCore.Framework
{
    public class DirectWorldPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public bool DirectPathFlag { get; set; }
        public int Visits { get; set; }

        public override string ToString()
        {
            return $"{X}|{Y}|{Z}";
        }

        public DirectWorldPosition(double x, double y, double z)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
        }

        public DirectWorldPosition(Vec3 vec)
        {
            this.X = vec.X;
            this.Y = vec.Y;
            this.Z = vec.Z;
        }

        public DirectWorldPosition(double x, double y, double z, bool flag)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.DirectPathFlag = flag;
        }

        public override bool Equals(object obj)
        {
            var tolerance = 0.5;
            if (object.ReferenceEquals(obj, null))
            {
                return false;
            }

            if (obj is DirectWorldPosition k)
            {
                return Math.Abs(X - k.X) < tolerance && Math.Abs(Y - k.Y) < tolerance && Math.Abs(Z - k.Z) < tolerance;
            }

            return false;
        }

        public bool Equals(object obj, double tolerance)
        {
            return Equals(obj);
        }

        public static bool operator ==(DirectWorldPosition a, DirectWorldPosition b)
        {
            if (object.ReferenceEquals(a, null))
            {
                if (object.ReferenceEquals(b, null))
                    return true;

                return false;
            }

            return a?.Equals(b) ?? false;
        }

        public static bool operator !=(DirectWorldPosition a, DirectWorldPosition b)
        {
            return !(a == b);
        }

        public static double GetDistance(DirectWorldPosition from, DirectWorldPosition to)
        {
            var deltaX = to.X - from.X;
            var deltaY = to.Y - from.Y;
            var deltaZ = to.Z - from.Z;
            var distance = Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return distance;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + X.GetHashCode();
                hash = hash * 23 + Y.GetHashCode();
                hash = hash * 23 + Z.GetHashCode();
                return hash;
            }
        }


        public static void DrawPath(List<DirectWorldPosition> path, bool clearDebugLines = true, bool skipFirst = true, bool isCircular = true)
        {

            if (path.Count < 2)
                return;

            var me = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (me != null)
            {
                var meWorldPos = me.DirectAbsolutePosition;

                if (clearDebugLines)
                    ESCache.Instance.DirectEve.SceneManager.ClearDebugLines();

                var prev = me.BallPos;

                if (isCircular)
                    path.Add(path.First());

                var n = 0;
                foreach (var waypoint in path)
                {
                    var wpPos = meWorldPos.GetDirectionalVectorTo(waypoint);
                    if (n != 0 && skipFirst)
                        ESCache.Instance.DirectEve.SceneManager.DrawLine(prev, wpPos);
                    prev = wpPos;
                    n++;
                }
            }
        }

        private static Random _rnd = new Random();


        public static void OnSessionChange()
        {
            _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();
        }

        private static Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)> _orbitCache = new Dictionary<DirectWorldPosition, (double, List<DirectWorldPosition>, List<DirectWorldPosition>)>();

        public bool Orbit(double radius, bool clearDebugLines = false, bool humanize = true, Vec3? radiusVector = null, Range<double> humanizeFactor = null)
        {
            var key = _orbitCache.Keys.Where(e => this.Equals(e, 2500)).FirstOrDefault();
            var activeShip = ESCache.Instance.DirectEve.ActiveShip.Entity;
            if (key != null && _orbitCache[key].Item1 == radius && _orbitCache[key].Item2.Any())
            {
                //if (!_orbitCache[key].Item2.Any())
                //{
                //    // If it exists already, re-use the previously generated path
                //    _orbitCache[key] = (radius, _orbitCache[key].Item3.ToList(), _orbitCache[key].Item3.ToList());
                //}
            }
            else
            {
                // Generate a path if there is none yet
                var p = GetRandomOrbitPath(radius, 10, humanize, radiusVector, humanizeFactor);

                // Find the closest point to us and start from there
                var closest = p.OrderBy(e => e.GetDistance(activeShip.DirectAbsolutePosition)).First();
                var index = p.IndexOf(closest);
                p = p.Skip(index).Concat(p.Take(index)).ToList();

                // Ensure we go the correct way around the orbit, the last point should be the second closes else reverse
                if (p.Last().GetDistance(activeShip.DirectAbsolutePosition) > p[1].GetDistance(activeShip.DirectAbsolutePosition))
                {
                    p.Reverse();
                }

                _orbitCache[this] = (radius, p.ToList(), p.ToList());
                key = this;
                ESCache.Instance.DirectEve.Log($"Generated a new path with length {p.Count} for {this} with radius {radius}.");
            }

            var entry = _orbitCache[key];

            // The current path, where we also remove items from
            var path = entry.Item2;

           
            var current = path.FirstOrDefault();

            // If we are too far away, return false
            if (current.GetDistance(activeShip.DirectAbsolutePosition) > radius * 1.5)
            {
                ESCache.Instance.DirectEve.Log("We are too far away.");
                return false;
            }

            // Draw the original path
            DrawPath(entry.Item3.ToList(), clearDebugLines: clearDebugLines);

            // If we are close pick the next wp and move to it
            if (current.GetDistance(activeShip.DirectAbsolutePosition) < Math.Min(radius / 2, 50_000))
            {
                if (DirectEve.Interval(10000))
                    ESCache.Instance.DirectEve.Log($"Removed a waypoint. {current}. Remaining [{path.Count}]");
                path.Remove(current);
                current = path.FirstOrDefault();
            }

            if (current != null)
            {
                ESCache.Instance.DirectEve.ActiveShip.MoveTo(current);
                return true;
            }

            return false;
        }

        private double RandomBetween(double smallNumber, double bigNumber)
        {
            return _rnd.NextDouble() * (bigNumber - smallNumber) + (smallNumber);
        }

        public List<DirectWorldPosition> GetRandomOrbitPath(double radius, int numPoints, bool humanize = true, Vec3? radiusVector = null, Range<double> humanizeFactor = null)
        {
            List<DirectWorldPosition> path = new List<DirectWorldPosition>();
            // Generate a random normal vector to define the plane of the orbit
            Vec3 planeNormal = new Vec3(_rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5, _rnd.NextDouble() - 0.5).Normalize();
            if (radiusVector != null)
            {
                // Generate a normal vector which is orthogonal to the radius vector
                planeNormal = radiusVector.Value.CrossProduct(planeNormal).Normalize();
            }

            // Generate two vectors that are orthogonal to the plane normal
            Vec3 tangent = new Vec3(planeNormal.Y, -planeNormal.X, 0.0).Normalize();
            if (tangent.Magnitude < 0.0001)
                tangent = new Vec3(0.0, planeNormal.Z, -planeNormal.Y).Normalize();
            Vec3 bitangent = planeNormal.CrossProduct(tangent).Normalize();

            // Generate the path points by rotating a vector around the plane

            if (!humanize)
            {
                double angleIncrement = 2 * Math.PI / numPoints;
                for (int i = 0; i < numPoints; i++)
                {
                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    Vec3 pointInSpace = pointOnPlane * radius + new Vec3(X, Y, Z);

                    path.Add(new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z));
                }
            }
            else
            {
                double angleIncrement = 2 * Math.PI / numPoints;
                for (int i = 0; i < numPoints; i++)
                {
                    double angle = i * angleIncrement;
                    Vec3 pointOnPlane = tangent * Math.Cos(angle) + bitangent * Math.Sin(angle);
                    var min = humanizeFactor?.Min ?? 0.75d;
                    var max = humanizeFactor?.Max ?? 1.0d;
                    Vec3 pointInSpace = pointOnPlane * (radius * RandomBetween(min, max)) + new Vec3(X, Y, Z);

                    path.Add(new DirectWorldPosition(pointInSpace.X, pointInSpace.Y, pointInSpace.Z));
                }
            }
            return path;
        }

        public List<DirectWorldPosition> GenerateNeighbours(int stepSize, DirectWorldPosition dest)
        {
            var ret = new List<DirectWorldPosition>()
            {
                new DirectWorldPosition(X + stepSize, Y + stepSize, Z + stepSize),
                new DirectWorldPosition(X + stepSize, Y + stepSize, Z - stepSize),
                new DirectWorldPosition(X + stepSize, Y - stepSize, Z + stepSize),
                new DirectWorldPosition(X + stepSize, Y - stepSize, Z - stepSize),
                new DirectWorldPosition(X - stepSize, Y + stepSize, Z + stepSize),
                new DirectWorldPosition(X - stepSize, Y + stepSize, Z - stepSize),
                new DirectWorldPosition(X - stepSize, Y - stepSize, Z + stepSize),
                new DirectWorldPosition(X - stepSize, Y - stepSize, Z - stepSize)
            };
            return ret;
        }

        public double? GetDistance(DirectWorldPosition to, double modifier = 0, double offsetX = 0, double offsetY = 0,
            double offsetZ = 0)
        {
            try
            {
                var deltaX = to.X - X + offsetX;
                var deltaY = to.Y - Y + offsetY;
                var deltaZ = to.Z - Z + offsetZ;
                var distance = Math.Sqrt(
                    deltaX * deltaX +
                    deltaY * deltaY +
                    deltaZ * deltaZ);

                if (modifier != 0)
                    return distance * modifier;

                return distance;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetDist Overflow: {e}");
                return null;
            }
        }

        public Vec3 GetVector()
        {
            return new Vec3(this.X, this.Y, this.Z);
        }

        public Vec3 GetDirectionalVectorTo(DirectWorldPosition to)
        {
            return new Vec3(to.X - this.X, to.Y - this.Y, to.Z - this.Z);
        }

        public double? GetInverseDistanceSquared(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            return 1 / GetDistanceSquared(to, modifier, offsetX, offsetY, offsetZ);
        }

        public double? GetInverseDistance(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            return 1 / GetDistance(to, modifier, offsetX, offsetY, offsetZ);
        }

        public double? GetDistanceSquared(DirectWorldPosition to, double modifier = 0, double offsetX = 0,
            double offsetY = 0, double offsetZ = 0)
        {
            try
            {
                var deltaX = to.X - X + offsetX;
                var deltaY = to.Y - Y + offsetY;
                var deltaZ = to.Z - Z + offsetZ;
                var distance = (deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);

                if (modifier != 0)
                    return distance * modifier;

                return distance;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetDistanceSquared Overflow: {e}");
                return null;
            }
        }
    }
}