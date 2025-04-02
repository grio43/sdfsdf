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

    public class DirectTelecomWindow : DirectWindow
    {
        #region Constructors

        internal DirectTelecomWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Methods

        public override bool Close(bool forceCloseContainerWnd = false)
        {
            string[] closeButtonPath = { "content", "main", "bottom", "ButtonGroup" };
            
            var buttonGroup = FindChildWithPath(PyWindow, closeButtonPath);
            var btn = buttonGroup["buttons"].ToList().FirstOrDefault(b => b.Attribute("name").ToUnicodeString() == "ok_dialog_button");
            var k = btn.IsValid;
            if (btn != null && DirectEve.Interval(400, 700, null, true)) 
                return DirectEve.ThreadedCall(btn.Attribute("OnClick"));
            else
                return false;

            //return DirectEve.ThreadedCall(PyWindow.Attribute("SelfDestruct"));
        }

        #endregion Methods
    }
}