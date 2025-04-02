extern alias SC;

using SC::SharedComponents.Py;
using System;

namespace EVESharpCore.Framework
{
    public class DirectBooster : DirectObject
    {
        #region Fields

        private PyObject _py;

        #endregion Fields

        #region Constructors

        internal DirectBooster(DirectEve directEve, PyObject py) : base(directEve)
        {
            _py = py;
            TypeID = _py.Attribute("boosterTypeID").ToInt();
            ExpireTime = _py.Attribute("expiryTime").ToDateTime();
            BoosterSlot = _py.Attribute("boosterSlot").ToFloat();
        }

        #endregion Constructors

        #region Properties

        public float BoosterSlot { get; private set; }
        public DateTime ExpireTime { get; private set; }
        public PyObject PyObject => _py;
        public int TypeID { get; private set; }

        #endregion Properties
    }

    //<KeyVal: {'typeID': 3898, 'boosterSlot': 1.0, 'expiryTime': 131556791200131939L, 'boosterID': 1026065316343L, 'boosterTypeID': 3898, 'boosterDuration': 5040000.0, 'sideEffectIDs': []}>
}