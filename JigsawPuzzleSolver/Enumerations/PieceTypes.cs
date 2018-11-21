using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Types of pieces
    /// </summary>
    public enum PieceTypes
    {
        /// <summary>
        /// Corner Piece (Containing 2 Line edges)
        /// </summary>
        CORNER,

        /// <summary>
        /// Border Piece (Containing 1 Line edge)
        /// </summary>
        BORDER,

        /// <summary>
        /// Inner Piece (Containing no Line edges)
        /// </summary>
        INNER
    }
}
