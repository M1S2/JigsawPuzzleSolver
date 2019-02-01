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
using System.Runtime.Serialization;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// The paradigm for edges is that if you walked along the edge of the contour from beginning to end, the piece will be to the left, and empty space to right.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/edge.cpp
    [DataContract]
    public class Edge : SaveableObject<Edge>, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
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

        private VectorOfPoint contour;                      //The original contour passed into the function.
        private VectorOfPoint normalized_contour;          //Normalized contour produces a contour that has its begining at (0,0) and its endpoint straight above it (0,y). This is used internally to classify the piece.
        private VectorOfPoint reverse_normalized_contour;

        /// <summary>
        /// Type of the Edge (LINE, BULB, HOLE)
        /// </summary>
        [DataMember]
        public EdgeTypes EdgeType { get; private set; }

        [DataMember]
        public PuzzleSolverParameters SolverParameters { get; private set; }

        [DataMember]
        public string PieceID { get; private set; }
        [DataMember]
        public int EdgeNumber { get; private set; }

        public Image<Rgb, byte> Full_color { get; private set; }
        public Image<Rgb, byte> ContourImg { get; private set; }

        private IProgress<LogBox.LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        //##############################################################################################################################################################################################

        public Edge(string pieceID, int edgeNumber, Image<Rgb, byte> full_color, VectorOfPoint edgeContour, PuzzleSolverParameters solverParameters, IProgress<LogBox.LogEvent> logHandle, CancellationToken cancelToken)
        {
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            SolverParameters = solverParameters;
            PieceID = pieceID;
            EdgeNumber = edgeNumber;
            contour = edgeContour;
            Full_color = full_color;

            normalized_contour = normalize(contour);    //Normalized contours are used for comparisons

            VectorOfPoint contourCopy = new VectorOfPoint(contour.ToArray().Reverse().ToArray());
            reverse_normalized_contour = normalize(contourCopy);   //same as normalized contour, but flipped 180 degrees

            classify();
        }

        public Edge()
        { }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Calculate the edge type (LINE, BULB, HOLE)
        /// </summary>
        private void classify()
        {
            EdgeType = EdgeTypes.UNKNOWN;
            if(normalized_contour.Size <= 1) { return; }

            ContourImg = Full_color.Clone();

            //See if it is an outer edge comparing the distance between beginning and end with the arc length.
            double contour_length = CvInvoke.ArcLength(normalized_contour, false);
            
            double begin_end_distance = Utils.Distance(normalized_contour.ToArray().First(), normalized_contour.ToArray().Last());
            if (contour_length < begin_end_distance * 1.3)
            {
                EdgeType = EdgeTypes.LINE;

                for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(ContourImg, Point.Round(contour[i]), 2, new MCvScalar(255, 0, 0), 1); }
                if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Edge " + EdgeNumber.ToString() + " " + EdgeType.ToString(), ContourImg.Bitmap)); }
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
            
            for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(ContourImg, Point.Round(contour[i]), 2, new MCvScalar(255, 0, 0), 1); }
            if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage(PieceID + " Edge " + EdgeNumber.ToString() + " " + EdgeType.ToString(), ContourImg.Bitmap)); }
        }

        //**********************************************************************************************************************************************************************************************
        
        /// <summary>
        /// This function takes in a vector of points, and transforms it so that it starts at the origin, and ends on the y-axis
        /// </summary>
        /// <param name="contour">Contour to normalize</param>
        /// <returns>normalized contour</returns>
        private VectorOfPoint normalize(VectorOfPoint contour)
        {
            if(contour.Size == 0) { return contour; }

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
                ret_contour.Push(new Point((int)new_x, (int)new_y));
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

        //##############################################################################################################################################################################################

        /// <summary>
        /// This comparison iterates over every point in "this" contour, finds the closest point in "edge2" contour and sums those distances up.
        /// The end result is the sum divided by length of the 2 contours.
        /// It also takes the difference of the distances of the contour endpoints into account.
        /// </summary>
        /// <param name="edge2">Edge to compare to this edge</param>
        /// <returns>Similarity factor of edges. Special values are:
        /// 300000000: Same piece
        /// 200000000: At least one edge is a line edge
        /// 150000000: The pieces have the same edge type
        /// 100000000: One of the contour sizes is 0</returns>
        public double Compare(Edge edge2)
        {
            try
            {
                //Return large numbers if we know that these shapes simply wont match...
                if (PieceID == edge2.PieceID) { return 300000000; }
                if (EdgeType == EdgeTypes.LINE || edge2.EdgeType == EdgeTypes.LINE) { return 200000000; }
                if (EdgeType == edge2.EdgeType) { return 150000000; }
                if (normalized_contour.Size == 0 || edge2.reverse_normalized_contour.Size == 0) { return 100000000; }
                double cost = 0;
                double total_length = CvInvoke.ArcLength(normalized_contour, false) + CvInvoke.ArcLength(edge2.reverse_normalized_contour, false);

                int windowSizePoints = (int)(Math.Max(normalized_contour.Size, edge2.reverse_normalized_contour.Size) * SolverParameters.EdgeCompareWindowSizePercent);

                double distEndpointsContour1 = Utils.Distance(normalized_contour[0], normalized_contour[normalized_contour.Size - 1]);
                double distEndpointsContour2 = Utils.Distance(edge2.reverse_normalized_contour[0], edge2.reverse_normalized_contour[edge2.reverse_normalized_contour.Size - 1]);
                double distEndpointContoursDiff = Math.Abs(distEndpointsContour1 - distEndpointsContour2);
                if (distEndpointContoursDiff <= SolverParameters.EdgeCompareEndpointDiffIgnoreThreshold) { distEndpointContoursDiff = 0; }

                for (int i = 0; i < Math.Min(normalized_contour.Size, edge2.reverse_normalized_contour.Size); i++)
                {
                    double min = 10000000;
                    for (int j = Math.Max(0, i - windowSizePoints); j < Math.Min(edge2.reverse_normalized_contour.Size, i + windowSizePoints); j++)
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        double dist = Utils.Distance(normalized_contour[i], edge2.reverse_normalized_contour[j]);
                        if (dist < min) min = dist;
                    }
                    cost += min;
                }
                double matchResult = cost / total_length;

                if (SolverParameters.SolverShowDebugResults)
                {
                    Image<Rgb, byte> contourOverlay = new Image<Rgb, byte>(500, 500);
                    VectorOfPoint contour1 = GetTranslatedContour(100, 0);
                    VectorOfPoint contour2 = edge2.GetTranslatedContourReverse(100, 0);
                    CvInvoke.DrawContours(contourOverlay, new VectorOfVectorOfPoint(contour1), -1, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.DrawContours(contourOverlay, new VectorOfVectorOfPoint(contour2), -1, new MCvScalar(0, 0, 255), 2);

                    _logHandle.Report(new LogBox.LogEventImage("Compare " + PieceID + "_Edge" + EdgeNumber + " <-->" + edge2.PieceID + "_Edge" + edge2.EdgeNumber + " ==> distEndpoint = " + distEndpointContoursDiff.ToString() + ", MatchResult = " + matchResult, contourOverlay.Bitmap));
                }

                return distEndpointContoursDiff + matchResult;
            }
            catch (OperationCanceledException ex)
            {
                throw ex;
            }
        }

    }
}
