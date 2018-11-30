using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        /// Euclidian distance between the point and the origin.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <returns>Distance between Point 1 and origin</returns>
        public static double DistanceToOrigin(PointF p1)
        {
            return CvInvoke.Norm(new VectorOfPointF(new PointF[1] { p1 }));
        }

        //**********************************************************************************************************************************************************************************************

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
                ret_contour.Push(new Point[1] { new Point((int)(contourIn[i].X + offset_x + 0.5), (int)(contourIn[i].Y + offset_y + 0.5)) });
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
                ret_contour.Push(new PointF[1] { new PointF((float)(contourIn[i].X + offset_x + 0.5), (float)(contourIn[i].Y + offset_y + 0.5)) });
            }
            return ret_contour;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Remove duplicate points
        /// </summary>
        /// <param name="vectorOfPoints">vector of points</param>
        /// <returns>vector of points with removed duplicates</returns>
        public static VectorOfPointF RemoveDuplicates(VectorOfPointF vectorOfPoints)
        {
#warning Test!!!
            List<PointF> listOfPoints = vectorOfPoints.ToArray().ToList();

            bool dupes_found = true;
            while (dupes_found)
            {
                dupes_found = false;
                int dup_at = -1;
                for (int i = 0; i < vectorOfPoints.Size; i++)
                {
                    for (int j = 0; j < vectorOfPoints.Size; j++)
                    {
                        if (j == i) continue;
                        if (vectorOfPoints[i] == vectorOfPoints[j])
                        {
                            dup_at = j;
                            dupes_found = true;
                            listOfPoints.RemoveAt(j);
                            break;
                        }
                    }
                    if (dupes_found)
                    {
                        break;
                    }
                }
            }
            return vectorOfPoints;
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

            Point[] vectorArraySubset = new Point[endIndex - startIndex + 1];
            Array.Copy(vectorArrayExtended, startIndex + vector.Size, vectorArraySubset, 0, endIndex - startIndex + 1);
            return new VectorOfPoint(vectorArraySubset);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// This function takes a directory, and returns a vector of every image opencv could extract from it.
        /// </summary>
        /// <param name="path">Path from where to get the images</param>
        /// <returns>List of all images in directory</returns>
        public static List<Image<Rgb, byte>> GetImagesFromDirectory(string path)
        {
            List<string> imageExtensions = new List<string>() { ".jpg", ".png", ".bmp" };
            List<Image<Rgb, byte>> imageList = new List<Image<Rgb, byte>>();

            DirectoryInfo folderInfo = new DirectoryInfo(path);
            List<FileInfo> imageFiles = folderInfo.GetFiles().ToList();
            imageFiles = imageFiles.Where(f => imageExtensions.Contains(f.Extension)).ToList();

            for (int i = 0; i < imageFiles.Count; i++)
            {
                imageList.Add(CvInvoke.Imread(path + "/" + imageFiles[i].Name).ToImage<Rgb, byte>());

                ProcessedImagesStorage.AddImage("Image Loaded " + i.ToString(), imageList.Last().Bitmap);
            }

            return imageList;
        }

        //**********************************************************************************************************************************************************************************************

        #region Filters

        /// <summary>
        /// Easy way to take a list of images and create a bw image at a specified threshold.
        /// </summary>
        /// <param name="color_images">List of color images</param>
        /// <param name="threshold">Binarization threshold</param>
        /// <returns>List of bw images</returns>
        public static List<Mat> ColorToBw(List<Mat> color_images, int threshold)
        {
            List<Mat> black_and_white = new List<Mat>();
            for (int i = 0; i < color_images.Count; i++)
            {
                Mat bw = new Mat();
                CvInvoke.CvtColor(color_images[i], bw, ColorConversion.Bgr2Gray);
                CvInvoke.Threshold(bw, bw, threshold, 255, ThresholdType.Binary);
                black_and_white.Add(bw);

                ProcessedImagesStorage.AddImage("Image BlackWhite " + i.ToString(), bw.Bitmap);
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
        public static List<Mat> MedianBlur(List<Mat> images, int k)
        {
            List<Mat> filtered_images = new List<Mat>();
            for (int i = 0; i < images.Count; i++)
            {
                Mat m = new Mat();
                CvInvoke.MedianBlur(images[i], m, k);
                filtered_images.Add(m);

                ProcessedImagesStorage.AddImage("Image MedianBlur " + i.ToString(), m.Bitmap);
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

                ProcessedImagesStorage.AddImage("Image Filter " + i.ToString(), bw.Bitmap);
            }
        }

        #endregion

    }
}
