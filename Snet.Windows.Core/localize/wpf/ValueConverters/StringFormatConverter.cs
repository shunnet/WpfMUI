
namespace Snet.Windows.Core.localize.wpf.ValueConverters
{
    #region Usings
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Data;
    #endregion

    /// <summary>
    /// Takes the first value as StringFormat and the other values as Parameter for the StringFormat
    /// </summary>
    public class StringFormatConverter : TypeValueConverterBase, IMultiValueConverter
    {
        /// <summary>
        /// True, if the SmartFormat assembly is available. Determined exactly once per app domain.
        /// </summary>
        private static readonly bool IsSmartFormatAvailable;

        /// <summary>
        /// The cached SmartFormat.Format(string, object[]) MethodInfo. Reflection happens exactly once.
        /// </summary>
        private static readonly MethodInfo SmartFormatMethod;

        /// <summary>
        /// Static constructor - performs the SmartFormat reflection exactly once per app domain.
        /// </summary>
        static StringFormatConverter()
        {
            try
            {
                // try to load SmartFormat Assembly
                var asSmartFormat = Assembly.Load("SmartFormat");
                var tt = asSmartFormat.GetType("SmartFormat.Smart");
                SmartFormatMethod = tt.GetMethod("Format", BindingFlags.Static | BindingFlags.Public, null, new Type[] { typeof(string), typeof(object[]) }, null);
                IsSmartFormatAvailable = SmartFormatMethod != null;
            }
            catch
            {
                // fallback just take String.Format
                SmartFormatMethod = null;
                IsSmartFormatAvailable = false;
            }
        }

        #region IMultiValueConverter
        /// <inheritdoc/>
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (!targetType.IsAssignableFrom(typeof(string)))
                throw new Exception("TargetType is not supported strings");

            if (values == null || values.Length < 1)
                throw new Exception("Not enough parameters");

            if (values[0] == null)
                return null;

            if (values.Length > 1 && values[1] == DependencyProperty.UnsetValue)
                return null;

            var format = values[0].ToString();
            if (values.Length == 1)
                return format;

            var args = values.Skip(1).ToArray();

            // Direct call in the common case (no SmartFormat); reflection only when SmartFormat is actually present.
            if (IsSmartFormatAvailable)
                return (string)SmartFormatMethod.Invoke(null, new object[] { format, args });

            return string.Format(format, args);
        }

        /// <inheritdoc/>
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
        #endregion
    }
}
