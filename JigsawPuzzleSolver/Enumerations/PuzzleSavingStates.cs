using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Puzzle saving states
    /// </summary>
    public enum PuzzleSavingStates
    {
        /// <summary>
        /// The puzzle is null
        /// </summary>
        PUZZLE_NULL,

        /// <summary>
        /// The puzzle wasn't saved yet
        /// </summary>
        NEW_UNSAVED,

        /// <summary>
        /// The current puzzle was loaded from a file
        /// </summary>
        LOADED,

        /// <summary>
        /// The puzzle is saved to a file
        /// </summary>
        SAVED,

        /// <summary>
        /// The puzzle is loading from a file
        /// </summary>
        LOADING,

        /// <summary>
        /// The puzzle is saving to a file
        /// </summary>
        SAVING,

        /// <summary>
        /// Error while saving
        /// </summary>
        ERROR
    }
}
