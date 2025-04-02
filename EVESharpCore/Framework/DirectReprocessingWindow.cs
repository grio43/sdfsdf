﻿// ------------------------------------------------------------------------------
//   <copyright from='2010' to='2015' company='THEHACKERWITHIN.COM'>
//     Copyright (c) TheHackerWithin.COM. All Rights Reserved.
//
//     Please look in the accompanying license.htm file for the license that
//     applies to this source code. (a copy can also be found at:
//     http://www.thehackerwithin.com/license.htm)
//   </copyright>
// -------------------------------------------------------------------------------

extern alias SC;

using SC::SharedComponents.Py;
using System.Collections.Generic;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectReprocessingWindow : DirectWindow
    {
        #region Fields

        private List<DirectReprocessingQuote> _quotes;

        #endregion Fields

        #region Constructors

        internal DirectReprocessingWindow(DirectEve directEve, PyObject pyWindow) : base(directEve, pyWindow)
        {
            NeedsQuote = !(bool)pyWindow.Attribute("sr").Attribute("quotesBtn").Attribute("disabled");
        }

        #endregion Constructors

        #region Properties

        /// <summary>
        ///     Do we need a new quote?
        /// </summary>
        public bool NeedsQuote { get; set; }

        /// <summary>
        ///     The quote given by the game (note, if NeedQuote is true, this will return the previous loaded quotes)
        /// </summary>
        public List<DirectReprocessingQuote> Quotes
        {
            get
            {
                if (_quotes == null)
                {
                    _quotes = new List<DirectReprocessingQuote>();
                    foreach (var quote in PyWindow.Attribute("quotes").ToDictionary<long>())
                        _quotes.Add(new DirectReprocessingQuote(DirectEve, quote.Key, quote.Value));
                }

                return _quotes;
            }
        }

        #endregion Properties

        #region Methods

        /// <summary>
        ///     Update quote information
        /// </summary>
        /// <returns></returns>
        public bool GetQuotes()
        {
            if (!NeedsQuote)
                return false;

            return DirectEve.ThreadedCall(PyWindow.Attribute("OnGetQoutes"));
        }

        /// <summary>
        ///     Perform the actual reprocessing
        /// </summary>
        /// <returns></returns>
        public bool Reprocess()
        {
            if (NeedsQuote)
                return false;

            return DirectEve.ThreadedCall(PyWindow.Attribute("OnOK"));
        }

        #endregion Methods
    }
}