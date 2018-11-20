using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Util;

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

        /// <summary>
        /// Euclidian distance between the point and the origin.
        /// </summary>
        /// <param name="p1">Point 1</param>
        /// <returns>Distance between Point 1 and origin</returns>
        public static double DistanceToOrigin(PointF p1)
        {
            return CvInvoke.Norm(new VectorOfPointF(new PointF[1] { p1 }));
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

        /// <summary>
        /// Remove duplicate points
        /// </summary>
        /// <param name="vectorOfPoints">vector of points</param>
        /// <returns>vector of points with removed duplicates</returns>
        public static VectorOfPoint RemoveDuplicates(VectorOfPoint vectorOfPoints)
        {
#warning Test!!!
            List<Point> listOfPoints = vectorOfPoints.ToArray().ToList();

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

    }
}
