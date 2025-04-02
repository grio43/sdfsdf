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

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectLoginSlot : DirectObject
    {
        #region Fields

        private PyObject _pySlot;

        #endregion Fields

        #region Constructors

        internal DirectLoginSlot(DirectEve directEve, PyObject pySlot) : base(directEve)
        {
            _pySlot = pySlot;
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Return the character id associated with this slot
        /// </summary>
        public long CharId => (long)_pySlot.Attribute("characterDetails").Attribute("charDetails").Attribute("characterID");

        /// <summary>
        ///     Return the character name associated with this slot
        /// </summary>
        public string CharName => (string)_pySlot.Attribute("characterDetails").Attribute("charDetails").Attribute("characterName");

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Activate this slot, this could make it main slot (if its slot 1 or 2) or login the character (slot 0)
        /// </summary>
        /// <returns></returns>
        public bool Activate()
        {
            //if (!DirectEve.HasSupportInstances())
            //{
            //    DirectEve.Log("DirectEve: Error: This method requires a support instance.");
            //    return false;
            //}

            var selectSlot = PySharp.Import("carbonui")
                .Attribute("uicore")
                .Attribute("uicore")
                .Attribute("layer")
                .Attribute("charsel")
                .Attribute("EnterGameWithCharacter");
            return DirectEve.ThreadedCall(selectSlot, _pySlot);
        }

        #endregion Methods
    }
}