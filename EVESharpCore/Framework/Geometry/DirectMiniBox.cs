extern alias SC;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;


    using System;
    using System.Collections.Generic;
    using System.Numerics;

    public class DirectMiniBox : IGeometry
    {
        public float X0 { get; private set; }
        public float X1 { get; private set; }
        public float X2 { get; private set; }

        public float Y0 { get; private set; }
        public float Y1 { get; private set; }
        public float Y2 { get; private set; }

        public float Z0 { get; private set; }
        public float Z1 { get; private set; }
        public float Z2 { get; private set; }
        public float C0 { get; private set; }
        public float C1 { get; private set; }
        public float C2 { get; private set; }

        public Vec3 XAxis { get; private set; }
        public Vec3 YAxis { get; private set; }
        public Vec3 ZAxis { get; private set; }
        public float XAxisLength { get; private set; }
        public float YAxisLength { get; private set; }
        public float ZAxisLength { get; private set; }
        public Vec3 XYZAxis { get; private set; }
        public Vec3 Corner { get; private set; }
        public Vec3 Center { get; private set; }
        public double Radius { get; private set; }
        public Vec3 P1 { get; private set; }
        public Vec3 P2 { get; private set; }
        public Vec3 P3 { get; private set; }
        public Vec3 P4 { get; private set; }
        public Vec3 P5 { get; private set; }
        public Vec3 P6 { get; private set; }
        public Vec3 P7 { get; private set; }
        public Vec3 P8 { get; private set; }

        public Matrix4x4 RotationMatrix { get; private set; }
        public Quaternion RotationQuaternion { get; private set; }
        public Quaternion RotationQuaternionNormalized { get; private set; }

        public Quaternion RotationQuaternionNormalizedConjugate { get; private set; }

        public Vec3 XNormalizedV3 { get; private set; }
        public Vec3 YNormalizedV3 { get; private set; }
        public Vec3 ZNormalizedV3 { get; private set; }
        public bool Traversable { get; }

        public double MaxBoundingRadius { get; set; }

        public double MaxBoundingRadiusSquared { get; set; }

        public DirectMiniBox(float x0, float x1, float x2, float y0, float y1, float y2, float z0, float z1, float z2, float c0, float c1, float c2)
        {
            X0 = x0;
            X1 = x1;
            X2 = x2;
            Y0 = y0;
            Y1 = y1;
            Y2 = y2;
            Z0 = z0;
            Z1 = z1;
            Z2 = z2;
            C0 = c0;
            C1 = c1;
            C2 = c2;
            Traversable = false;

            Init();
        }

        public DirectMiniBox(PyObject mb)
        {
            X0 = mb["x0"].ToFloat();
            X1 = mb["x1"].ToFloat();
            X2 = mb["x2"].ToFloat();

            Y0 = mb["y0"].ToFloat();
            Y1 = mb["y1"].ToFloat();
            Y2 = mb["y2"].ToFloat();

            Z0 = mb["z0"].ToFloat();
            Z1 = mb["z1"].ToFloat();
            Z2 = mb["z2"].ToFloat();

            C0 = mb["c0"].ToFloat();
            C1 = mb["c1"].ToFloat();
            C2 = mb["c2"].ToFloat();
            Traversable = false;
            Init();
        }

        private void Init()
        {
            XAxis = new Vec3(X0, X1, X2);
            YAxis = new Vec3(Y0, Y1, Y2);
            ZAxis = new Vec3(Z0, Z1, Z2);


            XAxisLength = (float)XAxis.Magnitude;
            YAxisLength = (float)YAxis.Magnitude;
            ZAxisLength = (float)ZAxis.Magnitude;
            Corner = new Vec3(C0, C1, C2);


            XYZAxis = XAxis + (YAxis + ZAxis);
            Center = Corner + new Vec3(XYZAxis.X * 0.5, XYZAxis.Y * 0.5, XYZAxis.Z * 0.5);
            Radius = (Corner - Center).Magnitude;

            MaxBoundingRadius = Radius;
            MaxBoundingRadiusSquared = MaxBoundingRadius * MaxBoundingRadius;

            XNormalizedV3 = XAxis.Normalize();
            YNormalizedV3 = YAxis.Normalize();
            ZNormalizedV3 = ZAxis.Normalize();

            RotationMatrix = new Matrix4x4((float)XNormalizedV3[0], (float)XNormalizedV3[1], (float)XNormalizedV3[2],
                (float)0,
                (float)YNormalizedV3[0], (float)YNormalizedV3[1], (float)YNormalizedV3[2], (float)0,
                (float)ZNormalizedV3[0], (float)ZNormalizedV3[1], (float)ZNormalizedV3[2], (float)0,
                (float)0, (float)0, (float)0, (float)1);

            RotationQuaternion = Quaternion.CreateFromRotationMatrix(RotationMatrix);
            RotationQuaternionNormalized = Quaternion.Normalize(RotationQuaternion);
            RotationQuaternionNormalizedConjugate = Quaternion.Conjugate(RotationQuaternionNormalized);


            var q = RotationQuaternionNormalized;
            //var q = Quaternion.Identity;


            var xAxisLengthHalf = XAxisLength / 2;
            var yAxisLengthHalf = YAxisLength / 2;
            var zAxisLengthHalf = ZAxisLength / 2;

            P1 = (q * new Vec3(+xAxisLengthHalf, +yAxisLengthHalf, +zAxisLengthHalf)) + Center;
            P2 = (q * new Vec3(+xAxisLengthHalf, +yAxisLengthHalf, -zAxisLengthHalf)) + Center;
            P3 = (q * new Vec3(+xAxisLengthHalf, -yAxisLengthHalf, +zAxisLengthHalf)) + Center;
            P4 = (q * new Vec3(+xAxisLengthHalf, -yAxisLengthHalf, -zAxisLengthHalf)) + Center;
            P5 = (q * new Vec3(-xAxisLengthHalf, +yAxisLengthHalf, +zAxisLengthHalf)) + Center;
            P6 = (q * new Vec3(-xAxisLengthHalf, +yAxisLengthHalf, -zAxisLengthHalf)) + Center;
            P7 = (q * new Vec3(-xAxisLengthHalf, -yAxisLengthHalf, +zAxisLengthHalf)) + Center;
            P8 = (q * new Vec3(-xAxisLengthHalf, -yAxisLengthHalf, -zAxisLengthHalf)) + Center;

            //// Reverse rotation and translation to bring the points back to their local space
            //P1 = (RotationQuaternionNormalizedConjugate * (P1 - Center)) + Center;
            //P2 = (RotationQuaternionNormalizedConjugate * (P2 - Center)) + Center;
            //P3 = (RotationQuaternionNormalizedConjugate * (P3 - Center)) + Center;
            //P4 = (RotationQuaternionNormalizedConjugate * (P4 - Center)) + Center;
            //P5 = (RotationQuaternionNormalizedConjugate * (P5 - Center)) + Center;
            //P6 = (RotationQuaternionNormalizedConjugate * (P6 - Center)) + Center;
            //P7 = (RotationQuaternionNormalizedConjugate * (P7 - Center)) + Center;
            //P8 = (RotationQuaternionNormalizedConjugate * (P8 - Center)) + Center;

            //// Re - apply the original rotation and translation
            //P1 = (RotationQuaternionNormalized * (P1 - Center)) + Center;
            //P2 = (RotationQuaternionNormalized * (P2 - Center)) + Center;
            //P3 = (RotationQuaternionNormalized * (P3 - Center)) + Center;
            //P4 = (RotationQuaternionNormalized * (P4 - Center)) + Center;
            //P5 = (RotationQuaternionNormalized * (P5 - Center)) + Center;
            //P6 = (RotationQuaternionNormalized * (P6 - Center)) + Center;
            //P7 = (RotationQuaternionNormalized * (P7 - Center)) + Center;
            //P8 = (RotationQuaternionNormalized * (P8 - Center)) + Center;


            //    p6 +--------+ p2
            //      /        /|
            //     /        / |
            // p5 +--------+p1|
            //    |        |  |
            //    |  p8 ---|- + p4 
            //    | /      | /
            //    |/       |/
            // p7 +--------+ p3
        }

        public bool IsPointWithin(Vec3 px, Vec3 sourcePos)
        {
            var d = px - Center - sourcePos;

            var ret = Math.Abs(d.DotProduct(XNormalizedV3)) <= XAxisLength / 2 &&
                      Math.Abs(d.DotProduct(YNormalizedV3)) <= YAxisLength / 2 &&
                      Math.Abs(d.DotProduct(ZNormalizedV3)) <= ZAxisLength / 2;
            return ret;
        }
        public bool IsLineIntersecting(Vec3 lineStart, Vec3 lineEnd, Vec3? offset = null)
        {
            if (offset.HasValue)
            {
                lineStart -= offset.Value;
                lineEnd -= offset.Value;
            }

            // Apply the inverse of the box's rotation to bring the box's axes back to their original orientation
            Vec3 inverseXAxis = Quaternion.Inverse(RotationQuaternionNormalized) * XAxis;
            Vec3 inverseYAxis = Quaternion.Inverse(RotationQuaternionNormalized) * YAxis;
            Vec3 inverseZAxis = Quaternion.Inverse(RotationQuaternionNormalized) * ZAxis;

            // Apply the inverse of the box's rotation to the line's points
            lineStart = Quaternion.Inverse(RotationQuaternionNormalized) * (lineStart - Center) + Center;
            lineEnd = Quaternion.Inverse(RotationQuaternionNormalized) * (lineEnd - Center) + Center;

            // Calculate the minimum and maximum points of the AABB with the inverse-rotated axes
            Vec3 minCorner = Center - (inverseXAxis * 0.5f) - (inverseYAxis * 0.5f) - (inverseZAxis * 0.5f);
            Vec3 maxCorner = Center + (inverseXAxis * 0.5f) + (inverseYAxis * 0.5f) + (inverseZAxis * 0.5f);

            // Perform intersection test using the unaltered line and the AABB of the box in its original orientation
            bool isIntersecting = IsLineIntersectingAABB(lineStart, lineEnd, minCorner, maxCorner);

            return isIntersecting;
        }

        private bool IsLineIntersectingAABB(Vec3 lineStart, Vec3 lineEnd, Vec3 minCorner, Vec3 maxCorner)
        {
            // Calculate the intersection ranges for each axis
            double tMinX = (minCorner.X - lineStart.X) / (lineEnd.X - lineStart.X);
            double tMaxX = (maxCorner.X - lineStart.X) / (lineEnd.X - lineStart.X);

            double tMinY = (minCorner.Y - lineStart.Y) / (lineEnd.Y - lineStart.Y);
            double tMaxY = (maxCorner.Y - lineStart.Y) / (lineEnd.Y - lineStart.Y);

            double tMinZ = (minCorner.Z - lineStart.Z) / (lineEnd.Z - lineStart.Z);
            double tMaxZ = (maxCorner.Z - lineStart.Z) / (lineEnd.Z - lineStart.Z);

            // Calculate the entry and exit values for the intersection ranges
            double tEnter = Math.Max(Math.Max(Math.Min(tMinX, tMaxX), Math.Min(tMinY, tMaxY)), Math.Min(tMinZ, tMaxZ));
            double tExit = Math.Min(Math.Min(Math.Max(tMinX, tMaxX), Math.Max(tMinY, tMaxY)), Math.Max(tMinZ, tMaxZ));

            // Check if the line intersects the AABB
            bool isIntersecting = tExit >= Math.Max(0, tEnter) && tEnter <= 1;

            return isIntersecting;
        }

        public override string ToString()
        {
            return $"P1 {P1} P2 {P2} P3 {P3} P4 {P4} P5 {P5} P6 {P6} P7 {P7} P8 {P8}";
        }

        public List<Vec3> Points => new List<Vec3>()
        {
            XAxis,
            YAxis,
            ZAxis,
            Corner
        };
    }
}