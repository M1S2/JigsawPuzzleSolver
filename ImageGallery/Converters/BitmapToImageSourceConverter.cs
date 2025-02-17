﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.Drawing;

namespace ImageGallery
{
    /// <summary>
    /// Convert bitmap to image source
    /// </summary>
    [ValueConversion(typeof(Bitmap), typeof(BitmapImage))]
    public class BitmapToImageSourceConverter : IValueConverter
    {
        // see: https://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Bitmap bitmap = new Bitmap((Bitmap)value);
            if (bitmap == null) { return null; }
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                bitmap.Dispose();
                return bitmapimage;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
