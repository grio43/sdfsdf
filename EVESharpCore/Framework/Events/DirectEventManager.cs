/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 29.08.2016
 * Time: 20:25
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using EVESharpCore.Cache;
using SC::SharedComponents.Events;
using SC::SharedComponents.IPC;
using System;
using System.Threading.Tasks;

namespace EVESharpCore.Framework.Events
{
    /// <summary>
    ///     Description of DirectEventManager.
    /// </summary>
    public static class DirectEventManager
    {
        #region Methods

        public static void NewEvent(DirectEvent directEvent)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        WCFClient.Instance.GetPipeProxy.OnDirectEvent(ESCache.Instance.CharName, directEvent);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        #endregion Methods
    }
}