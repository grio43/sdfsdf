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

using EVESharpCore.Cache;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public enum ButtonType
    {
        UNKNOWN,
        ACCEPT,
        CLOSE,
        COMPLETE_MISSION,
        DECLINE,
        DELAY,
        LOCATE_CHARACTER,
        NO_JOBS_AVAILABLE,
        QUIT_MISSION,
        REQUEST_MISSION,
        VIEW_MISSION,
    }

    public enum WindowState
    {
        MISSION_REQUEST_WINDOW,
        MISSION_DETAIL_WINDOW,
        LOADING
    }

    public class DirectAgentWindow : DirectWindow
    {
        #region Constructors

        internal DirectAgentWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
            var loading = pyWindow.Attribute("briefingBrowser").Attribute("_loading");
            IsReady = loading.IsValid && !(bool)loading;

            if (pyWindow.Attribute("briefingBrowser").IsValid)
            {
                loading = pyWindow.Attribute("objectiveBrowser").Attribute("_loading");
                IsReady &= loading.IsValid && !(bool)loading;
            }

            AgentId = (int)pyWindow.Attribute("agentID");
            //AgentSays = (string)pyWindow.Attribute("sr").Attribute("agentSays");

            Buttons = new List<DirectAgentButton>();

            // main.btnsmainparent.btns
            //var buttonPathRight = new[] { "__maincontainer", "main", "rightPane", "rightPaneBottom" };
            //var buttonPathLeft = new[] { "__maincontainer", "main", "rightPaneBottom" };
            //var buttonPathRight = new[] { "__maincontainer", "main", "btnsmainparent", "btns" };
            //var buttonPathLeft = new[] { "__maincontainer", "main", "btnsmainparent", "btns" };
            //var viewMode = (string)pyWindow.Attribute("viewMode");
            //var isRight = viewMode != "SinglePaneView";
            //var buttonPath = isRight ? buttonPathRight : buttonPathLeft;
            //var buttons = FindChildWithPath(pyWindow, buttonPath).Attribute("children").Attribute("_childrenObjects").ToList();

            var buttons = pyWindow.Attribute("buttonGroup").Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").GetItemAt(0)
                .Attribute("children").Attribute("_childrenObjects").ToList();
            foreach (var bt in buttons)
            {
                var btn = bt.Attribute("children").Attribute("_childrenObjects").GetItemAt(0);
                var button = new DirectAgentButton(directEve, btn);
                button.AgentId = AgentId;
                button.Text = (string)btn.Attribute("text");
                button.Type = GetButtonType(button.Text);
                button.Button = (string)btn.Attribute("name");
                Buttons.Add(button);
            }

            Briefing = (string)pyWindow.Attribute("briefingBrowser").Attribute("sr").Attribute("currentTXT");
            Objective = (string)pyWindow.Attribute("objectiveBrowser").Attribute("sr").Attribute("currentTXT");

            IsReady &= WindowState != WindowState.LOADING;

            if (WindowState == WindowState.MISSION_DETAIL_WINDOW)
                IsReady &= !ObjectiveEmpty;
        }

        #endregion Constructors

        #region Properties

        public DirectAgent Agent => DirectAgent.GetAgentById(DirectEve, this.AgentId);
        public long AgentId { get; internal set; }
        //public string AgentSays { get; internal set; }
        public string Briefing { get; internal set; }
        public List<DirectAgentButton> Buttons { get; internal set; }
        public bool IsReady { get; internal set; }
        public string Objective { get; internal set; }
        public bool ObjectiveEmpty => Objective?.Equals("<html><body></body></html>") ?? true;

        public int TotalISKReward
        {
            get
            {
                var isk = 0;
                var iskRegex = new Regex(@"([0-9]+)((\.([0-9]+))*) ISK", RegexOptions.Compiled);
                foreach (Match itemMatch in iskRegex.Matches(Objective))
                {
                    int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out var val);
                    isk += val;
                }
                return isk;
            }
        }

        public int TotalLPReward
        {
            get
            {
                var lps = 0;
                var lpRegex = new Regex(@"([0-9.]+) Loyalty Points", RegexOptions.Compiled);
                foreach (Match itemMatch in lpRegex.Matches(Objective))
                {
                    int.TryParse(Regex.Match(itemMatch.Value.Replace(".", ""), @"\d+").Value, out var val);
                    lps += val;
                }
                return lps;
            }
        }

        public WindowState WindowState
        {
            get
            {
                if (!Buttons.Any())
                    return WindowState.LOADING;

                if (Buttons.Any(b => b.Type == ButtonType.REQUEST_MISSION || b.Type == ButtonType.VIEW_MISSION))
                    return WindowState.MISSION_REQUEST_WINDOW;

                //var agent = Agent;
                //if (Buttons.Any(b => b.Type == ButtonType.COMPLETE_MISSION) && agent != null && agent.IsValid)
                //{
                //    var mission = agent.Mission;
                //    if (mission != null && !mission.Bookmarks.Any())
                //        return WindowState.LOADING;
                //}

                return WindowState.MISSION_DETAIL_WINDOW;
            }
        }

        public FactionType? GetFactionType()
        {
            if (ObjectiveEmpty)
                return null;

            var agentWindow = ESCache.Instance.Agent.Window;
            var html = agentWindow.Objective;
            var logoRegex = new Regex("img src=\"factionlogo:(?<factionlogo>\\d+)");
            var logoMatch = logoRegex.Match(html);
            if (logoMatch.Success)
            {
                var id = logoMatch.Groups["factionlogo"].Value;
                if (int.TryParse(id, out var res))
                {
                    return DirectFactions.GetFactionTypeById(res);
                }
            }

            return FactionType.Unknown;
        }

        private ButtonType GetButtonType(string s)
        {
            if (string.IsNullOrEmpty(s))
                return ButtonType.UNKNOWN;

            if (Enum.TryParse<ButtonType>(s.ToUpper().Replace(" ", "_"), out var type))
                return type;

            if (s.Contains("Sorry, I have no jobs available for you."))
                return ButtonType.NO_JOBS_AVAILABLE;

            return ButtonType.UNKNOWN;
        }

        #endregion Properties
    }
}