// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TextBlockEx.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Represents a TextBlock than can be disabled.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents a TextBlock than can be disabled.
    /// </summary>
    public class TextBlockEx : TextBlock
    {
        /// <summary>
        /// Initializes static members of the <see cref="TextBlockEx" /> class.
        /// </summary>
        static TextBlockEx()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TextBlockEx), new FrameworkPropertyMetadata(typeof(TextBlockEx)));
        }
    }
}