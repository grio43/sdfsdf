extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{

    public class DirectAgencyWindow : DirectWindow
    {
        internal DirectAgencyWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }
    }
}
