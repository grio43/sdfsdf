extern alias SC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;


    using System;
    using System.Numerics;

    public class DirectMiniCapsule : IGeometry
    {
        public float AX { get; }
        public float AY { get; }
        public float AZ { get; }
        public float BX { get; }
        public float BY { get; }
        public float BZ { get; }
        public double Radius { get; }

        public Vec3 PointA { get; }
        public Vec3 PointB { get; }

        public Vec3 Center { get; }
        public Vec3 Direction { get; }

        public double Length { get; }

        public double LengthSquared { get; }

        public double Pitch { get; }
        public double Yaw { get; }
        public double Roll { get; }
        public bool Traversable { get; }
        public Quaternion RotationQuat { get; }

        public double RadiusSquared { get; }
        public double MaxBoundingRadius { get; }

        public double MaxBoundingRadiusSquared { get; }

        public DirectMiniCapsule(PyObject mb)
        {
            AX = mb["ax"].ToFloat();
            AY = mb["ay"].ToFloat();
            AZ = mb["az"].ToFloat();

            BX = mb["bx"].ToFloat();
            BY = mb["by"].ToFloat();
            BZ = mb["bz"].ToFloat();
            Radius = mb["radius"].ToFloat();
            RadiusSquared = Radius * Radius;
            Traversable = false;
            PointA = new Vec3(AX, AY, AZ);
            PointB = new Vec3(BX, BY, BZ);
            Direction = PointA - PointB;
            Length = Direction.Magnitude;
            LengthSquared = Length * Length;
            Center = (PointA + PointB) * 0.5;
            Pitch = Math.Acos(Direction[1] / Length);
            Yaw = Math.Atan2(Direction[0], Direction[2]);
            Roll = 0;
            RotationQuat = Quaternion.CreateFromYawPitchRoll((float)Yaw, (float)Pitch, (float)Roll);
            MaxBoundingRadius = Radius + Length * 0.5;
            MaxBoundingRadiusSquared = MaxBoundingRadius * MaxBoundingRadius;
        }

        public DirectMiniCapsule(float ax, float ay, float az, float bx, float by, float bz, float radius)
        {
            AX = ax;
            AY = ay;
            AZ = az;
            BX = bx;
            BY = by;
            BZ = bz;
            Radius = radius;
            RadiusSquared = Radius * Radius;
            PointA = new Vec3(AX, AY, AZ);
            PointB = new Vec3(BX, BY, BZ);
            Direction = PointA - PointB;
            Length = Direction.Magnitude;
            LengthSquared = Length * Length;
            Center = (PointA + PointB) * 0.5;
            Pitch = Math.Acos(Direction[1] / Length);
            Yaw = Math.Atan2(Direction[0], Direction[2]);
            Roll = 0;
            RotationQuat = Quaternion.CreateFromYawPitchRoll((float)Yaw, (float)Pitch, (float)Roll);
            MaxBoundingRadius = Radius + Length * 0.5;
            MaxBoundingRadiusSquared = MaxBoundingRadius * MaxBoundingRadius;
            Traversable = false;
        }


        public bool SphereContains(Vec3 spherePos, Vec3 point)
        {
            return Vec3.DistanceSquared(spherePos, point) <= RadiusSquared;
        }

        private double ProjectPointOnCenterLine(Vec3 A, Vec3 B, Vec3 pt)
        {
            var range = B - A;

            return (pt.DotProduct(range) - A.DotProduct(range)) / range.DotProduct(range);
        }

        private double DistanceFromCenterLine(Vec3 A, Vec3 B, Vec3 pt, double tmin)
        {
            var range = B - A;
            var linePt = ParametricPointOnCenterLine(A, B, tmin);
            var diff = linePt - pt;
            return diff.DotProduct(diff);
        }

        private Vec3 ParametricPointOnCenterLine(Vec3 A, Vec3 B, double t)
        {
            var range = B - A;
            return new Vec3(A.X + t * range.X, A.Y + t * range.Y, A.Z + t * range.Z);
        }

        /// Squared distance between two line segments
        /// <param name="line1Point1"></param>
        /// <param name="line1Point2"></param>
        /// <param name="line2Point1"></param>
        /// <param name="line2Point2"></param>
        /// <returns></returns>
        public static double SquaredDistanceBetweenLineSegments(Vec3 line1Point1, Vec3 line1Point2, Vec3 line2Point1, Vec3 line2Point2)
        {
            // Calculate direction vectors for both line segments
            Vec3 line1Direction = new Vec3(line1Point2.X - line1Point1.X, line1Point2.Y - line1Point1.Y, line1Point2.Z - line1Point1.Z);
            Vec3 line2Direction = new Vec3(line2Point2.X - line2Point1.X, line2Point2.Y - line2Point1.Y, line2Point2.Z - line2Point1.Z);

            // Calculate the vector between the two starting points
            Vec3 startVector = new Vec3(line1Point1.X - line2Point1.X, line1Point1.Y - line2Point1.Y, line1Point1.Z - line2Point1.Z);

            // Calculate intermediate values
            double a = line1Direction.DotProduct(line1Direction);
            double b = line1Direction.DotProduct(line2Direction);
            double c = line2Direction.DotProduct(line2Direction);
            double d = line1Direction.DotProduct(startVector);
            double e = line2Direction.DotProduct(startVector);

            // Calculate the denominator for the parametric values
            double denominator = a * c - b * b;

            // Check if lines are parallel
            if (denominator == 0)
            {
                // Lines are parallel, find squared distance between the start points
                double squaredDist1 = startVector.DotProduct(startVector);
                double squaredDist2 = (line1Point2.X - line2Point1.X) * (line1Point2.X - line2Point1.X) +
                                      (line1Point2.Y - line2Point1.Y) * (line1Point2.Y - line2Point1.Y) +
                                      (line1Point2.Z - line2Point1.Z) * (line1Point2.Z - line2Point1.Z);

                double squaredDist3 = (line1Point1.X - line2Point2.X) * (line1Point1.X - line2Point2.X) +
                                      (line1Point1.Y - line2Point2.Y) * (line1Point1.Y - line2Point2.Y) +
                                      (line1Point1.Z - line2Point2.Z) * (line1Point1.Z - line2Point2.Z);

                return Math.Min(squaredDist1, Math.Min(squaredDist2, squaredDist3));
            }

            // Calculate the parametric values of the closest points on the lines
            double t1 = (b * e - c * d) / denominator;
            double t2 = (a * e - b * d) / denominator;

            // Clamp t1 and t2 to lie within the line segments
            t1 = Math.Max(0, Math.Min(1, t1));
            t2 = Math.Max(0, Math.Min(1, t2));

            // Calculate the closest points on each line segment
            Vec3 closestPointOnLine1 = new Vec3(line1Point1.X + t1 * line1Direction.X, line1Point1.Y + t1 * line1Direction.Y, line1Point1.Z + t1 * line1Direction.Z);
            Vec3 closestPointOnLine2 = new Vec3(line2Point1.X + t2 * line2Direction.X, line2Point1.Y + t2 * line2Direction.Y, line2Point1.Z + t2 * line2Direction.Z);

            // Calculate and return the squared distance between the closest points
            return (closestPointOnLine2 - closestPointOnLine1).Magnitude * (closestPointOnLine2 - closestPointOnLine1).Magnitude;
        }

        public bool IsLineWithinCapsule(Vec3 start, Vec3 end, Vec3? sourcePos = null)
        {
            // Translate the start and end points relative to the capsule's center

            if (sourcePos.HasValue)
            {
                start -= sourcePos.Value;
                end -= sourcePos.Value;
            }

            var dist = Math.Abs(SquaredDistanceBetweenLineSegments(start, end, PointA, PointB));

            //Console.WriteLine($"dist {dist} RadiusSquared {RadiusSquared}");

            if (dist <= RadiusSquared)
            {
                return true;
            }

            return false;
        }


        public bool IsPointWithin(Vec3 point, Vec3 sourcePos)
        {
            point -= sourcePos;
            if (SphereContains(PointA, point) || SphereContains(PointB, point))
            {
                return true;
            }

            var tmin = ProjectPointOnCenterLine(PointA, PointB, point);

            if (tmin < 0f || tmin > 1f)
                return false;

            var dist = DistanceFromCenterLine(PointA, PointB, point, tmin);

            if (dist <= RadiusSquared)
                return true;

            return false;
        }
    }
}