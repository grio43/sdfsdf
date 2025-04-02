// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

namespace EVESharpCore.Framework
{
    extern alias SC;

    // a scan result is like an entity but we cannot base directly of DirectEntity
    // as it stands today.  Maybe in the future DirectEntity can handle scan results
    // directly.
    public class DirectDirectionalScanResult : DirectInvType
    {
        #region Fields

        private DirectEntity _directEntity;
        private int _groupId;
        private long _itemId;
        private string _name;

        #endregion Fields

        #region Constructors

        internal DirectDirectionalScanResult(DirectEve directEve, long itemId, int groupId, int typeId)
            : base(directEve)
        {
            _itemId = itemId; // [0] ballId
            _groupId = groupId; // [1] groupId
            TypeId = typeId; // [2] typeId
        }

        #endregion Constructors

        #region Properties

        public double Distance => Entity != null && Entity.IsValid ? _directEntity.Distance : 0;

        public DirectEntity Entity
        {
            get
            {
                if (_directEntity == null)
                {
                    var ballpark = DirectEve.GetLocalSvc("michelle").Call("GetBallpark");
                    var slimItem = ballpark.Call("GetInvItem", _itemId);
                    var ball = ballpark.Call("GetBall", _itemId);
                    if (slimItem.IsValid && ball.IsValid)
                        _directEntity = new DirectEntity(DirectEve, ballpark, ball, slimItem, _itemId);
                }

                return _directEntity;
            }
        }

        public string Name
        {
            get
            {
                if (_name == null)
                {
                    var c = new DirectConst(DirectEve);
                    _name = TypeName;
                    if (GroupId == (int)c["groupHarvestableCloud"])
                        _name = (string)PySharp.Import("localization").Call("GetByLabel", "UI/Inventory/SlimItemNames/SlimHarvestableCloud", _name);
                    else if (CategoryId == (int)c["categoryAsteroid"])
                        _name = (string)PySharp.Import("localization").Call("GetByLabel", "UI/Inventory/SlimItemNames/SlimAsteroid", _name);
                    else
                        _name = DirectEve.GetLocationName(_itemId);
                }

                return _name;
            }
        }

        #endregion Properties
    }
}