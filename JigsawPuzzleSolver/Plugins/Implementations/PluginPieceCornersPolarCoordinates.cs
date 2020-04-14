using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using LogBox.LogEvents;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("PluginPieceCorners PolarCoordinates")]
    [PluginDescription("Plugin for finding piece corners using polar coordinates algorithm")]
    public class PluginPieceCornersPolarCoordinates : PluginGroupFindPieceCorners
    {
        public override PackIconBase PluginIcon => new PackIconMaterial() { Kind = PackIconMaterialKind.ChartDonut };

        //##############################################################################################################################################################################################

        private double _pieceFindCornersPeakDismissPercentage;
        [PluginSettingNumber(1, 0, 100)]
        [PluginSettingDescription("This setting is used to find the corners of the pieces using polar coordinates. The peaks under this threshold are dismissed.")]
        public double PieceFindCornersPeakDismissPercentage
        {
            get { return _pieceFindCornersPeakDismissPercentage; }
            set { _pieceFindCornersPeakDismissPercentage = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            PieceFindCornersPeakDismissPercentage = 0.1;
        }

        /// <summary>
        /// Get the piece corners by finding peaks in the polar representation of the contour points
        /// </summary>
        /// <param name="pieceID">ID of the piece</param>
        /// <param name="pieceImgBw">Black white image of piece</param>
        /// <param name="pieceImgColor">Color image of piece</param>
        /// <returns>List with corner points</returns>
        /// see: http://www.martijn-onderwater.nl/2016/10/13/puzzlemaker-extracting-the-four-sides-of-a-jigsaw-piece-from-the-boundary/
        /// see: https://web.stanford.edu/class/cs231a/prev_projects_2016/computer-vision-solve__1_.pdf
        public override List<Point> FindCorners(string pieceID, Bitmap pieceImgBw, Bitmap pieceImgColor)
        {
            List<Point> corners = new List<Point>();

            Size pieceMiddle = new Size(pieceImgBw.Width / 2, pieceImgBw.Height / 2);

            VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(new Image<Gray, byte>(pieceImgBw), contours, null, RetrType.List, ChainApproxMethod.ChainApproxSimple);
            if (contours.Size == 0) { return corners; }
            VectorOfPoint contour = contours[0];

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                using (Image<Rgb, byte> polarContourImg = new Image<Rgb, byte>(pieceImgColor))
                {
                    for (int i = 0; i < contour.Size; i++) { CvInvoke.Circle(polarContourImg, Point.Round(contour[i]), 2, new MCvScalar(255 * ((double)i / contour.Size), 0, 255 - 255 * ((double)i / contour.Size)), 1); }
                    PluginFactory.LogHandle.Report(new LogEventImage(pieceID + " Polar Contour", polarContourImg.Bitmap));
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
            List<PolarCoordinate> cornerCandidatesPolar = polarContour.Where(p => peakPosOut != null && peakPosOut[polarContour.IndexOf(p)] == 1 && p.Radius > contourRadius.Min() + PieceFindCornersPeakDismissPercentage * contourRadiusRange).ToList();
            cornerCandidatesPolar = cornerCandidatesPolar.OrderBy(p => p.Angle).ToList();

            List<PolarCoordinate> cornersPolar = new List<PolarCoordinate>();

            if (cornerCandidatesPolar.Count < 4)
            {
                PluginFactory.LogHandle.Report(new LogEventWarning(pieceID + " not enough corners found (" + cornerCandidatesPolar.Count.ToString() + ")"));
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
                
                for (int a = 270; a >= 0; a -= 90)
                {
                    PolarCoordinate polarCorner = cornerCandidatesPolar.OrderBy(p => Utils.AngleDiff(p.Angle, Utils.GetPositiveAngle(minAngle + a), true)).First();     // Get the corner candiate that has the minimum distance to the current ideal square point position
                    corners.Add(Point.Add(Point.Round(PolarCoordinate.PolarToCartesian(polarCorner)), pieceMiddle));
                    cornersPolar.Add(polarCorner);
                }
            }

            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
            {
                int maxRadius = (int)polarContour.Select(p => p.Radius).Max();
                using (Mat polarImg = new Mat(maxRadius, 360, DepthType.Cv8U, 3))
                {
                    for (int i = 0; i < polarContour.Count - 1; i++) { CvInvoke.Line(polarImg, new Point((int)polarContour[i].Angle, maxRadius - (int)polarContour[i].Radius), new Point((int)polarContour[i + 1].Angle, maxRadius - (int)polarContour[i + 1].Radius), new MCvScalar(255, 0, 0), 1, LineType.EightConnected); }
                    for (int i = 0; i < cornerCandidatesPolar.Count; i++) { CvInvoke.Circle(polarImg, new Point((int)cornerCandidatesPolar[i].Angle, maxRadius - (int)cornerCandidatesPolar[i].Radius), 3, new MCvScalar(0, 0, 255), -1); }
                    for (int i = 0; i < cornersPolar.Count; i++) { CvInvoke.Circle(polarImg, new Point((int)cornersPolar[i].Angle, maxRadius - (int)cornersPolar[i].Radius), 2, new MCvScalar(0, 255, 0), -1); }
                    PluginFactory.LogHandle.Report(new LogEventImage(pieceID + " Polar", polarImg.ToImage<Rgb, byte>().Bitmap));
                    polarImg.Dispose();

                    Image<Rgb, byte> corner_img = new Image<Rgb, byte>(pieceImgColor);
                    for (int i = 0; i < corners.Count; i++) { CvInvoke.Circle(corner_img, Point.Round(corners[i]), 7, new MCvScalar(255, 0, 0), -1); }
                    PluginFactory.LogHandle.Report(new LogEventImage(pieceID + " Found Corners (" + corners.Count.ToString() + ")", corner_img.Bitmap));
                    corner_img.Dispose();
                }
            }
            return corners;
        }
    }
}
