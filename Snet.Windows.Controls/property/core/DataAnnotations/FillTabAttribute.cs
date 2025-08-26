// --------------------------------------------------------------------------------------------------------------------
// <copyright file="FillTabAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies that the control representing the property should fill the entire size of the tab page.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies that the control representing the property should fill the entire size of the tab page.
    /// </summary>
    /// <remarks>This requires only one property on the tab page.</remarks>
    [AttributeUsage(AttributeTargets.Property)]
    public class FillTabAttribute : Attribute
    {
    }
}