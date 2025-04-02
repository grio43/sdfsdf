extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SC::SharedComponents.Py;

namespace EVESharpCore.Framework
{

    public enum DurationComboValue
    {
        IMMEDIATE = 0,
        DAY = 1,
        THREEDAYS = 3,
        WEEK = 7,
        TWOWEEKS = 14,
        MONTH = 30,
        THREEMONTHS = 90,
    }

    public class DirectMultiSellWindow : DirectWindow
    {
        internal DirectMultiSellWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
        }

        private List<double> _getSums;

        public long BaseStationId => PyWindow.Attribute("baseStationID").ToLong();

        public double BrokersFee => GetSums()[0];

        public double SalesTax => GetSums()[1];
        public double TotalSum => GetSums()[2];

        private List<double> GetSums()
        {
            if (_getSums == null)
            {
                var obj = PyWindow.Call("GetSums").ToList<double>();
                if (obj.Count > 2)
                    obj[2] = obj[2] - obj[1] - obj[0];
                _getSums = obj;
            }
            return _getSums;
        }

        public DurationComboValue GetDurationComboValue()
        {
            var val = PyWindow.Attribute("durationCombo").Attribute("selectedValue").ToInt();
            return (DurationComboValue)val;
        }

        public void SetDurationCombovalue(DurationComboValue v)
        {
            PyWindow.Attribute("durationCombo").Call("SetValue", (int)v);
        }

        public void PerformTrade()
        {
            if (GetSellItems().All(i => !i.HasBid))
            {
                DirectEve.Log($"Can't perform trade, only items without a bid are within the sell list.");
                return;
            }
            DirectEve.ThreadedCall(PyWindow.Attribute("PerformTrade"));
        }

        public void Cancel()
        {
            DirectEve.ThreadedCall(PyWindow.Attribute("Cancel"));
        }

        public bool AddingItemsThreadRunning => !PyWindow.Attribute("addItemsThread").Attribute("endTime").IsValid;

        public List<DirectMultiSellWindowItem> GetSellItems()
        {
            var ret = new List<DirectMultiSellWindowItem>();
            var list = PyWindow.Attribute("itemList").ToList();
            foreach (var item in list)
            {
                ret.Add(new DirectMultiSellWindowItem(DirectEve, item));
            }
            return ret;
        }
    }
}
