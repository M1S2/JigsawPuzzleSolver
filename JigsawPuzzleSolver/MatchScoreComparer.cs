using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    public enum ScoreOrders { HIGHEST_FIRST, LOWEST_FIRST }

    /// <summary>
    /// Use this class to order a list of MatchScores by the score
    /// </summary>
    public class MatchScoreComparer : IComparer<MatchScore>
    {
        /// <summary>
        /// How to order the scores (highest or lowest first)
        /// </summary>
        public ScoreOrders ScoreOrder { get; private set; }

        /// <summary>
        /// Create a new MatchScoreComparer
        /// </summary>
        /// <param name="scoreOrder">How to order the scores (highest or lowest first)</param>
        public MatchScoreComparer(ScoreOrders scoreOrder)
        {
            ScoreOrder = scoreOrder;
        }

        /// <summary>
        /// Compare the score of the MatchScores
        /// </summary>
        /// <param name="s1">MatchScore 1</param>
        /// <param name="s2">MatchScore 2</param>
        /// <returns>Compare result</returns>
        public int Compare(MatchScore s1, MatchScore s2)
        {
            if (ScoreOrder == ScoreOrders.HIGHEST_FIRST && s1.score < s2.score) { return 1; }
            else if (ScoreOrder == ScoreOrders.HIGHEST_FIRST && s1.score > s2.score) { return -1; }
            else if (ScoreOrder == ScoreOrders.LOWEST_FIRST && s1.score < s2.score) { return -1; }
            else if (ScoreOrder == ScoreOrders.LOWEST_FIRST && s1.score > s2.score) { return 1; }
            else { return 0; }
        }
    }
}
