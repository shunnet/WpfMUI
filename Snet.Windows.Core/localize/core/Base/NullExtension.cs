#region Copyright information
// <copyright file="NullExtension.cs">
//     Licensed under Microsoft Public License (Ms-PL)
//     https://github.com/Snet.Windows.Core.localize.core/Snet.Windows.Core.localize.core/blob/master/LICENSE
// </copyright>
// <author>Uwe Mayer</author>
#endregion

namespace Snet.Windows.Core.localize.core.Base
{
    /// <summary>
    /// An extension that returns null.
    /// </summary>
    public class NullExtension : NestedMarkupExtension
    {
        /// <summary>
        /// This function returns the properly prepared output of the markup extension.
        /// </summary>
        /// <param name="info">Information about the target.</param>
        /// <param name="endPoint">Information about the endpoint.</param>
        public override object FormatOutput(TargetInfo endPoint, TargetInfo info)
        {
            return null;
        }

        /// <inheritdoc/>
        protected override bool UpdateOnEndpoint(TargetInfo endpoint)
        {
            return false;
        }
    }
}
