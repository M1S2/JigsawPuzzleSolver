using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Markup;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert a point (distance to last piece) to an arrow path control data
    /// </summary>
    [ValueConversion(typeof(System.Drawing.Point), typeof(bool))]
    public class PreviousPieceDistanceArrowConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Drawing.Point point = (System.Drawing.Point)value;
            string direction = (string)parameter;
            Geometry path = null;

            if (direction == "X" && point.X < 0) { path = Geometry.Parse("M 0 10 L 10 0 L 10 20 Z"); }
            else if (direction == "X" && point.X > 0) { path = Geometry.Parse("M 0 0 L 10 10 L 0 20 Z"); }
            else if (direction == "X" && point.X == 0) { path = Geometry.Parse("M 0 10 A 10 10 180 1 0 0 9 Z"); }
            else if (direction == "Y" && point.Y < 0) { path = Geometry.Parse("M 0 10 L 10 0 L 20 10 Z"); }
            else if (direction == "Y" && point.Y > 0) { path = Geometry.Parse("M 0 0 L 20 0 L 10 10 Z"); }
            else if (direction == "Y" && point.Y == 0) { path = Geometry.Parse("M 0 10 A 10 10 180 1 0 0 9 Z"); }

            return path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
