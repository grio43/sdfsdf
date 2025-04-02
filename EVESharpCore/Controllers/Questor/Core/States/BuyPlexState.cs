/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 28.08.2016
 * Time: 02:28
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

namespace EVESharpCore.Controllers.Questor.Core.States
{
    /// <summary>
    ///     Description of BuyPlexState.
    /// </summary>
    public enum BuyPlexState
    {
        Idle,
        ActivateShuttle,
        TravelToDestinationStation,
        BuyPlex,
        AddOmegaCloneTime,
        Done,
        Error,
        DisabledForThisSession
    }
}