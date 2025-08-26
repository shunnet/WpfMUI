// --------------------------------------------------------------------------------------------------------------------
// <copyright file="EnableByRadioButtonAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the name of a property that controls the enabled/disabled state of the attributed property.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies the name of a property that controls the enabled/disabled state of the attributed property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class EnableByRadioButtonAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EnableByRadioButtonAttribute" /> class.
        /// </summary>
        /// <param name="propertyName">Name of the enumeration property.</param>
        /// <param name="value">The enumeration value that enables the attributed property.</param>
        public EnableByRadioButtonAttribute(string propertyName, object value)
        {
            this.PropertyName = propertyName;
            this.Value = value;
        }

        /// <summary>
        /// Gets the name of the property that determines if the decorated property is enabled or not.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName { get; private set; }

        /// <summary>
        /// Gets the value that enables the decorated property.
        /// </summary>
        /// <value>The value.</value>
        public object Value { get; private set; }
    }
}