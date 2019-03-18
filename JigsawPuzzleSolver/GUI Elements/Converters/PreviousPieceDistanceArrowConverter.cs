using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert a point (distance to last piece) to an arrow path control data
    /// </summary>
    [ValueConversion(typeof(System.Drawing.Point), typeof(GeometryDrawing))]
    public class PreviousPieceDistanceArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Drawing.Point point = (System.Drawing.Point)value;
            string direction = (string)parameter;
            string iconDataStr = null;
            Brush foregroundBrush = App.Current.TryFindResource("AccentColorBrush") as Brush;

            if (direction == "X" && point.X < 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CaretLeftSolid }).Data; }
            else if (direction == "X" && point.X > 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CaretRightSolid }).Data; }
            else if (direction == "X" && point.X == 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CircleSolid }).Data; }
            else if (direction == "Y" && point.Y < 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CaretUpSolid }).Data; }
            else if (direction == "Y" && point.Y > 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CaretDownSolid }).Data; }
            else if (direction == "Y" && point.Y == 0) { iconDataStr = (new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CircleSolid }).Data; }

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
