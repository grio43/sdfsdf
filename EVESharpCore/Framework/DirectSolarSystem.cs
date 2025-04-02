// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.FastPriorityQueue;
using SC::SharedComponents.Py;
using SC::SharedComponents.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectSolarSystem : DirectObject
    {
        #region Fields

        private HashSet<DirectSolarSystem> _neighbours;
        private List<DirectStation> _stations;

        #endregion Fields

        #region Constructors

        internal DirectSolarSystem(DirectEve directEve, PyObject pyo)
            : base(directEve)
        {
            Id = (int)pyo.Attribute("solarSystemID");
            Name = (string)DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("evelocations").Call("Get", Id).Attribute("name");
            ConstellationId = (long)pyo.Attribute("constellationID");
            FactionId = (long?)pyo.Attribute("factionID");
            SetSecurity((double)pyo.Attribute("securityStatus"));
            IsWormholeSystem = (long)directEve.Const.MapWormholeSystemMin < Id && Id < (long)directEve.Const.MapWormholeSystemMax;

            var n = pyo.Attribute("neighbours");
            var result = new List<int>();
            if (n.IsValid)
            {
                var list = n.ToList();
                foreach (var obj in list) result.Add(obj.Attribute("solarSystemID").ToInt());
            }

            _neighboursIds = result;

            X = pyo.Attribute("center").Attribute("x").ToFloat();
            Y = pyo.Attribute("center").Attribute("y").ToFloat();
            Z = pyo.Attribute("center").Attribute("z").ToFloat();

            Radius = DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("mapSolarSystemContentCache").DictionaryItem(Id).Attribute("radius")
                         .ToDouble() / 149598000000;
        }

        #endregion Constructors

        #region Properties

        public override int GetHashCode()
        {
            return Id;
        }

        public DirectConstellation Constellation => DirectEve.Constellations[ConstellationId];
        public long ConstellationId { get; private set; }
        public long? FactionId { get; private set; }
        public int Id { get; private set; }
        public bool IsWormholeSystem { get; private set; }
        public string Name { get; private set; }

        public HashSet<DirectSolarSystem> Neighbours
        {
            get
            {
                if (_neighbours == null)
                {
                    var result = new HashSet<DirectSolarSystem>();
                    foreach (var i in _neighboursIds) result.Add(DirectEve.SolarSystems[i]);
                    _neighbours = result;
                }

                return _neighbours;
            }
        }

        private bool? _isHighsecIsleSystem;


        public bool IsHighsecSystem => this.GetSecurity() >= 0.45;

        public bool IsHighsecIsleSystem()
        {
            if (_isHighsecIsleSystem == null)
            {
                _isHighsecIsleSystem = false;
                var routeToJita = this.CalculatePathTo(ESCache.Instance.DirectEve.SolarSystems[30000142], null, false, false).Item1.Any();
                var isHighsec = this.GetSecurity() >= 0.45;
                if (!routeToJita && isHighsec)
                {
                    _isHighsecIsleSystem = true;
                }
            }

            return _isHighsecIsleSystem.Value;
        }

        public double Radius { get; private set; }

        //
        //        public int GetClassOfWormhole()
        //        {
        //            var regionId = DirectEve.Constellations[ConstellationId].RegionId;
        //            return (int) DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Call("GetLocationWormholeClass", Id);
        //        }
        private double _security;

        public void SetSecurity(double value)
        {
            _security = value;
        }

        public double GetSecurity()
        {
            return _security;
        }


        /// <summary>
        ///     List all stations within this solar system
        /// </summary>
        public List<DirectStation> Stations
        {
            get { return _stations ?? (_stations = DirectEve.Stations.Values.Where(s => s.SolarSystemId == Id).ToList()); }
        }

        public int WormholeClass { get; private set; }
        public float X { get; private set; }
        public float Y { get; private set; }
        public float Z { get; private set; }
        private List<int> _neighboursIds { get; set; }

        #endregion Properties

        #region Methods

        public bool AnyLowSecFromCurrentSystem => DirectEve.Me.CurrentSolarSystem?.CalculatePathTo(this).Item1.Any(s => s.GetSecurity() < 0.45) ?? false;

        public Tuple<List<DirectSolarSystem>, int> CalculatePathTo(DirectSolarSystem destination, HashSet<DirectSolarSystem> excludeSystems = null, bool allowLowsec = false, bool allowNullSec = false)
        {
            var ms = 0;
            var path = new List<DirectSolarSystem>();
            using (new DisposableStopwatch(t => ms = (int)t.TotalMilliseconds))
            {
                if (destination == null || destination == this) return new Tuple<List<DirectSolarSystem>, int>(new List<DirectSolarSystem>() { this }, 0);

                var start = this;
                var cameFrom = new Dictionary<DirectSolarSystem, DirectSolarSystem>();
                var costSoFar = new Dictionary<DirectSolarSystem, float>();
                var frontier = new SimplePriorityQueue<DirectSolarSystem>();

                frontier.Enqueue(start, 0);
                cameFrom[start] = start;
                costSoFar[start] = 0;
                var amount = 0;
                while (frontier.Count > 0)
                {
                    var current = frontier.Dequeue();
                    amount++;

                    if (current.Equals(destination))
                        break;

                    var security = 0.45;

                    if (allowLowsec)
                        security = 0d;

                    if (allowNullSec)
                        security = -1.01d;

                    var neighbours = current.Neighbours
                        .Where(n => security == 0d ? n.GetSecurity() > security : n.GetSecurity() >= security).ToList()
                        .ToHashSet();

                    if (excludeSystems != null)
                        neighbours.ExceptWith(excludeSystems);

                    foreach (var next in neighbours)
                    {
                        var newCost = costSoFar[current] + 1;
                        if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])
                        {
                            costSoFar[next] = newCost;
                            var priority = newCost + Heuristic(next, destination);
                            frontier.Enqueue(next, priority);
                            cameFrom[next] = current;
                        }
                    }
                }

                var e = destination;
                while (e != start)
                {
                    path.Add(e);
                    if (!cameFrom.ContainsKey(e))
                    {
                        path.Clear();
                        break;
                    }

                    e = cameFrom[e];
                }

                if (path.Contains(destination))
                    path.Add(start);
                path.Reverse();
            }

            return new Tuple<List<DirectSolarSystem>, int>(path, ms);
        }

        public float GetDistance(DirectSolarSystem to)
        {
            //return 1;
            var deltaX = to.X - X;
            var deltaY = to.Y - Y;
            var deltaZ = to.Z - Z;
            var distance = (float)Math.Sqrt(
                deltaX * deltaX +
                deltaY * deltaY +
                deltaZ * deltaZ);
            return distance;
            //return (float)Math.Sqrt(Math.Pow((X - to.X), 2) + Math.Pow((Y - to.Y), 2));
        }

        /// <summary>
        ///     Returns all neighbours which are within X jumps reachable
        /// </summary>
        /// <param name="depth">Max number of jumps</param>
        /// <returns></returns>
        public List<DirectSolarSystem> GetNeighbours(int depth = 1)
        {
            return GetNeighboursRecursive(0, depth, this, new List<DirectSolarSystem>());
        }

        internal static int GetDistanceBetweenSolarsystems(int solarsystem1, int solarsystem2, DirectEve directEve)
        {
            return (int)directEve.GetLocalSvc("clientPathfinderService").Call("GetAutopilotJumpCount", solarsystem1, solarsystem2);
        }

        internal static Dictionary<string, int> _solarSystemByName = null;

        internal static Dictionary<int, DirectSolarSystem> GetSolarSystems(DirectEve directEve)
        {

            if (_solarSystemByName == null)
                _solarSystemByName = new Dictionary<string, int>();

            var result = new Dictionary<int, DirectSolarSystem>();
            var pyDict = directEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("mapSystemCache").ToDictionary<int>();
            foreach (var pair in pyDict)
            {
                result[pair.Key] = new DirectSolarSystem(directEve, pair.Value);
                _solarSystemByName[result[pair.Key].Name] = pair.Key;
            }

            return result;
        }

        private List<DirectSolarSystem> GetNeighboursRecursive(int currentDepth, int depth, DirectSolarSystem s, List<DirectSolarSystem> ss)
        {
            if (depth == currentDepth)
                return new List<DirectSolarSystem>();

            currentDepth++;
            foreach (var n in s.Neighbours)
            {
                ss.Add(n);
                ss.AddRange(GetNeighboursRecursive(currentDepth, depth, n, ss));
            }

            return ss.Distinct().ToList();
        }

        private float Heuristic(DirectSolarSystem a, DirectSolarSystem b)
        {
            return 0;
            //return (float)Math.Sqrt(Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z));
            //return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
        }

        #endregion Methods

        // radius of a solarsystem is determined by the greatest distance between all celestials (usually stargates)
        //public void Stargates()
        //{
        //    var stargates = DirectEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("mapSolarSystemContentCache").DictionaryItem(Id);
        //    if (stargates.IsValid)
        //    {
        //        foreach (var stargate in stargates.Attribute("stargates").ToDictionary())
        //        {
        //            DirectEve.Log(stargate.Value.Attribute("position").Attribute("x").ToFloat().ToString());
        //            DirectEve.Log(stargate.Value.Attribute("position").Attribute("y").ToFloat().ToString());
        //            DirectEve.Log(stargate.Value.Attribute("position").Attribute("z").ToFloat().ToString());
        //        }
        //    }
        //}
    }
}