// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ColumnDefinition.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Defines a column in a DataGrid.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System.Windows;

    /// <summary>
    /// Defines a column in a <see cref="DataGrid" />.
    /// </summary>
    public class ColumnDefinition : PropertyDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ColumnDefinition" /> class.
        /// </summary>
        public ColumnDefinition()
        {
            this.Width = GridLength.Auto;
        }

        /// <summary>
        /// Gets or sets the column width.
        /// </summary>
        /// <value>The width.</value>
        public GridLength Width { get; set; }
    }
}