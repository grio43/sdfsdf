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
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgentMission : DirectObject
    {
        #region Fields

        //private PyObject _pyAgentId;

        #endregion Fields

        #region Constructors

        internal DirectAgentMission(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long AgentId { get; internal set; }
        public List<DirectAgentMissionBookmark> Bookmarks { get; internal set; }
        public DateTime ExpiresOn { get; internal set; }
        public bool Important { get; internal set; }
        public string Name { get; internal set; }
        public int State { get; internal set; }
        public string Type { get; internal set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Ensure the journal mission tab is open before RemoveOffer is called
        /// </summary>
        /// <returns></returns>
        public bool RemoveOffer()
        {
            if (State != (int)PySharp.Import("__builtin__").Attribute("const").Attribute("agentMissionStateOffered"))
                return false;

            return DirectEve.ThreadedLocalSvcCall("agents", "RemoveOfferFromJournal", AgentId);
        }

        internal static List<DirectAgentMission> GetAgentMissions(DirectEve directEve)
        {
            var missions = new List<DirectAgentMission>();

            var pyMissions = directEve.GetLocalSvc("journal").Attribute("agentjournal").GetItemAt(0).ToList();

            foreach (var pyMission in pyMissions)
            {
                var mission = new DirectAgentMission(directEve)
                {
                    State = (int)pyMission.GetItemAt(0),
                    Important = (bool)pyMission.GetItemAt(1),
                    Type = (string)pyMission.GetItemAt(2),
                    //_pyAgentId = pyMission.Item(4),
                    AgentId = (long)pyMission.GetItemAt(4),
                    ExpiresOn = (DateTime)pyMission.GetItemAt(5),
                    Bookmarks = pyMission.GetItemAt(6).ToList().Select(b => new DirectAgentMissionBookmark(directEve, b)).ToList(),
                };

                var messageId = (int)pyMission.GetItemAt(3);
                if (messageId > 0)
                {
                    mission.Name = directEve.GetLocalizationMessageById(messageId);
                }
                else
                {
                    mission.Name = "none";
                    continue;
                }

                missions.Add(mission);
            }

            return missions;
        }

        #endregion Methods
    }
}