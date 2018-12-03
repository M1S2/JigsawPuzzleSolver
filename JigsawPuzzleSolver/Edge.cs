using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for edges is that if you walked along the edge of the contour from beginning to end, the piece will be to the left, and empty space to right.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/edge.cpp
    public class Edge
    {
        private VectorOfPoint contour;                      //The original contour passed into the function.
        private VectorOfPoint normalized_contour;          //Normalized contour produces a contour that has its begining at (0,0) and its endpoint straight above it (0,y). This is used internally to classify the piece.
        private VectorOfPoint reverse_normalized_contour;

        /// <summary>
        /// Type of the Edge (LINE, BULB, HOLE)
        /// </summary>
        public EdgeTypes EdgeType { get; private set; }

        public string PieceID { get; private set; }
        public int EdgeNumber { get; private set; }

        public Image<Rgb, byte> Full_color;

        //##############################################################################################################################################################################################

        public Edge(string pieceID, int edgeNumber, Image<Rgb, byte> full_color, VectorOfPoint edgeContour)
        {
            PieceID = pieceID;
            EdgeNumber = edgeNumber;
            contour = edgeContour;
            Full_color = full_color;

            normalized_contour = normalize(contour);    //Normalized contours are used for comparisons

            VectorOfPoint contourCopy = new VectorOfPoint(contour.ToArray().Reverse().ToArray());
            reverse_normalized_contour = normalize(contourCopy);   //same as normalized contour, but flipped 180 degrees

            classify();
        }

        //##############################################################################################################################################################################################
        
        /// <summary>
        /// Calculate the edge type (LINE, BULB, HOLE)
        /// </summary>
        private void classify()
        {
            EdgeType = EdgeTypes.UNKNOWN;
            if(normalized_contour.Size == 1) { return; }

            //See if it is an outer edge comparing the distance between beginning and end with the arc length.
            double contour_length = CvInvoke.ArcLength(normalized_contour, false);
            
            double begin_end_distance = Utils.Distance(normalized_contour.ToArray().First(), normalized_contour.ToArray().Last());
            if (contour_length < begin_end_distance * 1.3)
            {
                EdgeType = EdgeTypes.LINE;
                return;
            }

            //Find the minimum or maximum value for x in the normalized contour and base the classification on that
            int minx = 100000000;
            int maxx = -100000000;
            for (int i = 0; i < normalized_contour.Size; i++)
            {
                if (minx > normalized_contour[i].X) { minx = (int)normalized_contour[i].X; }
                if (maxx < normalized_contour[i].X) { maxx = (int)normalized_contour[i].X; }
            }

            if (Math.Abs(minx) > Math.Abs(maxx))
            {
                EdgeType = EdgeTypes.BULB;
            }
            else
            {
                EdgeType = EdgeTypes.HOLE;
            }

            Image<Rgb, byte> contour_img = Full_color.Clone();
            for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(contour_img, Point.Round(contour[i]), 2, new MCvScalar(255, 0, 0), 1); }
            ProcessedImagesStorage.AddImage(PieceID + " Edge " + EdgeNumber.ToString() + " " + EdgeType.ToString(), contour_img.Bitmap);
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// This function takes in a vector of points, and transforms it so that it starts at the origin, and ends on the y-axis
        /// </summary>
        /// <param name="contour">Contour to normalize</param>
        /// <returns>normalized contour</returns>
        private VectorOfPoint normalize(VectorOfPoint contour)
        {
            VectorOfPoint ret_contour = new VectorOfPoint();
            PointF a = new PointF(contour.ToArray().First().X, contour.ToArray().First().Y);
            PointF b = new PointF(contour.ToArray().Last().X, contour.ToArray().Last().Y);

            //Calculating angle from vertical
            b.X = b.X - a.X;
            b.Y = b.Y - a.Y;
            
            double theta = Math.Acos(b.Y / (Utils.DistanceToOrigin(b)));
            if (b.X < 0) { theta = -theta; }

            //Theta is the angle every point needs rotated. and -a is the translation
            for (int i = 0; i < contour.Size; i++)
            {
                //Apply translation
                PointF temp_point = new PointF(contour[i].X - a.X, contour[i].Y - a.Y);
                
                //Apply roatation
                double new_x = Math.Cos(theta) * temp_point.X - Math.Sin(theta) * temp_point.Y;
                double new_y = Math.Sin(theta) * temp_point.X + Math.Cos(theta) * temp_point.Y;
                ret_contour.Push(new Point[1] { new Point((int)new_x, (int)new_y) });
            }
    
            return ret_contour;
        }

        //##############################################################################################################################################################################################
        
        /// <summary>
        /// Translate the normalized_contour by the given offset
        /// </summary>
        /// <param name="offset_x">X Offset</param>
        /// <param name="offset_y">Y Offset</param>
        /// <returns>Translated contour</returns>
        public VectorOfPoint GetTranslatedContour(int offset_x, int offset_y)
        {
            return Utils.TranslateContour(normalized_contour, offset_x, offset_y);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Translate the reverse_normalized_contour by the given offset
        /// </summary>
        /// <param name="offset_x">X Offset</param>
        /// <param name="offset_y">Y Offset</param>
        /// <returns>Translated contour</returns>
        public VectorOfPoint GetTranslatedContourReverse(int offset_x, int offset_y)
        {
            return Utils.TranslateContour(reverse_normalized_contour, offset_x, offset_y);
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// Trying OpenCV's match shapes, hasn't worked as well as Compare2 function.
        /// </summary>
        /// <param name="edge2">Edge to compare to this edge</param>
        /// <returns>Similarity factor of edges</returns>
        public double Compare(Edge edge2)
        {
            //Return large numbers if we know that these shapes simply wont match...
            if (EdgeType == EdgeTypes.LINE || edge2.EdgeType == EdgeTypes.LINE) { return 1000000; }
            if (EdgeType == edge2.EdgeType) { return 10000000; }

            return CvInvoke.MatchShapes(contour, edge2.contour, ContoursMatchType.I2, 0);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// This comparison iterates over every point in "this" contour, finds the closest point in "edge2" contour and sums those distances up.
        /// The end result is the sum divided by length of the 2 contours
        /// </summary>
        /// <param name="edge2">Edge to compare to this edge</param>
        /// <returns>Similarity factor of edges</returns>
        public double Compare2(Edge edge2)
        {
            //Return large number if an impossible situation is happening
            if (EdgeType == EdgeTypes.LINE || edge2.EdgeType == EdgeTypes.LINE) { return 1000000; }
            if (EdgeType == edge2.EdgeType) { return 10000000; }
            double cost = 0;
            double total_length = CvInvoke.ArcLength(normalized_contour, false) + CvInvoke.ArcLength(edge2.reverse_normalized_contour, false);

            for(int i = 0; i < normalized_contour.Size; i++)
            {
                double min = 10000000;
                for(int j = 0; j < reverse_normalized_contour.Size; j++)
                {
                    double dist = Math.Sqrt(Math.Pow(normalized_contour[i].X - reverse_normalized_contour[j].X, 2) + Math.Pow(normalized_contour[i].Y - reverse_normalized_contour[j].Y, 2));
                    if (dist < min) min = dist;
                }
                cost += min;
            }
            return cost / total_length;
        }

    }
}
