﻿#region Copyright information
// <copyright file="CatExtension.cs">
//     Licensed under Microsoft Public License (Ms-PL)
//     https://github.com/Snet.Windows.Core.localize.core/Snet.Windows.Core.localize.core/blob/master/LICENSE
// </copyright>
// <author>Uwe Mayer</author>
#endregion

namespace Snet.Windows.Core.localize.core.Strings
{
    #region Uses
    using Snet.Windows.Core.localize.core.Base;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows.Markup;
    #endregion

    /// <summary>
    /// A string concatenation extension.
    /// </summary>
    [MarkupExtensionReturnType(typeof(String))]
    [ContentProperty("Items"), DefaultProperty("Items")]
    public class CatExtension : NestedMarkupExtension
    {
        private readonly List<object> items = new List<object>();
        /// <summary>
        /// Gets the list of items.
        /// </summary>
        public List<object> Items
        {
            get { return items; }
        }

        private string format = "";
        /// <summary>
        /// Gets or sets the format string.
        /// </summary>
        public string Format
        {
            get { return format; }
            set
            {
                if (format != value)
                {
                    format = value;
                    UpdateNewValue();
                }
            }
        }

        private readonly PropertyInfo pi;

        private IServiceProvider serviceProvider;

        /// <inheritdoc/>
        public CatExtension()
        {
            pi = this.GetType().GetProperty("Items");
        }

        /// <inheritdoc/>
        protected override void OnServiceProviderChanged(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
            base.OnServiceProviderChanged(serviceProvider);
        }

        /// <inheritdoc/>
        public override object FormatOutput(TargetInfo endPoint, TargetInfo info)
        {
            string s = Format;

            if (s == null)
                return null;

            for (int i = 0; i < items.Count; i++)
            {
                string tag = "{" + i + "}";

                if (s.Contains(tag))
                {
                    object t = items[i];

                    if (t is INestedMarkupExtension nme)
                    {
                        TargetInfo ti = new TargetInfo(this, pi, pi.PropertyType, i);

                        if (nme.IsConnected(ti))
                            t = nme.FormatOutput(endPoint, ti);
                        else
                            t = GetValue<object>(t, pi, i, endPoint, serviceProvider);
                    }

                    if (t != null)
                        s = s.Replace(tag, t.ToString());
                }
            }

            return s;
        }

        /// <inheritdoc/>
        protected override bool UpdateOnEndpoint(TargetInfo endpoint)
        {
            return false;
        }
    }
}
