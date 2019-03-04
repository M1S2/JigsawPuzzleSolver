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

namespace JigsawPuzzleSolver.GUI_Elements.Converters
{
    /// <summary>
    /// Return if it's the fifth element in the listbox (the middle element when using 9 elements)
    /// </summary>
    /// see: https://stackoverflow.com/questions/12125764/change-style-of-last-item-in-listbox
    [ValueConversion(typeof(DependencyObject), typeof(bool))]
    public class SurroundingPiecesIsMiddlePieceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DependencyObject item = (DependencyObject)value;
            ItemsControl ic = ItemsControl.ItemsControlFromItemContainer(item);
            return ic.ItemContainerGenerator.IndexFromContainer(item) == 4;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
