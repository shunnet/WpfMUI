// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutoUpdateTextAttribute.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Specifies that the text binding should be triggered at every change.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.core.DataAnnotations
{
    using System;

    /// <summary>
    /// Specifies that the text binding should be triggered at every change.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class AutoUpdateTextAttribute : Attribute
    {
    }
}