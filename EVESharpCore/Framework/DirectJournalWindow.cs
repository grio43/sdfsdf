extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{

    public enum MainTab
    {
        Unknown = -1,
        AgentMissions = 0,
        Research = 1,
        IncursionLPs = 2,
    };

    public class DirectJournalWindow : DirectWindow
    {
        #region Constructors

        internal DirectJournalWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Properties

        public MainTab SelectedMainTab => (MainTab)CurrentSelectedMainTabIndex;

        private int CurrentSelectedMainTabIndex => CurrentSelectedMainTab != null ? MainTabs.IndexOf(CurrentSelectedMainTab) : -1;

        private PyObject CurrentSelectedMainTab => MainTabs.FirstOrDefault(t => t.Attribute("_selected").ToBool());

        private List<PyObject> MainTabs => PyWindow.Attribute("tabGroup").Attribute("sr").Attribute("tabs").ToList();

        #endregion Properties

        #region Methods

        public void SwitchMaintab(MainTab t)
        {
            if (SelectedMainTab != MainTab.Unknown && SelectedMainTab != t)
            {
                var tab = MainTabs[(int)t];
                if (tab != null)
                {
                    DirectEve.ThreadedCall(PyWindow.Attribute("_SelectTab"), tab);
                }
            }
        }

        #endregion Methods
    }
}