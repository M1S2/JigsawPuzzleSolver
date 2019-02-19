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
using System.Windows.Shell;

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert PuzzleSolverState enum values to TaskbarItemProgressState
    /// </summary>
    [ValueConversion(typeof(PuzzleSolverState), typeof(TaskbarItemProgressState))]
    public class PuzzleStateToProgressBarStateConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((PuzzleSolverState)value)
            {
                case PuzzleSolverState.ERROR:
                    return TaskbarItemProgressState.Error;
                case PuzzleSolverState.UNSOLVED:
                case PuzzleSolverState.SOLVED:
                    return TaskbarItemProgressState.None;
                case PuzzleSolverState.INIT_PIECES:
                case PuzzleSolverState.COMPARE_EDGES:
                case PuzzleSolverState.SOLVE_PUZZLE:
                    return TaskbarItemProgressState.Normal;
                default:
                    return null;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
