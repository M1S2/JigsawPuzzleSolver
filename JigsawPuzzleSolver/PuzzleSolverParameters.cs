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

#warning Cleanup!!!

#if false
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

        /// <summary>
        /// Background color of the pieces
        /// </summary>
        [System.Xml.Serialization.XmlIgnore]
        public System.Drawing.Color PieceBackgroundColor
        {
            get { return System.Drawing.ColorTranslator.FromHtml(PieceBackgroundColorString); }
            set { _pieceBackgroundColorString = System.Drawing.ColorTranslator.ToHtml(value); OnPropertyChanged(); OnPropertyChanged("PieceBackgroundColorString"); }
        }

        private string _pieceBackgroundColorString;
        /// <summary>
        /// Background color string of the pieces
        /// </summary>
        public string PieceBackgroundColorString
        {
            get { return _pieceBackgroundColorString; }
            set { _pieceBackgroundColorString = value; OnPropertyChanged(); OnPropertyChanged("PieceBackgroundColor"); }
        }

        private double _pieceFindCornersPeakDismissPercentage;
        /// <summary>
        /// This setting is used to find the corners of the pieces using polar coordinates. The peaks under this threshold are dismissed.
        /// </summary>
        public double PieceFindCornersPeakDismissPercentage
        {
            get { return _pieceFindCornersPeakDismissPercentage; }
            set { _pieceFindCornersPeakDismissPercentage = value; OnPropertyChanged(); }
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
#endif

        //##############################################################################################################################################################################################

        public PuzzleSolverParameters()
        {
#if false
            SolverShowDebugResults = false;
            CompressPuzzleOutputFile = true;
            PuzzleMinPieceSize = 50;
            PieceBackgroundColor = System.Drawing.Color.FromArgb(255, 103, 141, 156);
            PuzzleSolverKeepMatchesThreshold = 5; //2.5f;
            PieceFindCornersPeakDismissPercentage = 0.1;
            EdgeCompareWindowSizePercent = 0.003; //0.002; //0.01; //0.15;
            EdgeCompareEndpointDiffIgnoreThreshold = 15;
            UseParallelLoops = true;
#endif
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Reset all parameters of the singleton instance to default values
        /// </summary>
        private static void ResetToDefaultSettings()
        {
            PuzzleSolverParameters tmpParams = new PuzzleSolverParameters();

#if false
            Instance.SolverShowDebugResults = tmpParams.SolverShowDebugResults;
            Instance.CompressPuzzleOutputFile = tmpParams.CompressPuzzleOutputFile;
            Instance.PuzzleMinPieceSize = tmpParams.PuzzleMinPieceSize;
            Instance.PieceBackgroundColor = tmpParams.PieceBackgroundColor;
            Instance.PuzzleSolverKeepMatchesThreshold = tmpParams.PuzzleSolverKeepMatchesThreshold;
            Instance.PieceFindCornersPeakDismissPercentage = tmpParams.PieceFindCornersPeakDismissPercentage;
            Instance.EdgeCompareWindowSizePercent = tmpParams.EdgeCompareWindowSizePercent;
            Instance.EdgeCompareEndpointDiffIgnoreThreshold = tmpParams.EdgeCompareEndpointDiffIgnoreThreshold;
            Instance.UseParallelLoops = tmpParams.UseParallelLoops;
#endif
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
