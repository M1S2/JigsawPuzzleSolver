using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Steps during the solving process of the puzzle
    /// </summary>
    public enum PuzzleSolverSteps
    {
        /// <summary>
        /// Extract all pieces from the input images, find the edges and classify them.
        /// </summary>
        INIT_PIECES = 0,

        /// <summary>
        /// Compare all edges against each other and calculate scores.
        /// </summary>
        COMPARE_EDGES = 1,

        /// <summary>
        /// Join the pieces together based on the calculated scores.
        /// </summary>
        SOLVE_PUZZLE = 2,

        /// <summary>
        /// The puzzle is solved.
        /// </summary>
        SOLVED = 3
    }
}
