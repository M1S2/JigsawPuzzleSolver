using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace JigsawPuzzleSolver.Plugins.Core
{
    /// <summary>
    /// Represents a chain of <see cref="IValueConverter"/>s to be executed in succession.
    /// </summary>
    /// https://stackoverflow.com/questions/1594357/wpf-how-to-use-2-converters-in-1-binding/8392590
    [ContentProperty("Converters")]
    [ContentWrapper(typeof(Collection<IValueConverter>))]
    public class ConverterChain : IValueConverter
    {
        /// <summary>Gets the converters to execute.</summary>
        public Collection<IValueConverter> Converters { get; } = new Collection<IValueConverter>();

        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Converters.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Converters.Reverse().Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
        }

        #endregion
    }
}
