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
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;

namespace JigsawPuzzleSolver
{
    public static class Utils
    {
        #region Math utilities

        /// <summary>
        /// Euclidian distance between 2 points.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <param name="p2">Point 2</param>
        /// <returns>Distance between Point 1 and Point 2</returns>
        public static double Distance(PointF p1, PointF p2)
        {
            return CvInvoke.Norm(new VectorOfPointF(new PointF[1] { PointF.Subtract(p1, new SizeF(p2)) }));
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Euclidian distance between the point and the origin (0, 0).
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <returns>Distance between Point 1 and origin (0, 0)</returns>
        public static double DistanceToOrigin(PointF p1)
        {
            return CvInvoke.Norm(new VectorOfPointF(new PointF[1] { p1 }));
        }

        #endregion

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        #region Contour, Vector, Array utilities

        /// <summary>
        /// Return a contour that is translated.
        /// </summary>
        /// <param name="contourIn">Contour that should be translated</param>
        /// <param name="offset_x">X translation</param>
        /// <param name="offset_y">Y translation</param>
        /// <returns>Translated contour</returns>
        public static VectorOfPoint TranslateContour(VectorOfPoint contourIn, int offset_x, int offset_y)
        {
            VectorOfPoint ret_contour = new VectorOfPoint();
            for (int i = 0; i < contourIn.Size; i++)
            {
                ret_contour.Push(new Point((int)(contourIn[i].X + offset_x + 0.5), (int)(contourIn[i].Y + offset_y + 0.5)));
            }
            return ret_contour;
        }

        /// <summary>
        /// Return a contour that is translated.
        /// </summary>
        /// <param name="contourIn">Contour that should be translated</param>
        /// <param name="offset_x">X translation</param>
        /// <param name="offset_y">Y translation</param>
        /// <returns>Translated contour</returns>
        public static VectorOfPointF TranslateContour(VectorOfPointF contourIn, int offset_x, int offset_y)
        {
            VectorOfPointF ret_contour = new VectorOfPointF();
            for (int i = 0; i < contourIn.Size; i++)
            {
                ret_contour.Push(new PointF((float)(contourIn[i].X + offset_x + 0.5), (float)(contourIn[i].Y + offset_y + 0.5)));
            }
            return ret_contour;
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// Get a subset of the given vector.
        /// </summary>
        /// <param name="vector">Vector to create the subset from</param>
        /// <param name="startIndex">Index of the first element that is part of the subset</param>
        /// <param name="endIndex">Index of the last element that is part of the subset</param>
        /// <returns>Subset of the vector</returns>
        public static VectorOfPointF GetSubsetOfVector(this VectorOfPointF vector, int startIndex, int endIndex)
        {
            PointF[] vectorArray = vector.ToArray();
            PointF[] vectorArrayExtended = new PointF[vector.Size * 3];
            for (int i = 0; i < 3; i++) { Array.Copy(vectorArray, 0, vectorArrayExtended, i * vector.Size, vector.Size); }

            if(endIndex < startIndex) { endIndex += vector.Size; }
            PointF[] vectorArraySubset = new PointF[endIndex - startIndex + 1];
            Array.Copy(vectorArrayExtended, startIndex + vector.Size, vectorArraySubset, 0, endIndex - startIndex + 1);
            return new VectorOfPointF(vectorArraySubset);
        }

        /// <summary>
        /// Get a subset of the given vector.
        /// </summary>
        /// <param name="vector">Vector to create the subset from</param>
        /// <param name="startIndex">Index of the first element that is part of the subset</param>
        /// <param name="endIndex">Index of the last element that is part of the subset</param>
        /// <returns>Subset of the vector</returns>
        public static VectorOfPoint GetSubsetOfVector(this VectorOfPoint vector, int startIndex, int endIndex)
        {
            Point[] vectorArray = vector.ToArray();
            Point[] vectorArrayExtended = new Point[vector.Size * 3];
            for (int i = 0; i < 3; i++) { Array.Copy(vectorArray, 0, vectorArrayExtended, i * vector.Size, vector.Size); }

            if (endIndex < startIndex) { endIndex += vector.Size; }
            Point[] vectorArraySubset = new Point[endIndex - startIndex + 1];
            Array.Copy(vectorArrayExtended, startIndex + vector.Size, vectorArraySubset, 0, endIndex - startIndex + 1);
            return new VectorOfPoint(vectorArraySubset);
        }

        //**********************************************************************************************************************************************************************************************

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
                if (lastContourSize < currentContourSize) { lastContourSize = currentContourSize; indexLargestContour = i; }
            }
            return contours[indexLargestContour];
        }

        //**********************************************************************************************************************************************************************************************

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
                if (tmp_distance < last_distance) { last_distance = tmp_distance; result = points[i]; }
            }
            return result;
        }

        //**********************************************************************************************************************************************************************************************

        public static void CopyToROI(this Matrix<int> src, Matrix<int> dst, Rectangle dstRegion)
        {
            for (int i = dstRegion.X; i < dstRegion.X + dstRegion.Width; i++)
            {
                for (int j = dstRegion.Y; j < dstRegion.Y + dstRegion.Height; j++)
                {
                    dst[j, i] = src[j - dstRegion.Y, i - dstRegion.X];      // elements are indexed by [row, column]
                }
            }
        }

        //**********************************************************************************************************************************************************************************************
        
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

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Push a single point to a vector of points
        /// </summary>
        /// <param name="vector">Vector to push the point to</param>
        /// <param name="point">Point to push to the vector</param>
        public static void Push(this VectorOfPoint vector, Point point)
        {
            vector.Push(new Point[] { point });
        }

        /// <summary>
        /// Push a single point to a vector of points
        /// </summary>
        /// <param name="vector">Vector to push the point to</param>
        /// <param name="point">Point to push to the vector</param>
        public static void Push(this VectorOfPointF vector, PointF point)
        {
            vector.Push(new PointF[] { point });
        }

        #endregion

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        #region Image utilities

        /// <summary>
        /// This function takes a directory, and returns a vector of every image opencv could extract from it.
        /// </summary>
        /// <param name="path">Path from where to get the images</param>
        /// <returns>List of all images in directory</returns>
        public static List<Image<Rgb, byte>> GetImagesFromDirectory(string path)
        {
            List<string> imageExtensions = new List<string>() { ".jpg", ".png", ".bmp", ".tiff" };
            List<Image<Rgb, byte>> imageList = new List<Image<Rgb, byte>>();

            FileAttributes attr = File.GetAttributes(path);
            List<FileInfo> imageFiles = new List<FileInfo>();
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)      //detect whether its a directory or file
            {
                DirectoryInfo folderInfo = new DirectoryInfo(path);
                imageFiles = folderInfo.GetFiles().ToList();
            }
            else
            {
                FileInfo fileInfo = new FileInfo(path);
                imageFiles.Add(fileInfo);
            }
            
            imageFiles = imageFiles.Where(f => imageExtensions.Contains(f.Extension)).ToList();

            for (int i = 0; i < imageFiles.Count; i++)
            {
                imageList.Add(CvInvoke.Imread(imageFiles[i].FullName).ToImage<Rgb, byte>());
            }

            return imageList;
        }

        //**********************************************************************************************************************************************************************************************

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

        //**********************************************************************************************************************************************************************************************

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

        //**********************************************************************************************************************************************************************************************

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
            if (image1 == null) { image1 = new Image<Rgb, byte>(1, 1); }
            if (image2 == null) { image2 = new Image<Rgb, byte>(1, 1); }

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

        //**********************************************************************************************************************************************************************************************

        public static Mat RotateMat(Mat src, int angle)
        {
            if (angle % 90 != 0) { return src; }

            System.Drawing.Size rotatedSize;
            if (Math.Abs(angle) == 90 || angle == 270) { rotatedSize = new System.Drawing.Size(src.Cols, src.Rows); }
            else { rotatedSize = new System.Drawing.Size(src.Rows, src.Cols); }

            Mat rotated = new Mat(rotatedSize.Width, rotatedSize.Height, DepthType.Cv8U, 1);

            switch (angle)
            {
                case 0: rotated = src.Clone(); break;
                case 90: CvInvoke.Rotate(src, rotated, RotateFlags.Rotate90CounterClockwise); break;
                case 180: CvInvoke.Rotate(src, rotated, RotateFlags.Rotate180); break;
                case 270:
                case -90: CvInvoke.Rotate(src, rotated, RotateFlags.Rotate90Clockwise); break;
            }
            
            return rotated;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Rotate the given matrix counter clockwise
        /// </summary>
        /// <param name="oldMatrix">Matrix to rotate</param>
        /// <returns>oldMatrix rotated counter clockwise</returns>
        /// see: https://stackoverflow.com/questions/18034805/rotate-mn-matrix-90-degrees
        public static Matrix<int> RotateMatrixCounterClockwise(Matrix<int> oldMatrix)
        {
            Matrix<int> newMatrix = new Matrix<int>(oldMatrix.Cols, oldMatrix.Rows);
            int newColumn, newRow = 0;
            for (int oldColumn = oldMatrix.Cols - 1; oldColumn >= 0; oldColumn--)
            {
                newColumn = 0;
                for (int oldRow = 0; oldRow < oldMatrix.Rows; oldRow++)
                {
                    newMatrix[newRow, newColumn] = oldMatrix[oldRow, oldColumn];
                    newColumn++;
                }
                newRow++;
            }
            return newMatrix;
        }

        #endregion

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        #region Filters

        /// <summary>
        /// Easy way to take a list of images and create a bw image at a specified threshold.
        /// </summary>
        /// <param name="color_images">List of color images</param>
        /// <param name="threshold">Binarization threshold</param>
        /// <returns>List of bw images</returns>
        public static List<Image<Gray, byte>> ColorToBw(List<Image<Rgb, byte>> color_images, int threshold)
        {
            List<Image<Gray, byte>> black_and_white = new List<Image<Gray, byte>>();
            for (int i = 0; i < color_images.Count; i++)
            {
                Mat bw = new Mat();
                CvInvoke.CvtColor(color_images[i], bw, ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(bw, bw, threshold, 255, ThresholdType.Binary);
                black_and_white.Add(bw.ToImage<Gray, byte>());
            }
            return black_and_white;
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// Performs a open then a close operation in order to remove small anomolies.
        /// </summary>
        /// <param name="images">List of images to filter with median blur</param>
        /// <param name="k">Kernel size</param>
        /// <returns>List of filtered images</returns>
        public static List<Image<Rgb, byte>> MedianBlur(List<Image<Rgb, byte>> images, int k)
        {
            List<Image<Rgb, byte>> filtered_images = new List<Image<Rgb, byte>>();
            for (int i = 0; i < images.Count; i++)
            {
                Mat m = new Mat();
                CvInvoke.MedianBlur(images[i], m, k);
                filtered_images.Add(m.ToImage<Rgb, byte>());
            }
            return filtered_images;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Performs a open then a close operation in order to remove small anomolies.
        /// </summary>
        /// <param name="images"></param>
        /// <param name="size"></param>
        public static void Filter(List<Mat> images, int size)
        {
            Mat k = CvInvoke.GetStructuringElement(ElementShape.Ellipse, new Size(size, size), new Point(-1, -1));
            for (int i = 0; i < images.Count; i++)
            {
                Mat bw = new Mat();
                //Opening and closing removes anything smaller than size
                CvInvoke.MorphologyEx(images[i], bw, MorphOp.Open, k, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
                CvInvoke.MorphologyEx(bw, images[i], MorphOp.Close, k, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0));
            }
        }

        //**********************************************************************************************************************************************************************************************

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

        #endregion

    }
}
