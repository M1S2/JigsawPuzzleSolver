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
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LogBox.LogEvents;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for the piece is that there are 4 edges the edge "numbers" go from 0-3 in counter clockwise order starting from the left.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/piece.cpp
    [DataContract]
    public class Piece : SaveableObject<Piece>, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. The CallerMemberName attribute that is applied to the optional propertyName parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName">Name of the property that is changed</param>
        /// see: https://docs.microsoft.com/de-de/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //##############################################################################################################################################################################################

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
        [DataMember]
        public string PieceSourceFileName
        {
            get { return _pieceSourceFileName; }
            private set { _pieceSourceFileName = value;  OnPropertyChanged(); }
        }

        private Point _pieceSourceFileLocation;
        /// <summary>
        /// Location of the piece in the source file in pixel.
        /// </summary>
        [DataMember]
        public Point PieceSourceFileLocation
        {
            get { return _pieceSourceFileLocation; }
            private set { _pieceSourceFileLocation = value; OnPropertyChanged(); }
        }

        private PieceTypes _pieceType;
        /// <summary>
        /// Type of the Piece (CORNER, BORDER, INNER)
        /// </summary>
        [DataMember]
        public PieceTypes PieceType
        {
            get { return _pieceType; }
            private set { _pieceType = value; OnPropertyChanged(); }
        }

        private string _pieceID;
        /// <summary>
        /// A string that is unique for each extracted Piece.
        /// </summary>
        [DataMember]
        public string PieceID
        {
            get { return _pieceID; }
            set { _pieceID = value; OnPropertyChanged(); }
        }

        private int _pieceIndex;
        /// <summary>
        /// Index of the extracted Piece.
        /// </summary>
        [DataMember]
        public int PieceIndex
        {
            get { return _pieceIndex; }
            set { _pieceIndex = value; OnPropertyChanged(); }
        }

        private LocalDriveBitmap _pieceImgColor;
        /// <summary>
        /// Color image of the extracted piece
        /// </summary>
        [DataMember]
        public LocalDriveBitmap PieceImgColor
        {
            get { return _pieceImgColor; }
            set { _pieceImgColor = value;  OnPropertyChanged(); }
        }

        private LocalDriveBitmap _pieceImgBw;
        /// <summary>
        /// Black white image of the extracted piece
        /// </summary>
        [DataMember]
        public LocalDriveBitmap PieceImgBw
        {
            get { return _pieceImgBw; }
            set { _pieceImgBw = value; OnPropertyChanged(); }
        }

        private int _solutionRotation;
        /// <summary>
        /// Rotation of the piece in the solution image
        /// </summary>
        [DataMember]
        public int SolutionRotation
        {
            get { return _solutionRotation; }
            set { _solutionRotation = value; OnPropertyChanged(); }
        }

        private Point _solutionLocation;
        /// <summary>
        /// Location of the piece in the solution image (not pixel coordinates, but row and column index)
        /// </summary>
        [DataMember]
        public Point SolutionLocation
        {
            get { return _solutionLocation; }
            set { _solutionLocation = value; OnPropertyChanged(); }
        }

        private int _solutionID;
        /// <summary>
        /// Id of the solution the piece belongs to (there are more solutions if not all pieces fit together and there are multiple groups of pieces)
        /// </summary>
        [DataMember]
        public int SolutionID
        {
            get { return _solutionID; }
            set { _solutionID = value; OnPropertyChanged(); }
        }

        private Size _pieceSize;
        /// <summary>
        /// Size of the piece in pixel
        /// </summary>
        [DataMember]
        public Size PieceSize
        {
            get { return _pieceSize; }
            set { _pieceSize = value; OnPropertyChanged(); }
        }

        [DataMember]
        public Edge[] Edges = new Edge[4];
        
        private VectorOfPoint corners = new VectorOfPoint();
        private IProgress<LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        //##############################################################################################################################################################################################

        public Piece(Image<Rgba, byte> color, Image<Gray, byte> bw, string pieceSourceFileName, Point pieceSourceFileLocation, IProgress<LogEvent> logHandle, CancellationToken cancelToken)
        {
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            PieceID = "Piece#" + NextPieceID.ToString();
            PieceIndex = NextPieceID;
            NextPieceID++;

            PieceImgColor = new LocalDriveBitmap(System.IO.Path.GetDirectoryName(pieceSourceFileName) + @"\Results\" + PieceID + "_Color.png", color.Bitmap);
            PieceImgBw = new LocalDriveBitmap(System.IO.Path.GetDirectoryName(pieceSourceFileName) + @"\Results\" + PieceID + "_Bw.png", bw.Bitmap);
            PieceSourceFileName = pieceSourceFileName;
            PieceSourceFileLocation = pieceSourceFileLocation;
            PieceSize = bw.Bitmap.Size;

            _logHandle.Report(new LogEventImage(PieceID + " Color", color.Bitmap));
            if (PuzzleSolverParameters.Instance.SolverShowDebugResults) { _logHandle.Report(new LogEventImage(PieceID + " Bw", bw.Bitmap)); }

            process();
        }

        public Piece()
        { }

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
            //find_corners_MaximumRectangle();

            find_corners_PolarCoordinates();

            extract_edges();
            classify();
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        #region find_corners functions

        /// <summary>
        /// Get the piece corners by finding peaks in the polar representation of the contour points
        /// </summary>
        /// see: http://www.martijn-onderwater.nl/2016/10/13/puzzlemaker-extracting-the-four-sides-of-a-jigsaw-piece-from-the-boundary/
        /// see: https://web.stanford.edu/class/cs231a/prev_projects_2016/computer-vision-solve__1_.pdf
        private void find_corners_PolarCoordinates()
        {
            Bitmap bwImg = PieceImgBw.Bmp;
            Size pieceMiddle = new Size(bwImg.Width / 2, bwImg.Height / 2);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(new Image<Gray, byte>(bwImg), contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            if (contours.Size == 0) { return; }
            VectorOfPoint contour = contours[0];

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {
                using (Image<Rgb, byte> polarContourImg = new Image<Rgb, byte>(PieceImgColor.Bmp))
                {
                    for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(polarContourImg, Point.Round(contour[i]), 2, new MCvScalar(255 * ((double)i / contour.Size), 0, 255 - 255 * ((double)i / contour.Size)), 1); }
                    _logHandle.Report(new LogEventImage(PieceID + " Polar Contour", polarContourImg.Bitmap));
                }
            }

            List<PolarCoordinate> polarContour = new List<PolarCoordinate>();
            for (int i = 0; i < contour.Size; i++)
            {
                polarContour.Add(PolarCoordinate.CartesianToPolar(Point.Subtract(contour[i], pieceMiddle)));        // Shift the origin to the middle of the piece to get the full 360 degree range for angles instead of only using 90 degree
            }

            //List<PointF> polarContourPoints = polarContour.Select(p => new PointF((float)p.Angle, (float)p.Radius)).ToList();
            //polarContourPoints = DouglasPeuckerLineApprox.DouglasPeuckerReduction(polarContourPoints, 1);
            //polarContour = polarContourPoints.Select(p => new PolarCoordinate(p.X, p.Y)).ToList();

            //List<double> smoothedValues = SmoothingFilter.SmoothData(polarContour.Select(p => p.Radius).ToList(), 7, 0.4);
            //List<PolarCoordinate> polarContourSmoothed = new List<PolarCoordinate>();
            //for (int i = 0; i < polarContour.Count; i++) { polarContourSmoothed.Add(new PolarCoordinate(polarContour[i].Angle, smoothedValues[i])); }
            //polarContour = polarContourSmoothed;
            
            List<double> contourRadius = polarContour.Select(p => p.Radius).ToList();
            List<int> peakPosOut = DifferencePeakFinder.FindPeaksCyclic(contourRadius, 5, 0, 1); //2, 0.999);
            double contourRadiusRange = contourRadius.Max() - contourRadius.Min();
            List<PolarCoordinate> cornerCandidatesPolar = polarContour.Where(p => peakPosOut[polarContour.IndexOf(p)] == 1 && p.Radius > contourRadius.Min() + PuzzleSolverParameters.Instance.PieceFindCornersPeakDismissPercentage * contourRadiusRange).ToList();
            cornerCandidatesPolar = cornerCandidatesPolar.OrderBy(p => p.Angle).ToList();

            List<PolarCoordinate> cornersPolar = new List<PolarCoordinate>();

            if (cornerCandidatesPolar.Count < 4)
            {
                _logHandle.Report(new LogEventWarning(PieceID + " not enough corners found (" + cornerCandidatesPolar.Count.ToString() + ")"));
            }
            else
            {
                //Rotate perfect square to find corners with minimum difference to it
                double minSum = double.MaxValue;
                int minAngle = 0;
                for (int i = 0; i < 360; i++)
                {
                    double angleDiffSum = 0;
                    for (int a = 0; a < 360; a += 90)
                    {
                        List<PolarCoordinate> rangePolarPoints = cornerCandidatesPolar.Where(p => Utils.IsAngleInRange(p.Angle, i + a - 45, i + a + 45)).ToList();
                        List<double> rangeDiffs = rangePolarPoints.Select(p => Math.Abs(Utils.GetPositiveAngle(i + a) - p.Angle)).ToList();
                        double angleDiff = rangeDiffs.Count <= 0 ? double.MaxValue : rangeDiffs.Sum();
                        angleDiffSum += angleDiff;
                    }
                    if (angleDiffSum < minSum)
                    {
                        minSum = angleDiffSum;
                        minAngle = i;
                    }
                }
                
                corners.Clear();
                for (int a = 270; a >= 0; a -= 90)
                {
                    PolarCoordinate polarCorner = cornerCandidatesPolar.OrderBy(p => Utils.AngleDiff(p.Angle, Utils.GetPositiveAngle(minAngle + a), true)).First();     // Get the corner candiate that has the minimum distance to the current ideal square point position
                    corners.Push(Point.Add(Point.Round(PolarCoordinate.PolarToCartesian(polarCorner)), pieceMiddle));
                    cornersPolar.Add(polarCorner);
                }
            }

            bwImg.Dispose();

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {                
                int maxRadius = (int)polarContour.Select(p => p.Radius).Max();
                using (Mat polarImg = new Mat(maxRadius, 360, DepthType.Cv8U, 3))
                {
                    for (int i = 0; i < polarContour.Count - 1; i++) { CvInvoke.Line(polarImg, new Point((int)polarContour[i].Angle, maxRadius - (int)polarContour[i].Radius), new Point((int)polarContour[i + 1].Angle, maxRadius - (int)polarContour[i + 1].Radius), new MCvScalar(255, 0, 0), 1, LineType.EightConnected); }
                    for (int i = 0; i < cornerCandidatesPolar.Count; i++) { CvInvoke.Circle(polarImg, new Point((int)cornerCandidatesPolar[i].Angle, maxRadius - (int)cornerCandidatesPolar[i].Radius), 3, new MCvScalar(0, 0, 255), -1); }
                    for (int i = 0; i < cornersPolar.Count; i++) { CvInvoke.Circle(polarImg, new Point((int)cornersPolar[i].Angle, maxRadius - (int)cornersPolar[i].Radius), 2, new MCvScalar(0, 255, 0), -1); }
                    _logHandle.Report(new LogEventImage(PieceID + " Polar", polarImg.ToImage<Rgb, byte>().Bitmap));
                    polarImg.Dispose();

                    Image<Rgb, byte> corner_img = new Image<Rgb, byte>(PieceImgColor.Bmp);
                    for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(corner_img, Point.Round(corners[i]), 7, new MCvScalar(255, 0, 0), -1); }
                    _logHandle.Report(new LogEventImage(PieceID + " Found Corners (" + corners.Size.ToString() + ")", corner_img.Bitmap));
                    corner_img.Dispose();
                }
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find the 4 strongest corners.
        /// </summary>
        /// see: http://docs.opencv.org/doc/tutorials/features2d/trackingmotion/corner_subpixeles/corner_subpixeles.html
        private void find_corners_GFTT()
        {
            _logHandle.Report(new LogEventInfo(PieceID + " Finding corners with GFTT algorithm"));

            string id = PieceID;

            double minDistance = PuzzleSolverParameters.Instance.PuzzleMinPieceSize;    //How close can 2 corners be?
            int blockSize = 5; //25;                     //How big of an area to look for the corner in.
            bool useHarrisDetector = true;
            double k = 0.04;

            double min = 0;
            double max = 1;
            int max_iterations = 100;
            bool found_all_corners = false;

            Image<Gray, byte> bw_clone = new Image<Gray, byte>(PieceImgBw.Bmp);

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

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {
                Bitmap colorImg = PieceImgColor.Bmp;
                Graphics g = Graphics.FromImage(colorImg);
                for (int i = 0; i < corners.Size; i++)
                {
                    g.DrawEllipse(new Pen(Color.Red), new RectangleF(PointF.Subtract(corners[i], new Size(2, 2)), new SizeF(4, 4)));
                    //CvInvoke.Circle(PieceImgColor, Point.Round(corners[i]), 4, new MCvScalar(255, 0, 0));
                }
                g.Dispose();
                _logHandle.Report(new LogEventImage(PieceID + " Corners", colorImg));
                colorImg.Dispose();
            }

            if (!found_all_corners)
            {
                _logHandle.Report(new LogEventError(PieceID + " Failed to find correct number of corners. " + corners.Size + " found."));
            }
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Get the piece corners by finding the biggest rectangle of the contour points
        /// </summary>
        private void find_corners_MaximumRectangle()
        {
            Bitmap bwImg = PieceImgBw.Bmp;

            double pieceFindCornersMaxAngleDiff = 10;
            double pieceFindCornersMaxCornerDistRatio = 1.5;

            _logHandle.Report(new LogEventInfo(PieceID + " Finding corners by finding the maximum rectangle within candidate points"));

            corners.Clear();

            // Find all dominant corner points using the GFTTDetector (this uses the Harris corner detector)
            GFTTDetector detector = new GFTTDetector(500, 0.01, 5, 2, true, 0.04);
            MKeyPoint[] keyPoints = detector.Detect(new Image<Gray, byte>(bwImg));
            List<Point> possibleCorners = keyPoints.Select(k => Point.Round(k.Point)).ToList();

            if (possibleCorners.Count > 0)
            {
                // Sort the dominant corners by the distance to upper left corner of the bounding rectangle (0, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperLeft = new List<Point>(possibleCorners);
                possibleCornersSortedUpperLeft.Sort(new DistanceToPointComparer(new Point(0, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperLeft = Utils.Distance(possibleCornersSortedUpperLeft[0], new PointF(0, 0));
                possibleCornersSortedUpperLeft = possibleCornersSortedUpperLeft.Where(c => Utils.Distance(c, new PointF(0, 0)) < minCornerDistUpperLeft * pieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to upper right corner of the bounding rectangle (ImageWidth, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperRight = new List<Point>(possibleCorners);
                possibleCornersSortedUpperRight.Sort(new DistanceToPointComparer(new Point(bwImg.Width, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperRight = Utils.Distance(possibleCornersSortedUpperRight[0], new PointF(bwImg.Width, 0));
                possibleCornersSortedUpperRight = possibleCornersSortedUpperRight.Where(c => Utils.Distance(c, new PointF(bwImg.Width, 0)) < minCornerDistUpperRight * pieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower right corner of the bounding rectangle (ImageWidth, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerRight = new List<Point>(possibleCorners);
                possibleCornersSortedLowerRight.Sort(new DistanceToPointComparer(new Point(bwImg.Width, bwImg.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerRight = Utils.Distance(possibleCornersSortedLowerRight[0], new PointF(bwImg.Width, bwImg.Height));
                possibleCornersSortedLowerRight = possibleCornersSortedLowerRight.Where(c => Utils.Distance(c, new PointF(bwImg.Width, bwImg.Height)) < minCornerDistLowerRight * pieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower left corner of the bounding rectangle (0, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerLeft = new List<Point>(possibleCorners);
                possibleCornersSortedLowerLeft.Sort(new DistanceToPointComparer(new Point(0, bwImg.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerLeft = Utils.Distance(possibleCornersSortedLowerLeft[0], new PointF(0, bwImg.Height));
                possibleCornersSortedLowerLeft = possibleCornersSortedLowerLeft.Where(c => Utils.Distance(c, new PointF(0, bwImg.Height)) < minCornerDistLowerLeft * pieceFindCornersMaxCornerDistRatio).ToList();

                bwImg.Dispose();

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
                                if (angleDiff > pieceFindCornersMaxAngleDiff) { continue; }

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

            if (corners.Size != 4) { _logHandle.Report(new LogEventError(PieceID + " Failed to find correct number of corners. " + corners.Size + " found.")); }

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {
                Bitmap colorImg = PieceImgColor.Bmp;
                using (Image<Rgb, byte> imgCorners = new Image<Rgb, byte>(colorImg))
                {
                    Features2DToolbox.DrawKeypoints(imgCorners, new VectorOfKeyPoint(keyPoints), imgCorners, new Bgr(0, 0, 255));       // Draw the dominant key points

                    for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(imgCorners, Point.Round(corners[i]), 4, new MCvScalar(0, Math.Max(255 - i * 50, 50), 0), 3); }
                    _logHandle.Report(new LogEventImage(PieceID + " Corners", imgCorners.Bitmap));
                    imgCorners.Dispose();
                }
                colorImg.Dispose();
            }
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
            Bitmap bwImg = PieceImgBw.Bmp;
            _logHandle.Report(new LogEventInfo(PieceID + " Extracting edges"));

            if (corners.Size != 4) { return; }

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(new Image<Gray, byte>(bwImg), contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            bwImg.Dispose();
            if (contours.Size == 0)
            {
                _logHandle.Report(new LogEventError(PieceID + " No contours found."));
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

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {
                Bitmap colorImg = PieceImgColor.Bmp;
                Image<Rgb, byte> edge_img = new Image<Rgb, byte>(colorImg);
                for (int i = 0; i < corners.Size; i++) { CvInvoke.Circle(edge_img, Point.Round(corners[i]), 2, new MCvScalar(255, 0, 0), 1); }
                _logHandle.Report(new LogEventImage(PieceID + " New corners", edge_img.Bitmap));
                colorImg.Dispose();
                edge_img.Dispose();
            }

            List<int> sections = find_all_in(contour, corners);

            //Make corners go in the correct order
            Point[] new_corners2 = new Point[4];
            int cornerIndexUpperLeft = -1;
            double cornerDistUpperLeft = double.MaxValue;
            for (int i = 0; i < 4; i++)
            {
                new_corners2[i] = contour[sections[i]];
                double cornerDist = Utils.DistanceToOrigin(contour[sections[i]]);
                if(cornerDist < cornerDistUpperLeft)
                {
                    cornerDistUpperLeft = cornerDist;
                    cornerIndexUpperLeft = i;
                }
            }
            new_corners2.Rotate(-cornerIndexUpperLeft);
            corners.Push(new_corners2);
            sections.Rotate(-cornerIndexUpperLeft);

            Edges[0] = new Edge(PieceID, 0, PieceImgColor, contour.GetSubsetOfVector(sections[0], sections[1]), _logHandle, _cancelToken);
            Edges[1] = new Edge(PieceID, 1, PieceImgColor, contour.GetSubsetOfVector(sections[1], sections[2]), _logHandle, _cancelToken);
            Edges[2] = new Edge(PieceID, 2, PieceImgColor, contour.GetSubsetOfVector(sections[2], sections[3]), _logHandle, _cancelToken);
            Edges[3] = new Edge(PieceID, 3, PieceImgColor, contour.GetSubsetOfVector(sections[3], sections[0]), _logHandle, _cancelToken);
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
                _logHandle.Report(new LogEventError(PieceID + " Found too many edges for this piece. " + count.ToString() + " found."));
            }

            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
            {
                Bitmap colorImg = PieceImgColor.Bmp;
                _logHandle.Report(new LogEventImage(PieceID + " Type " + PieceType.ToString(), colorImg));
                colorImg.Dispose();
            }
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

            Bitmap colorImg = PieceImgColor.Bmp, bwImg = PieceImgBw.Bmp;
            Image<Rgb, byte> cvImgColor = new Image<Rgb, byte>(colorImg);
            Image<Gray, byte> cvImgBw = new Image<Gray, byte>(bwImg);

            colorImg.Dispose();
            bwImg.Dispose();
            PieceImgColor.DeleteFile();
            PieceImgBw.DeleteFile();

            colorImg = cvImgColor.Rotate(times_to_rotate * 90, new Rgb(255, 255, 255), false).Bitmap;
            bwImg = cvImgBw.Rotate(times_to_rotate * 90, new Gray(0), false).Bitmap;
            
            PieceImgColor.Bmp = colorImg;
            PieceImgBw.Bmp = bwImg;

            colorImg.Dispose();
            bwImg.Dispose();
            cvImgColor.Dispose();
            cvImgBw.Dispose();
        }

    }
}
