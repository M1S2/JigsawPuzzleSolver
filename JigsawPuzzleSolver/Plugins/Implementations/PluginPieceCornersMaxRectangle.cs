using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using LogBox.LogEvents;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("PluginPieceCorners MaxRectangle")]
    [PluginDescription("Plugin for finding piece corners using maximum rectangle algorithmus")]
    public class PluginPieceCornersMaxRectangle : PluginGroupFindPieceCorners
    {
        private double _pieceFindCornersMaxAngleDiff;
        [PluginSettingNumber(1, 0, 360)]
        [PluginSettingDescription("Maximum allowed angle difference from perfect rectangle. If sum of angle deviation of all 4 corners from 90 degree is greaten than this parameter. The rectangle is dismissed.")]
        public double PieceFindCornersMaxAngleDiff
        {
            get { return _pieceFindCornersMaxAngleDiff; }
            set { _pieceFindCornersMaxAngleDiff = value; OnPropertyChanged(); }
        }

        private double _pieceFindCornersMaxCornerDistRatio;
        [PluginSettingNumber(1, 1, 1000)]
        [PluginSettingDescription("Dismiss all corners that are too far away from the nearest corner.")]
        public double PieceFindCornersMaxCornerDistRatio
        {
            get { return _pieceFindCornersMaxCornerDistRatio; }
            set { _pieceFindCornersMaxCornerDistRatio = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            PieceFindCornersMaxAngleDiff = 10;
            PieceFindCornersMaxCornerDistRatio = 1.5;
        }

        /// <summary>
        /// Get the piece corners by finding the biggest rectangle of the contour points
        /// </summary>
        /// <param name="pieceID">ID of the piece</param>
        /// <param name="pieceImgBw">Black white image of piece</param>
        /// <param name="pieceImgColor">Color image of piece</param>
        /// <returns>List with corner points</returns>
        public override List<Point> FindCorners(string pieceID, Bitmap pieceImgBw, Bitmap pieceImgColor)
        {
            PluginFactory.LogHandle.Report(new LogEventInfo(pieceID + " Finding corners by finding the maximum rectangle within candidate points"));

            List<Point> corners = new List<Point>();

            // Find all dominant corner points using the GFTTDetector (this uses the Harris corner detector)
            GFTTDetector detector = new GFTTDetector(500, 0.01, 5, 2, true, 0.04);
            MKeyPoint[] keyPoints = detector.Detect(new Image<Gray, byte>(pieceImgBw));
            List<Point> possibleCorners = keyPoints.Select(k => Point.Round(k.Point)).ToList();

            if (possibleCorners.Count > 0)
            {
                // Sort the dominant corners by the distance to upper left corner of the bounding rectangle (0, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperLeft = new List<Point>(possibleCorners);
                possibleCornersSortedUpperLeft.Sort(new DistanceToPointComparer(new Point(0, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperLeft = Utils.Distance(possibleCornersSortedUpperLeft[0], new PointF(0, 0));
                possibleCornersSortedUpperLeft = possibleCornersSortedUpperLeft.Where(c => Utils.Distance(c, new PointF(0, 0)) < minCornerDistUpperLeft * PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to upper right corner of the bounding rectangle (ImageWidth, 0) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedUpperRight = new List<Point>(possibleCorners);
                possibleCornersSortedUpperRight.Sort(new DistanceToPointComparer(new Point(pieceImgBw.Width, 0), DistanceOrders.NEAREST_FIRST));
                double minCornerDistUpperRight = Utils.Distance(possibleCornersSortedUpperRight[0], new PointF(pieceImgBw.Width, 0));
                possibleCornersSortedUpperRight = possibleCornersSortedUpperRight.Where(c => Utils.Distance(c, new PointF(pieceImgBw.Width, 0)) < minCornerDistUpperRight * PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower right corner of the bounding rectangle (ImageWidth, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerRight = new List<Point>(possibleCorners);
                possibleCornersSortedLowerRight.Sort(new DistanceToPointComparer(new Point(pieceImgBw.Width, pieceImgBw.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerRight = Utils.Distance(possibleCornersSortedLowerRight[0], new PointF(pieceImgBw.Width, pieceImgBw.Height));
                possibleCornersSortedLowerRight = possibleCornersSortedLowerRight.Where(c => Utils.Distance(c, new PointF(pieceImgBw.Width, pieceImgBw.Height)) < minCornerDistLowerRight * PieceFindCornersMaxCornerDistRatio).ToList();

                // Sort the dominant corners by the distance to lower left corner of the bounding rectangle (0, ImageHeight) and keep only the corners that are near enough to this point
                List<Point> possibleCornersSortedLowerLeft = new List<Point>(possibleCorners);
                possibleCornersSortedLowerLeft.Sort(new DistanceToPointComparer(new Point(0, pieceImgBw.Height), DistanceOrders.NEAREST_FIRST));
                double minCornerDistLowerLeft = Utils.Distance(possibleCornersSortedLowerLeft[0], new PointF(0, pieceImgBw.Height));
                possibleCornersSortedLowerLeft = possibleCornersSortedLowerLeft.Where(c => Utils.Distance(c, new PointF(0, pieceImgBw.Height)) < minCornerDistLowerLeft * PieceFindCornersMaxCornerDistRatio).ToList();

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
                                if (PluginFactory.CancelToken.IsCancellationRequested) { PluginFactory.CancelToken.ThrowIfCancellationRequested(); }

                                // Possible corner combination
                                Point[] tmpCorners = new Point[]
                                {
                                    possibleCornersSortedUpperLeft[indexUpperLeft],         // the corners are ordered beginning in the upper left corner and going counter clock wise
                                    possibleCornersSortedLowerLeft[indexLowerLeft],
                                    possibleCornersSortedLowerRight[indexLowerRight],
                                    possibleCornersSortedUpperRight[indexUpperRight]
                                };
                                double angleDiff = RectangleDifferenceAngle(tmpCorners);
                                if (angleDiff > PieceFindCornersMaxAngleDiff) { continue; }

                                double area = CvInvoke.ContourArea(new VectorOfPoint(tmpCorners));
                                FindCornerRectangleScore score = new FindCornerRectangleScore() { AngleDiff = angleDiff, RectangleArea = area, PossibleCorners = tmpCorners };
                                scores.Add(score);
                            }
                        }
                    }
                }

                // Order the scores by rectangle area (biggest first) and take the PossibleCorners of the biggest rectangle as corners
                scores = scores.OrderByDescending(s => s.RectangleArea).ToList();
                if (scores.Count > 0) { corners.AddRange(scores[0].PossibleCorners); }
            }

            if (corners.Count != 4) { PluginFactory.LogHandle.Report(new LogEventError(pieceID + " Failed to find correct number of corners. " + corners.Count + " found.")); }

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                using (Image<Rgb, byte> imgCorners = new Image<Rgb, byte>(pieceImgColor))
                {
                    Features2DToolbox.DrawKeypoints(imgCorners, new VectorOfKeyPoint(keyPoints), imgCorners, new Bgr(0, 0, 255));       // Draw the dominant key points

                    for (int i = 0; i < corners.Count; i++) { CvInvoke.Circle(imgCorners, Point.Round(corners[i]), 4, new MCvScalar(0, Math.Max(255 - i * 50, 50), 0), 3); }
                    PluginFactory.LogHandle.Report(new LogEventImage(pieceID + " Corners", imgCorners.Bitmap));
                    imgCorners.Dispose();
                }
            }
            return corners;
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
    }
}
