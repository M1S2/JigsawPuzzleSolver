using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;

namespace JigsawPuzzleSolver
{
    public static class Tools
    {
        /// <summary>
        /// Convert a Bitmap to a BitmapImage that can be used with WPF Image controls
        /// </summary>
        /// <param name="bitmap">Bitmap to convert</param>
        /// <returns>BitmapImage</returns>
        /// see: https://stackoverflow.com/questions/22499407/how-to-display-a-bitmap-in-a-wpf-image
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapimage = new BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }

        /*
        // DOESN'T WORK !!!!!
        // see: https://stackoverflow.com/questions/44752240/how-to-remove-shadow-from-scanned-images-using-opencv
        public static Image<Gray, byte> RemoveShadows(Image<Rgb, byte> sourceImg)
        {
            Image<Gray, byte>[] rgb_planes = sourceImg.Split();
            VectorOfMat result_norm_planes = new VectorOfMat();

            foreach(Image<Gray, byte> plane in rgb_planes)
            {
                Image<Gray, byte> dilated_img = plane.Dilate(7);

                Image<Gray, byte> bg_img = new Image<Gray, byte>(dilated_img.Size);
                CvInvoke.MedianBlur(dilated_img, bg_img, 21);

                Image<Gray, byte> diff_img = new Image<Gray, byte>(bg_img.Size);
                CvInvoke.AbsDiff(plane, bg_img, diff_img);
                diff_img = diff_img.AbsDiff(new Gray(255));

                Image<Gray, byte> norm_img = new Image<Gray, byte>(diff_img.Size);
                CvInvoke.Normalize(diff_img, norm_img, 0, 255, NormType.MinMax, DepthType.Cv8U);

                result_norm_planes.Push(norm_img);
            }
            
            Mat result_norm = new Mat();
            CvInvoke.Merge(result_norm_planes, result_norm);

            return result_norm.ToImage<Gray, byte>();
        }*/

    }
}
