using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public class Piece : ObservableObject
    {
        public static int NextPieceID { get; set; }

        static Piece()
        {
            NextPieceID = 0;
        }

        //##############################################################################################################################################################################################

        private string _pieceSourceFileName;
        /// <summary>
        /// The name of the file from where the piece was extracted.
        /// </summary>
        public string PieceSourceFileName
        {
            get { return _pieceSourceFileName; }
            private set { _pieceSourceFileName = value;  OnPropertyChanged(); }
        }

        private PieceTypes _pieceType;
        /// <summary>
        /// Type of the Piece (CORNER, BORDER, INNER)
        /// </summary>
        public PieceTypes PieceType
        {
            get { return _pieceType; }
            private set { _pieceType = value; OnPropertyChanged(); }
        }

        private string _pieceID;
        /// <summary>
        /// A string that is unique for each extracted Piece.
        /// </summary>
        public string PieceID
        {
            get { return _pieceID; }
            set { _pieceID = value; OnPropertyChanged(); }
        }

        private Image<Rgb, byte> _pieceImgColor;
        /// <summary>
        /// Color image of the extracted piece
        /// </summary>
        public Image<Rgb, byte> PieceImgColor
        {
            get { return _pieceImgColor; }
            set { _pieceImgColor = value;  OnPropertyChanged(); }
        }

        private Image<Gray, byte> _pieceImgBw;
        /// <summary>
        /// Black white image of the extracted piece
        /// </summary>
        public Image<Gray, byte> PieceImgBw
        {
            get { return _pieceImgBw; }
            set { _pieceImgBw = value; OnPropertyChanged(); }
        }

        private int _solutionRotation;
        /// <summary>
        /// Rotation of the piece in the solution image
        /// </summary>
        public int SolutionRotation
        {
            get { return _solutionRotation; }
            set { _solutionRotation = value; OnPropertyChanged(); }
        }

        private Point _solutionLocation;
        /// <summary>
        /// Location of the piece in the solution image (not pixel coordinates, but row and column index)
        /// </summary>
        public Point SolutionLocation
        {
            get { return _solutionLocation; }
            set { _solutionLocation = value; OnPropertyChanged(); }
        }

        private string _solutionID;
        /// <summary>
        /// Id of the solution the piece belongs to (there are more solutions if not all pieces fit together and there are multiple groups of pieces)
        /// </summary>
        public string SolutionID
        {
            get { return _solutionID; }
            set { _solutionID = value; OnPropertyChanged(); }
        }

        public Edge[] Edges = new Edge[4];

        public PuzzleSolverParameters SolverParameters { get; private set; }

        private VectorOfPoint corners = new VectorOfPoint();
        private IProgress<LogBox.LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        //##############################################################################################################################################################################################

        public Piece(Image<Rgb, byte> color, Image<Gray, byte> bw, string pieceSourceFileName, PuzzleSolverParameters solverParameters, IProgress<LogBox.LogEvent> logHandle, CancellationToken cancelToken)
        {
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            SolverParameters = solverParameters;
            PieceID = "Piece#" + NextPieceID.ToString();
            NextPieceID++;
            PieceImgColor = color.Clone();
            PieceImgBw = bw.Clone();
            PieceSourceFileName = pieceSourceFileName;
            
            _logHandle.Report(new LogBox.LogEventImage(PieceID + " Color", color.Clone().Bitmap));
            if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Bw", bw.Clone().Bitmap)); }

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
        /// <returns>Indices of all elements in vector 1 that exist in vector 2 too (ordered by occurence in vec2)</returns>
        private List<int> find_all_in(VectorOfPoint vec1, VectorOfPoint vec2)
        {
            List<int> places = new List<int>();
            for (int i2 = 0; i2 < vec2.Size; i2++)
            {
                for (int i1 = 0; i1 < vec1.Size; i1++)
                {
                    if (vec1[i1].X == vec2[i2].X && vec1[i1].Y == vec2[i2].Y)
                    {
                        places.Add(i1);
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
            _logHandle.Report(new LogBox.LogEventInfo(PieceID + " Finding corners with GFTT algorithm"));

            string id = PieceID;

            double minDistance = SolverParameters.PuzzleMinPieceSize;    //How close can 2 corners be?
            int blockSize = 5; //25;                     //How big of an area to look for the corner in.
            bool useHarrisDetector = true;
            double k = 0.04;

            double min = 0;
            double max = 1;
            int max_iterations = 100;
            bool found_all_corners = false;

            Image<Gray, byte> bw_clone = PieceImgBw.Clone();

            //Binary search, altering quality until exactly 4 corners are found. Usually done in 1 or 2 iterations
            while (0 < max_iterations--)
            {
                if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

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
                CvInvoke.Circle(PieceImgColor, Point.Round(corners[i]), 4, new MCvScalar(255, 0, 0));
            }
            if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Corners", PieceImgColor.Clone().Bitmap)); }

            if (!found_all_corners)
            {
                _logHandle.Report(new LogBox.LogEventError(PieceID + " Failed to find correct number of corners. " + corners.Size + " found."));
            }
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Get the piece corners by finding the biggest rectangle of the contour points
        /// </summary>
        private void find_corners_MaximumRectangle()
        {
            _logHandle.Report(new LogBox.LogEventInfo(PieceID + " Finding corners by finding the maximum rectangle within candidate points"));

            corners.Clear();
            Image<Rgb, byte> imgCorners = PieceImgColor.Clone();

            // Find all dominant corner points using the GFTTDetector (this uses the Harris corner detector)
            GFTTDetector detector = new GFTTDetector(SolverParameters.PieceFindCornersGFTTMaxCorners, SolverParameters.PieceFindCornersGFTTQualityLevel, SolverParameters.PieceFindCornersGFTTMinDist, SolverParameters.PieceFindCornersGFTTBlockSize, true, 0.04);
            MKeyPoint[] keyPoints = detector.Detect(PieceImgBw.Clone());
            List<Point> possibleCorners = keyPoints.Select(k => Point.Round(k.Point)).ToList();

            if (possibleCorners.Count > 0)
            {
                // Sort the dominant corners by the distance to upper left corner of the bounding rectangle (0, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperLeft = new List<Point>(possibleCorners);
                possibleCornersSortedUpperLeft.Sort(new DistanceToPointComparer(new Point(0, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperLeft = Utils.Distance(possibleCornersSortedUpperLeft[0], new PointF(0, 0));
                possibleCornersSortedUpperLeft = possibleCornersSortedUpperLeft.Where(c => Utils.Distance(c, new PointF(0, 0)) < minCornerDistUpperLeft * SolverParameters.PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to upper right corner of the bounding rectangle (ImageWidth, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperRight = new List<Point>(possibleCorners);
                possibleCornersSortedUpperRight.Sort(new DistanceToPointComparer(new Point(PieceImgBw.Width, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperRight = Utils.Distance(possibleCornersSortedUpperRight[0], new PointF(PieceImgBw.Width, 0));
                possibleCornersSortedUpperRight = possibleCornersSortedUpperRight.Where(c => Utils.Distance(c, new PointF(PieceImgBw.Width, 0)) < minCornerDistUpperRight * SolverParameters.PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower right corner of the bounding rectangle (ImageWidth, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerRight = new List<Point>(possibleCorners);
                possibleCornersSortedLowerRight.Sort(new DistanceToPointComparer(new Point(PieceImgBw.Width, PieceImgBw.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerRight = Utils.Distance(possibleCornersSortedLowerRight[0], new PointF(PieceImgBw.Width, PieceImgBw.Height));
                possibleCornersSortedLowerRight = possibleCornersSortedLowerRight.Where(c => Utils.Distance(c, new PointF(PieceImgBw.Width, PieceImgBw.Height)) < minCornerDistLowerRight * SolverParameters.PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower left corner of the bounding rectangle (0, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerLeft = new List<Point>(possibleCorners);
                possibleCornersSortedLowerLeft.Sort(new DistanceToPointComparer(new Point(0, PieceImgBw.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerLeft = Utils.Distance(possibleCornersSortedLowerLeft[0], new PointF(0, PieceImgBw.Height));
                possibleCornersSortedLowerLeft = possibleCornersSortedLowerLeft.Where(c => Utils.Distance(c, new PointF(0, PieceImgBw.Height)) < minCornerDistLowerLeft * SolverParameters.PieceFindCornersMaxCornerDistRatio).ToList();

                // Combine all possibleCorners from the four lists and discard all combination with too bad angle differences
                List<FindCornerRectangleScore> scores = new List<FindCornerRectangleScore>();
                for (int indexUpperLeft = 0; indexUpperLeft < possibleCornersSortedUpperLeft.Count; indexUpperLeft++)
                {
                    for (int indexUpperRight = 0; indexUpperRight < possibleCornersSortedUpperRight.Count; indexUpperRight++)
                    {
                        for (int indexLowerRight = 0; indexLowerRight < possibleCornersSortedLowerRight.Count; indexLowerRight++)
                        {
                            for (int indexLowerLeft = 0; indexLowerLeft < possibleCornersSortedLowerLeft.Count; indexLowerLeft++)
                            {
                                if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                                // Possible corner combination
                                Point[] tmpCorners = new Point[]
                                {
                                    possibleCornersSortedUpperLeft[indexUpperLeft],         // the corners are ordered beginning in the upper left corner and going counter clock wise
                                    possibleCornersSortedLowerLeft[indexLowerLeft],
                                    possibleCornersSortedLowerRight[indexLowerRight],
                                    possibleCornersSortedUpperRight[indexUpperRight]
                                };
                                double angleDiff = RectangleDifferenceAngle(tmpCorners);
                                if (angleDiff > SolverParameters.PieceFindCornersMaxAngleDiff) { continue; }

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
            }

            if (corners.Size != 4)
            {
                _logHandle.Report(new LogBox.LogEventError(PieceID + " Failed to find correct number of corners. " + corners.Size + " found."));
                return;
            }

            Features2DToolbox.DrawKeypoints(imgCorners, new VectorOfKeyPoint(keyPoints), imgCorners, new Bgr(0, 0, 255));       // Draw the dominant key points
            //for (int i = 0; i < 4; i++) { CvInvoke.Circle(imgCorners, Point.Round(corners[i]), 4, new MCvScalar(0, 255, 0), 3); }
            CvInvoke.Circle(imgCorners, Point.Round(corners[0]), 4, new MCvScalar(0, 255, 0), 3);
            CvInvoke.Circle(imgCorners, Point.Round(corners[1]), 4, new MCvScalar(0, 200, 0), 3);
            CvInvoke.Circle(imgCorners, Point.Round(corners[2]), 4, new MCvScalar(0, 150, 0), 3);
            CvInvoke.Circle(imgCorners, Point.Round(corners[3]), 4, new MCvScalar(0, 100, 0), 3);
            if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Corners", imgCorners.Bitmap)); }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Get the sum of the differences of all 4 corner angles from 90 degree.
        /// </summary>
        /// <param name="points">List of 4 points that form rectangle (approximately)</param>
        /// <returns>Sum of the differences of all 4 corner angles from 90 degree</returns>
        private double RectangleDifferenceAngle(Point[] points)
        {
            double angle1 = Math.Abs(System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[1].X - points[0].X, points[1].Y - points[0].Y), new System.Windows.Vector(points[3].X - points[0].X, points[3].Y - points[0].Y)));
            double angle2 = Math.Abs(System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[2].X - points[1].X, points[2].Y - points[1].Y), new System.Windows.Vector(points[0].X - points[1].X, points[0].Y - points[1].Y)));
            double angle3 = Math.Abs(System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[3].X - points[2].X, points[3].Y - points[2].Y), new System.Windows.Vector(points[1].X - points[2].X, points[1].Y - points[2].Y)));
            double angle4 = Math.Abs(System.Windows.Vector.AngleBetween(new System.Windows.Vector(points[0].X - points[3].X, points[0].Y - points[3].Y), new System.Windows.Vector(points[2].X - points[3].X, points[2].Y - points[3].Y)));
            double sum90DegreeDiff = Math.Abs(90 - angle1) + Math.Abs(90 - angle2) + Math.Abs(90 - angle3) + Math.Abs(90 - angle4);

            return sum90DegreeDiff;
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
            _logHandle.Report(new LogBox.LogEventInfo(PieceID + " Extracting edges"));

            if (corners.Size != 4) { return; }

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(PieceImgBw.Clone(), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            if (contours.Size == 0)
            {
                _logHandle.Report(new LogBox.LogEventError(PieceID + " No contours found."));
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

            Image<Rgb, byte> edge_img = PieceImgColor.Clone();

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

            if (SolverParameters.SolverShowDebugResults)
            {
                for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(edge_img, Point.Round(corners[i]), 2, new MCvScalar(255, 0, 0), 1); }
                _logHandle.Report(new LogBox.LogEventImage(PieceID + " New corners", edge_img.Bitmap));
            }

            ////We need the beginning of the vector to correspond to the begining of an edge.
            //int indexOfFirstCornerInContour = contour.ToArray().ToList().IndexOf(corners[0]);
            //Point[] contourArray = contour.ToArray();
            //contourArray.Rotate(indexOfFirstCornerInContour); //find_first_in(contour, corners));
            //contour = new VectorOfPoint(contourArray);

            List<int> sections = find_all_in(contour, corners);

            //Make corners go in the correct order
            VectorOfPoint new_corners2 = new VectorOfPoint();
            for (int i = 0; i < 4; i++)
            {
                new_corners2.Push(contour[sections[i]]);
            }
            corners = new_corners2;
            
            Edges[0] = new Edge(PieceID, 0, PieceImgColor, contour.GetSubsetOfVector(sections[0], sections[1]), SolverParameters, _logHandle, _cancelToken);
            Edges[1] = new Edge(PieceID, 1, PieceImgColor, contour.GetSubsetOfVector(sections[1], sections[2]), SolverParameters, _logHandle, _cancelToken);
            Edges[2] = new Edge(PieceID, 2, PieceImgColor, contour.GetSubsetOfVector(sections[2], sections[3]), SolverParameters, _logHandle, _cancelToken);
            Edges[3] = new Edge(PieceID, 3, PieceImgColor, contour.GetSubsetOfVector(sections[3], sections[0]), SolverParameters, _logHandle, _cancelToken);
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
                _logHandle.Report(new LogBox.LogEventError(PieceID + " Found too many edges for this piece. " + count.ToString() + " found."));
            }

            if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Type " + PieceType.ToString(), PieceImgColor.Clone().Bitmap)); }
        }

        //##############################################################################################################################################################################################

        public PointF GetCorner(int id)
        {
            if(corners.Size == 0) { return new PointF(0, 0); }
            return corners[id];
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// This method rotates the corners and edges so they are in a correct order.
        /// </summary>
        /// <param name="times">Number of times to rotate</param>
        public void Rotate(int times)
        {
            int times_to_rotate = times % 4;
            Edges.Rotate(times_to_rotate);

            Point[] cornersArray = corners.ToArray();
            cornersArray.Rotate(times_to_rotate);
            corners = new VectorOfPoint(cornersArray);

            PieceImgColor = PieceImgColor.Rotate(times_to_rotate * 90, new Rgb(Color.Transparent), false);
            PieceImgBw = PieceImgBw.Rotate(times_to_rotate * 90, new Gray(0), false);
        }

    }
}
