﻿using System;
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

        /// <summary>
        /// Combine the two images into one (horizontal)
        /// </summary>
        /// <param name="image1">Image 1</param>
        /// <param name="image2">Image 2</param>
        /// <param name="spacing">Space between the two images</param>
        /// <returns>Combination of image1 and image2</returns>
        /// see: https://stackoverflow.com/questions/29488507/add-two-sub-images-into-one-new-image-using-emgu-cv
        public static Image<Gray, byte> Combine2ImagesHorizontal(Image<Gray, byte> image1, Image<Gray, byte> image2, int spacing)
        {
            return Combine2ImagesHorizontal(image1.Convert<Rgb, byte>(), image2.Convert<Rgb, byte>(), spacing).Convert<Gray, byte>();
        }

        /// <summary>
        /// Combine the two images into one (horizontal)
        /// </summary>
        /// <param name="image1">Image 1</param>
        /// <param name="image2">Image 2</param>
        /// <param name="spacing">Space between the two images</param>
        /// <returns>Combination of image1 and image2</returns>
        /// see: https://stackoverflow.com/questions/29488507/add-two-sub-images-into-one-new-image-using-emgu-cv
        public static Image<Rgb, byte> Combine2ImagesHorizontal(Image<Rgb, byte> image1, Image<Rgb, byte> image2, int spacing)
        {
            int ImageWidth = image1.Width + image2.Width + spacing;
            int ImageHeight = Math.Max(image1.Height, image2.Height);

            Bitmap combinedBitmap = new Bitmap(ImageWidth, ImageHeight);
            using (Graphics g = Graphics.FromImage(combinedBitmap))
            {
                g.DrawImage(image1.Bitmap, 0, 0);
                g.DrawImage(image2.Bitmap, image1.Width + spacing, 0);
            }

            return new Image<Rgb, byte>(combinedBitmap);
        }

        /// <summary>
        /// Find the largest contour in the list of contours
        /// </summary>
        /// <param name="contours">list of contours returned by FindContours</param>
        /// <returns>largest contour</returns>
        public static VectorOfPoint GetLargestContour(VectorOfVectorOfPoint contours)
        {
            int indexLargestContour = -1;
            double lastContourSize = -1;
            for (int i = 0; i < contours.Size; i++)
            {
                double currentContourSize = CvInvoke.ContourArea(contours[i]);
                if(lastContourSize < currentContourSize) { lastContourSize = currentContourSize; indexLargestContour = i; }
            }
            return contours[indexLargestContour];
        }
        
        
        /// <summary>
        /// Find the point in the list of points that has the minimum distance to the origin point
        /// </summary>
        /// <param name="points">List of points</param>
        /// <param name="origin">Point to which the distances are calculated</param>
        /// <returns>point in points with the minimum distance to origin</returns>
        public static Point GetNearestPoint(List<Point> points, Point origin)
        {
            Point result = new Point();
            double last_distance = double.MaxValue;
            for (int i = 0; i < points.Count; i++)
            {
                double tmp_distance = Utils.Distance(origin, points[i]);
                if(tmp_distance < last_distance) { last_distance = tmp_distance; result = points[i]; }
            }
            return result;
        }

        /// <summary>
        /// Rotate the array count times
        /// </summary>
        /// <typeparam name="T">Type of the array</typeparam>
        /// <param name="array">array to rotate</param>
        /// <param name="count">number of rotations</param>
        /// see: https://stackoverflow.com/questions/38482696/how-to-efficiently-rotate-an-array
        public static void Rotate<T>(this T[] array, int count)
        {
            if (array == null || array.Length < 2) return;
            count %= array.Length;
            if (count == 0) return;
            int left = count < 0 ? -count : array.Length + count;
            int right = count > 0 ? count : array.Length - count;
            if (left <= right)
            {
                for (int i = 0; i < left; i++)
                {
                    var temp = array[0];
                    Array.Copy(array, 1, array, 0, array.Length - 1);
                    array[array.Length - 1] = temp;
                }
            }
            else
            {
                for (int i = 0; i < right; i++)
                {
                    var temp = array[array.Length - 1];
                    Array.Copy(array, 0, array, 1, array.Length - 1);
                    array[0] = temp;
                }
            }
        }

        /// <summary>
        /// Calculate the standard deviation of the given values
        /// </summary>
        /// <param name="values">List of doubles</param>
        /// <returns>Standard deviation of the given values</returns>
        /// see: https://stackoverflow.com/questions/5336457/how-to-calculate-a-standard-deviation-array
        public static double CalculateStandardDeviation(List<double> values)
        {
            double average = values.Average();
            double sumOfSquaresOfDifferences = values.Sum(val => (val - average) * (val - average));
            return Math.Sqrt(sumOfSquaresOfDifferences / values.Count);
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
