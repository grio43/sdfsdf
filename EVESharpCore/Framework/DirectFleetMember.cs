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

using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectFleetMember : DirectObject
    {
        #region Constructors

        internal DirectFleetMember(DirectEve directEve, PyObject memberObject)
            : base(directEve)
        {
            CharacterId = (int)memberObject.Attribute("charID");
            SquadID = (long)memberObject.Attribute("squadID");
            WingID = (long)memberObject.Attribute("wingID");
            //Skills = new List<int>
            //{
            //    (int) memberObject.Attribute("skills").ToList()[0],
            //    (int) memberObject.Attribute("skills").ToList()[1],
            //    (int) memberObject.Attribute("skills").ToList()[2]
            //};

            if ((int)memberObject.Attribute("job") == (int)directEve.Const.FleetJobCreator)
                Job = JobRole.Boss;
            else
                Job = JobRole.RegularMember;

            if ((int)memberObject.Attribute("role") == (int)directEve.Const.FleetRoleLeader)
                Role = FleetRole.FleetCommander;
            else if ((int)memberObject.Attribute("role") == (int)directEve.Const.FleetRoleWingCmdr)
                Role = FleetRole.WingCommander;
            else if ((int)memberObject.Attribute("role") == (int)directEve.Const.FleetRoleSquadCmdr)
                Role = FleetRole.SquadCommander;
            else if ((int)memberObject.Attribute("role") == (int)directEve.Const.FleetRoleMember)
                Role = FleetRole.Member;

            ShipTypeID = (int?)memberObject.Attribute("shipTypeID");
            SolarSystemID = (int)memberObject.Attribute("solarSystemID");
        }

        #endregion Constructors

        #region Enums

        public enum FleetRole
        {
            FleetCommander,
            WingCommander,
            SquadCommander,
            Member
        }

        public enum JobRole
        {
            Boss,
            RegularMember
        }

        #endregion Enums

        #region Properties

        public int CharacterId { get; internal set; }
        public JobRole Job { get; internal set; }
        public string Name => DirectEve.GetOwner(CharacterId).Name;
        public FleetRole Role { get; internal set; }
        public int? ShipTypeID { get; internal set; }
        //public List<int> Skills { get; internal set; }
        public long SolarSystemID { get; internal set; }
        public long SquadID { get; internal set; }
        public long WingID { get; internal set; }

        // TODO: We need to check if the ship is actual boarded by a character too! (Done - isPlayer tells)
        public DirectEntity Entity => DirectEve.Entities.FirstOrDefault(e => e.OwnerId == this.CharacterId && e.IsPlayer);

        #endregion Properties

        #region Methods

        public bool WarpToMember(double distance = 0)
        {
            return DirectEve.ThreadedLocalSvcCall("menu", "WarpToMember", CharacterId, distance);
        }

        #endregion Methods
    }
}