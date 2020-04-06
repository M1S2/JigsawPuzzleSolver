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
using System.Windows.Shapes;
using System.Drawing;
using System.IO;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CroppingImageLibrary;
using MahApps.Metro.Controls;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.GUI_Elements;

namespace JigsawPuzzleSolver.Plugins.Controls
{
    //https://www.codeproject.com/Articles/131708/WPF-Color-Picker-Construction-Kit

    // Possible choice:  https://github.com/dsafa/wpf-color-picker
    //https://github.com/Dirkster99/ColorPickerLib

    /// <summary>
    /// Interaction logic for PieceBackgroundColorPickerWindow.xaml
    /// </summary>
    public partial class PieceBackgroundColorPickerWindow : MetroWindow, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. The CallerMemberName attribute that is applied to the optional propertyName parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName">Name of the property that is changed</param>
        /// see: https://docs.microsoft.com/de-de/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //##############################################################################################################################################################################################
        
        private string _puzzlePieceImageFileName;
        /// <summary>
        /// Filename of the Image file that is displayed for background color extraction
        /// </summary>
        public string PuzzlePieceImageFileName
        {
            get { return _puzzlePieceImageFileName; }
            private set { _puzzlePieceImageFileName = value; OnPropertyChanged(); }
        }
        
        private BitmapSource _croppedImage;
        /// <summary>
        /// Image that was cropped by user
        /// </summary>
        public BitmapSource CroppedImage
        {
            get { return _croppedImage; }
            private set
            {
                _croppedImage = value;
                SelectedBackgroundColor = GetMeanColorFromImage(BitmapFromSource(_croppedImage));
                OnPropertyChanged();
                OnPropertyChanged("SelectedBackgroundColor");
            }
        }

        private System.Drawing.Color _selectedBackgroundColor;
        /// <summary>
        /// Color selected by user
        /// </summary>
        public System.Drawing.Color SelectedBackgroundColor
        {
            get { return _selectedBackgroundColor; }
            private set { _selectedBackgroundColor = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        public CroppingAdorner CroppingAdorner;
        private string _puzzlePiecesFolderPath;

        //##############################################################################################################################################################################################

        #region Commands

        private ICommand _okButtonCommand;
        public ICommand OkButtonCommand
        {
            get
            {
                if (_okButtonCommand == null)
                {
                    _okButtonCommand = new RelayCommand(param => { DialogResult = true; Close(); });
                }
                return _okButtonCommand;
            }
        }

        private ICommand _cancelButtonCommand;
        public ICommand CancelButtonCommand
        {
            get
            {
                if (_cancelButtonCommand == null)
                {
                    _cancelButtonCommand = new RelayCommand(param => { DialogResult = false; Close(); });
                }
                return _cancelButtonCommand;
            }
        }

        #endregion

        //##############################################################################################################################################################################################

        public PieceBackgroundColorPickerWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            _puzzlePiecesFolderPath = "";
            PuzzlePieceImageFileName = "";
        }

        public PieceBackgroundColorPickerWindow(string puzzlePiecesFolderPath, System.Drawing.Color pieceBackgroundColor)
        {
            InitializeComponent();
            this.DataContext = this;
            _puzzlePiecesFolderPath = puzzlePiecesFolderPath;
            SelectedBackgroundColor = pieceBackgroundColor;
        }

        //##############################################################################################################################################################################################
      
        /// <summary>
        /// Get Mean Color from given Bitmap
        /// </summary>
        /// <param name="img">Image to calculate mean color</param>
        /// <returns>Mean color of given image</returns>
        private System.Drawing.Color GetMeanColorFromImage(Bitmap img)
        {
            Image<Rgba, byte> imgCV = new Image<Rgba, byte>(img);
            MCvScalar meanColorScalar = CvInvoke.Mean(imgCV);
            System.Drawing.Color meanColor = System.Drawing.Color.FromArgb((int)meanColorScalar.V0, (int)meanColorScalar.V1, (int)meanColorScalar.V2);
            imgCV.Dispose();
            return meanColor;
        }
        
        /// <summary>
        /// Capture Mouse on mouse button down
        /// </summary>
        /// see: https://github.com/dmitryshelamov/UI-Cropping-Image
        private void CroppingRootGrid_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CroppingAdorner.CaptureMouse();
            CroppingAdorner.MouseLeftButtonDownEventHandler(sender, e);
        }

        /// <summary>
        /// Initialize Cropping Adorner and find puzzle pieces image
        /// </summary>
        private void PieceBackgroundColorPickerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            AdornerLayer adornerLayer = AdornerLayer.GetAdornerLayer(CroppingCanvasPanel);
            CroppingAdorner = new CroppingAdorner(CroppingCanvasPanel);
            adornerLayer.Add(CroppingAdorner);
            CroppingAdorner.OnRectangleSizeChangedEvent += (s, args) => { CroppedImage = CroppingAdorner.GetCroppedBitmapFrame(); };
            CroppingAdorner.OnRectangleLocationChangedEvent += (s, args) => { CroppedImage = CroppingAdorner.GetCroppedBitmapFrame(); };
            CroppingAdorner.SizeChanged += (s, args) => { CroppedImage = CroppingAdorner.GetCroppedBitmapFrame(); };

            if (Directory.Exists(_puzzlePiecesFolderPath))
            {
                List<string> imageExtensions = new List<string>() { ".jpg", ".png", ".bmp", ".tiff" };
                DirectoryInfo dirInfo = new DirectoryInfo(_puzzlePiecesFolderPath);
                List<FileInfo> fileInfos = dirInfo.GetFiles("*.*").ToList();
                fileInfos = fileInfos.Where(f => imageExtensions.Contains(f.Extension)).ToList();
                PuzzlePieceImageFileName = fileInfos?.First().FullName;
            }
        }

        /// <summary>
        /// Convert BitmapSource to Bitmap
        /// </summary>
        /// <param name="bitmapsource">BitmapSource to convert</param>
        /// <returns>Converted Bitmap</returns>
        /// see: https://stackoverflow.com/questions/3751715/convert-system-windows-media-imaging-bitmapsource-to-system-drawing-image
        private Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new Bitmap(outStream);
            }
            return bitmap;
        }

    }
}
