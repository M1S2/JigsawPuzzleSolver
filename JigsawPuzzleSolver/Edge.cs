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
using LogBox.LogEvents;
using ImageGallery.LocalDriveBitmaps;
using JigsawPuzzleSolver.Plugins;

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

        private VectorOfPoint contour;                                  //The original contour passed into the function.
        public VectorOfPoint NormalizedContour { get; set; }            //Normalized contour produces a contour that has its begining at (0,0) and its endpoint straight above it (0,y). This is used internally to classify the piece.
        public VectorOfPoint ReverseNormalizedContour { get; set; }

        /// <summary>
        /// Type of the Edge (LINE, BULB, HOLE)
        /// </summary>
        [DataMember]
        public EdgeTypes EdgeType { get; private set; }

        [DataMember]
        public string PieceID { get; private set; }
        [DataMember]
        public int EdgeNumber { get; private set; }

        public LocalDriveBitmap PieceImgColor { get; private set; }
        public LocalDriveBitmap ContourImg { get; private set; }

        private IProgress<LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        //##############################################################################################################################################################################################

        public Edge(string pieceID, int edgeNumber, LocalDriveBitmap pieceImgColor, VectorOfPoint edgeContour, IProgress<LogEvent> logHandle, CancellationToken cancelToken)
        {
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            PieceID = pieceID;
            EdgeNumber = edgeNumber;
            contour = edgeContour;
            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                PieceImgColor = pieceImgColor;
                ContourImg = new LocalDriveBitmap(System.IO.Path.GetDirectoryName(PieceImgColor.LocalFilePath) + @"\Edges\" + PieceID + "_Edge#" + edgeNumber.ToString() + ".png", null);
            }

            NormalizedContour = normalize(contour);    //Normalized contours are used for comparisons

            VectorOfPoint contourCopy = new VectorOfPoint(contour.ToArray().Reverse().ToArray());
            ReverseNormalizedContour = normalize(contourCopy);   //same as normalized contour, but flipped 180 degrees
            contourCopy.Dispose();

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
            try
            {
                EdgeType = EdgeTypes.UNKNOWN;
                if (NormalizedContour.Size <= 1) { return; }

                //See if it is an outer edge comparing the distance between beginning and end with the arc length.
                double contour_length = CvInvoke.ArcLength(NormalizedContour, false);

                double begin_end_distance = Utils.Distance(NormalizedContour.ToArray().First(), NormalizedContour.ToArray().Last());
                if (contour_length < begin_end_distance * 1.3)
                {
                    EdgeType = EdgeTypes.LINE;
                }

                if (EdgeType == EdgeTypes.UNKNOWN)
                {
                    //Find the minimum or maximum value for x in the normalized contour and base the classification on that
                    int minx = 100000000;
                    int maxx = -100000000;
                    for (int i = 0; i < NormalizedContour.Size; i++)
                    {
                        if (minx > NormalizedContour[i].X) { minx = (int)NormalizedContour[i].X; }
                        if (maxx < NormalizedContour[i].X) { maxx = (int)NormalizedContour[i].X; }
                    }

                    if (Math.Abs(minx) > Math.Abs(maxx))
                    {
                        EdgeType = EdgeTypes.BULB;
                    }
                    else
                    {
                        EdgeType = EdgeTypes.HOLE;
                    }
                }

                if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                {
                    Bitmap contourImg = PieceImgColor.Bmp;
                    for (int i = 0; i < contour.Size; i++)
                    {
                        Graphics g = Graphics.FromImage(contourImg);
                        g.DrawEllipse(new Pen(Color.Red), new RectangleF(PointF.Subtract(contour[i], new Size(1, 1)), new SizeF(2, 2)));
                    }
                    _logHandle.Report(new LogEventImage(PieceID + " Edge " + EdgeNumber.ToString() + " " + EdgeType.ToString(), contourImg));
                    ContourImg.Bmp = contourImg;
                    contourImg.Dispose();
                }
            }
            catch(Exception ex)
            {
                _logHandle.Report(new LogBox.LogEvents.LogEventError(ex.Message));
            }
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

    }
}
