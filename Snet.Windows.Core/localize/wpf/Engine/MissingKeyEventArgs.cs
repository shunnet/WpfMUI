﻿

namespace Snet.Windows.Core.localize.wpf.Engine
{
    #region Usings
    using System;
    #endregion

    /// <summary>
    /// Event arguments for a missing key event.
    /// </summary>
    public class MissingKeyEventArgs : EventArgs
    {
        /// <summary>
        /// The key that is missing or has no data.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// A flag indicating that a reload should be performed.
        /// </summary>
        public bool Reload { get; set; }

        /// <summary>
        /// A custom returnmessage for the missing key
        /// </summary>
        public string MissingKeyResult { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="MissingKeyEventArgs"/>.
        /// </summary>
        /// <param name="key">The missing key.</param>
        public MissingKeyEventArgs(string key)
        {
            Key = key;
            Reload = false;
            MissingKeyResult = null;
        }
    }
}
