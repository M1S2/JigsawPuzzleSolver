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
using Emgu.CV.Features2D;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for the piece is that there are 4 edges the edge "numbers" go from 0-3 in counter clockwise order starting from the left.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/piece.cpp
    public class Piece
    {
        public static int NextPieceID { get; private set; }

        static Piece()
        {
            NextPieceID = 0;
        }

        //##############################################################################################################################################################################################

        private VectorOfPoint corners = new VectorOfPoint();
        private int piece_size;

        /// <summary>
        /// Type of the Piece (CORNER, BORDER, INNER)
        /// </summary>
        public PieceTypes PieceType { get; private set; }

        public string PieceID { get; set; }
        public Image<Rgb, byte> Full_color;
        public Image<Gray, byte> Bw;
        public Edge[] Edges = new Edge[4];

        //##############################################################################################################################################################################################

        public Piece(Image<Rgb, byte> color, Image<Gray, byte> bw, int estimated_piece_size)
        {
            PieceID = "Piece#" + NextPieceID.ToString();
            NextPieceID++;
            Full_color = color.Clone();
            Bw = bw.Clone();
            piece_size = estimated_piece_size;

            ProcessedImagesStorage.AddImage(PieceID + " Color", color.Clone().Bitmap);
            ProcessedImagesStorage.AddImage(PieceID + " Bw", bw.Clone().Bitmap);

            Edges[0] = new Edge(PieceID, 0, Full_color, new VectorOfPoint());
            Edges[1] = new Edge(PieceID, 1, Full_color, new VectorOfPoint());
            Edges[2] = new Edge(PieceID, 2, Full_color, new VectorOfPoint());
            Edges[3] = new Edge(PieceID, 3, Full_color, new VectorOfPoint());

            process();
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Find the first occurence of the elements of the second vector in the first vector and return the index in the first vector.
        /// </summary>
        /// <param name="vec1">Vector that is searched for the occurence of the elements of vector 2</param>
        /// <param name="vec2">Vector with element to be searched</param>
        /// <returns>Index of the first element in vector 1 that exist in vector 2 too</returns>
        private int find_first_in(VectorOfPoint vec1, VectorOfPoint vec2)
        {
            List<int> all_occurences = find_all_in(vec1, vec2);
            return (all_occurences?.First()).Value;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find all occurences of the elements of the second vector in the first vector and return the indices in the first vector.
        /// </summary>
        /// <param name="vec1">Vector that is searched for the occurences of the elements of vector 2</param>
        /// <param name="vec2">Vector with element to be searched</param>
        /// <returns>Indices of all elements in vector 1 that exist in vector 2 too</returns>
        private List<int> find_all_in(VectorOfPoint vec1, VectorOfPoint vec2)
        {
            List<int> places = new List<int>();
            for(int i = 0; i < vec1.Size; i++)
            {
                for (int j = 0; j < vec2.Size; j++)
                {
                    if(vec1[i].X == vec2[j].X && vec1[i].Y == vec2[j].Y)
                    {
                        places.Add(i);
                    }
                }
            }

            return places;
        } 

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find corners, extract the edges and classify the piece
        /// </summary>
        private void process()
        {
            //find_corners_GFTT();

            find_corners_MaximumRectangle();
            
            extract_edges();
            classify();
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        #region find_corners functions

        /// <summary>
        /// Find the 4 strongest corners.
        /// </summary>
        /// see: http://docs.opencv.org/doc/tutorials/features2d/trackingmotion/corner_subpixeles/corner_subpixeles.html
        private void find_corners_GFTT()
        {
            string id = PieceID;

            double minDistance = piece_size;        //How close can 2 corners be?
            int blockSize = 5; //25;                     //How big of an area to look for the corner in.
            bool useHarrisDetector = true;
            double k = 0.04;

            double min = 0;
            double max = 1;
            int max_iterations = 100;
            bool found_all_corners = false;

            Image<Gray, byte> bw_clone = Bw.Clone();

            //Binary search, altering quality until exactly 4 corners are found. Usually done in 1 or 2 iterations
            while (0 < max_iterations--)
            {
                corners.Clear();
                double qualityLevel = (min + max) / 2;
                
                VectorOfKeyPoint keyPoints = new VectorOfKeyPoint();
                GFTTDetector featureDetector = new GFTTDetector(100, qualityLevel, minDistance, blockSize, useHarrisDetector, k);
                
                featureDetector.DetectRaw(bw_clone, keyPoints);
                
                if (keyPoints.Size > 4)
                {
                    min = qualityLevel;     //Found too many corners increase quality
                }
                else if (keyPoints.Size < 4)
                {
                    max = qualityLevel;
                }
                else
                {
                    for (int i = 0; i < keyPoints.Size; i++)
                    {
                        corners.Push(Point.Round(keyPoints[i].Point));
                    }

                    found_all_corners = true;       //found all corners
                    break;
                }
            }

            //Find the sub-pixel locations of the corners.
            //Size winSize = new Size(blockSize, blockSize);
            //Size zeroZone = new Size(-1, -1);
            //MCvTermCriteria criteria = new MCvTermCriteria(40, 0.001);
            
            // Calculate the refined corner locations
            //CvInvoke.CornerSubPix(bw_clone, corners, winSize, zeroZone, criteria);
            
            //More debug stuff, this will mark the corners with a white circle and save the image
            for( int i = 0; i < corners.Size; i++ )
            {
                CvInvoke.Circle(Full_color, Point.Round(corners[i]), 4, new MCvScalar(255, 0, 0));
            }
            ProcessedImagesStorage.AddImage(PieceID + " Corners", Full_color.Clone().Bitmap);

            if (!found_all_corners)
            {
                System.Windows.MessageBox.Show("Failed to find correct number of corners " + corners.Size);
            }
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        private void find_corners_ArcLen()
        {
            corners.Clear();

            GFTTDetector detector = new GFTTDetector(1000, 0.01, 5, 6, true, 0.04); //new GFTTDetector(1000, 0.0001, 0, 5, true, 0.0004);
            MKeyPoint[] keyPoints = detector.Detect(Bw.Clone());

            /*float xRangeInvalidStart = 0.3f * Bw.Width;
            float xRangeInvalidEnd = Bw.Width - 0.3f * Bw.Width;
            float yRangeInvalidStart = 0.3f * Bw.Height;
            float yRangeInvalidEnd = Bw.Height - 0.3f * Bw.Height;
            keyPoints = keyPoints.Where(k => ((k.Point.X <= xRangeInvalidStart || k.Point.X >= xRangeInvalidEnd) && (k.Point.Y <= yRangeInvalidStart || k.Point.Y >= yRangeInvalidEnd))).ToArray();*/

            Image<Rgb, byte> color_clone = Full_color.Clone();
            Features2DToolbox.DrawKeypoints(color_clone, new VectorOfKeyPoint(keyPoints), color_clone, new Bgr(0, 0, 255));
            ProcessedImagesStorage.AddImage(PieceID + " GFTT detector", color_clone.Bitmap);

            List<Point> possibleCorners = keyPoints.Select(k => Point.Round(k.Point)).ToList();
            List<Point> possibleCornersSorted = new List<Point>(possibleCorners);

            possibleCornersSorted.Sort(new DistanceToPointComparer(new Point(0, 0), DistanceOrders.NEAREST_FIRST));
            foreach (Point possibleCorner in possibleCornersSorted)
            {
                if (CheckPossibleCorner(possibleCorner)) { corners.Push(possibleCorner); break; }
            }

            possibleCornersSorted.Sort(new DistanceToPointComparer(new Point(Bw.Width, 0), DistanceOrders.NEAREST_FIRST));
            foreach (Point possibleCorner in possibleCornersSorted)
            {
                if (CheckPossibleCorner(possibleCorner)) { corners.Push(possibleCorner); break; }
            }

            possibleCornersSorted.Sort(new DistanceToPointComparer(new Point(Bw.Width, Bw.Height), DistanceOrders.NEAREST_FIRST));
            foreach (Point possibleCorner in possibleCornersSorted)
            {
                if (CheckPossibleCorner(possibleCorner)) { corners.Push(possibleCorner); break; }
            }

            possibleCornersSorted.Sort(new DistanceToPointComparer(new Point(0, Bw.Height), DistanceOrders.NEAREST_FIRST));
            foreach (Point possibleCorner in possibleCornersSorted)
            {
                if (CheckPossibleCorner(possibleCorner)) { corners.Push(possibleCorner); break; }
            }

            if(corners.Size != 4)
            {
                System.Windows.MessageBox.Show("Failed to find correct number of corners: " + corners.Size + " found.");
                return;
            }

            for (int i = 0; i < 4; i++) { CvInvoke.Circle(color_clone, Point.Round(corners[i]), 4, new MCvScalar(0, 255, 0), 3); }
            ProcessedImagesStorage.AddImage(PieceID + " Corners", color_clone.Bitmap);
        }

        //**********************************************************************************************************************************************************************************************
        
        private bool CheckPossibleCorner(PointF possibleCorner)
        {
            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(Bw.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            VectorOfPoint contour = contours[0];
            List<Point> contourList = contour.ToArray().ToList();

            Point possibleCornerOnContour = Utils.GetNearestPoint(contourList, Point.Round(possibleCorner));
            int indexOfCorner = contourList.IndexOf(possibleCornerOnContour);
            int numberOfAnalysisPoints = 15;
            int numberOfGapPoints = 3;
            VectorOfPoint cornerNextPoints = contour.GetSubsetOfVector(indexOfCorner + numberOfGapPoints, indexOfCorner + numberOfGapPoints + numberOfAnalysisPoints);
            VectorOfPoint cornerPreviousPoints = contour.GetSubsetOfVector(indexOfCorner - numberOfGapPoints - numberOfAnalysisPoints, indexOfCorner - numberOfGapPoints);

            List<double> nextPointsAngles = new List<double>();
            List<double> previousPointsAngles = new List<double>();

            for (int i = 0; i < cornerNextPoints.Size; i++)
            {
                nextPointsAngles.Add(System.Windows.Vector.AngleBetween(new System.Windows.Vector(cornerNextPoints[i].X - possibleCorner.X, cornerNextPoints[i].Y - possibleCorner.Y), new System.Windows.Vector(1, 0)));
            }
            for (int i = 0; i < cornerPreviousPoints.Size; i++)
            {
                previousPointsAngles.Add(System.Windows.Vector.AngleBetween(new System.Windows.Vector(cornerPreviousPoints[i].X - possibleCorner.X, cornerPreviousPoints[i].Y - possibleCorner.Y), new System.Windows.Vector(1, 0)));
            }

            double nextAngleAverage = nextPointsAngles.Average();
            double previousAngleAverage = previousPointsAngles.Average();
            double cornerAngle = Math.Abs(nextAngleAverage - previousAngleAverage);
            if(cornerAngle > 180) { cornerAngle = 360 - cornerAngle; }
            //if (Math.Abs(cornerAngle) > 180) { cornerAngle = Math.Sign(cornerAngle) * (360 - Math.Abs(cornerAngle)); }

            /*double standardDeviationNext = Tools.CalculateStandardDeviation(nextPointsAngles);
            double standardDeviationPrevious = Tools.CalculateStandardDeviation(previousPointsAngles);

            bool isCornerValid = (standardDeviationNext < 11 && standardDeviationPrevious < 11);*/

            double nextArcLen = CvInvoke.ArcLength(cornerNextPoints, false);
            double previousArcLen = CvInvoke.ArcLength(cornerPreviousPoints, false);
            double nextDirectLen = Utils.Distance(cornerNextPoints[0], cornerNextPoints[cornerNextPoints.Size - 1]);
            double previousDirectLen = Utils.Distance(cornerPreviousPoints[0], cornerPreviousPoints[cornerPreviousPoints.Size - 1]);
            double nextLenRatio = nextArcLen / nextDirectLen;
            double previousLenRatio = previousArcLen / previousDirectLen;

            double ratioLimit = 1.2; //(cornerAngle > 70 && cornerAngle < 110) ? 1.25 : 1.2;
            bool isCornerValid = (((nextLenRatio < ratioLimit && previousLenRatio < ratioLimit) || nextLenRatio == 1 || previousLenRatio == 1) && cornerAngle > 50 && cornerAngle < 140);

            Image<Rgb, byte> color_clone = Full_color.Clone();
            CvInvoke.Circle(color_clone, possibleCornerOnContour, 4, new MCvScalar(255, 0, 0), 2);
            for (int i = 0; i < cornerNextPoints.Size; i++) { CvInvoke.Circle(color_clone, Point.Round(cornerNextPoints[i]), 2, new MCvScalar(0, 255, 0), 1); }
            for (int i = 0; i < cornerPreviousPoints.Size; i++) { CvInvoke.Circle(color_clone, Point.Round(cornerPreviousPoints[i]), 2, new MCvScalar(0, 0, 255), 1); }
            //ProcessedImagesStorage.AddImage(PieceID + " Check Corner " + possibleCorner.ToString() + "  " + standardDeviationNext.ToString() + "  |  " + standardDeviationPrevious.ToString() + "   Valid: " + isCornerValid.ToString(), color_clone.Bitmap);
            ProcessedImagesStorage.AddImage(PieceID + " Check Corner " + possibleCorner.ToString() + "  " + nextLenRatio.ToString() + "  |  " + previousLenRatio.ToString() + "  |  " + cornerAngle.ToString() + "°   Valid: " + isCornerValid.ToString(), color_clone.Bitmap);

            return isCornerValid;
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Get the piece corners by finding the biggest rectangle of the contour points
        /// </summary>
        private void find_corners_MaximumRectangle()
        {
            corners.Clear();

            double maxAngleDiff = 5;
            double maxCornerDistRatio = 1.5;

            // Find all dominant corner points using the GFTTDetector (this uses the Harris corner detector)
            GFTTDetector detector = new GFTTDetector(1000, 0.01, 5, 6, true, 0.04);
            MKeyPoint[] keyPoints = detector.Detect(Bw.Clone());
            List<Point> possibleCorners = keyPoints.Select(k => Point.Round(k.Point)).ToList();

            // Draw the dominant key points
            Image<Rgb, byte> imgCorners = Full_color.Clone();
            Features2DToolbox.DrawKeypoints(imgCorners, new VectorOfKeyPoint(keyPoints), imgCorners, new Bgr(0, 0, 255));
            ProcessedImagesStorage.AddImage(PieceID + " Dominant Corners", imgCorners.Bitmap);

            // Sort the dominant corners by the distance to upper left corner of the bounding rectangle (0, 0) and keep only the corners that are near enough to this point
            List<Point> possibleCornersSortedUpperLeft = new List<Point>(possibleCorners);
            possibleCornersSortedUpperLeft.Sort(new DistanceToPointComparer(new Point(0, 0), DistanceOrders.NEAREST_FIRST));
            double minCornerDistUpperLeft = Utils.Distance(possibleCornersSortedUpperLeft[0], new PointF(0, 0));
            possibleCornersSortedUpperLeft = possibleCornersSortedUpperLeft.Where(c => Utils.Distance(c, new PointF(0, 0)) < minCornerDistUpperLeft * maxCornerDistRatio).ToList();

            // Sort the dominant corners by the distance to upper right corner of the bounding rectangle (ImageWidth, 0) and keep only the corners that are near enough to this point
            List<Point> possibleCornersSortedUpperRight = new List<Point>(possibleCorners);
            possibleCornersSortedUpperRight.Sort(new DistanceToPointComparer(new Point(Bw.Width, 0), DistanceOrders.NEAREST_FIRST));
            double minCornerDistUpperRight = Utils.Distance(possibleCornersSortedUpperRight[0], new PointF(Bw.Width, 0));
            possibleCornersSortedUpperRight = possibleCornersSortedUpperRight.Where(c => Utils.Distance(c, new PointF(Bw.Width, 0)) < minCornerDistUpperRight * maxCornerDistRatio).ToList();

            // Sort the dominant corners by the distance to lower right corner of the bounding rectangle (ImageWidth, ImageHeight) and keep only the corners that are near enough to this point
            List<Point> possibleCornersSortedLowerRight = new List<Point>(possibleCorners);
            possibleCornersSortedLowerRight.Sort(new DistanceToPointComparer(new Point(Bw.Width, Bw.Height), DistanceOrders.NEAREST_FIRST));
            double minCornerDistLowerRight = Utils.Distance(possibleCornersSortedLowerRight[0], new PointF(Bw.Width, Bw.Height));
            possibleCornersSortedLowerRight = possibleCornersSortedLowerRight.Where(c => Utils.Distance(c, new PointF(Bw.Width, Bw.Height)) < minCornerDistLowerRight * maxCornerDistRatio).ToList();

            // Sort the dominant corners by the distance to lower left corner of the bounding rectangle (0, ImageHeight) and keep only the corners that are near enough to this point
            List<Point> possibleCornersSortedLowerLeft = new List<Point>(possibleCorners);
            possibleCornersSortedLowerLeft.Sort(new DistanceToPointComparer(new Point(0, Bw.Height), DistanceOrders.NEAREST_FIRST));
            double minCornerDistLowerLeft = Utils.Distance(possibleCornersSortedLowerLeft[0], new PointF(0, Bw.Height));
            possibleCornersSortedLowerLeft = possibleCornersSortedLowerLeft.Where(c => Utils.Distance(c, new PointF(0, Bw.Height)) < minCornerDistLowerLeft * maxCornerDistRatio).ToList();

            // Combine all possibleCorners from the four lists and discard all combination with too bad angle differences
            List<FindCornerRectangleScore> scores = new List<FindCornerRectangleScore>();
            for(int indexUpperLeft = 0; indexUpperLeft < possibleCornersSortedUpperLeft.Count; indexUpperLeft++)
            {
                for (int indexUpperRight = 0; indexUpperRight < possibleCornersSortedUpperRight.Count; indexUpperRight++)
                {
                    for (int indexLowerRight = 0; indexLowerRight < possibleCornersSortedLowerRight.Count; indexLowerRight++)
                    {
                        for (int indexLowerLeft = 0; indexLowerLeft < possibleCornersSortedLowerLeft.Count; indexLowerLeft++)
                        {
                            // Possible corner combination
                            Point[] tmpCorners = new Point[]
                            {
                                possibleCornersSortedUpperLeft[indexUpperLeft],
                                possibleCornersSortedUpperRight[indexUpperRight],
                                possibleCornersSortedLowerRight[indexLowerRight],
                                possibleCornersSortedLowerLeft[indexLowerLeft]
                            };
                            double angleDiff = RectangleDifferenceAngle(tmpCorners);
                            if(angleDiff > maxAngleDiff) { continue; }
                            
                            double area = CvInvoke.ContourArea(new VectorOfPoint(tmpCorners));
                            FindCornerRectangleScore score = new FindCornerRectangleScore() { AngleDiff = angleDiff, RectangleArea = area, PossibleCorners = tmpCorners };
                            scores.Add(score);
                        }
                    }
                }
            }

            // Order the scores by rectangle area (biggest first) and take the PossibleCorners of the biggest rectangle as corners
            scores = scores.OrderByDescending(s => s.RectangleArea).ToList();
            if (scores.Count > 0) { corners.Push(scores[0].PossibleCorners); }

            if (corners.Size != 4)
            {
                System.Windows.MessageBox.Show("Failed to find correct number of corners: " + corners.Size + " found.");
                return;
            }

            for (int i = 0; i < 4; i++) { CvInvoke.Circle(imgCorners, Point.Round(corners[i]), 4, new MCvScalar(0, 255, 0), 3); }
            ProcessedImagesStorage.AddImage(PieceID + " Corners", imgCorners.Bitmap);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Get the sum of the differences of all 4 corner angles from 90 degree.
        /// </summary>
        /// <param name="points">List of 4 points that form rectangle (approximately)</param>
        /// <returns>Sum of the differences of all 4 corner angles from 90 degree</returns>
        private double RectangleDifferenceAngle(Point[] points)
        {
            double angle1 = System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[1].X - points[0].X, points[1].Y - points[0].Y), new System.Windows.Vector(points[3].X - points[0].X, points[3].Y - points[0].Y));
            double angle2 = System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[2].X - points[1].X, points[2].Y - points[1].Y), new System.Windows.Vector(points[0].X - points[1].X, points[0].Y - points[1].Y));
            double angle3 = System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[3].X - points[2].X, points[3].Y - points[2].Y), new System.Windows.Vector(points[1].X - points[2].X, points[1].Y - points[2].Y));
            double angle4 = System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[0].X - points[3].X, points[0].Y - points[3].Y), new System.Windows.Vector(points[2].X - points[3].X, points[2].Y - points[3].Y));
            double sum90DegreeDiff = Math.Abs(90 - angle1) + Math.Abs(90 - angle2) + Math.Abs(90 - angle3) + Math.Abs(90 - angle4);

            return sum90DegreeDiff;
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        private void find_corners_Curvature()
        {
            corners.Clear();
            Image<Rgb, byte> color_clone = Full_color.Clone();

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(Bw.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple); //ChainApproxMethod.ChainApproxNone);
            VectorOfPoint contour = contours[0];
            List<Point> contourList = contour.ToArray().ToList();

            //CvInvoke.DrawContours(color_clone, contours, 0, new MCvScalar(0, 0, 255), 1);
            for(int i = 0; i < contour.Size; i++) { CvInvoke.Circle(color_clone, contour[i], 2, new MCvScalar(0, 0, 255), 1); }
            ProcessedImagesStorage.AddImage(PieceID + " Contour", color_clone.Bitmap);

            List<double> curvature = Utils.CalculateCurvature(contourList, 2);

            List<double> curvatureDraw = new List<double>(curvature);
            curvatureDraw = curvatureDraw.Select(c => (double.IsInfinity(c) ? 25 : c)).ToList();
            Image<Rgb, byte> curvatureImg = new Image<Rgb, byte>(curvatureDraw.Count, 512);
            VectorOfPoint curvatureContour = new VectorOfPoint();
            double scale = 255 / Math.Max(Math.Abs(curvatureDraw.Max()), Math.Abs(curvatureDraw.Min()));
            for (int i = 0; i < curvatureDraw.Count; i++)
            {
                curvatureContour.Push(new Point(i, (int)(scale * curvatureDraw[i] + 255)));
            }
            CvInvoke.DrawContours(curvatureImg, new VectorOfVectorOfPoint(curvatureContour), -1, new MCvScalar(0, 255, 0));
            ProcessedImagesStorage.AddImage(PieceID + " Curvature Img", curvatureImg.Bitmap);


            Image<Rgb, byte> color_clone2 = Full_color.Clone();
            for (int i = 0; i < curvature.Count; i++)
            {
                if (curvature[i] > 0.1) { CvInvoke.Circle(color_clone2, Point.Round(contour[i]), 4, new MCvScalar(0, 0, 255), 1); }
            }

            /*List<double> curvatureSorted = new List<double>(curvature);
            curvatureSorted.Sort();
            curvatureSorted.Reverse();
            int cntHighestCurvatures = 0;
            for (int i = 0; i < curvatureSorted.Count; i++)
            {
                if (double.IsInfinity(curvatureSorted[i])) { CvInvoke.Circle(color_clone2, Point.Round(contour[curvature.IndexOf(curvatureSorted[i])]), 4, new MCvScalar(255, 0, 0), 1); }
                else { CvInvoke.Circle(color_clone2, Point.Round(contour[curvature.IndexOf(curvatureSorted[i])]), 4, new MCvScalar(0, 0, 255), 1); cntHighestCurvatures++; }

                if(cntHighestCurvatures == 4) { break; }
            }*/

            ProcessedImagesStorage.AddImage(PieceID + " Curvature Points", color_clone2.Bitmap);
            

            if (corners.Size != 4)
            {
                //System.Windows.MessageBox.Show("Failed to find correct number of corners: " + corners.Size + " found.");
                return;
            }

            for (int i = 0; i < 4; i++) { CvInvoke.Circle(color_clone, Point.Round(corners[i]), 4, new MCvScalar(0, 255, 0), 3); }
            ProcessedImagesStorage.AddImage(PieceID + " Corners", color_clone.Bitmap);
        }

        #endregion

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Extract the contour.
        /// TODO: probably should have this passed in from the puzzle, since it already does this. It was done this way because the contours don't correspond to the correct pixel locations in this cropped version of the image.
        /// </summary>
        private void extract_edges()
        {
            if(corners.Size != 4) { return; }

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(Bw.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            if (contours.Size == 0)
            {
                System.Windows.MessageBox.Show("Found no contours!");
                return;
            }

            int indexLargestContour = 0;                // Find the largest contour
            double largestContourArea = 0;
            for(int i = 0; i < contours.Size; i++)
            {
                double contourAreaTmp = CvInvoke.ContourArea(contours[i]);
                if(contourAreaTmp > largestContourArea) { largestContourArea = contourAreaTmp; indexLargestContour = i; }
            }

            VectorOfPoint contour = contours[indexLargestContour];
            //contour = Utils.RemoveDuplicates(contour);

            Image<Rgb, byte> edge_img = Full_color.Clone();

            VectorOfPoint new_corners = new VectorOfPoint();
            for (int i = 0; i < corners.Size; i++)      //out of all of the found corners, find the closest points in the contour, these will become the endpoints of the edges
            {
                double best = 10000000000;
                Point closest_point = contour[0];
                for (int j = 0; j < contour.Size; j++)
                {
                    double d = Utils.Distance(corners[i], contour[j]);
                    if (d < best)
                    {
                        best = d;
                        closest_point = contour[j];
                    }
                }
                new_corners.Push(closest_point);
            }
            corners = new_corners;
            
            for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(edge_img, Point.Round(corners[i]), 2, new MCvScalar(255, 0, 0), 1); }
            ProcessedImagesStorage.AddImage(PieceID + " New corners", edge_img.Bitmap);

            ////We need the beginning of the vector to correspond to the begining of an edge.
            //Point[] contourArray = contour.ToArray();
            //contourArray.Rotate(find_first_in(contour, corners));
            //contour = new VectorOfPoint(contourArray);

            List<int> sections = find_all_in(contour, corners);

            //Make corners go in the correct order
            VectorOfPoint new_corners2 = new VectorOfPoint();
            for (int i = 0; i < 4; i++)
            {
                new_corners2.Push(contour[sections[i]]);
            }
            corners = new_corners2;
            
            Edges[0] = new Edge(PieceID, 0, Full_color, contour.GetSubsetOfVector(sections[0], sections[1]));
            Edges[1] = new Edge(PieceID, 1, Full_color, contour.GetSubsetOfVector(sections[1], sections[2]));
            Edges[2] = new Edge(PieceID, 2, Full_color, contour.GetSubsetOfVector(sections[2], sections[3]));
            Edges[3] = new Edge(PieceID, 3, Full_color, contour.GetSubsetOfVector(sections[3], sections[0]));
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Classify the type of piece
        /// </summary>
        private void classify()
        {
            int count = 0;
            for (int i = 0; i < 4; i++)
            {
                if(Edges[i] == null) { return; }
                if (Edges[i].EdgeType == EdgeTypes.LINE) count++;
            }
            if (count == 0)
            {
                PieceType = PieceTypes.INNER;
            }
            else if (count == 1)
            {
                PieceType = PieceTypes.BORDER;
            }
            else if (count == 2)
            {
                PieceType = PieceTypes.CORNER;
            }
            else
            {
                System.Windows.MessageBox.Show("Problem, found too many outer edges for this piece.");
            }

            ProcessedImagesStorage.AddImage(PieceID + " Type " + PieceType.ToString(), Full_color.Clone().Bitmap);
        }

        //##############################################################################################################################################################################################

        public PointF GetCorner(int id)
        {
            if(corners.Size == 0) { return new PointF(0, 0); }
            return corners[id];
        }

        //**********************************************************************************************************************************************************************************************
        
        
        /// <summary>
        /// This method "rotates the corners and edges so they are in a correct order.
        /// </summary>
        /// <param name="times">Number of times to rotate</param>
        public void Rotate(int times)
        {
            int times_to_rotate = times % 4;
            Edges.Rotate(times_to_rotate);

            Point[] cornersArray = corners.ToArray();
            cornersArray.Rotate(times_to_rotate);
            corners = new VectorOfPoint(cornersArray);

            Full_color = Full_color.Rotate(times_to_rotate * 90, new Rgb(Color.Transparent), false);
            Bw = Bw.Rotate(times_to_rotate * 90, new Gray(0), false);
        }

    }
}
