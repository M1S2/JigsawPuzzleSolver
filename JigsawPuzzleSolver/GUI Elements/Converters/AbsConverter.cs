using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Return the absolute value.
    /// </summary>
    [ValueConversion(typeof(double), typeof(double))]
    public class AbsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Math.Abs(System.Convert.ToDouble(value));
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
