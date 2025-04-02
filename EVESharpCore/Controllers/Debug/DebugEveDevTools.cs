extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using SC::SharedComponents.Py;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers.Debug
{
    public partial class DebugEveDevTools : Form
    {
        public DebugEveDevTools()
        {
            InitializeComponent();
            InitializeButtons();
        }
        private List<PyObject> Tools => ESCache.Instance.DirectEve.PySharp.Import("eve")["devtools"]["script"]["enginetools"]["TOOLS"].ToList();

        private void InitializeButtons()
        {
            ActionQueueAction action = null;
            action = new ActionQueueAction(new Action(() =>
            {
                try
                {
                   
                    foreach (var tool in Tools)
                    {
                        var innerList = tool.ToList();
                        var name = innerList[0].ToUnicodeString();
                        var wnd = innerList[1];
                        var role = innerList[2];

                        if (wnd.IsValid)
                        {
                            this.flowLayoutPanel1.Invoke(new Action(() =>
                            {
                                Button newButton = new Button();    
                                newButton.Text = name;
                                Size preferredSize = TextRenderer.MeasureText(newButton.Text, newButton.Font) + new Size(10, 20);
                                newButton.Size = preferredSize;
                                newButton.Click += (sender, e) =>
                                {

                                    action = new ActionQueueAction(new Action(() =>
                                    {
                                        try
                                        {
                                            ModifyButtons(false);
                                            var wnd = Tools.Where(e => e.ToList()[0].ToUnicodeString().Equals(name)).FirstOrDefault().ToList()[1];
                                            var call = wnd["Open"];
                                            if (wnd.IsValid && call.IsValid)
                                            {
                                                ESCache.Instance.DirectEve.ThreadedCall(call);
                                            }
                                            ModifyButtons(true);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log.WriteLine(ex.ToString());
                                        }
                                    }));

                                    action.Initialize().QueueAction();

                                };
                                flowLayoutPanel1.Controls.Add(newButton);
                            }));
                        }
                    } 
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            }));
            action.Initialize().QueueAction();
        }

        private void ModifyButtons(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (var b in Controls)
                    if (b is Button button && !((Button)b).Text.Equals("Cancel"))
                    {
                        button.Enabled = enabled;
                    }
            }));
        }
    }
}
