extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Framework
{
    public class DirectKeyActivationWindow : DirectWindow
    {
        #region Constructors

        internal DirectKeyActivationWindow(DirectEve directEve, PyObject pyWindow)
            : base(directEve, pyWindow)
        {
        }

        public enum AbyssSelectedGameMode
        {
            Cruiser = 1,
            Destroyers = 2,
            Frigates = 3,
        }

        #endregion Constructors

        #region Properties

        public bool AnyError => CurrentController.Attribute("errors").ToList().Any();
        public bool IsFinished => CurrentController.Attribute("isFinished").ToBool();
        public bool IsJumping => CurrentController.Attribute("isJumping").ToBool();
        public bool IsReady => CurrentController.Attribute("isReady").ToBool();
        public float Tier => Controller.Attribute("tier").ToFloat();
        public string TierDescription => Controller.Attribute("tierDescription").ToUnicodeString();
        public string TimerDescription => Controller.Attribute("timerDescription").ToUnicodeString();
        public int TypeId => Controller.Attribute("typeID").ToInt();
        public float Weather => Controller.Attribute("weather").ToInt();
        public string WeatherDescription => Controller.Attribute("weatherDescription").ToUnicodeString();
        public string WeatherName => Controller.Attribute("weatherName").ToUnicodeString();
        private PyObject Controller => PyWindow.Attribute("controller");


        private PyObject CurrentController => GameModeCombo["selectedValue"].ToInt() switch
        {
            1 => ActivationController,
            2 => TwoPlayerActivationController,
            3 => CoOpActivationController,
            _ => null
        };

        private PyObject ActivationController => Controller["activationController"];
        private PyObject TwoPlayerActivationController => Controller["twoPlayerActivationController"];
        private PyObject CoOpActivationController => Controller["coOpActivationController"];

        private PyObject BottomContainer => PyWindow.Attribute("bottomCont");
        private PyObject GameModeCombo => FindChildWithPath(BottomContainer, new List<string>() { "ContainerAutoSize", "abyssGameModeCombo" });

        public AbyssSelectedGameMode SelectedGameMode => GameModeCombo["selectedValue"].ToInt() switch
        {
            1 => AbyssSelectedGameMode.Cruiser,
            2 => AbyssSelectedGameMode.Destroyers,
            3 => AbyssSelectedGameMode.Frigates,
            _ => AbyssSelectedGameMode.Cruiser,
        };

        public bool SelectGameMode(AbyssSelectedGameMode gameMode)
        {
            if (SelectedGameMode != gameMode)
            {
                DirectEve.ThreadedCall(GameModeCombo.Attribute("SelectItemByIndex"), (int)gameMode - 1);
                return true;
            }
            return false;
        }


        public bool IsCorrectDropDownSelected
        {
            get
            {
                return DirectEve.ActiveShip.Entity.IsFrigate && SelectedGameMode == AbyssSelectedGameMode.Frigates ||
                            DirectEve.ActiveShip.Entity.IsDestroyer && SelectedGameMode == AbyssSelectedGameMode.Destroyers ||
                            DirectEve.ActiveShip.Entity.IsCruiser && SelectedGameMode == AbyssSelectedGameMode.Cruiser;
            }
        }

        public void SelectCorrectDropdown()
        {
            if (DirectEve.ActiveShip.Entity.IsFrigate)
                SelectGameMode(AbyssSelectedGameMode.Frigates);

            if (DirectEve.ActiveShip.Entity.IsDestroyer)
                SelectGameMode(AbyssSelectedGameMode.Destroyers);

            if (DirectEve.ActiveShip.Entity.IsCruiser)
                SelectGameMode(AbyssSelectedGameMode.Cruiser);

        }

        #endregion Properties

        #region Methods

        public bool Activate()
        {
            if (!IsCorrectDropDownSelected)
            {
                SelectCorrectDropdown();
                return false;
            }

            if (IsJumping || IsFinished || AnyError || !IsReady)
                return false;

            DirectSession.SetSessionNextSessionReady(4500, 6000);
            return DirectEve.ThreadedCall(CurrentController.Attribute("Activate"));
        }

        public override string ToString()
        {
            return $"{nameof(Controller)}: {Controller}, {nameof(TypeId)}: {TypeId}, {nameof(Weather)}: {Weather}, {nameof(WeatherDescription)}: {WeatherDescription}, {nameof(WeatherName)}: {WeatherName}, {nameof(Tier)}: {Tier}, {nameof(TierDescription)}: {TierDescription}, {nameof(TimerDescription)}: {TimerDescription}, {nameof(IsReady)}: {IsReady}, {nameof(IsJumping)}: {IsJumping}, {nameof(IsFinished)}: {IsFinished}, {nameof(AnyError)}: {AnyError}";
        }

        #endregion Methods
    }
}