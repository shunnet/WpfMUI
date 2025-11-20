// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ISortDescriptionComparer.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Defines a comparer that uses a collection of sort descriptions.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System.Collections;
    using System.ComponentModel;

    /// <summary>
    /// Defines a comparer that uses a collection of sort descriptions.
    /// </summary>
    /// <seealso cref="System.Collections.IComparer" />
    public interface ISortDescriptionComparer : IComparer
    {
        /// <summary>
        /// Gets the sort descriptions.
        /// </summary>
        /// <value>
        /// The sort descriptions.
        /// </value>
        SortDescriptionCollection SortDescriptions { get; }
    }
}