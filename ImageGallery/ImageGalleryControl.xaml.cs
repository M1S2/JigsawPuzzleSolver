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
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Drawing;

namespace ImageGallery
{
    /// <summary>
    /// Interaktionslogik für ImageGalleryControl.xaml
    /// </summary>
    public partial class ImageGalleryControl : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
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

        public static readonly DependencyProperty ImgListDependencyProperty = DependencyProperty.Register("ImgList", typeof(ObservableCollection<ImageDescribed>), typeof(ImageGalleryControl));
        public ObservableCollection<ImageDescribed> ImgList
        {
            get { return (ObservableCollection<ImageDescribed>)GetValue(ImgListDependencyProperty); }
            set { SetValue(ImgListDependencyProperty, value); }
        }

        private ImageDescribed _selectedImage;
        public ImageDescribed SelectedImage
        {
            get { return _selectedImage; }
            set { _selectedImage = value; OnPropertyChanged(); }
        }

        private int _selectedImgIndex;
        public int SelectedImgIndex
        {
            get { return _selectedImgIndex; }
            set { _selectedImgIndex = value; OnPropertyChanged(); }
        }

        public static readonly DependencyProperty NextAtEndBeginsNewDependencyProperty = DependencyProperty.Register("NextAtEndBeginsNew", typeof(bool), typeof(ImageGalleryControl));
        public bool NextAtEndBeginsNew
        {
            get { return (bool)GetValue(NextAtEndBeginsNewDependencyProperty); }
            set { SetValue(NextAtEndBeginsNewDependencyProperty, value); }
        }

        //##############################################################################################################################################################################################

        private ICommand _nextImgCommand;
        public ICommand NextImgCommand
        {
            get
            {
                if (_nextImgCommand == null)
                {
                    _nextImgCommand = new RelayCommand(param => 
                    {
                        if (SelectedImgIndex == ImgList.Count - 1 && NextAtEndBeginsNew) { SelectedImgIndex = 0; }
                        else if(SelectedImgIndex < ImgList.Count - 1) { SelectedImgIndex++; }
                    }, param => { return ImgList != null; });
                }
                return _nextImgCommand;
            }
        }

        private ICommand _previousImgCommand;
        public ICommand PreviousImgCommand
        {
            get
            {
                if (_previousImgCommand == null)
                {
                    _previousImgCommand = new RelayCommand(param => 
                    {
                        if (SelectedImgIndex == 0 && NextAtEndBeginsNew) { SelectedImgIndex = ImgList.Count - 1; }
                        else if (SelectedImgIndex > 0) { SelectedImgIndex--; }
                    }, param => { return ImgList != null; });
                }
                return _previousImgCommand;
            }
        }

        //##############################################################################################################################################################################################

        public ImageGalleryControl()
        {
            InitializeComponent();
            NextAtEndBeginsNew = true;
        }

    }
}
