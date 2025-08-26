// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BasePathPropertyAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies a base path property for relative path names.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies a base path property for relative path names.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class BasePathPropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BasePathPropertyAttribute" /> class.
        /// </summary>
        /// <param name="basePathPropertyName">Name of the base path property.</param>
        public BasePathPropertyAttribute(string basePathPropertyName)
        {
            this.BasePathPropertyName = basePathPropertyName;
        }

        /// <summary>
        /// Gets or sets the name of the base path property.
        /// </summary>
        /// <value>The name of the base path property.</value>
        public string BasePathPropertyName { get; set; }
    }
}