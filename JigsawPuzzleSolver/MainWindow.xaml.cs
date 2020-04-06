using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Drawing;
using System.IO;
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using JigsawPuzzleSolver.GUI_Elements;
using JigsawPuzzleSolver.WindowTheme;
using LogBox.LogEvents;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
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

        //##############################################################################################################################################################################################

        private Puzzle _puzzleHandle;
        public Puzzle PuzzleHandle
        {
            get { return _puzzleHandle; }
            set { _puzzleHandle = value; OnPropertyChanged(); }
        }

        private PuzzleSavingStates _puzzleSavingState;
        public PuzzleSavingStates PuzzleSavingState
        {
            get { return _puzzleSavingState; }
            set { _puzzleSavingState = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        #region Commands

        private ICommand _openNewPuzzleCommand;
        public ICommand OpenNewPuzzleCommand
        {
            get
            {
                if (_openNewPuzzleCommand == null) { _openNewPuzzleCommand = new RelayCommand(param => this.OpenNewPuzzle(), param => { return (PuzzleSavingState != PuzzleSavingStates.SAVING && PuzzleSavingState != PuzzleSavingStates.LOADING); }); }
                return _openNewPuzzleCommand;
            }
        }

        private ICommand _savePuzzleCommand;
        public ICommand SavePuzzleCommand
        {
            get
            {
                if (_savePuzzleCommand == null) { _savePuzzleCommand = new RelayCommand(async param => await this.SavePuzzle(), param => { return (PuzzleHandle != null && PuzzleSavingState != PuzzleSavingStates.SAVING && PuzzleSavingState != PuzzleSavingStates.LOADING); }); }
                return _savePuzzleCommand;
            }
        }

        private ICommand _loadPuzzleCommand;
        public ICommand LoadPuzzleCommand
        {
            get
            {
                if (_loadPuzzleCommand == null) { _loadPuzzleCommand = new RelayCommand(async param => await this.LoadPuzzle(), param => { return (PuzzleSavingState != PuzzleSavingStates.SAVING && PuzzleSavingState != PuzzleSavingStates.LOADING); }); }
                return _loadPuzzleCommand;
            }
        }

        private ICommand _openSettingsFlyoutCommand;
        public ICommand OpenSettingsFlyoutCommand
        {
            get
            {
                if(_openSettingsFlyoutCommand == null)
                {
                    _openSettingsFlyoutCommand = new RelayCommand(param =>
                    {
#warning Settings temporary disabled !!!
                        Flyout flyout = this.Flyouts.Items[1] as Flyout; //this.Flyouts.Items[0] as Flyout;
                        flyout.IsOpen = !flyout.IsOpen;
                    });
                }
                return _openSettingsFlyoutCommand;
            }
        }

        private ICommand _settingsPieceBackgroundCommand;
        public ICommand SettingsPieceBackgroundCommand
        {
            get
            {
                if (_settingsPieceBackgroundCommand == null)
                {
                    _settingsPieceBackgroundCommand = new RelayCommand(param =>
                    {
                        Plugins.Controls.PieceBackgroundColorPickerWindow window = new Plugins.Controls.PieceBackgroundColorPickerWindow(PuzzleHandle?.PuzzlePiecesFolderPath, PuzzleSolverParameters.Instance.PieceBackgroundColor);
                        bool? windowResult = window.ShowDialog();
                        if (windowResult.HasValue && windowResult.Value == true)
                        {
                            PuzzleSolverParameters.Instance.PieceBackgroundColor = window.SelectedBackgroundColor;
                        }
                    });
                }
                return _settingsPieceBackgroundCommand;
            }
        }

        #endregion

        //##############################################################################################################################################################################################

        private IProgress<LogEvent> logHandle;

        //##############################################################################################################################################################################################

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;

            PuzzleSavingState = PuzzleSavingStates.PUZZLE_NULL;

            //PuzzleSolverParameters.Instance.SolverShowDebugResults = false;
            //PuzzleSolverParameters.Instance.PuzzleIsInputBackgroundWhite = false;
            //PuzzleSolverParameters.Instance.CompressPuzzleOutputFile = true;

            logBox1.AutoScrollToLastLogEntry = true;
            logHandle = new Progress<LogEvent>(progressValue =>
            {
                logBox1.LogEvent(progressValue);
            });
        }

        //##############################################################################################################################################################################################

        private async void FileFolder_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                string droppedFileOrFolder = files.FirstOrDefault();

                FileAttributes attr = File.GetAttributes(droppedFileOrFolder);
                if (attr.HasFlag(FileAttributes.Directory))      //detect whether its a directory or file
                {
                    OpenNewPuzzle(droppedFileOrFolder);
                }
                else
                {
                    await LoadPuzzle(droppedFileOrFolder);
                }
            }
        }

        private void OpenNewPuzzle()
        {
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select a folder containing all scanned puzzle piece images.";
            if(PuzzleHandle != null) { folderBrowserDialog1.SelectedPath = PuzzleHandle.PuzzlePiecesFolderPath; }
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                OpenNewPuzzle(folderBrowserDialog1.SelectedPath);
            }

//#warning Only for faster testing !!!
            //OpenNewPuzzle(@"..\..\..\Test_Pictures\ScannedImages\4");
        }

        private void OpenNewPuzzle(string piecesFolderPath)
        {
            PuzzleHandle = new Puzzle(piecesFolderPath, logHandle);
            logHandle.Report(new LogEventInfo("New puzzle created from \"" + PuzzleHandle.PuzzlePiecesFolderPath + "\""));
            PuzzleSavingState = PuzzleSavingStates.NEW_UNSAVED;
        }

        //**********************************************************************************************************************************************************************************************

        private async Task SavePuzzle()
        {
            try
            {
                if(PuzzleHandle == null) { return; }
                System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
                saveFileDialog.Title = "Please enter the output file name";
                saveFileDialog.Filter = "XML file|*.xml";
                saveFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(PuzzleHandle.PuzzleXMLOutputPath);
                saveFileDialog.RestoreDirectory = true;
                saveFileDialog.FileName = System.IO.Path.GetFileName(PuzzleHandle.PuzzleXMLOutputPath);
                if (saveFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    PuzzleSavingState = PuzzleSavingStates.SAVING;
                    logHandle.Report(new LogEventInfo("Saving puzzle to \"" + PuzzleHandle.PuzzleXMLOutputPath + "\""));
                    PuzzleHandle.PuzzleXMLOutputPath = saveFileDialog.FileName;
                    await Task.Run(() => { PuzzleHandle.Save(PuzzleHandle.PuzzleXMLOutputPath, PuzzleSolverParameters.Instance.CompressPuzzleOutputFile); });
                    PuzzleSavingState = PuzzleSavingStates.SAVED;
                    logHandle.Report(new LogEventInfo("Saving puzzle ready."));
                    CommandManager.InvalidateRequerySuggested();
                }                
            }
            catch(Exception ex)
            {
                PuzzleSavingState = PuzzleSavingStates.ERROR;
                logHandle.Report(new LogEventError("Error while saving: " + ex.Message));
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Show a dialog to choose a XML file containing the solved puzzle result and open the file
        /// </summary>
        private async Task LoadPuzzle()
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Title = "Please choose the file to open";
            openFileDialog.Filter = "XML file|*.xml";
            if (PuzzleHandle != null)
            {
                openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(PuzzleHandle.PuzzleXMLOutputPath);
                openFileDialog.RestoreDirectory = true;
                openFileDialog.FileName = System.IO.Path.GetFileName(PuzzleHandle.PuzzleXMLOutputPath);
            }
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                await LoadPuzzle(openFileDialog.FileName);
            }
        }

        /// <summary>
        /// Load the solved puzzle result from the given XML file
        /// </summary>
        /// <param name="puzzleFileName">Path to an XML file containing the solved puzzle result</param>
        private async Task LoadPuzzle(string puzzleFileName)
        {
            try
            {
                PuzzleSavingState = PuzzleSavingStates.LOADING;
                logHandle.Report(new LogEventInfo("Loading puzzle from \"" + puzzleFileName + "\""));
                PuzzleHandle = new Puzzle() { PuzzleXMLOutputPath = puzzleFileName };      // Neccessary to show the path while loading (when PuzzleHandle is null, no path is displayed)
                PuzzleHandle = await Task.Run(() => { return Puzzle.Load(puzzleFileName, PuzzleSolverParameters.Instance.CompressPuzzleOutputFile); });
                PuzzleHandle.PuzzleXMLOutputPath = puzzleFileName;
                PuzzleSavingState = PuzzleSavingStates.LOADED;
                logHandle.Report(new LogEventInfo("Loading puzzle ready."));
                CommandManager.InvalidateRequerySuggested();
            }
            catch (Exception ex)
            {
                PuzzleSavingState = PuzzleSavingStates.ERROR;
                logHandle.Report(new LogEventError("Error while loading: " + ex.Message));
            }
        }

        //**********************************************************************************************************************************************************************************************


        //see: https://github.com/MahApps/MahApps.Metro/issues/1022
        private bool ShouldClose = false;
        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (ShouldClose == false)
            {
                e.Cancel = true; //stop the window from closing.

                if (PuzzleSavingState == PuzzleSavingStates.SAVING)
                {
                    ShouldClose = (await this.ShowMessageAsync("Puzzle currently saving", "The Puzzle is currently saving. Do you want to close anyway?\nThe File will probably be corrupted!!!", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Close", NegativeButtonText = "Cancel", DialogResultOnCancel = MessageDialogResult.Canceled }) == MessageDialogResult.Affirmative);
                }
                else if (PuzzleHandle != null && PuzzleHandle.IsSolverRunning)
                {
                    ShouldClose = (await this.ShowMessageAsync("Puzzle solver running", "The Puzzle solver is currently running. Do you want to close anyway?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Close", NegativeButtonText = "Cancel", DialogResultOnCancel = MessageDialogResult.Canceled }) == MessageDialogResult.Affirmative);
                }
                else if (PuzzleSavingState == PuzzleSavingStates.NEW_UNSAVED && PuzzleHandle != null && PuzzleHandle?.CurrentSolverState != PuzzleSolverState.UNSOLVED && PuzzleHandle?.CurrentSolverState != PuzzleSolverState.ERROR)
                {
                    ShouldClose = (await this.ShowMessageAsync("Puzzle not saved yet", "The Puzzle wasn't saved yet. Do you want to close anyway?", MessageDialogStyle.AffirmativeAndNegative, new MetroDialogSettings() { AffirmativeButtonText = "Close", NegativeButtonText = "Cancel", DialogResultOnCancel = MessageDialogResult.Canceled }) == MessageDialogResult.Affirmative);
                }
                else { ShouldClose = true; }

                if(ShouldClose)         // Save all application settings and shutdown the application
                {
                    Plugins.PluginFactory.SavePluginSettings();
                    Properties.Settings.Default.Save();
                    Application.Current.Shutdown();
                }     
            }
        }

    }
}
