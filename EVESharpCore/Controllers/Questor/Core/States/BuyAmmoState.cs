/*
 * ---------------------------------------
 * User: duketwo
 * Date: 09.10.2015
 * Time: 18:36
 *
 * ---------------------------------------
 */

namespace EVESharpCore.Controllers.Questor.Core.States
{
    public enum BuyAmmoState
    {
        Idle,
        ActivateTransportShip,
        CreateBuyList,
        TravelToDestinationStation,
        BuyAmmo,
        MoveItemsToCargo,
        TravelToHomeSystem,
        Done,
        Error,
        DisabledForThisSession
    }
}