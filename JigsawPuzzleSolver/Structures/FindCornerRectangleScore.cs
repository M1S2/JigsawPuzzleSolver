using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Structure that is used by the find_corners_MaximumRectangle() method in class Piece.
    /// It holds the points of a quadraliteral, the angle difference from a perfect rectangle and the quadraliteral area
    /// </summary>
    public struct FindCornerRectangleScore
    {
        /// <summary>
        /// Possible corners of the puzzle piece.
        /// </summary>
        public Point[] PossibleCorners { get; set; }

        /// <summary>
        /// The sum of the differences of all 4 corner angles from 90 degree.
        /// </summary>
        public double AngleDiff { get; set; }

        /// <summary>
        /// The area of the quadraliteral.
        /// </summary>
        public double RectangleArea { get; set; }
    }
}
