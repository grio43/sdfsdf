﻿// ------------------------------------------------------------------------------
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

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgentMissionBookmark : DirectBookmark
    {
        #region Fields

        public long? AgentId;
        public int? Flag;
        public bool? IsDeadspace;
        public int? LocationNumber;
        public string LocationType;
        public long? SolarSystemId;

        #endregion Fields

        #region Constructors

        internal DirectAgentMissionBookmark(DirectEve directEve, PyObject pyBookmark) : base(directEve, pyBookmark)
        {
            AgentId = (long?)pyBookmark.Attribute("agentID");
            IsDeadspace = (bool?)pyBookmark.Attribute("deadspace");
            Flag = (int?)pyBookmark.Attribute("flag");
            LocationNumber = (int?)pyBookmark.Attribute("locationNumber");
            LocationType = (string)pyBookmark.Attribute("locationType");
            Title = (string)pyBookmark.Attribute("hint");
            SolarSystemId = (long?)pyBookmark.Attribute("solarsystemID");
        }

        #endregion Constructors
    }
}