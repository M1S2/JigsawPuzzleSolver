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
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static readonly DependencyProperty ProcessedImgSourceProperty = DependencyProperty.Register("ProcessedImgSource", typeof(ImageSource), typeof(MainWindow), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        /// <summary>
        /// Processed Image source property
        /// </summary>
        public ImageSource ProcessedImgSource
        {
            get { return (ImageSource)GetValue(ProcessedImgSourceProperty); }
            set { SetValue(ProcessedImgSourceProperty, value); }
        }

        //##############################################################################################################################################################################################

        public MainWindow()
        {
            InitializeComponent();
            list_imageDescriptions.ItemsSource = ProcessedImagesStorage.ImageDescriptions;
            this.DataContext = this;
        }

        //##############################################################################################################################################################################################

        Puzzle puzzle;
        
        private void btn_init_puzzle_Click(object sender, RoutedEventArgs e)
        {
            ProcessedImagesStorage.ClearAllImages();

            //puzzle = new Puzzle(@"..\..\..\Test_Pictures\ScannedImages", 20, 50, false);

            puzzle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen\Test1.png", 20, 50, true);
            //puzzle = new Puzzle(@"..\..\..\Scans\ToyStoryBack", 20, 50, true);
        }

        private void btn_solve_puzzle_Click(object sender, RoutedEventArgs e)
        {
            puzzle.solve();
        }

        private void list_imageDescriptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(list_imageDescriptions.SelectedItem == null) { return; }
            ProcessedImgSource = Utils.BitmapToImageSource(ProcessedImagesStorage.GetImage(list_imageDescriptions.SelectedItem.ToString()));
        }
    }
}
