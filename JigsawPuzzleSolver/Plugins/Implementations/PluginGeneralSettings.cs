using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.Core;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("General Settings")]
    [PluginDescription("Plugin holding general settings")]
    public class PluginGeneralSettings : PluginGroupGeneralSettings
    {
        public override PackIconBase PluginIcon => new PackIconMaterial() { Kind = PackIconMaterialKind.Settings };

        //##############################################################################################################################################################################################

        private bool _solverShowDebugResults;
        [PluginSettingBool("Show", "Hide")]
        [PluginSettingDescription("Enable this option to show debug informations in the log output. Not neccessary during normal operation.")]
        public bool SolverShowDebugResults
        {
            get { return _solverShowDebugResults; }
            set { _solverShowDebugResults = value; OnPropertyChanged(); }
        }

        private bool _compressPuzzleOutputFile;
        [PluginSettingBool("Compress", "Normal")]
        [PluginSettingDescription("Enable this option to compress the output XML file that is created while saving the puzzle. The settings must correspond to the file when loading.")]
        public bool CompressPuzzleOutputFile
        {
            get { return _compressPuzzleOutputFile; }
            set { _compressPuzzleOutputFile = value; OnPropertyChanged(); }
        }

        private bool _useParallelLoops;
        [PluginSettingBool("Parallel", "Sequential")]
        [PluginSettingDescription("Enable this option to use multiple threads for processing. If the option is disabled only one thread is used.")]
        public bool UseParallelLoops
        {
            get { return _useParallelLoops; }
            set { _useParallelLoops = value; OnPropertyChanged(); }
        }

        private int _puzzleMinPieceSize;
        [PluginSettingNumber(1, 0, 2000)]
        [PluginSettingDescription("Minimum puzzle piece size in pixels. If the width or height of a possible piece is lower than this setting, the piece is discarded.")]
        public int PuzzleMinPieceSize
        {
            get { return _puzzleMinPieceSize; }
            set { _puzzleMinPieceSize = value; OnPropertyChanged(); }
        }

        private double _puzzleSolverKeepMatchesThreshold;
        [PluginSettingNumber(1, 0, 500000000)]
        [PluginSettingDescription("Only the best matching piece pairs should be kept. This parameter is used to discard too bad pairs. The higher this value is the more pairs are kept.")]
        public double PuzzleSolverKeepMatchesThreshold
        {
            get { return _puzzleSolverKeepMatchesThreshold; }
            set { _puzzleSolverKeepMatchesThreshold = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            SolverShowDebugResults = false;
            CompressPuzzleOutputFile = true;
            UseParallelLoops = false;
            PuzzleMinPieceSize = 50;
            PuzzleSolverKeepMatchesThreshold = 5;
        }
    }
}
