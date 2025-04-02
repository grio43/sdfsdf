// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Controllers.Questor.Core.States;

namespace EVESharpCore.Controllers.Questor.Core.Actions.Base
{
    public class Action
    {
        #region Constructors

        public Action()
        {
            Parameters = new Dictionary<string, List<string>>();
        }

        #endregion Constructors

        #region Properties

        public Dictionary<string, List<string>> Parameters { get; private set; }
        public ActionState State { get; set; }

        #endregion Properties

        #region Methods

        public void AddParameter(string parameter, string value)
        {
            if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(value))
                return;

            List<string> values;
            if (!Parameters.TryGetValue(parameter.ToLower(), out values))
                values = new List<string>();

            values.Add(value);
            Parameters[parameter.ToLower()] = values;
        }

        public string GetParameterValue(string parameter)
        {
            if (!Parameters.TryGetValue(parameter.ToLower(), out var values))
                return null;

            return values.FirstOrDefault();
        }

        public List<string> GetParameterValues(string parameter)
        {
            if (!Parameters.TryGetValue(parameter.ToLower(), out var values))
                return new List<string>();

            return values;
        }

        public override string ToString()
        {
            var output = State.ToString();

            foreach (var key in Parameters.Keys)
                foreach (var value in Parameters[key])
                    output += string.Format(" [{0}: {1}]", key, value);

            return output;
        }

        #endregion Methods
    }
}