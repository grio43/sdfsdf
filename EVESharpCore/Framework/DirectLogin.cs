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

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectLogin : DirectObject
    {
        #region Fields

        /// <summary>
        ///     Character slot cache
        /// </summary>
        private List<DirectLoginSlot> _slots;

        #endregion Fields

        #region Constructors

        internal DirectLogin(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     The character selection screen is open
        /// </summary>
        public bool AtCharacterSelection => (bool)CharSelectLayer.Attribute("isopen") || (bool)CharSelectLayer.Attribute("isopening");

        /// <summary>
        ///     The login screen is open
        /// </summary>
        public bool AtLogin => (bool)LoginLayer.Attribute("isopen") || (bool)LoginLayer.Attribute("isopening");

        /// <summary>
        ///     Return the 3 character slots
        /// </summary>
        public List<DirectLoginSlot> CharacterSlots
        {
            get
            {
                if (_slots == null)
                {
                    _slots = new List<DirectLoginSlot>();
                    foreach (var slot in CharSelectLayer.Attribute("characterSlotList").ToList())
                        _slots.Add(new DirectLoginSlot(DirectEve, slot));
                }

                return _slots;
            }
        }

        /// <summary>
        ///     Is the character selection screen ready
        /// </summary>
        public bool IsCharacterSelectionReady => (bool)CharSelectLayer.Attribute("ready");

        /// <summary>
        ///     EVE is connecting/logging in
        /// </summary>
        public bool IsConnecting => (bool)LoginLayer.Attribute("connecting");

        /// <summary>
        ///     Either the character selection screen or login screen is loading
        /// </summary>
        public bool IsLoading => (bool)LoginLayer.Attribute("isopening") || (bool)CharSelectLayer.Attribute("isopening");

        /// <summary>
        ///     The server status string
        /// </summary>
        public string ServerStatus => (string)LoginLayer.Attribute("serverStatusTextControl").Attribute("text");

        private PyObject CharSelectLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("charsel");
        private PyObject LoginLayer => PySharp.Import("carbonui").Attribute("uicore").Attribute("uicore").Attribute("layer").Attribute("login");

        #endregion Properties
    }
}