using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace ImageGallery.LocalDriveBitmaps
{
    /// <summary>
    /// Class that capsulates a Bitmap with description text
    /// </summary>
    [Serializable]
    public class ImageDescribedLight : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field:NonSerialized]
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

        private string _description;
        [DataMember]
        public string Description
        {
            get { return _description; }
            set { _description = value; OnPropertyChanged(); }
        }

        private LocalDriveBitmap _img;
        [DataMember]
        public LocalDriveBitmap Img
        {
            get { return _img; }
            set { _img = value; OnPropertyChanged(); }
        }

        public ImageDescribedLight(string description, LocalDriveBitmap img)
        {
            Description = description;
            Img = img;
        }

        public ImageDescribedLight(string description, string bitmapFilePath, Bitmap img)
        {
            Description = description;
            Img = new LocalDriveBitmap(bitmapFilePath, img, false, false);
        }

        public ImageDescribedLight(string description, string bitmapFilePath)
        {
            Description = description;
            Img = new LocalDriveBitmap(bitmapFilePath, false, false);
        }
    }
}
