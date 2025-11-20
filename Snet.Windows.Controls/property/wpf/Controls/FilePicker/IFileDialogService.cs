// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IFileDialogService.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   The file dialog interface.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    /// <summary>
    /// The file dialog interface.
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Shows the open file dialog.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="defaultExtension">The default extension.</param>
        /// <returns>
        /// True if the user pressed ok.
        /// </returns>
        bool ShowOpenFileDialog(ref string filename, string filter, string defaultExtension);

        /// <summary>
        /// Shows the open files dialog.
        /// </summary>
        /// <param name="filenames">The filenames.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="defaultExtension">The default extension.</param>
        /// <returns>
        /// True if the user pressed ok.
        /// </returns>
        bool ShowOpenFilesDialog(ref string[] filenames, string filter, string defaultExtension);

        /// <summary>
        /// Shows the save file dialog.
        /// </summary>
        /// <param name="filename">The filename.</param>
        /// <param name="filter">The filter.</param>
        /// <param name="defaultExtension">The default extension.</param>
        /// <returns>
        /// True if the user pressed ok.
        /// </returns>
        bool ShowSaveFileDialog(ref string filename, string filter, string defaultExtension);
    }
}