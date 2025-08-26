// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProgressCellDefinition.cs" company="PropertyTools">
//   Copyright (c) 2014 PropertyTools contributors
// </copyright>
// <summary>
//   Defines a cell that contains a progress value property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    /// <summary>
    /// Defines a cell that contains a progress value property.
    /// </summary>
    /// <seealso cref="Snet.Windows.Controls.property.wpf.CellDefinition" />
    public class ProgressCellDefinition : CellDefinition
    {
        /// <summary>
        /// Gets or sets the minimum value of the progress bar control.
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Gets or sets the maximum value of the progress bar control.
        /// </summary>
        public double Maximum { get; set; }
    }
}