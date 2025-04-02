extern alias SC;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Py;
using System;
using System.Linq;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Framework
{
    public class DirectAbyssActivationWindow : DirectWindow
    {
        #region Constructors

        internal DirectAbyssActivationWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        #endregion Constructors

        #region Properties

        public bool IsSuspicious => jumpController.Attribute("isSuspicious").ToBool();
        public bool AnyError => jumpController.Attribute("errors").ToList().Any();
        public bool IsActivating => jumpController.Attribute("isActivating").ToBool();
        public bool IsFinished => jumpController.Attribute("isFinished").ToBool();
        public bool IsJumping => jumpController.Attribute("isJumping").ToBool();
        public bool IsReady => jumpController.Attribute("isReady").ToBool();

        public float Tier => Controller.Attribute("tier").ToFloat();
        public string TierDescription => Controller.Attribute("tierDescription").ToUnicodeString();
        public string TimerDescription => Controller.Attribute("timerDescription").ToUnicodeString();
        public int TypeId => Controller.Attribute("typeID").ToInt();
        public float Weather => Controller.Attribute("weather").ToInt();
        public string WeatherDescription => Controller.Attribute("weatherDescription").ToUnicodeString();
        public string WeatherName => Controller.Attribute("weatherName").ToUnicodeString();
        private PyObject Controller => PyWindow.Attribute("controller");

        private PyObject jumpController => Controller["jumpController"];

        #endregion Properties

        #region Methods

        public bool Activate()
        {
            if (IsJumping)
            {
                Logging.Log.WriteLine("Activate: IsJumping: return false;");
                return false;
            }

            if (IsFinished)
            {
                Logging.Log.WriteLine("Activate: IsFinished: return false;");
                return false;
            }

            if (DirectEve.Entities.OrderBy(x => x.Distance).FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace).Distance > (double)Distances.GateActivationRange)
            {
                DirectEntity abyssalTrace = DirectEve.Entities.OrderBy(x => x.Distance).FirstOrDefault(i => i.GroupId == (int)Group.AbyssalTrace);
                Logging.Log.WriteLine("We are too far away from the abyssal trace to activate it. Distance: " + abyssalTrace.Distance);
                return false;
            }

            if (!IsReady)
            {
                Logging.Log.WriteLine("Activate: !IsReady: return false;");
                return false;
            }
            
            if (DirectEve.ThreadedCall(jumpController.Attribute("Activate")))
            {
                Logging.Log.WriteLine("DirectEve.ThreadedCall(jumpController.Attribute(Activate))");
                DirectSession.SetSessionNextSessionReady(8000, 9000);
                return true;
            }

            return false;
        }

        public override string ToString()
        {
            return $"{nameof(Controller)}: {Controller}, {nameof(TypeId)}: {TypeId}, {nameof(Weather)}: {Weather}, {nameof(WeatherDescription)}: {WeatherDescription}, {nameof(WeatherName)}: {WeatherName}, {nameof(Tier)}: {Tier}, {nameof(TierDescription)}: {TierDescription}, {nameof(TimerDescription)}: {TimerDescription}, {nameof(IsReady)}: {IsReady}, {nameof(IsJumping)}: {IsJumping}, {nameof(IsFinished)}: {IsFinished}, {nameof(AnyError)}: {AnyError}";
        }

        #endregion Methods
    }
}