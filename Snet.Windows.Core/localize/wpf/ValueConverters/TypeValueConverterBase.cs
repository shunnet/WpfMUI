

namespace Snet.Windows.Core.localize.wpf.ValueConverters
{
    #region Usings
    using System;
    using System.Windows.Markup;
    #endregion

    /// <summary>
    /// Baseclass for ValueTypeConvertes which implements easy usage as MarkupExtension
    /// </summary>
    public abstract class TypeValueConverterBase : MarkupExtension
    {
        #region MarkupExtension
        /// <inheritdoc/>
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return this;
        }
        #endregion
    }
}
