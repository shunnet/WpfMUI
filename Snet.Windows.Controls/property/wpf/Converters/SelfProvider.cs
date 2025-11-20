// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SelfProvider.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   The self provider.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System;
    using System.Windows.Markup;

    /// <summary>
    /// The self provider.
    /// </summary>
    [Obsolete]
    public abstract class SelfProvider : MarkupExtension
    {
        /// <summary>
        /// The provide value.
        /// </summary>
        /// <param name="serviceProvider">The service provider.</param>
        /// <returns>
        /// The provide value.
        /// </returns>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
    }
}