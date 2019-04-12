using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace JigsawPuzzleSolver
{
    public class PuzzleSolverParameters : INotifyPropertyChanged
    {
        #region Singleton
        /// <summary>
        /// Settings are saved in application settings
        /// </summary>
        public static PuzzleSolverParameters Instance
        {
            get
            {
                if (Properties.Settings.Default.SolverParameters == null) { Properties.Settings.Default.SolverParameters = new PuzzleSolverParameters(); }
                return Properties.Settings.Default.SolverParameters;
            }
        }
        #endregion

        //##############################################################################################################################################################################################

        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. The CallerMemberName attribute that is applied to the optional propertyName parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName">Name of the property that is changed</param>
        /// see: https://docs.microsoft.com/de-de/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //##############################################################################################################################################################################################

        private bool _solverShowDebugResults;
        /// <summary>
        /// Enable this option to show debug informations in the log output. Not neccessary during normal operation.
        /// </summary>
        public bool SolverShowDebugResults
        {
            get { return _solverShowDebugResults; }
            set { _solverShowDebugResults = value; OnPropertyChanged(); }
        }

        private bool _compressPuzzleOutputFile;
        /// <summary>
        /// Enable this option to compress the output XML file that is created while saving the puzzle. The settings must correspond to the file when loading.
        /// </summary>
        public bool CompressPuzzleOutputFile
        {
            get { return _compressPuzzleOutputFile; }
            set { _compressPuzzleOutputFile = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private int _puzzleMinPieceSize;
        /// <summary>
        /// Minimum puzzle piece size in pixels. If the width or height of a possible piece is lower than this setting, the piece is discarded.
        /// </summary>
        public int PuzzleMinPieceSize
        {
            get { return _puzzleMinPieceSize; }
            set { _puzzleMinPieceSize = value; OnPropertyChanged(); }
        }

        private bool _puzzleApplyMedianBlurFilter;
        /// <summary>
        /// Enable this option to apply a median blur filter to the input images. The median filter reduces noise and persist edges.
        /// </summary>
        public bool PuzzleApplyMedianBlurFilter
        {
            get { return _puzzleApplyMedianBlurFilter; }
            set { _puzzleApplyMedianBlurFilter = value; OnPropertyChanged(); }
        }

        private bool _puzzleIsInputBackgroundWhite;
        /// <summary>
        /// Is the background of the input images white or black (true = white, false = black).
        /// </summary>
        public bool PuzzleIsInputBackgroundWhite
        {
            get { return _puzzleIsInputBackgroundWhite; }
            set { _puzzleIsInputBackgroundWhite = value; OnPropertyChanged(); }
        }

        private double _puzzleSolverKeepMatchesThreshold;
        /// <summary>
        /// Only the best matching piece pairs should be kept. This parameter is used to discard too bad pairs. The higher this value is the more pairs are kept.
        /// </summary>
        public double PuzzleSolverKeepMatchesThreshold
        {
            get { return _puzzleSolverKeepMatchesThreshold; }
            set { _puzzleSolverKeepMatchesThreshold = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private int _pieceFindCornersGFTTMaxCorners;
        /// <summary>
        /// Maximum number of features that should be detected by the GFTT detector (Good Features To Track). Reduce this setting for speed improvements.
        /// </summary>
        public int PieceFindCornersGFTTMaxCorners
        {
            get { return _pieceFindCornersGFTTMaxCorners; }
            set { _pieceFindCornersGFTTMaxCorners = value; OnPropertyChanged(); }
        }

        private double _pieceFindCornersGFTTQualityLevel;
        /// <summary>
        /// Minimal accepted quality of image corners of the GFTT detector (Good Features To Track).
        /// </summary>
        public double PieceFindCornersGFTTQualityLevel
        {
            get { return _pieceFindCornersGFTTQualityLevel; }
            set { _pieceFindCornersGFTTQualityLevel = value; OnPropertyChanged(); }
        }

        private double _pieceFindCornersGFTTMinDist;
        /// <summary>
        /// Minimum euclidian distance between returned corners of the GFTT detector (Good Features To Track). The higher this setting the less points are returned.
        /// </summary>
        public double PieceFindCornersGFTTMinDist
        {
            get { return _pieceFindCornersGFTTMinDist; }
            set { _pieceFindCornersGFTTMinDist = value; OnPropertyChanged(); }
        }

        private int _pieceFindCornersGFTTBlockSize;
        /// <summary>
        /// Size of the averaging block of the GFTT detector (Good Features To Track).
        /// </summary>
        public int PieceFindCornersGFTTBlockSize
        {
            get { return _pieceFindCornersGFTTBlockSize; }
            set { _pieceFindCornersGFTTBlockSize = value; OnPropertyChanged(); }
        }

        private double _pieceFindCornersMaxAngleDiff;
        /// <summary>
        /// This setting is used to find the corners of the pieces. Only corners with angles near 90 degree are kept. The higher this setting the more the squares can be twisted.
        /// </summary>
        public double PieceFindCornersMaxAngleDiff
        {
            get { return _pieceFindCornersMaxAngleDiff; }
            set { _pieceFindCornersMaxAngleDiff = value; OnPropertyChanged(); }
        }

        private double _pieceFindCornersMaxCornerDistRatio;
        /// <summary>
        /// This setting limits the contour points that are used to find the corners of the piece. The piece is split in four squares (Top-Left, Top-Right, Bottom-Left, Bottom-Right) and points that are too far from the corners are discarded. The higher this setting the more points are kept.
        /// </summary>
        public double PieceFindCornersMaxCornerDistRatio
        {
            get { return _pieceFindCornersMaxCornerDistRatio; }
            set { _pieceFindCornersMaxCornerDistRatio = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private double _edgeCompareWindowSizePercent;
        /// <summary>
        /// Not all points are taken into account to speed up the calculation. Therefore a window is defined by the percentage of total points in the longer contour.
        /// </summary>
        public double EdgeCompareWindowSizePercent
        {
            get { return _edgeCompareWindowSizePercent; }
            set { _edgeCompareWindowSizePercent = value; OnPropertyChanged(); }
        }

        private double _edgeCompareEndpointDiffIgnoreThreshold;
        /// <summary>
        /// Edges whose endpoints are close enough to each other are regarded as optimal.
        /// </summary>
        public double EdgeCompareEndpointDiffIgnoreThreshold
        {
            get { return _edgeCompareEndpointDiffIgnoreThreshold; }
            set { _edgeCompareEndpointDiffIgnoreThreshold = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private bool _useParallelLoops;
        /// <summary>
        /// Enable this option to use multiple threads for processing. If the option is disabled only one thread is used.
        /// </summary>
        public bool UseParallelLoops
        {
            get { return _useParallelLoops; }
            set { _useParallelLoops = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        public PuzzleSolverParameters()
        {
            SolverShowDebugResults = false;
            CompressPuzzleOutputFile = true;
            PuzzleMinPieceSize = 50;
            PuzzleApplyMedianBlurFilter = true;
            PuzzleIsInputBackgroundWhite = true;
            PuzzleSolverKeepMatchesThreshold = 5; //2.5f;
            PieceFindCornersGFTTMaxCorners = 500;
            PieceFindCornersGFTTQualityLevel = 0.01;
            PieceFindCornersGFTTMinDist = 5; //10;
            PieceFindCornersGFTTBlockSize = 2; //6;
            PieceFindCornersMaxAngleDiff = 10;
            PieceFindCornersMaxCornerDistRatio = 1.5;
            EdgeCompareWindowSizePercent = 0.003; //0.002; //0.01; //0.15;
            EdgeCompareEndpointDiffIgnoreThreshold = 15;
            UseParallelLoops = true;
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Reset all parameters of the singleton instance to default values
        /// </summary>
        private static void ResetToDefaultSettings()
        {
            PuzzleSolverParameters tmpParams = new PuzzleSolverParameters();

            Instance.SolverShowDebugResults = tmpParams.SolverShowDebugResults;
            Instance.CompressPuzzleOutputFile = tmpParams.CompressPuzzleOutputFile;
            Instance.PuzzleMinPieceSize = tmpParams.PuzzleMinPieceSize;
            Instance.PuzzleApplyMedianBlurFilter = tmpParams.PuzzleApplyMedianBlurFilter;
            Instance.PuzzleIsInputBackgroundWhite = tmpParams.PuzzleIsInputBackgroundWhite;
            Instance.PuzzleSolverKeepMatchesThreshold = tmpParams.PuzzleSolverKeepMatchesThreshold;
            Instance.PieceFindCornersGFTTMaxCorners = tmpParams.PieceFindCornersGFTTMaxCorners;
            Instance.PieceFindCornersGFTTQualityLevel = tmpParams.PieceFindCornersGFTTQualityLevel;
            Instance.PieceFindCornersGFTTMinDist = tmpParams.PieceFindCornersGFTTMinDist;
            Instance.PieceFindCornersGFTTBlockSize = tmpParams.PieceFindCornersGFTTBlockSize;
            Instance.PieceFindCornersMaxAngleDiff = tmpParams.PieceFindCornersMaxAngleDiff;
            Instance.PieceFindCornersMaxCornerDistRatio = tmpParams.PieceFindCornersMaxCornerDistRatio;
            Instance.EdgeCompareWindowSizePercent = tmpParams.EdgeCompareWindowSizePercent;
            Instance.EdgeCompareEndpointDiffIgnoreThreshold = tmpParams.EdgeCompareEndpointDiffIgnoreThreshold;
            Instance.UseParallelLoops = tmpParams.UseParallelLoops;
        }

        private static System.Windows.Input.ICommand _defaultSettingsCommand;
        /// <summary>
        /// Command to reset all parameters of the singleton instance to default values
        /// </summary>
        public static System.Windows.Input.ICommand DefaultSettingsCommand
        {
            get
            {
                if (_defaultSettingsCommand == null)
                {
                    _defaultSettingsCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => { ResetToDefaultSettings(); });
                }
                return _defaultSettingsCommand;
            }
        }
    }
}
