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
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using SC::SharedComponents.Utility;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectWindow : DirectObject
    {
        // FROM CONST: probably better to read the values from the game
        //ID_NONE = 0
        //ID_OK = 1
        //ID_CANCEL = 2
        //ID_YES = 6
        //ID_NO = 7
        //ID_CLOSE = 8
        //ID_HELP = 9

        #region Fields

        internal PyObject PyWindow;

        private static Dictionary<string, Dictionary<string, WindowType>> _windowTypeDict;

        private static WindowType[] _windowTypes = new[]
        {
            new WindowType("__guid__", "form.AgentDialogueWindow", (directEve, pyWindow) => new DirectAgentWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.PVPTrade", (directEve, pyWindow) => new DirectTradeWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationItems", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "invCont.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Inventory", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InventoryPrimary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InventorySecondary", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DroneView", (directEve, pyWindow) => new DirectDronesInSpaceWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationItems", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StationShips", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ShipCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ActiveShipCargo", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.DockedCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InflightCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LootCargoView", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StructureItemHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.StructureShipHangar", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RegionalMarket", (directEve, pyWindow) => new DirectMarketWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.FittingMgmt", (directEve, pyWindow) => new DirectFittingManagerWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.ReprocessingDialog", (directEve, pyWindow) => new DirectReprocessingWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LPStore", (directEve, pyWindow) => new DirectLoyaltyPointStoreWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.RepairShopWindow", (directEve, pyWindow) => new DirectRepairShopWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.Journal", (directEve, pyWindow) => new DirectJournalWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.KeyActivationWindow", (directEve, pyWindow) => new DirectKeyActivationWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.AbyssActivationWindow", (directEve, pyWindow) => new DirectAbyssActivationWindow(directEve, pyWindow)),
            new WindowType("name", "SellItemsWindow", (directEve, pyWindow) => new DirectMultiSellWindow(directEve, pyWindow)),
            new WindowType("__guid__", "AgencyWndNew", (directEve, pyWindow) => new DirectAgencyWindow(directEve, pyWindow)),
            new WindowType("__guid__", "FittingWindow", (directEve, pyWindow) => new DirectFittingWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.LobbyWnd", (directEve, pyWindow) => new DirectLobbyWindow(directEve, pyWindow)),
            new WindowType("__guid__", "form.InsurgenceDashboard", (directEve, pyWindow) => new DirectInsurgencyDashboardWindow(directEve, pyWindow)),
            new WindowType("windowID", "overview", (directEve, pyWindow) => new DirectOverviewWindow(directEve, pyWindow)),
            new WindowType("windowID", "selecteditemview", (directEve, pyWindow) => new DirectSelectedItemWindow(directEve, pyWindow)),
            new WindowType("_caption", "Drone Bay", (directEve, pyWindow) => new DirectContainerWindow(directEve, pyWindow)),
            new WindowType("windowID", "walletWindow", (directEve, pyWindow) => new DirectWalletWindow(directEve, pyWindow)),
            new WindowType("windowID", "solar_system_map_panel", (directEve, pyWindow) => new DirectMapViewWindow(directEve, pyWindow)),
            new WindowType("name", "marketbuyaction", (directEve, pyWindow) => new DirectMarketActionWindow(directEve, pyWindow)),
            new WindowType("name", "telecom", (directEve, pyWindow) => new DirectTelecomWindow(directEve, pyWindow)),
            new WindowType("default_windowID", "XmppChat", (directEve, pyWindow) => new DirectChatWindow(directEve, pyWindow)),

        };

        private string html;

        #endregion Fields

        public string MessageKey { get; private set; }

        #region Constructors

        internal DirectWindow(DirectEve directEve, PyObject pyWindow) : base(directEve)
        {
            PyWindow = pyWindow;
            Guid = (string)pyWindow.Attribute("__guid__") ?? "";
            WindowId = (string)pyWindow.Attribute("windowID") ?? "";
            Name = (string)pyWindow.Attribute("name") ?? "";
            IsKillable = (bool)pyWindow.Attribute("killable");
            IsDialog = (bool)pyWindow.Attribute("isDialog");
            IsModal = (bool)pyWindow.Attribute("isModal");
            Caption = (string)pyWindow.Call("GetCaption") ?? "";
            ViewMode = (string)pyWindow.Attribute("viewMode") ?? "";
            MessageKey = (string)pyWindow.Attribute("msgKey") ?? "";
        }

        #endregion Constructors

        #region Enums

        public enum ModalResultType
        {
            NONE,
            OK,
            CANCEL,
            YES,
            NO,
            CLOSE,
            HELP
        }

        #endregion Enums

        #region Properties

        private static void SetupDict()
        {
            if (_windowTypeDict == null)
            {
                _windowTypeDict = new Dictionary<string, Dictionary<string, WindowType>>();

                foreach (var k in _windowTypes)
                {
                    if (!_windowTypeDict.ContainsKey(k.Attribute))
                    {
                        var d = new Dictionary<string, WindowType>();
                        d.Add(k.Value, k);
                        _windowTypeDict.Add(k.Attribute, d);
                    }
                    else
                    {
                        var d = _windowTypeDict[k.Attribute];
                        d.Add(k.Value, k);
                    }
                }
            }
        }

        public string Caption { get; internal set; }


        public bool IsWindowActive
        {
            get
            {
                var reg = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"]["registry"];
                var active = reg.Call("GetActive");
                return active.PyRefPtr == this.PyWindow.PyRefPtr;
            }
        }


        /// <summary>
        ///     Don't call this, use DirectWindowManager
        /// </summary>
        public void SetActive()
        {
            if (IsWindowActive)
                return;

            var reg = DirectEve.PySharp.Import("carbonui")["uicore"]["uicore"]["registry"];
            var focus = reg["SetFocus"];
            if (focus.IsValid)
            {
                //var active = reg.Call("GetActive");
                var stack = this.PyWindow["sr"]["stack"];
                if (stack.IsValid)
                {
                    //var active = stack.Call("GetActiveWindow");
                    var showWnd = stack["ShowWnd"];
                    DirectEve.ThreadedCall(showWnd, this.PyWindow);
                }
                DirectEve.ThreadedCall(focus, this.PyWindow);
            }
        }

        public string Html
        {
            get
            {
                if (!String.IsNullOrEmpty(html))
                    return html;


                try
                {
                    var paragraphs = PyWindow.Attribute("edit").Attribute("sr").Attribute("paragraphs").ToList();
                    html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                    if (String.IsNullOrEmpty(html))
                        html = (string)PyWindow.Attribute("edit").Attribute("sr").Attribute("currentTXT");
                    if (String.IsNullOrEmpty(html))
                    {
                        paragraphs = PyWindow.Attribute("sr").Attribute("messageArea").Attribute("sr").Attribute("paragraphs").ToList();
                        html = paragraphs.Aggregate("", (current, paragraph) => current + (string)paragraph.Attribute("text"));
                    }

                    if (String.IsNullOrEmpty(html))
                    {
                        html = PyWindow["_message_label"]["text"].ToUnicodeString();
                    }

                    if (String.IsNullOrEmpty(html))
                    {
                        string[] textChildPath = { "content", "main", "form", "textField", "text" };
                        var textChild = FindChildWithPath(PyWindow, textChildPath);


                        if (!textChild.IsValid)
                        {
                            //DirectEve.Log("Textchild not valid!");
                        }

                        if (textChild["text"].IsValid)
                        {
                            html = textChild["text"].ToUnicodeString();

                        }
                    }

                    if (String.IsNullOrEmpty(html))
                    {
                        string[] textChildPath = { "content", "main", "ContainerAutoSize", "EveLabelMediumBold" };
                        var textChild = FindChildWithPath(PyWindow, textChildPath);


                        if (!textChild.IsValid)
                        {
                            //DirectEve.Log("Textchild not valid!");
                        }

                        if (textChild["text"].IsValid)
                        {
                            html = textChild["text"].ToUnicodeString();

                        }
                    }

                }
                catch (Exception ex)
                {
                    DirectEve.Log("Exception in DirectWindow.Html: " + ex.Message);
                    return string.Empty;
                }

                if (html == null)
                    html = string.Empty;

                return html;
            }
        }

        public bool IsDialog { get; internal set; }

        public bool IsKillable { get; internal set; }

        public bool IsModal { get; internal set; }

        public string Name { get; internal set; }

        public string WindowId { get; internal set; }

        public bool Ready
        {
            get
            {
                var edit = PyWindow.Attribute("edit");
                if (edit.IsValid && edit.Attribute("_loading").ToBool())
                    return false;

                if (PyWindow.Attribute("startingup").ToBool())
                    return false;

                return true;
            }
        }

        public string Guid { get; internal set; }
        public string ViewMode { get; internal set; }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Answers a modal window
        /// </summary>
        /// <param name="button">a string indicating which button to press. Possible values are: Yes, No, Ok, Cancel, Suppress</param>
        /// <returns>true if successfull</returns>
        public bool SetModalResult(string button)
        {
            //string[] buttonPath = { "__maincontainer", "bottom", "btnsmainparent", "btns", "Yes_Btn" };

            if (!IsModal)
                return false;

            var mr = ModalResultType.YES;

            switch (button)
            {
                case "Yes":
                    break;

                case "No":
                    mr = ModalResultType.NO;
                    break;

                case "OK":
                case "Ok":
                    if (Name == "Set Quantity")
                    {
                        if (PyWindow != null)
                        {
                            PyWindow.Call("Confirm", 12345);
                            return true;
                        }

                        return false;
                    }

                    mr = ModalResultType.OK;
                    break;

                case "Cancel":
                    mr = ModalResultType.CANCEL;
                    break;

                default:
                    return false;
            }

            if (DirectEve.Interval(400, 700, null, true))
            {
                return SetModalResult(mr);
            }
            return false;
        }

        private DateTime _modalNextOperation;

        private static Random _random = new Random();


        public List<string> GetModalButtonList()
        {
            PyObject buttonGroup = FindChildWithPath(PyWindow, _buttonPath);

            if (!buttonGroup.IsValid)
            {
                buttonGroup = FindChildWithPath(PyWindow, _buttonPath2);
            }

            if (!buttonGroup.IsValid)
            {
                return new List<string>();
            }
            var buttons = buttonGroup["buttons"].ToList();

            List<string> buttonNames = new List<string>();

            foreach (var b in buttons)
            {
                var btnStr = $"[Name: {b["name"].ToUnicodeString()}, Text: {b["text"].ToUnicodeString()}]";
                buttonNames.Add(btnStr);
            }
            return buttonNames;
        }

        private string[] _buttonPath = { "content", "main", "bottom", "ButtonGroup" };
        private string[] _buttonPath2 = { "content", "main", "ContainerAutoSize", "bottomCont", "buttonCont", "ButtonGroup" };

        public bool AnswerModal(string button)
        {

            string funcName = "OnClick";
            var btnName = "";
            var btnText = "";
            switch (button)
            {
                case "Yes":
                    btnName = "yes_dialog_button";
                    btnText = "Yes";
                    break;
                case "No":
                    btnName = "no_dialog_button";
                    btnText = "No";
                    break;
                case "OK":
                case "Ok":
                    btnName = "ok_dialog_button";
                    btnText = "Ok";
                    break;
                case "Cancel":
                    btnName = "cancel_dialog_button";
                    btnText = "Cancel";
                    break;
                //case "Suppress":
                //    string[] suppress = { "content", "main", "suppressContainer", "suppress" };
                //    buttonPath = suppress;
                //    break;
                default:
                    return false;
            }
            PyObject buttonGroup = FindChildWithPath(PyWindow, _buttonPath);
            //if (btn != null)

            if (!buttonGroup.IsValid)
            {
                buttonGroup = FindChildWithPath(PyWindow, _buttonPath2);
            }


            if (!buttonGroup.IsValid)
            {
                DirectEve.Log("Buttongroup not valid?");
            }

            var btn = buttonGroup["buttons"].ToList().FirstOrDefault(b => b.Attribute("name").ToUnicodeString() == btnName || (b["text"].ToUnicodeString() == btnText && !string.IsNullOrEmpty(btnText)));
            //var buttons = buttonGroup["buttons"].ToList();

            //foreach(var b in buttons)
            //{
            //    DirectEve.Log($"{b["name"].ToUnicodeString()}");
            //}

            if (btn == null || !btn.IsValid)
            {
                //buttonPath[2] = "ButtonGroup";
                //btn = FindChildWithPath(PyWindow, buttonPath);

                //if (!btn.IsValid)
                //{
                DirectEve.Log("Modal button not found! (FindChildWithPath)");
                return false;
                //}
            }
            //DirectEve.Log("Modal button found!");
            //DirectEve.Log(btn.LogObject());


            //If there is a suppress checkbox and it is not checked, ensure it is being checked

            var checkBox = PyWindow["sr"]["suppCheckbox"];
            if (checkBox.IsValid)
            {
                if (checkBox["_checked"].ToBool() == false)
                {
                    DirectEve.Log("There is a suppress checkbox available, checking it.");
                    _modalNextOperation = DateTime.UtcNow.AddMilliseconds(_random.Next(1200, 2500));
                    DirectEve.ThreadedCall(checkBox["ToggleState"]);
                    return false;
                }
            }

            if (_modalNextOperation > DateTime.UtcNow)
            {
                DirectEve.Log($"Waiting for next modal operation, _modalNextOperation [{_modalNextOperation}]");
                return false;
            }

            if (DirectEve.Interval(400, 700, null, true))
                return DirectEve.ThreadedCall(btn.Attribute(funcName));
            return false;
        }

        /// <summary>
        /// Closes the window
        /// Container windows are a special case and can't be closed as we are opening them automatically while
        /// retrieving the corresponding container. Use forceCloseContainerWnd with caution!
        /// </summary>
        /// <param name="forceCloseContainerWnd"></param>
        /// <returns></returns>
        public virtual bool Close(bool forceCloseContainerWnd = false)
        {
            if (!Name.Equals("NewFeatureNotifyWnd") && !IsKillable)
                return false;

            if (!forceCloseContainerWnd && this.GetType() == typeof(DirectContainerWindow))
            {
                DirectEve.Log($"Container windows can't be closed.");
                return false;
            }

            if (DirectEve.Interval(400, 700, null, true))
            {
                return DirectEve.ThreadedCall(PyWindow.Attribute("CloseByUser"));
            }
            return false;
        }

        public int GetModalResult(ModalResultType mr)
        {
            var modalResult = 0;
            switch (mr)
            {
                case ModalResultType.NONE:
                    modalResult = 0;
                    break;

                case ModalResultType.OK:
                    modalResult = 1;
                    break;

                case ModalResultType.CANCEL:
                    modalResult = 2;
                    break;

                case ModalResultType.YES:
                    modalResult = 6;
                    break;

                case ModalResultType.NO:
                    modalResult = 7;
                    break;

                case ModalResultType.CLOSE:
                    modalResult = 8;
                    break;

                case ModalResultType.HELP:
                    modalResult = 9;
                    break;
            }

            return modalResult;
        }

        public bool SetModalResult(ModalResultType mr)
        {
            var modalResult = GetModalResult(mr);
            if (IsModal)
                return DirectEve.ThreadedCall(PyWindow.Attribute("SetModalResult"), new object[] { modalResult });
            return false;
        }



        /// <summary>
        ///     Find a child object (usually button)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal static PyObject FindChild(PyObject container, string name)
        {
            var childs = container.Attribute("children").Attribute("_childrenObjects").ToList();
            var ret = childs.Find(c => String.Compare((string)c.Attribute("name"), name) == 0) ?? PySharp.PyZero;

            if (ret == PySharp.PyZero && container.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").IsValid)
            {
                childs = container.Attribute("children").Attribute("_childrenObjects").Attribute("_childrenObjects").ToList();
                ret = childs.Find(c => String.Compare((string)c.Attribute("name"), name) == 0) ?? PySharp.PyZero;
            }
            return ret;
        }

        /// <summary>
        ///     Find a child object (using the supplied path)
        /// </summary>
        /// <param name="container"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        internal static PyObject FindChildWithPath(PyObject container, IEnumerable<string> path)
        {
            return path.Aggregate(container, FindChild);
        }

        internal static List<DirectWindow> GetModalWindows(DirectEve directEve)
        {
            var windows = new List<DirectWindow>();

            var pySharp = directEve.PySharp;
            var carbonui = pySharp.Import("carbonui");
            var pyWindows = carbonui.Attribute("uicore").Attribute("uicore").Attribute("registry").Attribute("windows").ToList();
            foreach (var pyWindow in pyWindows)
            {
                if ((bool)pyWindow.Attribute("destroyed"))
                    continue;

                var name = pyWindow.Attribute("name");
                var nameStr = name.IsValid ? name.ToUnicodeString() : String.Empty;

                if (nameStr.Equals("modal") || (bool)pyWindow.Attribute("isModal"))
                {
                    var window = new DirectWindow(directEve, pyWindow);
                    windows.Add(window);
                }

                if (nameStr == "telecom")
                {
                    var window = new DirectTelecomWindow(directEve, pyWindow);
                    windows.Add(window);
                }
            }

            return windows;
        }

        internal static List<DirectWindow> GetWindows(DirectEve directEve)
        {
            var windows = new List<DirectWindow>();
            if (_windowTypeDict == null)
            {
                SetupDict();
            }

            var pySharp = directEve.PySharp;
            var carbonui = pySharp.Import("carbonui");
            var pyWindows = carbonui.Attribute("uicore").Attribute("uicore").Attribute("registry")
                .Attribute("windows").ToList();
            foreach (var pyWindow in pyWindows)
            {
                // Ignore destroyed windows
                if ((bool)pyWindow.Attribute("destroyed"))
                    continue;

                DirectWindow window = null;

                foreach (var kv in _windowTypeDict)
                {
                    var attr = pyWindow.Attribute(kv.Key).ToUnicodeString();

                    if (attr == null)
                        continue;

                    var dict = kv.Value;

                    if (dict.TryGetValue(attr, out var type))
                    {
                        window = type.Creator(directEve, pyWindow);
                        break;
                    }
                }

                if (window == null)
                    window = new DirectWindow(directEve, pyWindow);

                windows.Add(window);
            }

            return windows;
        }

        #endregion Methods

        #region Nested type: WindowType

        private class WindowType
        {
            #region Constructors

            public WindowType(string attribute, string value, Func<DirectEve, PyObject, DirectWindow> creator)
            {
                Attribute = attribute;
                Value = value;
                Creator = creator;
            }

            #endregion Constructors

            #region Properties

            public string Attribute { get; set; }
            public Func<DirectEve, PyObject, DirectWindow> Creator { get; set; }
            public string Value { get; set; }

            #endregion Properties
        }

        #endregion Nested type: WindowType
    }
}