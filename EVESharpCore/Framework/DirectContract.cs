/*
 * Created by SharpDevelop.
 * User: duketwo
 * Date: 12.12.2016
 * Time: 16:30
 *
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public enum AvailabilityOptions
    {
        PUBLIC = 0,
        PRIVATE = 1,
    }

    public enum ContractType
    {
        TYPE_ITEMEXCHANGE = 1,
        TYPE_AUCTION = 2,
        TYPE_COURIER = 3
    }

    public enum ExpireTime
    {
        ONE_DAY = 24 * 60,
        THREE_DAYS = 24 * 60 * 3,
        ONE_WEEK = 24 * 60 * 7,
        TWO_WEEKS = 24 * 60 * 14
    }

    public class CourierDestination
    {
        #region Constructors

        public CourierDestination(int id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion Constructors

        #region Properties

        public int Id { get; set; }
        public string Name { get; set; }

        #endregion Properties
    }

    public class CourierProvider
    {
        #region Constructors

        public CourierProvider(int id, string name)
        {
            Id = id;
            Name = name;
        }

        #endregion Constructors

        #region Properties

        public int Id { get; set; }
        public string Name { get; set; }

        #endregion Properties
    }

    /// <summary>
    ///     Description of DirectContract.
    /// </summary>
    public class DirectContract : DirectObject
    {
        #region Fields

        public CourierDestination AMARR = new CourierDestination(60008494, "Amarr VIII (Oris) - Emperor Family Academy");
        public CourierDestination JITA = new CourierDestination(60003760, "Jita IV - Moon 4 - Caldari Navy Assembly Plant");
        public CourierProvider PUSH_X = new CourierProvider(98079862, "Push Industries");

        // https://api.eveonline.com/eve/CharacterID.xml.aspx?names=Push%20Industries
        // https://api.eveonline.com/eve/CharacterID.xml.aspx?names=Red%20Frog%20Freight
        public CourierProvider RF_Freight = new CourierProvider(1495741119, "Red Frog Freight");

        private static Random RANDOM = new Random();

        private DateTime _nextLoadPageInfo = DateTime.MinValue;

        #endregion Fields

        //		        def FinishStep2(self):
        //        if hasattr(self.data, 'price'):
        //            self.data.price = int(self.data.price)
        //        if hasattr(self.data, 'reward'):
        //            self.data.reward = int(self.data.reward)
        //        if hasattr(self.data, 'collateral'):
        //            self.data.collateral = int(self.data.collateral)
        //        if len(self.data.description) > MAX_TITLE_LENGTH:

        //    def FinishStep2_CourierContract(self):
        //        if not self.data.endStation and len(self.form.sr.endStationName.GetValue()) > 0:
        //            self.SearchStationFromEdit(self.form.sr.endStationName)
        //            if not self.data.endStation:
        //                return False
        //        if not self.data.endStation:
        //            errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMustSpecifyContractDestination')
        //            raise UserError('CustomInfo', {'info': errorLabel})
        //        if not self.data.assigneeID:
        //            if self.data.reward < MIN_CONTRACT_MONEY:
        //                errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMinimumRewardNotMet', minimum=MIN_CONTRACT_MONEY)
        //                raise UserError('CustomInfo', {'info': errorLabel})
        //            if self.data.collateral < MIN_CONTRACT_MONEY:
        //                errorLabel = GetByLabel('UI/Contracts/ContractsService/UserErrorMinimumCollateralNotMet', minimum=MIN_CONTRACT_MONEY)
        //                raise UserError('CustomInfo', {'info': errorLabel})
        //        return True

        // DEST CAN'T BE CURRENT STATION!!! xD
        //(22:33:37) duketwo: Hold on there! You can't deliver a courier package to the same place where it came from, that would be ridiculous!
        //(22:33:37) duketwo: Ahh, you almost had me there. This is a joke, right? Very funny! Now go on and select another destination, you big comedian.

        #region Constructors

        public DirectContract(DirectEve directEve) : base(directEve)
        {
        }

        #endregion Constructors

        #region Properties

        public bool CanFinishCourierContract
        {
            get
            {
                if (IsCreateContractWindowOpen)
                {
                    var wnd = GetCreateContractWindow;
                    var data = wnd.PyWindow.Attribute("data");
                    if (data.IsValid)
                    {
                        var reward = data.HasAttrString("reward");
                        var expTime = data.HasAttrString("expiretime");
                        var name = data.HasAttrString("name");
                        var assigneeID = data.HasAttrString("assigneeID");
                        var type = data.HasAttrString("type");
                        var endStationName = data.HasAttrString("endStationName");
                        var endStation = data.HasAttrString("endStation");
                        var duration = data.HasAttrString("duration");
                        var avail = data.HasAttrString("avail");
                        return reward && expTime && name && assigneeID && type && endStationName &&
                               endStation && duration && avail;
                    }
                }

                return false;
            }
        }

        public DirectWindow GetCreateContractWindow
        {
            get { return DirectEve.Windows.FirstOrDefault(w => w.Guid.Equals("form.CreateContract")); }
        }

        public bool IsCreateContractWindowOpen => GetCreateContractWindow != null;

        #endregion Properties

        #region Methods

        public void CreateContract(IEnumerable<DirectItem> items)
        {
            if (!items.Any())
                return;

            var contractsSvc = DirectEve.GetLocalSvc("contracts");
            if (contractsSvc.IsValid)
            {
                var keywords = new Dictionary<string, object>();
                keywords.Add("items", items.Select(i => i.PyItem));
                DirectEve.ThreadedCallWithKeywords(contractsSvc.Attribute("OpenCreateContract"), keywords);
            }
        }

        public bool CreateContract()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                var wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("CreateContract").ToBool();
            }

            return false;
        }

        public bool FinishStep1()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                var wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("FinishStep1").ToBool();
            }

            return false;
        }

        public bool FinishStep2()
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                var wnd = GetCreateContractWindow;

                return wnd.PyWindow.Call("FinishStep2").ToBool();
            }

            return false;
        }

        public int GetNumContractsLeft()
        {
            return DirectEve.GetLocalSvc("contracts").Attribute("myPageInfo").Attribute("numContractsLeft").ToInt();
        }

        public void GotoPage(int n)
        {
            if (CanFinishCourierContract && IsCreateContractWindowOpen)
            {
                var wnd = GetCreateContractWindow;
                DirectEve.ThreadedCall(wnd.PyWindow.Attribute("GotoPage"), n);
            }
        }

        public bool IsPageInfoLoaded()
        {
            return DirectEve.GetLocalSvc("contracts").Attribute("myPageInfo").IsValid;
        }

        public bool LoadPageInfo()
        {
            if (_nextLoadPageInfo < DateTime.UtcNow)
            {
                _nextLoadPageInfo = DateTime.UtcNow.AddMilliseconds(RANDOM.Next(4000, 7000));
                DirectEve.ThreadedLocalSvcCall("contracts", "CollectMyPageInfo");
                return true;
            }

            return false;
        }

        public void SetAssignee(CourierProvider provider)
        {
            var id = provider.Id;
            var name = provider.Name;
            SetAssigneeId(id);
            SetAssigneeName(name);
        }

        public void SetAvailabilityOptions(AvailabilityOptions option)
        {
            var val = (int)option;
            SetDataValue("avail", val);
        }

        public void SetCollateral(int collateral)
        {
            SetDataValue("collateral", collateral);
        }

        public void SetContractType(ContractType type)
        {
            var typeInt = (int)type;
            SetDataValue("type", typeInt);
        }

        public void SetCourierContract(int reward, int collateral, int durationDays, ExpireTime expireTime, CourierDestination destination,
                                            CourierProvider provider)
        {
            var type = ContractType.TYPE_COURIER;
            SetContractType(type);
            SetReward(reward);
            SetCollateral(collateral);
            SetDuration(durationDays);
            SetExpireTime(expireTime);
            SetAssignee(provider);
            SetCourierDestination(destination);
            SetDescription(String.Empty);
            SetAvailabilityOptions(AvailabilityOptions.PRIVATE);
        }

        public void SetCourierDestination(CourierDestination dest)
        {
            var id = dest.Id;
            var name = dest.Name;
            SetEndStation(id);
            SetEndStationName(name);
        }

        public void SetDescription(string description)
        {
            SetDataValue("description", description);
        }

        public void SetDuration(int days)
        {
            if (days < 1 || days > 30)
                return;
            SetDataValue("duration", days);
        }

        public void SetExpireTime(ExpireTime expireTime)
        {
            var expTime = (int)expireTime;
            SetDataValue("expiretime", expTime);
        }

        public void SetPrice(int price)
        {
            // not implemented yet... only courier contracts are supported for now
        }

        public void SetReward(int reward)
        {
            SetDataValue("reward", reward);
        }

        private void SetAssigneeId(int id)
        {
            SetDataValue("assigneeID", id);
        }

        private void SetAssigneeName(string name)
        {
            SetDataValue("name", name);
        }

        private void SetDataValue(string key, object value)
        {
            if (IsCreateContractWindowOpen)
            {
                var wnd = GetCreateContractWindow;
                var data = wnd.PyWindow.Attribute("data");
                if (data.IsValid) DirectEve.ThreadedCall(data.Attribute("Set"), key, value);
            }
        }

        private void SetEndStation(int stationId)
        {
            SetDataValue("endStation", stationId);
        }

        private void SetEndStationName(string name)
        {
            SetDataValue("endStationName", name);
        }

        #endregion Methods
    }
}