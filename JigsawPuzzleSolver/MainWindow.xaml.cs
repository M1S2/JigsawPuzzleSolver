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

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
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

        private Stopwatch _stopWatchSolver;
        public Stopwatch StopWatchSolver
        {
            get { return _stopWatchSolver; }
            set { _stopWatchSolver = value; OnPropertyChanged(); }
        }

        private Puzzle _puzzleHandle;
        public Puzzle PuzzleHandle
        {
            get { return _puzzleHandle; }
            set { _puzzleHandle = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        #region Commands

        private ICommand _openNewPuzzleCommand;
        public ICommand OpenNewPuzzleCommand
        {
            get
            {
                if (_openNewPuzzleCommand == null) { _openNewPuzzleCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => this.OpenNewPuzzle()); }
                return _openNewPuzzleCommand;
            }
        }

        private ICommand _savePuzzleCommand;
        public ICommand SavePuzzleCommand
        {
            get
            {
                if (_savePuzzleCommand == null) { _savePuzzleCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => this.SavePuzzle(), param => { return PuzzleHandle != null; }); }
                return _savePuzzleCommand;
            }
        }

        private ICommand _loadPuzzleCommand;
        public ICommand LoadPuzzleCommand
        {
            get
            {
                if (_loadPuzzleCommand == null) { _loadPuzzleCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => this.LoadPuzzle()); }
                return _loadPuzzleCommand;
            }
        }

        private ICommand _infoCommand;
        public ICommand InfoCommand
        {
            get
            {
                if (_infoCommand == null)
                {
                    _infoCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param =>
                    {
                        AssemblyInfoHelper_WPF.WindowAssemblyInfo windowAssemblyInfo = new AssemblyInfoHelper_WPF.WindowAssemblyInfo();
                        windowAssemblyInfo.ShowDialog();
                    });
                }
                return _infoCommand;
            }
        }

        private ICommand _startSolvingCommand;
        public ICommand StartSolvingCommand
        {
            get
            {
                if (_startSolvingCommand == null) { _startSolvingCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => this.StartSolving(), param => { return PuzzleHandle?.IsSolverRunning == false; }); }
                return _startSolvingCommand;
            }
        }

        private ICommand _stopSolvingCommand;
        public ICommand StopSolvingCommand
        {
            get
            {
                if (_stopSolvingCommand == null) { _stopSolvingCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => { cancelTokenSource?.Cancel(); CommandManager.InvalidateRequerySuggested(); }, param => { return PuzzleHandle?.IsSolverRunning == true; }); }
                return _stopSolvingCommand;
            }
        }

        #endregion

        //##############################################################################################################################################################################################

        private IProgress<LogBox.LogEvent> logHandle;
        private DispatcherTimer stopWatchDispatcherTimer;       // This timer is used to notify the GUI that the StopWatchSolver.Elapsed property has changed
        private CancellationTokenSource cancelTokenSource;
        private Task workerTask;

        //##############################################################################################################################################################################################

        public MainWindow()
        {
            InitializeComponent();
            StopWatchSolver = new Stopwatch();
            stopWatchDispatcherTimer = new DispatcherTimer();
            stopWatchDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            stopWatchDispatcherTimer.Tick += new EventHandler((obj, e) => this.OnPropertyChanged("StopWatchSolver"));

            PuzzleSolverParameters.SolverShowDebugResults = false;
            PuzzleSolverParameters.PuzzleIsInputBackgroundWhite = false;
            PuzzleSolverParameters.CompressPuzzleOutputFile = true;

            logBox1.AutoScrollToLastLogEntry = true;
            logHandle = new Progress<LogBox.LogEvent>(progressValue =>
            {
                logBox1.LogEvent(progressValue);
            });
        }

        //##############################################################################################################################################################################################

        private void OpenNewPuzzle()
        {
            cancelTokenSource = new CancellationTokenSource();

            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select a folder containing all scanned puzzle piece images.";
            if(PuzzleHandle != null) { folderBrowserDialog1.SelectedPath = PuzzleHandle.PuzzlePiecesFolderPath; }
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PuzzleHandle = new Puzzle(folderBrowserDialog1.SelectedPath, logHandle, cancelTokenSource.Token);
            }

//#warning Only for faster testing !!!
            //PuzzleHandle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen\Test\Test3.png", logHandle, cancelTokenSource.Token);
            //PuzzleHandle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen", logHandle, cancelTokenSource.Token);
        }

        //**********************************************************************************************************************************************************************************************

        private async void SavePuzzle()
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
                    PuzzleHandle.PuzzleXMLOutputPath = saveFileDialog.FileName;
                    logHandle.Report(new LogBox.LogEventInfo("Saving puzzle to \"" + PuzzleHandle.PuzzleXMLOutputPath + "\""));
                    await Task.Run(() => { PuzzleHandle.Save(PuzzleHandle.PuzzleXMLOutputPath, PuzzleSolverParameters.CompressPuzzleOutputFile); });
                    logHandle.Report(new LogBox.LogEventInfo("Saving puzzle ready."));
                }                
            }
            catch(Exception ex)
            {
                logHandle.Report(new LogBox.LogEventError("Error while saving: " + ex.Message));
            }
        }

        //**********************************************************************************************************************************************************************************************

        private async void LoadPuzzle()
        {
            try
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
                    string xmlPath = openFileDialog.FileName;
                    logHandle.Report(new LogBox.LogEventInfo("Loading puzzle from \"" + xmlPath + "\""));
                    PuzzleHandle = await Task.Run(() => { return Puzzle.Load(xmlPath, PuzzleSolverParameters.CompressPuzzleOutputFile); });
                    PuzzleHandle.PuzzleXMLOutputPath = xmlPath;
                    logHandle.Report(new LogBox.LogEventInfo("Loading puzzle ready."));
                }
            }
            catch (Exception ex)
            {
                logHandle.Report(new LogBox.LogEventError("Error while loading: " + ex.Message));
            }
        }

        //**********************************************************************************************************************************************************************************************

        private async void StartSolving()
        {
            if(PuzzleHandle == null) { return; }

            logBox1.AutoScrollToLastLogEntry = false;
            cancelTokenSource = new CancellationTokenSource();
            PuzzleHandle.ResetCancelToken(cancelTokenSource.Token);
            
            stopWatchDispatcherTimer.Start();
            StopWatchSolver.Restart();
            try
            {
                workerTask = PuzzleHandle.Init();
                await workerTask;
                workerTask = PuzzleHandle.Solve();
                await workerTask;
            }
            catch (OperationCanceledException) { /* the exceptions are catches inside the methods */ }
            StopWatchSolver.Stop();
            stopWatchDispatcherTimer.Stop();
            CommandManager.InvalidateRequerySuggested();
            logBox1.AutoScrollToLastLogEntry = true;
        }

        //**********************************************************************************************************************************************************************************************

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
                if(workerTask != null && !workerTask.IsCompleted)
                {
                    this.Hide();
                    e.Cancel = true;
                    try
                    {
                        await workerTask;
                    }
                    catch (OperationCanceledException) { /* Nothing to do here because we expect the task to cancel */ }
                    this.Close();
                }
            }
        }

    }
}
