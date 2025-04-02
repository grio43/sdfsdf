using System;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectPlexVault : DirectObject
    {
        #region Constructors

        internal DirectPlexVault(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Methods

        public void BuyOmegaTime()
        {
            try
            {
                var window = DirectEve.Windows.FirstOrDefault(w =>
                    w.PyWindow.IsValid && w.PyWindow.Attribute("__guid__").ToUnicodeString().Equals("form.InventoryPrimary"));
                var obj = window.PyWindow.Attribute("invCont").Attribute("actions");
                if (obj.IsValid) DirectEve.ThreadedCall(obj.Attribute("BuyOmegaTime"));
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public float GetPlexVaultBalance()
        {
            try
            {
                if (IsPlexVaultOpen())
                {
                    var window = DirectEve.Windows.FirstOrDefault(w =>
                        w.PyWindow.IsValid && w.PyWindow.Attribute("__guid__").ToUnicodeString().Equals("form.InventoryPrimary"));
                    var ret = window.PyWindow.Attribute("invCont").Attribute("vault").Attribute("balance").ToFloat();

                    if (ret % 1 != 0)
                        return -1;
                    return ret;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return -1;
        }

        public bool IsPlexVaultOpen()
        {
            try
            {
                var window = DirectEve.Windows.FirstOrDefault(w =>
                    w.PyWindow.IsValid && w.PyWindow.Attribute("__guid__").ToUnicodeString().Equals("form.InventoryPrimary"));

                if (window == null)
                    return false;

                if (window.PyWindow.Attribute("invCont").Attribute("vault").IsValid)
                    return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            return false;
        }

        public void OpenPlexVault()
        {
            DirectEve.ExecuteCommand(DirectCmd.OpenPlexVault);
        }

        #endregion Methods

        //.view.BuyOffer();
        // carbonui.uicore.uicore.registry.windows[9].view.panel
        // carbonui.uicore.uicore.registry.windows[9].view.controller
        // get current plex vault value?
        //eve\client\script\ui\services\menuSvcExtras\menuFunctions.py -> def RedeemCurrency(item, qty):
        // sm.GetService('invCache').GetInventoryMgr().DepositPlexToVault(session.stationid or session.structureid, item.itemID, qty)
        // eve.client.script.environment.invControllers.PlexVault
        // aurBalance = sm.GetService('vgsService').GetStore().GetAccount().GetAurumBalance()
        // carbonui.uicore.uicore.registry.windows[5].invCont.vault.balance
        // carbonui.uicore.uicore.registry.windows[5].invCont.actions
    }
}