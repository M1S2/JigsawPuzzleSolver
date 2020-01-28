using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Media;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{    
    /// <summary>
    /// Convert System.Drawing.Color to SolidColorBrush
    /// </summary>
    [ValueConversion(typeof(System.Drawing.Color), typeof(SolidColorBrush))]
    class ColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            System.Drawing.Color color1 = (System.Drawing.Color)value;
            return new SolidColorBrush(System.Windows.Media.Color.FromRgb(color1.R, color1.G, color1.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}
