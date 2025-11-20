// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CategoryControlType.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies the control type for categories.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    /// <summary>
    /// Specifies the control type for categories.
    /// </summary>
    public enum CategoryControlType
    {
        /// <summary>
        /// No surrounding control
        /// </summary>
        None,

        /// <summary>
        /// Group boxes.
        /// </summary>
        GroupBox,

        /// <summary>
        /// Expander controls.
        /// </summary>
        Expander,

        /// <summary>
        /// Content control. Remember to set the CategoryControlTemplate.
        /// </summary>
        Template
    }
}