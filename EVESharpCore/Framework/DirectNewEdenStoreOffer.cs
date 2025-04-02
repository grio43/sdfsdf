extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public class DirectNewEdenStoreOffer : DirectObject
    {
        private readonly PyObject offer;
        private readonly PyObject detailContainer;

        public DirectNewEdenStoreOffer(DirectEve directEve, PyObject offer, PyObject detailContainer) : base(directEve)
        {
            this.offer = offer;
            this.detailContainer = detailContainer;
        }

        public string OfferName => offer.Attribute("name").ToUnicodeString(); // "30 Day Omega Clone time"
        public float Price => offer.Attribute("offerPricings").ToList()[0].Attribute("price").ToFloat(); // 500
        public int OfferId => offer.Attribute("id").ToInt(); // 2293

        public void BuyOffer()
        {
            var buyButton = detailContainer.Attribute("activeBottomPanel").Attribute("buyButton");
            if (buyButton.IsValid)
            {
                DirectEve.ThreadedCall(buyButton.Attribute("OnClick"));
            }
        }

        public bool DoesBuyButtonExist()
        {
            var buyButton = detailContainer.Attribute("activeBottomPanel").Attribute("buyButton");
            if (buyButton.IsValid)
            {
                var onClick = buyButton.Attribute("OnClick");
                if (onClick.IsValid)
                    return true;
            }
            return false;
        }
    }
}
