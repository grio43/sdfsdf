extern alias SC;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    using EVESharpCore.Cache;
    using EVESharpCore.Framework.Lookup;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    public static class DirectRayCasting
    {

        /// <summary>
        /// IsLineOfSightFree
        /// </summary>
        /// <param name="start">Absolute world position (X,Y,Z)</param>
        /// <param name="end">Absolute world position (X,Y,Z)</param>
        /// <returns></returns>
        public static (bool, Dictionary<DirectEntity, List<IGeometry>>) IsLineOfSightFree(Vec3 start, Vec3 end, bool ignoreAbyssEntities = false, bool ignoreTrackingPolyons = false,
            bool ignoreAutomataPylon = false, bool ignoreWideAreaAutomataPylon = false, bool ignoreFilaCouds = false,
            bool ignoreBioClouds = false, bool ignoreTachClouds = false, bool ignoreSentryGuns = false, List<DirectEntity> ignoredEntities = null, bool debugLog = false)
        {
            //var intersectEnts = ColliderEntities.Where(e => e.DirectAbsolutePosition.GetDistanceSquared(worldPos) < (e.RealRadius * e.RealRadius));
            if (debugLog)
            {
                // log all values of the parameters of that method
                Console.WriteLine("Method Parameters and Values:");
                Console.WriteLine($"start: {start}");
                Console.WriteLine($"end: {end}");
                Console.WriteLine($"ignoreAbyssEntities: {ignoreAbyssEntities}");
                Console.WriteLine($"ignoreTrackingPolyons: {ignoreTrackingPolyons}");
                Console.WriteLine($"ignoreAutomataPylon: {ignoreAutomataPylon}");
                Console.WriteLine($"ignoreWideAreaAutomataPylon: {ignoreWideAreaAutomataPylon}");
                Console.WriteLine($"ignoreFilaCouds: {ignoreFilaCouds}");
                Console.WriteLine($"ignoreBioClouds: {ignoreBioClouds}");
                Console.WriteLine($"ignoreTachClouds: {ignoreTachClouds}");
                Console.WriteLine($"ignoreSentryGuns: {ignoreSentryGuns}");
                Console.WriteLine($"ignoredEntities: {(ignoredEntities == null ? "null" : string.Join(", ", ignoredEntities))}");
                Console.WriteLine($"debugLog: {debugLog}");
            }

            Func<DirectEntity, bool> nonTraversableAndAbyssEntities = ise =>
            {
                if (ise.GroupId == (int)Group.SentryGun && !ignoreSentryGuns)
                {
                    return true;
                }

                if (ignoreAbyssEntities)
                {
                    ignoreTrackingPolyons = true;
                    ignoreAutomataPylon = true;
                    ignoreWideAreaAutomataPylon = true;
                    ignoreFilaCouds = true;
                    ignoreBioClouds = true;
                    ignoreTachClouds = true;
                }

                // Always include non traverssable collider entities, we need to filter entities later however, which do both have non traversable and traversable colliders
                if (ise.HasAnyNonTraversableColliders)
                    return true;

                if (ise.IsTrackingPylon && !ignoreTrackingPolyons
                    || ise.IsAutomataPylon && !ignoreAutomataPylon
                    || ise.IsWideAreaAutomataPylon && !ignoreWideAreaAutomataPylon
                    || ise.IsFilaCould && !ignoreFilaCouds
                    || ise.IsBioCloud && !ignoreBioClouds
                    || ise.IsTachCloud && !ignoreTachClouds)
                    return true;


                return false;
            };

            // Filter entities based on distance and the defined predicate
            var colliderEntities = DirectEntity.ColliderEntities
                .Where(ise => ise.Distance < 3_000_000 && nonTraversableAndAbyssEntities(ise))
                .Except(ignoredEntities ?? Enumerable.Empty<DirectEntity>())
                .ToList();


            if (ignoredEntities == null && debugLog)
            {
                Console.WriteLine("IgnoredEntList is null");
            }

            if (ignoredEntities != null && colliderEntities.Any(e => ignoredEntities.Any(b => b == e)) && debugLog)
            {
                if (DirectEve.Interval(1000))
                    Console.WriteLine("Final collider list contains ent(s) which should be ignored.");
            }

            if (debugLog)
            {
                // print all the typenames of the collider entities
                int i = 0;
                foreach (var col in colliderEntities)
                {
                    i++;
                    Console.WriteLine($"ColEnt [{i}] [{col.TypeName}]");
                }
            }

            Dictionary<DirectEntity, List<IGeometry>> colliders = new Dictionary<DirectEntity, List<IGeometry>>();
            var distanceStartEnd = (start - end).Magnitude;
            foreach (var ent in colliderEntities)
            {
                if ((ent.DirectAbsolutePosition.GetVector() - start).Magnitude - distanceStartEnd > ent.BoundingSphereRadius())
                {
                    continue;
                }

                var miniBalls = ent.MiniBalls;
                var miniCapsules = ent.MiniCapsules;
                var miniBoxes = ent.MiniBoxes;

                // Handle entities correctly which do have both traversable and non traversable colliders
                if (ignoreTrackingPolyons && ent.IsTrackingPylon || ignoreWideAreaAutomataPylon && ent.IsWideAreaAutomataPylon || ignoreAutomataPylon && ent.IsAutomataPylon)
                {
                    miniBalls = miniBalls.Where(e => !e.Traversable).ToList();
                    miniCapsules = miniCapsules.Where(e => !e.Traversable).ToList();
                    miniBoxes = miniBoxes.Where(e => !e.Traversable).ToList();
                }

                var isFree = DirectRayCasting.IsLineOfSightFree(start, end, miniBalls, miniCapsules, miniBoxes, ent.DirectAbsolutePosition.GetVector());

                if (colliders.ContainsKey(ent))
                {
                    colliders[ent].AddRange(isFree.Item2);
                }
                else
                {
                    if (isFree.Item2.Any())
                        colliders.Add(ent, isFree.Item2);
                }
            }

            if (colliders.Any())
            {
                return (false, colliders);
            }

            return (true, colliders);
        }

        public static (bool, List<IGeometry>) IsLineOfSightFree(Vec3 startPoint, Vec3 targetPoint, List<DirectMiniBall> spheres, List<DirectMiniCapsule> capsules, List<DirectMiniBox> rectangles, Vec3? offset = null)
        {
            if (startPoint == targetPoint)
            {
                // offset targetpoint by 0.05
                targetPoint = targetPoint + new Vec3(0.05d, 0.05d, 0.05d);
            }

            List<IGeometry> colliders = new List<IGeometry>(200);
            foreach (var sphere in spheres)
            {
                if (IntersectsSphere(startPoint, targetPoint, sphere, offset))
                {
                    colliders.Add(sphere);
                }
            }

            foreach (var capsule in capsules)
            {
                if (!FastIntersectsGeometryBoundingSphere(startPoint, targetPoint, capsule, offset))
                    continue;

                if (IntersectsCapsule(startPoint, targetPoint, capsule, offset))
                {
                    colliders.Add(capsule);
                }
            }

            foreach (var rectangle in rectangles)
            {

                if (!FastIntersectsGeometryBoundingSphere(startPoint, targetPoint, rectangle, offset))
                    continue;

                if (IntersectsRectangle(startPoint, targetPoint, rectangle, offset))
                {
                    colliders.Add(rectangle);
                }
            }

            if (colliders.Any())
            {
                return (false, colliders);
            }

            return (true, colliders);
        }

        public static bool FastIntersectsGeometryBoundingSphere(Vec3 A, Vec3 B, IGeometry geometry, Vec3? offset = null)
        {
            if (offset != null)
            {
                A -= offset.Value;
                B -= offset.Value;
            }

            var C = geometry.Center;

            // Check if A or B is inside the sphere by comparing squared distances
            Vec3 diffA = A - C;
            Vec3 diffB = B - C;

            double distASquared = Vec3.DotProduct(diffA, diffA);
            double distBSquared = Vec3.DotProduct(diffB, diffB);

            if (distASquared < geometry.MaxBoundingRadiusSquared || distBSquared < geometry.MaxBoundingRadiusSquared)
            {
                return true;
            }

            // Compute direction vector of the line
            Vec3 d = B - A;

            // Coefficients for the quadratic equation
            double a = Vec3.DotProduct(d, d);
            double b = 2 * Vec3.DotProduct(d, diffA);
            double c = distASquared - geometry.MaxBoundingRadiusSquared;

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

        public static bool IntersectsSphere(Vec3 startPoint, Vec3 targetPoint, DirectMiniBall sphere, Vec3? offset = null)
        {
            return sphere.LineIntersects(startPoint, targetPoint, offset);
        }

        public static bool IntersectsCapsule(Vec3 start, Vec3 end, DirectMiniCapsule capsule, Vec3? offset = null)
        {
            return capsule.IsLineWithinCapsule(start, end, offset);
        }

        public static bool IntersectsRectangle(Vec3 startPoint, Vec3 targetPoint, DirectMiniBox rectangle, Vec3? offset = null)
        {
            return rectangle.IsLineIntersecting(targetPoint, startPoint, offset);
        }

    }
}