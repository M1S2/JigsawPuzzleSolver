using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;

namespace TextBlockPathEllipsis
{
    /// <summary>
    /// Interaction logic for TextBlockPathEllipsisControl.xaml
    /// The control must be emdedded into a Grid (or something like that). This Grid handles the size changed events because the TextBlock doesn't fire the SizeChanged event correctly.
    /// </summary>
    public partial class TextBlockPathEllipsisControl : TextBlock
    {
        private FrameworkElement _parentElement;        // Element that handles the resizing events of the textblock

        /// <summary>
        /// Path that should be shown in the TextBlock
        /// </summary>
        public string Path
        {
            get { return (string)GetValue(PathProperty); }
            set { SetValue(PathProperty, value); }
        }
        public static readonly DependencyProperty PathProperty = DependencyProperty.Register("Path", typeof(string), typeof(TextBlockPathEllipsisControl), new PropertyMetadata("", new PropertyChangedCallback(OnPathChanged)));

        /// <summary>
        /// Recalculate the TextBlock.Text property if the Path changes
        /// </summary>
        public static void OnPathChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            TextBlockPathEllipsisControl ctrl = (TextBlockPathEllipsisControl)obj;
            ctrl.Text = ctrl.GetTrimmedPath();
        }

        /// <summary>
        /// Hook up the Loaded event for the control
        /// </summary>
        public TextBlockPathEllipsisControl()
        {
            InitializeComponent();
            this.Loaded += TextBlockPathEllipsisControl_Loaded;
        }

        /// <summary>
        /// Find the visual parent of the TextBlockPathEllipsisControl and hook up the SizeChanged event for this parent control.
        /// </summary>
        private void TextBlockPathEllipsisControl_Loaded(object sender, RoutedEventArgs e)
        {
            _parentElement = VisualTreeHelper.GetParent(this) as FrameworkElement;
            if (_parentElement != null) { _parentElement.SizeChanged += TextBlockPathEllipsisControl_SizeChanged; }
        }

        /// <summary>
        /// Recalculate the TextBlock.Text property if the size changes
        /// </summary>
        private void TextBlockPathEllipsisControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Text = GetTrimmedPath();
        }

        /// <summary>
        /// Calculate the trimmed path.
        /// </summary>
        /// <returns>Trimmed path</returns>
        /// see: https://www.codeproject.com/Tips/467054/WPF-PathTrimmingTextBlock
        private string GetTrimmedPath()
        {
            if(string.IsNullOrEmpty(Path)) { return ""; }

            string filename = System.IO.Path.GetFileName(Path);
            string directory = System.IO.Path.GetDirectoryName(Path);
            FormattedText formattedText;
            bool widthOK = false;
            bool changedWidth = false;

            do
            {
                formattedText = new FormattedText(directory + "...\\" + filename, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, FontFamily.GetTypefaces().First(), FontSize, Foreground);
                widthOK = formattedText.Width < (_parentElement.ActualWidth - this.Margin.Left - this.Margin.Right - 30);

                if (!widthOK)
                {
                    changedWidth = true;
                    directory = directory.Substring(0, directory.Length - 1);
                    if (directory.Length == 0) return "...\\" + filename;
                }
            } while (!widthOK);

            if (!changedWidth)
            {
                return Path;
            }

            return directory + "..." + filename;
        }
    }
}
