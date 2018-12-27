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

            PuzzleSolverParameters solverParameters = new PuzzleSolverParameters() { SolverShowDebugResults = false };
            //puzzle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen\Test\Test3.png", solverParameters);
            puzzle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen", solverParameters);
        }

        private void btn_solve_puzzle_Click(object sender, RoutedEventArgs e)
        {
            //CalculateCurvatureTests();

            puzzle.Solve();
        }

        private void list_imageDescriptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(list_imageDescriptions.SelectedItem == null) { return; }
            ProcessedImgSource = Utils.BitmapToImageSource(ProcessedImagesStorage.GetImage(list_imageDescriptions.SelectedItem.ToString()));
        }

        //##############################################################################################################################################################################################
        //##### TESTS ##################################################################################################################################################################################
        //##############################################################################################################################################################################################

        private void CalculateCurvatureTests()
        {
            VectorOfPoint contour = new VectorOfPoint();
            //for (double t = 0; t <= 2 * Math.PI; t += 0.01)
            //{
            //    contour.Push(new System.Drawing.Point((int)(50 * Math.Cos(t)) + 50, (int)(25 * Math.Sin(t)) + 50));
            //}

            for (int x = 10; x < 90; x++) { contour.Push(new System.Drawing.Point(x, 20)); }
            for (int y = 20; y < 80; y++) { contour.Push(new System.Drawing.Point(90, y)); }
            for (int x = 90; x > 10; x--) { contour.Push(new System.Drawing.Point(x, 80)); }
            for (int y = 80; y > 20; y--) { contour.Push(new System.Drawing.Point(10, y)); }

            Image<Rgb, byte> contourImg = new Image<Rgb, byte>(100, 100);
            for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(contourImg, contour[i], 1, new MCvScalar(0, 0, 255), 1); }
            ProcessedImagesStorage.AddImage("Contour", contourImg.Bitmap);

            List<double> curvature = Utils.CalculateCurvature(contour.ToArray().ToList(), 3);

            List<double> curvatureDraw = new List<double>(curvature);
            curvatureDraw = curvatureDraw.Select(c => (double.IsInfinity(c) ? 25 : c)).ToList();
            Image<Rgb, byte> curvatureImg = new Image<Rgb, byte>(curvatureDraw.Count, 512);
            VectorOfPoint curvatureContour = new VectorOfPoint();
            double scale = 255 / Math.Max(Math.Abs(curvatureDraw.Max()), Math.Abs(curvatureDraw.Min()));
            for (int i = 0; i < curvatureDraw.Count; i++)
            {
                curvatureContour.Push(new System.Drawing.Point(i, (int)(scale * curvatureDraw[i] + 255)));
            }
            CvInvoke.DrawContours(curvatureImg, new VectorOfVectorOfPoint(curvatureContour), -1, new MCvScalar(0, 255, 0));
            ProcessedImagesStorage.AddImage("Curvature Img", curvatureImg.Bitmap);
        }
    }
}
