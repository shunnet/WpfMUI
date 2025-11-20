// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PopupBox.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// <summary>
//   Represents a popup control that provides a data template for the popup.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    /// Represents a popup control that provides a data template for the popup.
    /// </summary>
    public class PopupBox : ComboBox
    {
        /// <summary>
        /// Identifies the <see cref="PopupTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PopupTemplateProperty = DependencyProperty.Register(
            nameof(PopupTemplate),
            typeof(DataTemplate),
            typeof(PopupBox),
            new UIPropertyMetadata(null));

        /// <summary>
        /// Initializes static members of the <see cref="PopupBox" /> class.
        /// </summary>
        static PopupBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(PopupBox), new FrameworkPropertyMetadata(typeof(PopupBox)));
        }

        /// <summary>
        /// Gets or sets the popup template.
        /// </summary>
        /// <value>The popup template.</value>
        public DataTemplate PopupTemplate
        {
            get
            {
                return (DataTemplate)this.GetValue(PopupTemplateProperty);
            }

            set
            {
                this.SetValue(PopupTemplateProperty, value);
            }
        }
    }
}