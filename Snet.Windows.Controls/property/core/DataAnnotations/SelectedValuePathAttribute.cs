// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelectedValuePathAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the path used to get the selected value of an item.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies the path used to get the selected value of an item.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SelectedValuePathAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectedValuePathAttribute" /> class.
        /// </summary>
        /// <param name="path">The path used to get the selected value.</param>
        public SelectedValuePathAttribute(string path)
        {
            this.Path = path;
        }

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }
    }
}