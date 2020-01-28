using System;
using System.Windows;

// Copied from https://github.com/dmitryshelamov/UI-Cropping-Image

namespace CroppingImageLibrary.Utils
{
    public class LocationChangedEventArgs : EventArgs
    {
        public Point NewLocation { get; set; }
    }
}
