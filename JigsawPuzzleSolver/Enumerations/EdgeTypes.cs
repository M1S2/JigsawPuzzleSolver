using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Types of piece edges
    /// </summary>
    public enum EdgeTypes
    {
        /// <summary>
        /// Edge without bulb or hole, outer edge
        /// </summary>
        LINE,

        /// <summary>
        /// Edge with a bulb
        /// </summary>
        BULB,

        /// <summary>
        /// Edge with a hole
        /// </summary>
        HOLE,

        /// <summary>
        /// Unclassified Edge
        /// </summary>
        UNKNOWN
    }
}
