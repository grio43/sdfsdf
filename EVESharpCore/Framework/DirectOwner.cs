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
    public class DirectOwner : DirectInvType
    {
        #region Constructors

        internal DirectOwner(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public string Name { get; private set; }
        public long OwnerId { get; private set; }

        #endregion Properties

        #region Methods

        public static DirectOwner GetOwner(DirectEve directEve, long ownerId)
        {
            var pyOwner = directEve.PySharp.Import("__builtin__").Attribute("cfg").Attribute("eveowners").Call("GetIfExists", ownerId);

            var owner = new DirectOwner(directEve);
            owner.OwnerId = (long)pyOwner.Attribute("ownerID");
            owner.Name = (string)pyOwner.Attribute("ownerName");
            owner.TypeId = (int)pyOwner.Attribute("typeID");
            return owner;
        }

        #endregion Methods
    }
}