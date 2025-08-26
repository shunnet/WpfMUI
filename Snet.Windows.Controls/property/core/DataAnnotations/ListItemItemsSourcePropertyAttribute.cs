// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ListItemItemsSourcePropertyAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the name of a property that contains values for the items in the decorated list property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies the name of a property that contains values for the items in the decorated list property.
    /// </summary>
    public class ListItemItemsSourcePropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListItemItemsSourcePropertyAttribute" /> class.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        public ListItemItemsSourcePropertyAttribute(string propertyName)
        {
            this.PropertyName = propertyName;
        }

        /// <summary>
        /// Gets or sets the name of the property.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; set; }
    }
}