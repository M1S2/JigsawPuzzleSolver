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

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Convert PuzzleSavingState enum values to icon filenames that can be used to display icons in an image control
    /// </summary>
    [ValueConversion(typeof(PuzzleSavingStates), typeof(string))]
    public class PuzzleSavingStateToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch ((PuzzleSavingStates)value)
            {
                case PuzzleSavingStates.NEW_UNSAVED:
                    return null;
                case PuzzleSavingStates.SAVED:
                    return "/JigsawPuzzleSolver;component/Resources/Save_16x.png";
                case PuzzleSavingStates.LOADED:
                    return "/JigsawPuzzleSolver;component/Resources/OpenFolder_16x.png";
                case PuzzleSavingStates.SAVING:
                case PuzzleSavingStates.LOADING:
                    return "/JigsawPuzzleSolver;component/Resources/Hourglass_16x.png";
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
