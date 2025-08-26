// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InputDirectionAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the input direction for the decorated property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies the input direction for the decorated property.
    /// </summary>
    public class InputDirectionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDirectionAttribute" /> class.
        /// </summary>
        /// <param name="inputDirection">The input direction.</param>
        public InputDirectionAttribute(InputDirection inputDirection)
        {
            this.InputDirection = inputDirection;
        }

        /// <summary>
        /// Gets or sets the input direction.
        /// </summary>
        /// <value>The input direction.</value>
        public InputDirection InputDirection { get; set; }
    }
}