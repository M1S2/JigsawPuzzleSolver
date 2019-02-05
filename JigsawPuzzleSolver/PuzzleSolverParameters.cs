using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver
{
    public static class PuzzleSolverParameters
    {
        public static bool SolverShowDebugResults { get; set; }
        public static bool CompressPuzzleOutputFile { get; set; }

        public static int PuzzleMinPieceSize { get; set; }
        public static bool PuzzleApplyMedianBlurFilter { get; set; }
        public static bool PuzzleIsInputBackgroundWhite { get; set; }      // true -> white background on input images, otherwise black background
        public static float PuzzleSolverKeepMatchesThreshold { get; set; }

        public static int PieceFindCornersGFTTMaxCorners { get; set; }
        public static double PieceFindCornersGFTTQualityLevel { get; set; }
        public static double PieceFindCornersGFTTMinDist { get; set; }
        public static int PieceFindCornersGFTTBlockSize { get; set; }
        public static double PieceFindCornersMaxAngleDiff { get; set; }
        public static double PieceFindCornersMaxCornerDistRatio { get; set; }

        public static double EdgeCompareWindowSizePercent { get; set; }        // Not all points are taken into account to speed up the calculation. Therefore a window is defined by the percentage of total points in the longer contour
        public static double EdgeCompareEndpointDiffIgnoreThreshold { get; set; }

        static PuzzleSolverParameters()
        {
            SolverShowDebugResults = false;
            CompressPuzzleOutputFile = true;
            PuzzleMinPieceSize = 50;
            PuzzleApplyMedianBlurFilter = true;
            PuzzleIsInputBackgroundWhite = true;
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
