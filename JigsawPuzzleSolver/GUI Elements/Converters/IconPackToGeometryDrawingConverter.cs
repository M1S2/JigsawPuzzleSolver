using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Globalization;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using MahApps.Metro;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert a MahApps.Metro.IconPack control to an GeometryDrawing
    /// </summary>
    /// see: https://github.com/MahApps/MahApps.Metro.IconPacks/issues/39
    /// see: https://gist.github.com/Phyxion/160a6f04e6083016d4b2a3aed3c4fe71
    [ValueConversion(typeof(object), typeof(GeometryDrawing))]
    public class IconPackToGeometryDrawingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if(value == null) { return null; }

            Brush foregroundBrush = App.Current.TryFindResource("IdealForegroundColorBrush") as Brush;

            PackIconBase iconBase = ((PackIconBase)value);
            string iconDataStr = iconBase.GetType().GetProperty("Data").GetValue(iconBase, null) as string;
            
            Geometry iconGeometry = Geometry.Parse(iconDataStr);
            GeometryDrawing iconGeometryDrawing = new GeometryDrawing(foregroundBrush, new Pen(foregroundBrush, 1), iconGeometry);

            return iconGeometryDrawing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
