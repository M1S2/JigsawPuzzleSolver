using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Media;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert PuzzleSavingState enum values to GeometryDrawing representation of the SavingState icon
    /// </summary>
    [ValueConversion(typeof(PuzzleSavingStates), typeof(GeometryDrawing))]
    public class PuzzleSavingStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string iconDataStr = null;
            Brush foregroundBrush = App.Current.TryFindResource("IdealForegroundColorBrush") as Brush;

            switch ((PuzzleSavingStates)value)
            {
                case PuzzleSavingStates.PUZZLE_NULL: return null;
                case PuzzleSavingStates.NEW_UNSAVED:
                    iconDataStr = (new PackIconModern() { Kind = PackIconModernKind.Page }).Data; break;
                case PuzzleSavingStates.SAVED:
                    iconDataStr = (new PackIconModern() { Kind = PackIconModernKind.Save }).Data; break;
                case PuzzleSavingStates.LOADED:
                    iconDataStr = (new PackIconModern() { Kind = PackIconModernKind.FolderOpen }).Data; break;
                case PuzzleSavingStates.SAVING:
                case PuzzleSavingStates.LOADING:
                    iconDataStr = (new PackIconModern() { Kind = PackIconModernKind.Hourglass }).Data; break;
                case PuzzleSavingStates.ERROR:
                    iconDataStr = (new PackIconEntypo() { Kind = PackIconEntypoKind.CircleWithCross }).Data; break;
                default:
                    return null;
            }

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
