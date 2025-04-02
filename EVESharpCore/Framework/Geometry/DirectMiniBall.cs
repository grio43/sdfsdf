extern alias SC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;
    using System;

    public class DirectMiniBall : IGeometry
    {
        public double X { get; }
        public double Y { get; }
        public double Z { get; }
        public double Radius { get; }
        public double RadiusSquared { get; }
        public bool Traversable { get; }
        public Vec3 Center => new Vec3(X, Y, Z);

        public double MaxBoundingRadius { get; }

        public double MaxBoundingRadiusSquared { get; }

        public DirectMiniBall(PyObject mb)
        {
            X = mb["x"].ToDouble();
            Y = mb["y"].ToDouble();
            Z = mb["z"].ToDouble();
            Radius = mb["radius"].ToDouble();
            RadiusSquared = Radius * Radius;
            MaxBoundingRadius = Radius;
            MaxBoundingRadiusSquared = RadiusSquared;
            Traversable = false;
        }

        public DirectMiniBall(double x, double y, double z, double radius, bool traversable = false)
        {
            X = x;
            Y = y;
            Z = z;
            Radius = radius;
            RadiusSquared = radius * radius;
            Traversable = traversable;
            MaxBoundingRadius = Radius;
            MaxBoundingRadiusSquared = RadiusSquared;
        }

        public bool IsPointWithin(Vec3 p)
        {
            return IsPointWithin(p, new Vec3(0, 0, 0));
        }

        public bool IsPointWithin(Vec3 p, Vec3 ballRelative)
        {
            return Vec3.DistanceSquared(p, ballRelative + new Vec3(X, Y, Z)) <= RadiusSquared;
        }
        public bool IsPointWithin(DirectWorldPosition p, DirectWorldPosition ballRelative)
        {
            return Vec3.DistanceSquared(p.GetVector(), ballRelative.GetVector() + new Vec3(X, Y, Z)) < RadiusSquared;
        }

        public override string ToString()
        {
            return $"X: {X}, Y: {Y}, Z: {Z}, Radius: {Radius}";
        }

        public bool LineIntersectsX(Vec3 startPoint, Vec3 targetPoint, Vec3? offset = null)
        {

            if (offset.HasValue)
            {
                startPoint -= offset.Value;
                targetPoint -= offset.Value;
            }

            Vec3 v1 = new Vec3(startPoint.X, startPoint.Y, startPoint.Z);
            Vec3 v2 = new Vec3(targetPoint.X, targetPoint.Y, targetPoint.Z);
            Vec3 vc = new Vec3(X, Y, Z);

            double radius = Radius;

            // Normalize the direction vector to improve precision
            Vec3 direction = (v2 - v1);

            // Check if the direction vector has a non-zero magnitude
            if (direction.Magnitude == 0)
            {
                // Handle the case where the ball is at the origin
                return v1.Magnitude <= radius || v2.Magnitude <= radius;
            }

            direction = direction.Normalize();

            // Calculate the scale factor to bring the coordinates closer to the origin
            double maxComponent = Math.Max(Math.Max(Math.Abs(X), Math.Abs(Y)), Math.Abs(Z));
            double scaleFactor = maxComponent != 0 ? maxComponent : 1.0;
            v1 /= scaleFactor;
            v2 /= scaleFactor;
            vc /= scaleFactor;
            radius /= scaleFactor;

            Vec3 A = v1 - vc;
            Vec3 B = v2 - vc;
            Vec3 C = v1 - v2;

            // Compute the squared magnitudes to avoid square root calculations
            double squaredMagnitudeA = A.Magnitude * A.Magnitude;
            double squaredMagnitudeB = B.Magnitude * B.Magnitude;

            if (squaredMagnitudeA < radius * radius || squaredMagnitudeB < radius * radius)
            {
                return true;
            }

            // Check if either A or B has a zero magnitude (to handle undefined angle)
            if (A.Magnitude == 0 || B.Magnitude == 0)
            {
                return true;
            }

            // Check if the angle between A and B is less than 90 degrees
            if (Vec3.AngleBetween(A, B) < Math.PI / 2)
            {
                return false;
            }

            // Calculate the perpendicular distance H from the line segment AB to the center of the sphere
            double H = Math.Sqrt(squaredMagnitudeA * (1 - Math.Pow(Vec3.UnitProjection(A, C), 2)));

            if (H < radius)
            {
                return true;
            }

            return false;
        }


        public bool LineIntersects(Vec3 A, Vec3 B, Vec3? offset = null)
        {
            if (offset != null)
            {
                A -= offset.Value;
                B -= offset.Value;
            }
            var C = Center;

            // Check if A or B is inside the sphere by comparing squared distances
            Vec3 diffA = A - C;
            Vec3 diffB = B - C;

            double distASquared = Vec3.DotProduct(diffA, diffA);
            double distBSquared = Vec3.DotProduct(diffB, diffB);

            if (distASquared < RadiusSquared || distBSquared < RadiusSquared)
            {
                return true;
            }

            // Compute direction vector of the line
            Vec3 d = B - A;

            // Coefficients for the quadratic equation
            double a = Vec3.DotProduct(d, d);
            double b = 2 * Vec3.DotProduct(d, diffA);
            double c = distASquared - RadiusSquared;

            // Discriminant
            double delta = b * b - 4 * a * c;

            // If delta is negative, there's no intersection
            if (delta < 0)
            {
                return false;
            }

            // Calculate conditions for t values without computing the square root twice
            double sqrtDelta = Math.Sqrt(delta);
            double t1_condition = (-b - sqrtDelta) / (2 * a);
            double t2_condition = (-b + sqrtDelta) / (2 * a);

            // If both t values are outside the range [0, 1], there's no intersection
            if ((t1_condition < 0 && t2_condition < 0) || (t1_condition > 1 && t2_condition > 1))
            {
                return false;
            }
            return true;
        }
    }
}