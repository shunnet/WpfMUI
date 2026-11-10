// --------------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultLocalizableOperator.cs" company="Snet.Windows.Controls.property.core">
//   Copyright (c) 2014 Snet.Windows.Controls.property.core contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace Snet.Windows.Controls.property.wpf.Operators
{
    using System;

    /// <summary>
    /// Provides a default implementation of a localizable operator that supports customizable localization of strings
    /// and descriptions.
    /// </summary>
    /// <remarks>This class allows for the delegation of localization logic to a custom operator by calling
    /// <see cref="UseLocalizableOperator"/>. If no custom operator is set, it returns the provided key as the localized
    /// value. This is useful as a fallback or base implementation for localization scenarios.</remarks>
    public class DefaultLocalizableOperator : ILocalizableOperator, ICustomLocalizableOperator
    {
        private ILocalizableOperator customLocalizableOperator;

        /// <inheritdoc/>        
        public void UseLocalizableOperator(ILocalizableOperator value)
        {
            if (value == this)
            {
                throw new ArgumentException("Cannot use itself as custom operator");
            }

            this.customLocalizableOperator = value;
        }

        /// <summary>
        /// Gets the localized description.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="declaringType">Type of the declaring.</param>
        /// <returns>
        /// The localized description.
        /// </returns>
        public virtual string GetLocalizedDescription(string key, Type declaringType)
        {
            if (this.customLocalizableOperator != null)
            {
                return this.customLocalizableOperator.GetLocalizedDescription(key, declaringType);
            }

            // 没有自定义本地化器时回退到字符串本地化（默认实现下两者都原样返回 key）：
            // PropertyGridOperator.SetLang 现在通过 GetLocalizedDescription 本地化描述，
            // 只重写了 GetLocalizedString 的子类（此前描述就是经由它本地化的）因此不会丢掉描述的本地化
            return this.GetLocalizedString(key, declaringType);
        }

        /// <summary>
        /// Gets the localized string.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="declaringType">The declaring type.</param>
        /// <returns>
        /// The localized string.
        /// </returns>
        public virtual string GetLocalizedString(string key, Type declaringType)
        {
            if (this.customLocalizableOperator != null)
            {
                return this.customLocalizableOperator.GetLocalizedString(key, declaringType);
            }

            return key;
        }
    }
}