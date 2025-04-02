using System.Linq;
using EVESharpCore.Controllers.Questor.Core.Actions.Base;
using EVESharpCore.Logging;

namespace EVESharpCore.Controllers.Questor.Core.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private void IgnoreAction(Action action)
        {
            if (!bool.TryParse(action.GetParameterValue("clear"), out var clear))
                clear = false;

            var add = action.GetParameterValues("add");
            var remove = action.GetParameterValues("remove");

            if (clear)
            {
                IgnoreTargets.Clear();
            }
            else
            {
                add.ForEach(a => IgnoreTargets.Add(a.Trim()));
                remove.ForEach(a => IgnoreTargets.Remove(a.Trim()));
            }

            Log.WriteLine("Updated ignore list");
            if (IgnoreTargets.Any())
                Log.WriteLine("Currently ignoring: " + IgnoreTargets.Aggregate((current, next) => "[" + current + "][" + next + "]"));
            else
                Log.WriteLine("Your ignore list is empty");

            Nextaction();
            return;
        }

        #endregion Methods
    }
}