using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    public struct MatchScore
    {
        public int edgeIndex1;
        public int edgeIndex2;
        public double score;
    }

    public struct MatchScoreComparer : IComparer<MatchScore>
    {
        public int Compare(MatchScore a, MatchScore b)
        {
#warning Not sure !!!
            return (a.score < b.score) ? 1 : -1;
        }
    }
}
