// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ContentAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies that the value contains content that should be handled by a ContentControl.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies that the value contains content that should be handled by a ContentControl.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ContentAttribute : Attribute
    {
    }
}