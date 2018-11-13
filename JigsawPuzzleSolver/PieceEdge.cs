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
        /// Type of the Edge (Bulb, Hole, Line)
        /// </summary>
        public EdgeTypes EdgeType { get; set; }

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
                if(EdgeLocation == EdgeLocations.TOP || EdgeLocation == EdgeLocations.BOTTOM)
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
        }

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

    }
}
