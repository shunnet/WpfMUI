// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFolderBrowserDialogService.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   The browser dialog interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    /// <summary>
    /// The browser dialog interface.
    /// </summary>
    public interface IFolderBrowserDialogService
    {
        /// <summary>
        /// Shows the folder browser dialog.
        /// </summary>
        /// <param name="directory">The directory.</param>
        /// <param name="showNewFolderButton">show the new folder button if set to <c>true</c> .</param>
        /// <param name="description">The description.</param>
        /// <param name="useDescriptionForTitle">Use description for title if set to <c>true</c> .</param>
        /// <returns>
        /// True if the user pressed ok.
        /// </returns>
        bool ShowFolderBrowserDialog(
            ref string directory,
            bool showNewFolderButton = true,
            string description = null,
            bool useDescriptionForTitle = true);
    }
}