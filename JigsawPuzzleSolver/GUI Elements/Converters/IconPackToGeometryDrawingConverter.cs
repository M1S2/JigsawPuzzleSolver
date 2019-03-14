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
            Brush foregroundBrush = App.Current.TryFindResource("IdealForegroundColorBrush") as Brush;
            string iconDataStr = null;
            
            if (value is PackIconFontAwesomeKind) { iconDataStr = (new PackIconFontAwesome() { Kind = (PackIconFontAwesomeKind)value }).Data; }
            else if (value is PackIconMaterialKind) { iconDataStr = (new PackIconMaterial() { Kind = (PackIconMaterialKind)value }).Data; }
            else if (value is PackIconMaterialLightKind) { iconDataStr = (new PackIconMaterialLight() { Kind = (PackIconMaterialLightKind)value }).Data; }
            else if (value is PackIconModernKind) { iconDataStr = (new PackIconModern() { Kind = (PackIconModernKind)value }).Data; }
            else if (value is PackIconEntypoKind) { iconDataStr = (new PackIconEntypo() { Kind = (PackIconEntypoKind)value }).Data; }
            else if (value is PackIconOcticonsKind) { iconDataStr = (new PackIconOcticons() { Kind = (PackIconOcticonsKind)value }).Data; }

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
