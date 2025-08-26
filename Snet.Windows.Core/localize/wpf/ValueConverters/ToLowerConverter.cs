﻿

namespace Snet.Windows.Core.localize.wpf.ValueConverters
{
    #region Usings
    using System;
    using System.Globalization;
    using System.Windows.Data;
    #endregion

    /// <summary>
    /// ToLowerConverter return the value as value.ToUpper()
    /// </summary>
    public class ToLowerConverter : TypeValueConverterBase, IValueConverter
    {
        #region IValueConverter
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return value.ToString().ToLower();
            }

            return null;
        }

        /// <inheritdoc/>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
