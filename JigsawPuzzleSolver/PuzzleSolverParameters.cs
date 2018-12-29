using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    public class PuzzleSolverParameters
    {
        public bool SolverShowDebugResults { get; set; }

        public int PuzzleMinPieceSize { get; set; }
        public bool PuzzleApplyMedianBlurFilter { get; set; }
        public float PuzzleSolverKeepMatchesThreshold { get; set; }

        public int PieceFindCornersGFTTMaxCorners { get; set; }
        public double PieceFindCornersGFTTQualityLevel { get; set; }
        public double PieceFindCornersGFTTMinDist { get; set; }
        public int PieceFindCornersGFTTBlockSize { get; set; }
        public double PieceFindCornersMaxAngleDiff { get; set; }
        public double PieceFindCornersMaxCornerDistRatio { get; set; }

        public double EdgeCompareWindowSizePercent { get; set; }        // Not all points are taken into account to speed up the calculation. Therefore a window is defined by the percentage of total points in the longer contour
        public double EdgeCompareEndpointDiffIgnoreThreshold { get; set; }

        public PuzzleSolverParameters()
        {
            SolverShowDebugResults = false;
            PuzzleMinPieceSize = 50;
            PuzzleApplyMedianBlurFilter = true;
            PuzzleSolverKeepMatchesThreshold = 4; //2.5f;
            PieceFindCornersGFTTMaxCorners = 500;
            PieceFindCornersGFTTQualityLevel = 0.01;
            PieceFindCornersGFTTMinDist = 5; //10;
            PieceFindCornersGFTTBlockSize = 2; //6;
            PieceFindCornersMaxAngleDiff = 5;
            PieceFindCornersMaxCornerDistRatio = 1.5;
            EdgeCompareWindowSizePercent = 0.01; //0.15;
            EdgeCompareEndpointDiffIgnoreThreshold = 15;
        }
    }
}
