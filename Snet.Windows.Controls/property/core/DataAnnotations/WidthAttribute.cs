// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WidthAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the width of the editing control.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies the width of the editing control.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class WidthAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WidthAttribute" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        public WidthAttribute(double width)
        {
            this.Width = width;
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        public double Width { get; set; }
    }
}