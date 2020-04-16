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
using JigsawPuzzleSolver.Plugins.Core;
using LogBox.LogEvents;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupCompareEdges
{
    [PluginName("PluginCompareEdges Contour Diff")]
    [PluginDescription("Plugin comparing edges using contour difference")]
    public class PluginCompareEdgesContourDiff : PluginGroupCompareEdges
    {
        public override PackIconBase PluginIcon => new PackIconFontAwesome() { Kind = PackIconFontAwesomeKind.EqualsSolid };

        //##############################################################################################################################################################################################

        private double _edgeCompareWindowSizePercent;
        [PluginSettingNumber(0.001, 0, 10)]
        [PluginSettingDescription("Not all points are taken into account to speed up the calculation. Therefore a window is defined by the percentage of total points in the longer contour.")]
        public double EdgeCompareWindowSizePercent
        {
            get { return _edgeCompareWindowSizePercent; }
            set { _edgeCompareWindowSizePercent = value; OnPropertyChanged(); }
        }

        private double _edgeCompareEndpointDiffIgnoreThreshold;
        [PluginSettingNumber(1, 0, 100)]
        [PluginSettingDescription("Edges whose endpoints are close enough to each other are regarded as optimal.")]
        public double EdgeCompareEndpointDiffIgnoreThreshold
        {
            get { return _edgeCompareEndpointDiffIgnoreThreshold; }
            set { _edgeCompareEndpointDiffIgnoreThreshold = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            EdgeCompareWindowSizePercent = 0.003;
            EdgeCompareEndpointDiffIgnoreThreshold = 15;
        }

        /// <summary>
        /// This comparison iterates over every point in "edge1" contour, finds the closest point in "edge2" contour and sums up those distances.
        /// The end result is the sum divided by length of the both contours.
        /// It also takes the difference of the distances of the contour endpoints into account.
        /// </summary>
        /// <param name="edge1">First Edge to compare to edge2</param>
        /// <param name="edge2">Second Edge to compare to edge1</param>
        /// <returns>Similarity factor of edges. Special values are:
        /// 300000000: Same piece
        /// 200000000: At least one edge is a line edge
        /// 150000000: The pieces have the same edge type
        /// 100000000: One of the contour sizes is 0</returns>
        public override double CompareEdges(Edge edge1, Edge edge2)
        {
            try
            {
                //Return large numbers if we know that these shapes simply wont match...
                if (edge1.PieceID == edge2.PieceID) { return 300000000; }
                if (edge1.EdgeType == EdgeTypes.LINE || edge2.EdgeType == EdgeTypes.LINE) { return 200000000; }
                if (edge1.EdgeType == edge2.EdgeType) { return 150000000; }
                if (edge1.NormalizedContour.Size == 0 || edge2.ReverseNormalizedContour.Size == 0) { return 100000000; }
                double cost = 0;
                double total_length = CvInvoke.ArcLength(edge1.NormalizedContour, false) + CvInvoke.ArcLength(edge2.ReverseNormalizedContour, false);

                int windowSizePoints = (int)(Math.Max(edge1.NormalizedContour.Size, edge2.ReverseNormalizedContour.Size) * EdgeCompareWindowSizePercent);
                if (windowSizePoints < 1) { windowSizePoints = 1; }

                double distEndpointsContour1 = Utils.Distance(edge1.NormalizedContour[0], edge1.NormalizedContour[edge1.NormalizedContour.Size - 1]);
                double distEndpointsContour2 = Utils.Distance(edge2.ReverseNormalizedContour[0], edge2.ReverseNormalizedContour[edge2.ReverseNormalizedContour.Size - 1]);
                double distEndpointContoursDiff = Math.Abs(distEndpointsContour1 - distEndpointsContour2);
                if (distEndpointContoursDiff <= EdgeCompareEndpointDiffIgnoreThreshold) { distEndpointContoursDiff = 0; }

                for (int i = 0; i < Math.Min(edge1.NormalizedContour.Size, edge2.ReverseNormalizedContour.Size); i++)
                {
                    double min = 10000000;
                    for (int j = Math.Max(0, i - windowSizePoints); j < Math.Min(edge2.ReverseNormalizedContour.Size, i + windowSizePoints); j++)
                    {
                        if (PluginFactory.CancelToken.IsCancellationRequested) { PluginFactory.CancelToken.ThrowIfCancellationRequested(); }

                        double dist = Utils.Distance(edge1.NormalizedContour[i], edge2.ReverseNormalizedContour[j]);
                        if (dist < min) min = dist;
                    }
                    cost += min;
                }
                double matchResult = cost / total_length;

                if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                {
                    Image<Rgb, byte> contourOverlay = new Image<Rgb, byte>(500, 500);
                    VectorOfPoint contour1 = Utils.TranslateContour(edge1.NormalizedContour, 100, 0);
                    VectorOfPoint contour2 = Utils.TranslateContour(edge2.ReverseNormalizedContour, 100, 0);
                    CvInvoke.DrawContours(contourOverlay, new VectorOfVectorOfPoint(contour1), -1, new MCvScalar(0, 255, 0), 2);
                    CvInvoke.DrawContours(contourOverlay, new VectorOfVectorOfPoint(contour2), -1, new MCvScalar(0, 0, 255), 2);

                    PluginFactory.LogHandle.Report(new LogEventImage("Compare " + edge1.PieceID + "_Edge" + edge1.EdgeNumber + " <-->" + edge2.PieceID + "_Edge" + edge2.EdgeNumber + " ==> distEndpoint = " + distEndpointContoursDiff.ToString() + ", MatchResult = " + matchResult, contourOverlay.Bitmap));
                }

                return distEndpointContoursDiff + matchResult;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
        }
    }
}
