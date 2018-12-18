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
        public MainWindow()
        {
            InitializeComponent();
            list_imageDescriptions.ItemsSource = ProcessedImagesStorage.ImageDescriptions;
        }
        
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
            img_Processed.Source = Utils.BitmapToImageSource(ProcessedImagesStorage.GetImage(list_imageDescriptions.SelectedItem.ToString()));
        }
    }
}
