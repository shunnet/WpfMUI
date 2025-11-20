// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ICellDefinitionFactory.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies a cell definition factory for the DataGrid.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    /// <summary>
    /// Specifies a <see cref="CellDefinition" /> factory for the <see cref="DataGrid" />.
    /// </summary>
    public interface ICellDefinitionFactory
    {
        /// <summary>
        /// Creates the cell definition for the specified cell.
        /// </summary>
        /// <param name="d">The cell descriptor.</param>
        /// <returns>
        /// The cell definition
        /// </returns>
        CellDefinition CreateCellDefinition(CellDescriptor d);
    }
}