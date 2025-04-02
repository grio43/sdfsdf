extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Logging;
using System;
using System.IO;
using Util = SC::SharedComponents.Utility.Util;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class DumpAgentsAction : Base.ActionQueueAction
    {
        #region Constructors

        public DumpAgentsAction()
        {
            Action = new Action(() =>
            {
                try
                {
                    var filename = Util.AssemblyPath + Path.DirectorySeparatorChar + "AllAgentsByName.txt";

                    Log.WriteLine($"Dumping agents to {filename}");

                    if (File.Exists(filename))
                        File.Delete(filename);

                    var n = Environment.NewLine;
                    var str = "public static Dictionary<string, long> AllAgentsByName = new Dictionary<string, long>()" + n + "{";

                    foreach (var agent in ESCache.Instance.DirectEve.GetAllAgents())
                        str += "{" + "\"" + agent.Key + "\"" + "," + agent.Value + "}," + n;

                    str += n + "};";

                    File.WriteAllText(filename, str);
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception: " + ex);
                    return;
                }
            });
        }

        #endregion Constructors
    }
}