extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{
    public class DirectMultiSellWindowItem : DirectObject
    {
        private PyObject _pyObj;

        internal DirectMultiSellWindowItem(DirectEve directEve, PyObject obj) : base(directEve)
        {
            _pyObj = obj;
        }

        public float AveragePrice => _pyObj.Attribute("averagePrice").ToFloat();

        public float BestPrice => _pyObj.Attribute("bestPrice").ToFloat();

        public float PricePercentage => BestPrice / (AveragePrice / 100);

        public int BrokersFee => _pyObj.Attribute("brokersFee").ToInt();

        public float BrokersFeePerc => _pyObj.Attribute("brokersFeePerc").ToFloat();

        public bool HasBid => _pyObj.Attribute("bestBid").IsValid && _pyObj.Attribute("estimatedSellCount").ToInt() != 0;

        public long ItemId => _pyObj.Attribute("itemID").ToLong();

        public string ItemName => _pyObj.Attribute("itemName").ToUnicodeString();

        public long LocationId => _pyObj.Attribute("locationID").ToLong();

        public long RegionId => _pyObj.Attribute("regionID").ToLong();

        public long StationId => _pyObj.Attribute("stationID").ToLong();

        public int SolarSystemId => _pyObj.Attribute("solarSystemID").ToInt();

        public void RemoveItem()
        {
            DirectEve.ThreadedCall(_pyObj.Attribute("RemoveItem"));
        }

        public override string ToString()
        {
            return $"{nameof(AveragePrice)}: {AveragePrice}, {nameof(BestPrice)}: {BestPrice}," +
                   $" {nameof(BrokersFee)}: {BrokersFee}, {nameof(BrokersFeePerc)}: {BrokersFeePerc}," +
                   $" {nameof(ItemId)}: {ItemId}, {nameof(ItemName)}: {ItemName}, {nameof(LocationId)}: {LocationId}," +
                   $" {nameof(RegionId)}: {RegionId}, {nameof(StationId)}: {StationId}, {nameof(SolarSystemId)}: {SolarSystemId}";
        }
    }
}
