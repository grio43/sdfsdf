﻿extern alias SC;

using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectWalletWindow : DirectWindow
    {
        #region Constructors

        internal DirectWalletWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Methods

        #endregion Methods
    }
}