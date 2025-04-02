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

    public class DirectReprocessingQuoteRecoverable : DirectInvType
    {
        #region Constructors

        internal DirectReprocessingQuoteRecoverable(DirectEve directEve, PyObject recoverable) : base(directEve)
        {
            TypeId = (int)recoverable.GetItemAt(0);
            YouReceive = (long)recoverable.GetItemAt(1);
            WeTake = (long)recoverable.GetItemAt(2);
            Unrecoverable = (long)recoverable.GetItemAt(3);
        }

        #endregion Constructors

        #region Properties

        public long Unrecoverable { get; private set; }
        public long WeTake { get; private set; }
        public long YouReceive { get; private set; }

        #endregion Properties
    }
}