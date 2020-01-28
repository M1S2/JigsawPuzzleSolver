using System;
using System.Windows.Media.Imaging;

// Copied from https://github.com/dmitryshelamov/UI-Cropping-Image

namespace CroppingImageLibrary.Utils
{
    public class DoubleClickEventArgs : EventArgs
    {
        public BitmapFrame BitmapFrame { get; set; }
    }
}
