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
    public enum PieceTypes { CORNER, BORDER, INNER }

    public class Piece_old
    {
        public static int NextPieceID { get; private set; }

        static Piece_old()
        {
            NextPieceID = 0;
        }

        //**********************************************************************************************************************************************************************************************

        public Image<Rgb, byte> SourceImg { get; set; }
        public Image<Gray, byte> Mask { get; set; }
        public string OriginImageName { get; set; }
        public string PieceID { get; set; }
        public PieceEdge_old[] Edges { get; set; } = new PieceEdge_old[4];
        public PieceTypes PieceType { get; set; }

        //**********************************************************************************************************************************************************************************************

        public Piece_old(Image<Rgb, byte> sourceImg, Image<Gray, byte> mask)
        {
            SourceImg = sourceImg;
            Mask = mask;
            PieceID = "Piece#" + NextPieceID.ToString();
            NextPieceID++;

            ProcessedImagesStorage.AddImage(PieceID + " source", SourceImg.ToBitmap());
            ProcessedImagesStorage.AddImage(PieceID + " mask", Mask.ToBitmap());
        }

        //**********************************************************************************************************************************************************************************************

        public void InitPieceEdges()
        {
            /*VectorOfMat contours = new VectorOfMat();
            CvInvoke.FindContours(Mask, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            CvInvoke.DrawContours(SourceImg, contours, -1, new MCvScalar(0, 0, 255));

            bool convex = CvInvoke.IsContourConvex(contours[0]);

            VectorOfPoint hullPoints = new VectorOfPoint();
            CvInvoke.ConvexHull(contours[0], hullPoints, false, true);
            CvInvoke.DrawContours(SourceImg, new VectorOfVectorOfPoint(hullPoints), -1, new MCvScalar(0, 255, 0));

            Mat hullIndices = new Mat();
            Mat convexityDefects = new Mat();
            CvInvoke.ConvexHull(contours[0], hullIndices, false, false);
            CvInvoke.ConvexityDefects(contours[0], hullIndices, convexityDefects);*/


            //CvInvoke.Circle(SourceImg, new Point(contours[0].GetData(convexityDefects.GetData(1 * 4 + 2)[0] * 2)[0], contours[0].GetData(convexityDefects.GetData(1 * 4 + 2)[0] * 2 + 1)[0]), 3, new MCvScalar(255, 0, 0));

            /*for (int i = 0; i < convexityDefects.Rows; ++i)
            {
                CvInvoke.Circle(SourceImg, new Point(contours[0].GetData(convexityDefects.GetData(i * 4 + 2)[0] * 2)[0], contours[0].GetData(convexityDefects.GetData(i * 4 + 2)[0] * 2 + 1)[0]), 3, new MCvScalar(255, 0, 0));

                let start = new cv.Point(cnt.data32S[defect.data32S[i * 4] * 2],
                                         cnt.data32S[defect.data32S[i * 4] * 2 + 1]);
                let end = new cv.Point(cnt.data32S[defect.data32S[i * 4 + 1] * 2],
                                       cnt.data32S[defect.data32S[i * 4 + 1] * 2 + 1]);
                let far = new cv.Point(cnt.data32S[defect.data32S[i * 4 + 2] * 2],
                                       cnt.data32S[defect.data32S[i * 4 + 2] * 2 + 1]);
                cv.line(dst, start, end, lineColor, 2, cv.LINE_AA, 0);
                cv.circle(dst, far, 3, circleColor, -1);
            }*/




            /*Image<Gray, byte> cannyImg = new Image<Gray, byte>(Mask.Size);
            CvInvoke.Canny(Mask, cannyImg, 50, 200);
            ProcessedImagesStorage.AddImage(PieceID + " Canny", cannyImg.ToBitmap());
            LineSegment2D[] lines = CvInvoke.HoughLinesP(cannyImg, 1, Math.PI / 180, 1, 7);
            Image<Rgb, byte> sourceWithLines = SourceImg.Copy();
            foreach (LineSegment2D line in lines)
            {
                CvInvoke.Line(sourceWithLines, line.P1, line.P2, new MCvScalar(0, 0, 255), 3);
            }
            ProcessedImagesStorage.AddImage(PieceID + " Lines", sourceWithLines.ToBitmap());*/



            Image<Rgb, byte> sourceWithEdgeTypes = SourceImg.Copy();

            int sliceNumberPerSide = 2;
            int sliceHeight = Mask.Height / sliceNumberPerSide;
            int sliceWidth = Mask.Width / sliceNumberPerSide;

            int numberBulbs = 0, numberHoles = 0, numberLines = 0;

            for(int edgeNo = 0; edgeNo < 4; edgeNo++)
            {
                Rectangle edgeRect = new Rectangle();
                switch (edgeNo)
                {
                    case 0: edgeRect = new Rectangle(0, 0, Mask.Width, sliceHeight); break;                             // Top edge
                    case 1: edgeRect = new Rectangle(Mask.Width - sliceWidth, 0, sliceWidth, Mask.Height); break;       // Right edge
                    case 2: edgeRect = new Rectangle(0, Mask.Height - sliceHeight, Mask.Width, sliceHeight); break;     // Bottom edge
                    case 3: edgeRect = new Rectangle(0, 0, sliceWidth, Mask.Height); break;                             // Left edge
                }
                Edges[edgeNo] = new PieceEdge_old(PieceID, Mask.Copy(edgeRect), (EdgeLocations)edgeNo);
                Edges[edgeNo].CalculateEdgeType();

                Size edgeIconSize = new Size(20, 20);
                PointF edgeRectCenter;
                if ((EdgeLocations)edgeNo == EdgeLocations.TOP || (EdgeLocations)edgeNo == EdgeLocations.BOTTOM)
                {
                    edgeRectCenter = new PointF(edgeRect.Left + edgeRect.Width * Edges[edgeNo].ExtremaLocationPercent, edgeRect.Top + ((EdgeLocations)edgeNo == EdgeLocations.TOP ? edgeIconSize.Height / 2 : edgeRect.Height - edgeIconSize.Height / 2));
                }
                else
                {
                    edgeRectCenter = new PointF(edgeRect.Left + ((EdgeLocations)edgeNo == EdgeLocations.LEFT ? edgeIconSize.Width / 2 : edgeRect.Width - edgeIconSize.Width / 2), edgeRect.Top + edgeRect.Height * Edges[edgeNo].ExtremaLocationPercent);
                }
                switch (Edges[edgeNo].EdgeType)
                {
                    case EdgeTypes.BULB: numberBulbs++; sourceWithEdgeTypes.Draw(new Cross2DF(edgeRectCenter, edgeIconSize.Width, edgeIconSize.Height), new Rgb(Color.Blue), 2); break;
                    case EdgeTypes.HOLE: numberHoles++; sourceWithEdgeTypes.Draw(new CircleF(edgeRectCenter, edgeIconSize.Width / 2), new Rgb(Color.Red), 2); break;
                    case EdgeTypes.LINE: numberLines++; sourceWithEdgeTypes.Draw(new Rectangle(Point.Subtract(Point.Round(edgeRectCenter), edgeIconSize), edgeIconSize), new Rgb(Color.Green), 2); break;
                }
            }

            // edgeNo = 0 --> clockwiseEdgeNo = 1; counterClockwiseEdgeNo = 3
            // edgeNo = 1 --> clockwiseEdgeNo = 2; counterClockwiseEdgeNo = 0
            // edgeNo = 2 --> clockwiseEdgeNo = 3; counterClockwiseEdgeNo = 1
            // edgeNo = 3 --> clockwiseEdgeNo = 0; counterClockwiseEdgeNo = 2
            for (int edgeNo = 0; edgeNo < 4; edgeNo++)      // Fill the edges PieceEdgeTypeNextClockwise and PieceEdgeTypeNextCounterClockwise properties
            {
                Edges[edgeNo].PieceEdgeTypeNextClockwise = Edges[(edgeNo + 1) % 4].EdgeType;
                Edges[edgeNo].PieceEdgeTypeNextCounterClockwise = Edges[(edgeNo + 3) % 4].EdgeType;
            }

            ProcessedImagesStorage.AddImage(PieceID + " Edge types", sourceWithEdgeTypes.ToBitmap());

            switch(numberLines)
            {
                case 0: PieceType = PieceTypes.INNER; break;
                case 1: PieceType = PieceTypes.BORDER; break;
                case 2: PieceType = PieceTypes.CORNER; break;
                case 3:
                case 4:
                    System.Windows.MessageBox.Show(numberLines.ToString() + " Line edges were found for " + PieceID + ".\nA maximum of 2 Line edges are allowed.", "Too much Line edges found.", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning); break;
            }
        }

    }
}
