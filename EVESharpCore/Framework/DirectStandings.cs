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
using SC::SharedComponents.EVE;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectStandings : DirectObject
    {
        #region Constructors

        internal DirectStandings(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public bool IsReady
        {
            get
            {
                var isReady = AddressBook.Attribute("contacts").IsValid;
                isReady &= AddressBook.Attribute("corporateContacts").IsValid;
                isReady &= AddressBook.Attribute("allianceContacts").IsValid;
                return isReady;
            }
        }

        private PyObject AddressBook => DirectEve.GetLocalSvc("addressbook");

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Return the effective standing between you and something else.
        /// </summary>
        /// <param name="fromID">
        ///     The ID of the thing you are checking standings with.  If this thing is an NPC the result is
        ///     modified by skills.
        /// </param>
        /// <param name="toID">The ID of the thing you are checking standings to.  This is typically DirectEve.Session.CharacterId.</param>
        /// <returns>The standing between toID and fromID.</returns>
        public float? EffectiveStanding(long fromID, long toID)
        {
            var result = DirectEve.GetLocalSvc("standing").Call("GetStandingWithSkillBonus", fromID, toID);

            if (result.IsValid)
                return (float)result.ToFloat();

            return (float?)null;
        }

        /// </param>
        /// <param name="toID">The ID of the thing you are checking standings to.  This is typically DirectEve.Session.CharacterId.</param>
        /// <returns>The standing between toID and fromID.</returns>
        public float? EffectiveStanding(long from)
        {
            return DirectEve.Session.CharacterId.HasValue ? EffectiveStanding(from, DirectEve.Session.CharacterId.Value) : (float?)null;
        }

        public float? GetStandingToFaction(FactionType faction)
        {
            var res = DirectFactions.GetFactionIdByType(faction);
            if (res.HasValue)
            {
                return EffectiveStanding(res.Value);
            }
            return null;
        }

        public float GetAllianceRelationship(long id)
        {
            if (!IsReady)
                return 0;

            return (float)AddressBook.Attribute("allianceContacts").Call("get", id, PySharp.PyNone).Attribute("relationshipID");
        }

        public float GetCorporationRelationship(long id)
        {
            if (!IsReady)
                return 0;

            return (float)AddressBook.Attribute("corporateContacts").Call("get", id, PySharp.PyNone).Attribute("relationshipID");
        }

        public float GetMaxStanding(long id)
        {
            var a = GetPersonalRelationship(id);
            var b = GetCorporationRelationship(id);
            var c = GetAllianceRelationship(id);
            return Math.Max(a, Math.Max(b, c));
        }

        public float GetMaxStanding(DirectCharacter c)
        {
            return Math.Max(GetMaxStanding(c.AllianceId), Math.Max(GetMaxStanding(c.CharacterId), GetMaxStanding(c.CorporationId)));
        }

        public float GetMinStanding(long id)
        {
            var a = GetPersonalRelationship(id);
            var b = GetCorporationRelationship(id);
            var c = GetAllianceRelationship(id);
            return Math.Min(a, Math.Min(b, c));
        }

        public float GetMinStanding(DirectCharacter c)
        {
            return Math.Min(GetMinStanding(c.AllianceId), Math.Min(GetMinStanding(c.CharacterId), GetMinStanding(c.CorporationId)));
        }

        public float GetPersonalRelationship(long id)
        {
            if (!IsReady)
                return 0;

            return (float)AddressBook.Attribute("contacts").Call("get", id, PySharp.PyNone).Attribute("relationshipID");
        }

        public bool LoadStandings()
        {
            return DirectEve.ThreadedLocalSvcCall("addressbook", "GetContacts");
        }

        #endregion Methods
    }
}