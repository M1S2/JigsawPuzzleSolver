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
    /// Convert PuzzleSolverState enum values to GeometryDrawing representation of the SolverState icon
    /// </summary>
    [ValueConversion(typeof(PuzzleSolverState), typeof(GeometryDrawing))]
    public class PuzzleStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Geometry iconGeometry = null;
            Brush foregroundBrush = App.Current.TryFindResource("IdealForegroundColorBrush") as Brush;

            switch ((PuzzleSolverState)value)
            {
                case PuzzleSolverState.UNSOLVED:
                    iconGeometry = Geometry.Parse((new PackIconModern() { Kind = PackIconModernKind.Page }).Data); break;
                case PuzzleSolverState.INIT_PIECES:
                    iconGeometry = Geometry.Parse((new PackIconMaterial() { Kind = PackIconMaterialKind.Magnify }).Data); break;
                case PuzzleSolverState.COMPARE_EDGES:
                    iconGeometry = Geometry.Parse((new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.EqualsSolid }).Data); break;
                case PuzzleSolverState.SOLVE_PUZZLE:
                    iconGeometry = Geometry.Parse((new PackIconModern() { Kind = PackIconModernKind.LayerAdd }).Data); break;
                case PuzzleSolverState.SOLVED:
                    iconGeometry = Geometry.Parse((new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.CheckSolid }).Data); break;
                case PuzzleSolverState.ERROR:
                    iconGeometry = Geometry.Parse((new PackIconEntypo() { Kind = PackIconEntypoKind.CircleWithCross }).Data); break;
                default:
                    return null;
            }
            
            GeometryDrawing iconGeometryDrawing = new GeometryDrawing(foregroundBrush, new Pen(foregroundBrush, 1), iconGeometry);
            return iconGeometryDrawing;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
