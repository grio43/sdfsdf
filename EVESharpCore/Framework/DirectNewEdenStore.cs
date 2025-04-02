using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Framework
{
    public class DirectNewEdenStore : DirectObject
    {
        // Constants for all the valid view states for the EVE client
        private const string Login = "login";
        private const string Intro = "intro";
        private const string CharacterSelector = "charsel";
        private const string Space = "inflight";
        private const string Station = "station";
        private const string Hangar = "hangar";
        private const string StarMap = "starmap";
        private const string DockPanel = "dockpanelview";
        private const string SystemMap = "systemmap";
        private const string Planet = "planet";
        private const string ShipTree = "shiptree";
        private const string ActivityTracker = "activitytracker";
        private const string CharacterCreation = "charactercreation";
        private const string VirtualGoodsStore = "virtual_goods_store";
        private const string Structure = "structure";

        // VGS Store category constants
        private const string CATEGORY_GAME_TIME = "gametime";
        private const string CATEGORY_PLEX = "plex";

        public DirectNewEdenStore(DirectEve directEve) : base(directEve)
        {

        }

        public bool IsStoreOpen
        {
            get
            {
                var viewStateSvc = DirectEve.GetLocalSvc("viewState");
                if (viewStateSvc.IsValid)
                {
                    var isViewActive = viewStateSvc.Attribute("IsViewActive");
                    if (isViewActive.IsValid)
                    {
                        return viewStateSvc.Call("IsViewActive", VirtualGoodsStore).ToBool();
                    }
                }
                return false;
            }
        }

        public void OpenStore()
        {
            if (IsStoreOpen)
                return;

            var vgsService = DirectEve.GetLocalSvc("vgsService");
            if (vgsService.IsValid)
            {
                var openStore = vgsService.Attribute("OpenStore");
                if (openStore.IsValid)
                {
                    DirectEve.ThreadedCallWithKeywords(openStore, new Dictionary<string, object>() { { "categoryTag", CATEGORY_GAME_TIME } });
                }
            }
        }

        public void CloseStore()
        {
            if (!IsStoreOpen)
                return;

            DirectEve.ExecuteCommand(DirectCmd.ToggleAurumStore);
        }

        public void ShowOffer(int id)
        {
            if (!IsStoreOpen)
                return;

            var vgsService = DirectEve.GetLocalSvc("vgsService");
            if (vgsService.IsValid)
            {
                var showOffer = vgsService.Attribute("ShowOffer");
                if (showOffer.IsValid)
                {
                    DirectEve.ThreadedCallWithKeywords(showOffer, new Dictionary<string, object>() { { "offerId", id } });
                }
            }
        }

        public bool IsOfferOpen()
        {
            var vgsService = DirectEve.GetLocalSvc("vgsService");
            if (vgsService.IsValid)
            {
                var offerContainer = vgsService.Attribute("uiController").Attribute("detailContainer").Attribute("offerContainer");
                if (offerContainer.IsValid)
                {
                    return !offerContainer.Attribute("destroyed").ToBool();
                }
            }
            return false;
        }

        public DirectNewEdenStoreOffer Offer
        {
            get
            {
                var vgsService = DirectEve.GetLocalSvc("vgsService");
                if (vgsService.IsValid)
                {
                    var offer = vgsService.Attribute("uiController").Attribute("detailContainer").Attribute("offer");
                    var detailContainer = vgsService.Attribute("uiController").Attribute("detailContainer");
                    if (offer.IsValid)
                    {
                        return new DirectNewEdenStoreOffer(DirectEve, offer, detailContainer);
                    }
                }
                return null;
            }
        }
    }
}
