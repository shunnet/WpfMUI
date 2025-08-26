

namespace Snet.Windows.Core.localize.wpf.ValueConverters
{
    #region Usings
    using System;
    using System.Globalization;
    using System.Windows.Data;
    #endregion

    /// <summary>
    /// ToUpperConverter return the value as value.ToUpper()
    /// </summary>
    public class ToUpperConverter : TypeValueConverterBase, IValueConverter
    {
        #region IValueConverter
        /// <inheritdoc/>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value != null)
            {
                return value.ToString().ToUpper();
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
