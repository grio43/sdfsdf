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
using System.Linq;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectAgentButton : DirectObject
    {
        #region Fields

        private readonly PyObject _pyBtn;

        #endregion Fields

        #region Constructors

        internal DirectAgentButton(DirectEve directEve, PyObject pyBtn)
            : base(directEve)
        {
            _pyBtn = pyBtn;
        }

        #endregion Constructors

        #region Properties

        public long AgentId { get; internal set; }
        public string Button { get; internal set; }
        public string Text { get; internal set; }
        public ButtonType Type { get; internal set; }

        #endregion Properties

        #region Methods

        public bool Click()
        {
            return DirectEve.ThreadedCall(_pyBtn.Attribute("OnClick"));
        }

        #endregion Methods
    }
}