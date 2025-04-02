// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

namespace EVESharpCore.Framework
{
    public class DirectCharacter : DirectObject
    {
        #region Constructors

        internal DirectCharacter(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public long AllianceId { get; internal set; }
        public long CharacterId { get; internal set; }
        public long CorporationId { get; internal set; }
        public string Name => DirectEve.GetOwner(CharacterId).Name;
        public long WarFactionId { get; internal set; }

        #endregion Properties
    }
}