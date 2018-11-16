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
    public enum EdgeTypes { LINE, BULB, HOLE }
    public enum EdgeLocations { TOP, RIGHT, BOTTOM, LEFT }

    public class PieceEdge
    {
        /// <summary>
        /// ID of the Piece that this PieceEdge belongs to
        /// </summary>
        public string PieceID { get; set; }

        /// <summary>
        /// Mask image of the edge
        /// </summary>
        public Image<Gray, byte> EdgeMask { get; set; }

        /// <summary>
        /// Mask image of the edge extrema
        /// </summary>
        //public Image<Gray, byte> EdgeExtremaMask { get; set; }

        /// <summary>
        /// Type of the Edge (Bulb, Hole, Line)
        /// </summary>
        public EdgeTypes EdgeType { get; set; }

        /// <summary>
        /// The type of the next piece edge in clockwise direction (Bulb, Hole, Line)
        /// </summary>
        public EdgeTypes PieceEdgeTypeNextClockwise { get; set; }
        
        /// <summary>
        /// The type of the next piece edge in counter clockwise direction (Bulb, Hole, Line)
        /// </summary>
        public EdgeTypes PieceEdgeTypeNextCounterClockwise { get; set; }

        /// <summary>
        /// Location of the edge in the Piece (Top, Right, Bottom, Left)
        /// </summary>
        public EdgeLocations EdgeLocation { get; set; }

        /// <summary>
        /// The location of the bulb or hole in percent of the hole piece width/height (depending on the EdgeLocation). If the EdgeType is Line this property is 0.5.
        /// </summary>
        public float ExtremaLocationPercent { get; set; }

        private int extremaSliceIndex;

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Constructor of the PieceEdge
        /// </summary>
        /// <param name="pieceID">ID of the Piece that this PieceEdge belongs to</param>
        /// <param name="edgeMask">Mask image of the edge</param>
        /// <param name="edgeLocation">Location of the edge in the Piece (Top, Right, Bottom, Left)</param>
        public PieceEdge(string pieceID, Image<Gray, byte> edgeMask, EdgeLocations edgeLocation)
        {
            PieceID = pieceID;
            EdgeMask = edgeMask;
            EdgeLocation = edgeLocation;
            ProcessedImagesStorage.AddImage(PieceID + " " + EdgeLocation.ToString(), EdgeMask.ToBitmap());
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Determine the edge type of the PieceEdge (Bulb, Hole or Line)
        /// This function only uses the outer half of the edge mask image. Otherwise there may be some parts of holes/bulbs of other edges in the image.
        /// </summary>
        public void CalculateEdgeType()
        {
            int sliceNumber = 25;
            float[] sliceWeights = new float[sliceNumber];
            Rectangle sliceRect = new Rectangle();
            float sliceHeight = 0, sliceWidth = 0;
            extremaSliceIndex = -1;

            for (int sliceIndex = 0; sliceIndex < sliceNumber; sliceIndex++)
            {
                if (EdgeLocation == EdgeLocations.TOP || EdgeLocation == EdgeLocations.BOTTOM)
                {
                    sliceWidth = EdgeMask.Width / (float)sliceNumber;
                    sliceHeight = EdgeMask.Height / 2;
                    sliceRect = new Rectangle((int)(sliceIndex * sliceWidth), (EdgeLocation == EdgeLocations.TOP ? 0 : (int)sliceHeight), (int)sliceWidth, (int)sliceHeight);
                }
                else
                {
                    sliceWidth = EdgeMask.Width / 2;
                    sliceHeight = EdgeMask.Height / (float)sliceNumber;
                    sliceRect = new Rectangle((EdgeLocation == EdgeLocations.LEFT ? 0 : (int)sliceWidth), (int)(sliceIndex * sliceHeight), (int)sliceWidth, (int)sliceHeight);
                }

                Image<Gray, byte> sliceImg = EdgeMask.Copy(sliceRect);
                float sliceArea = sliceHeight * sliceWidth;
                sliceWeights[sliceIndex] = CvInvoke.CountNonZero(sliceImg) / sliceArea;

                //ProcessedImagesStorage.AddImage(PieceID + " " + EdgeLocation.ToString() + " Slice #" + sliceIndex.ToString(), sliceImg.ToBitmap());
            }

#warning ExtremaLocationPercent isn't completely correct, because it's not the position of the extrema on the edge but on the complete image (if there's a bulb at the top, the image height is greater than the length of the right edge)

            bool isHole = IsHoleEdge(sliceWeights);
            if (isHole)
            {
                EdgeType = EdgeTypes.HOLE;
                ExtremaLocationPercent = extremaSliceIndex / (float)sliceNumber;
            }
            else
            {
                bool isBulb = IsBulbEdge(sliceWeights);
                if (isBulb)
                {
                    EdgeType = EdgeTypes.BULB;
                    ExtremaLocationPercent = extremaSliceIndex / (float)sliceNumber;
                }
                else
                {
                    EdgeType = EdgeTypes.LINE;
                    ExtremaLocationPercent = 0.5f;
                }
            }

            //CalculateExtremaMask();
        }

        //**********************************************************************************************************************************************************************************************

        /*private void CalculateExtremaMask()
        { 
            Rectangle extremaRect = new Rectangle();
            float extremaHeight = 0, extremaWidth = 0;

            if (EdgeLocation == EdgeLocations.TOP || EdgeLocation == EdgeLocations.BOTTOM)
            {
                extremaWidth = EdgeMask.Width / 2f;
                extremaHeight = EdgeMask.Height;
                if(EdgeType == EdgeTypes.BULB) { extremaHeight /= 2; }
                extremaRect = new Rectangle((int)(EdgeMask.Width * ExtremaLocationPercent - extremaWidth / 2) , (int)(EdgeLocation == EdgeLocations.TOP ? 0 : EdgeMask.Height - extremaHeight), (int)extremaWidth, (int)extremaHeight);
            }
            else
            {
                extremaWidth = EdgeMask.Width;
                extremaHeight = EdgeMask.Height / 2f;
                if (EdgeType == EdgeTypes.BULB) { extremaWidth /= 2; }
                extremaRect = new Rectangle((int)(EdgeLocation == EdgeLocations.LEFT ? 0 : EdgeMask.Width - extremaWidth), (int)(EdgeMask.Height * ExtremaLocationPercent - extremaHeight / 2), (int)extremaWidth, (int)extremaHeight);
            }
            EdgeExtremaMask = EdgeMask.Copy(extremaRect);

            ProcessedImagesStorage.AddImage(PieceID + " " + EdgeLocation.ToString() + " Extrema", EdgeExtremaMask.ToBitmap());
        }*/

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Check if the Edge is a BulbEdge.
        /// Example for sliceWeights (Bulb): 0.54; 0.59; 0.76; 0.98; 0.72; 0.43; 0.38
        /// </summary>
        /// <param name="sliceWeights">Array with slice weights. For each slice the percentage of white pixels is calculated.</param>
        /// <returns>true -> BulbEdge; false -> no BulbEdge</returns>
        private bool IsBulbEdge(float[] sliceWeights)
        {
            float[] sliceWeightsSorted = (float[])sliceWeights.Clone();
            Array.Sort(sliceWeightsSorted);                                     // Lowest weight at index 0
            sliceWeightsSorted = sliceWeightsSorted.Reverse().ToArray();        // Reverse to have the highest weight at index 0
            int indexHighestWeight = Array.IndexOf(sliceWeights, sliceWeightsSorted[0]);

            float weightThreshold = 0.5f; //0.8f * sliceWeightsSorted[0];

            if(sliceWeightsSorted[1] < weightThreshold)             // The highest weight is far higher than the second highest weight => bulb
            {
                return true;
            }

            int indexBeginHighWeightBlock = indexHighestWeight, indexEndHighWeightBlock = indexHighestWeight;        // Indices of the array elements that are higher than the weightThreshold and surround the highest value

            while(indexBeginHighWeightBlock >= 0 && indexBeginHighWeightBlock < sliceWeights.Length && sliceWeights[indexBeginHighWeightBlock] >= weightThreshold)
            {
                indexBeginHighWeightBlock--;
            }
            while(indexEndHighWeightBlock >= 0 && indexEndHighWeightBlock < sliceWeights.Length && sliceWeights[indexEndHighWeightBlock] >= weightThreshold)
            {
                indexEndHighWeightBlock++;
            }
            indexBeginHighWeightBlock++;
            indexEndHighWeightBlock--;

            if(indexBeginHighWeightBlock == 0 || indexEndHighWeightBlock == sliceWeights.Length - 1 || (indexEndHighWeightBlock - indexBeginHighWeightBlock) > sliceWeights.Length / 2)     // The bulb must have low weight areas around it and should be narrow
            {
                return false;
            }

            for(int i = 0; i < indexBeginHighWeightBlock; i++)                              // Check if all values before the begin of the high weight block are below the weightThreshold. If not it isn't a bulb.
            {
                if(sliceWeights[i] > weightThreshold) { return false; }
            }
            for (int i = indexEndHighWeightBlock + 1; i < sliceWeights.Length; i++)         // Check if all values after the end of the high weight block are below the weightThreshold. If not it isn't a bulb.
            {
                if (sliceWeights[i] > weightThreshold) { return false; }
            }

            extremaSliceIndex = (indexEndHighWeightBlock + indexBeginHighWeightBlock) / 2;      // The bulb location is in the middle of the detected high weight block
            return true;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Check if the Edge is a HoleEdge.
        /// Example for sliceWeights (Hole): 0.37; 0.65; 0.92; 0.25; 0.89; 0.76; 0.26
        /// </summary>
        /// <param name="sliceWeights">Array with slice weights. For each slice the percentage of white pixels is calculated.</param>
        /// <returns>true -> HoleEdge; false -> no HoleEdge</returns>
        private bool IsHoleEdge(float[] sliceWeights)
        {
            /*float[] sliceWeightsSorted = (float[])sliceWeights.Clone();
            Array.Sort(sliceWeightsSorted);                                     // Lowest weight at index 0
            sliceWeightsSorted = sliceWeightsSorted.Reverse().ToArray();        // Reverse to have the highest weight at index 0
            */

            float weightThreshold = 0.5f; //0.8f * sliceWeightsSorted[0];

            int indexBeginHighWeightBlockLeft = -1, indexEndHighWeightBlockLeft = -1, indexBeginHighWeightBlockRight = -1, indexEndHighWeightBlockRight = -1;       // Indices of the high weight blocks (blocks that are surrounding a hole)

            for (int i = 0; i < sliceWeights.Length; i++)
            {
                if (indexBeginHighWeightBlockLeft == -1 && sliceWeights[i] > weightThreshold) { indexBeginHighWeightBlockLeft = i; }
                if (indexBeginHighWeightBlockLeft != -1 && sliceWeights[i] < weightThreshold) { indexEndHighWeightBlockLeft = i - 1; break; }
            }

            for (int i = sliceWeights.Length -1; i >= 0; i--)
            {
                if (indexEndHighWeightBlockRight == -1 && sliceWeights[i] > weightThreshold) { indexEndHighWeightBlockRight = i; }
                if (indexEndHighWeightBlockRight != -1 && sliceWeights[i] < weightThreshold) { indexBeginHighWeightBlockRight = i + 1; break; }
            }

            if(indexEndHighWeightBlockLeft != -1 && indexBeginHighWeightBlockRight != -1 && indexEndHighWeightBlockLeft < indexBeginHighWeightBlockRight && Math.Abs(indexBeginHighWeightBlockRight - indexEndHighWeightBlockLeft) < sliceWeights.Length / 2)
            {
                extremaSliceIndex = (indexBeginHighWeightBlockRight + indexEndHighWeightBlockLeft) / 2;
                return true;
            }
            else { return false; }
        }

        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************
        //**********************************************************************************************************************************************************************************************

        public float CalculateEdgeMatchFactor(PieceEdge edge2)
        {
            float matchFactorBulbHole = CalculateEdgeBulbHoleMatchFactor(edge2);
            float matchFactorShape = CalculateEdgeShapeMatchFactor(edge2);
            float matchFactor = Math.Min(matchFactorBulbHole, matchFactorShape);

            return matchFactor;
        }

        //**********************************************************************************************************************************************************************************************

        private float CalculateEdgeBulbHoleMatchFactor(PieceEdge edge2)
        {
            float matchFactorBulbHole;
            
            if ((this.EdgeType == EdgeTypes.BULB && edge2.EdgeType == EdgeTypes.HOLE) || (this.EdgeType == EdgeTypes.HOLE && edge2.EdgeType == EdgeTypes.BULB))
            {
                //float extremaLocation1 = this.ExtremaLocationPercent;       // !!!!!!!!!!!!!!! adapt because of edge location
                //float extremaLocation2 = edge2.ExtremaLocationPercent;
                //return 1 - Math.Abs(extremaLocation1 - extremaLocation2);

                matchFactorBulbHole = 1;
            }
            else
            {
                matchFactorBulbHole = 0;    // 0 % match because it's not a bulb/hole combination
            }

            ProcessedImagesStorage.AddImage(this.PieceID + " " + this.EdgeLocation.ToString() + " <==>" + edge2.PieceID + " " + edge2.EdgeLocation.ToString() + "  BulbHoleFactor = " + matchFactorBulbHole.ToString(), Tools.Combine2ImagesHorizontal(this.EdgeMask, edge2.EdgeMask, 20).ToBitmap());
            return matchFactorBulbHole;
        }

        //**********************************************************************************************************************************************************************************************

        public float CalculateEdgeShapeMatchFactor(PieceEdge edge2)
        {
            Image<Gray, byte> edgeMask1rotated = this.EdgeMask.Copy();
            Image<Gray, byte> edgeMask2rotated = edge2.EdgeMask.Copy();

            switch (this.EdgeLocation)       // Rotate this edge, so that the extrema (bulb/hole) is right
            {
                case EdgeLocations.TOP: edgeMask1rotated = this.EdgeMask.Rotate(90, new Gray(0), false); break;
                case EdgeLocations.RIGHT: break;
                case EdgeLocations.BOTTOM: edgeMask1rotated = this.EdgeMask.Rotate(-90, new Gray(0), false); break;
                case EdgeLocations.LEFT: edgeMask1rotated = this.EdgeMask.Rotate(180, new Gray(0), false); break;
            }
            switch (edge2.EdgeLocation)     // Rotate edge2, so that the extrema (bulb/hole) is left
            {
                case EdgeLocations.TOP: edgeMask2rotated = edge2.EdgeMask.Rotate(-90, new Gray(0), false); break;
                case EdgeLocations.RIGHT: edgeMask2rotated = edge2.EdgeMask.Rotate(180, new Gray(0), false); break;
                case EdgeLocations.BOTTOM: edgeMask2rotated = edge2.EdgeMask.Rotate(90, new Gray(0), false); break;
                case EdgeLocations.LEFT: break;
            }

            Image<Gray, byte> holeExtendingImg;

            if (this.EdgeType == EdgeTypes.BULB)        // get the right half of the edge mask
            {
                edgeMask1rotated = edgeMask1rotated.Copy(new Rectangle(edgeMask1rotated.Width / 2, 0, edgeMask1rotated.Width / 2, edgeMask1rotated.Height));
            }
            else if (this.EdgeType == EdgeTypes.HOLE)
            {
                int holeRectStartY = (PieceEdgeTypeNextCounterClockwise == EdgeTypes.BULB ? edgeMask1rotated.Height / 4 : 0);
                int holeRectEndY = (PieceEdgeTypeNextClockwise == EdgeTypes.BULB ? edgeMask1rotated.Height / 4 : 0);
                edgeMask1rotated = edgeMask1rotated.Copy(new Rectangle(0, holeRectStartY, edgeMask1rotated.Width, edgeMask1rotated.Height - holeRectStartY - holeRectEndY));

                holeExtendingImg = new Image<Gray, byte>(10, edgeMask1rotated.Height);
                holeExtendingImg.SetValue(new Gray(0));
                edgeMask1rotated = Tools.Combine2ImagesHorizontal(edgeMask1rotated, holeExtendingImg, 0);
            }

            if (edge2.EdgeType == EdgeTypes.BULB)       // get the left half of the edge mask
            {
                edgeMask2rotated = edgeMask2rotated.Copy(new Rectangle(0, 0, edgeMask2rotated.Width / 2, edgeMask1rotated.Height));
            }
            else if(edge2.EdgeType == EdgeTypes.HOLE)
            {
                int holeRectStartY = (edge2.PieceEdgeTypeNextClockwise == EdgeTypes.BULB ? edgeMask2rotated.Height / 4 : 0);
                int holeRectEndY = (edge2.PieceEdgeTypeNextCounterClockwise == EdgeTypes.BULB ? edgeMask2rotated.Height / 4 : 0);
                edgeMask2rotated = edgeMask2rotated.Copy(new Rectangle(0, holeRectStartY, edgeMask2rotated.Width, edgeMask2rotated.Height - holeRectStartY - holeRectEndY));

                holeExtendingImg = new Image<Gray, byte>(10, edgeMask2rotated.Height);
                holeExtendingImg.SetValue(new Gray(0));
                edgeMask2rotated = Tools.Combine2ImagesHorizontal(holeExtendingImg, edgeMask2rotated, 0);
            }

            ProcessedImagesStorage.AddImage(this.PieceID + " " + this.EdgeLocation.ToString() + " <==>" + edge2.PieceID + " " + edge2.EdgeLocation.ToString() + " Rotated ", Tools.Combine2ImagesHorizontal(edgeMask1rotated, edgeMask2rotated, 20).ToBitmap());
            
#warning cut out extremas before calculating the contours

            VectorOfVectorOfPoint contours1 = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(edgeMask1rotated, contours1, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            VectorOfPoint contour1 = Tools.GetLargestContour(contours1);
            Image<Rgb, byte> contour1Img = edgeMask1rotated.Convert<Rgb, byte>();
            CvInvoke.DrawContours(contour1Img, new VectorOfVectorOfPoint(contour1), -1, new MCvScalar(255, 0, 0));

            VectorOfVectorOfPoint contours2 = new VectorOfVectorOfPoint();
            Image<Gray, byte> edge2MaskInverted = edgeMask2rotated.Copy();
            edge2MaskInverted._Not();
            CvInvoke.FindContours(edge2MaskInverted, contours2, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            VectorOfPoint contour2 = Tools.GetLargestContour(contours2);
            Image<Rgb, byte> contour2Img = edge2MaskInverted.Convert<Rgb, byte>();
            CvInvoke.DrawContours(contour2Img, new VectorOfVectorOfPoint(contour2), -1, new MCvScalar(255, 0, 0));

            double shapeMatchFactor = CvInvoke.MatchShapes(contour1, contour2, ContoursMatchType.I1);

            ProcessedImagesStorage.AddImage(this.PieceID + " " + this.EdgeLocation.ToString() + " <==>" + edge2.PieceID + " " + edge2.EdgeLocation.ToString() + " Contours " + shapeMatchFactor.ToString(), Tools.Combine2ImagesHorizontal(contour1Img, contour2Img, 20).ToBitmap());
            
            return 1;
        }

    }
}
