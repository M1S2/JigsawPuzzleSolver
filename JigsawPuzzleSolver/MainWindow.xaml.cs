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

        private PuzzleSolverParameters solverParameters;

        private IProgress<LogBox.LogEvent> logHandle;
        private DispatcherTimer stopWatchDispatcherTimer;       // This timer is used to notify the GUI that the StopWatchSolver.Elapsed property has changed
        private CancellationTokenSource cancelTokenSource;

        //##############################################################################################################################################################################################

        public MainWindow()
        {
            InitializeComponent();
            StopWatchSolver = new Stopwatch();
            stopWatchDispatcherTimer = new DispatcherTimer();
            stopWatchDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            stopWatchDispatcherTimer.Tick += new EventHandler((obj, e) => this.OnPropertyChanged("StopWatchSolver"));

            solverParameters = new PuzzleSolverParameters() { SolverShowDebugResults = false, PuzzleIsInputBackgroundWhite = false };
            
            logHandle = new Progress<LogBox.LogEvent>(progressValue =>
            {
                logBox1.LogEvent(progressValue);
            });
        }

        //##############################################################################################################################################################################################

        private void btn_open_new_puzzle_Click(object sender, RoutedEventArgs e)
        {
            cancelTokenSource = new CancellationTokenSource();

            /*System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog1.Description = "Select a folder containing all scanned puzzle piece images.";
            if(PuzzleHandle != null) { folderBrowserDialog1.SelectedPath = PuzzleHandle.PuzzlePiecesFolderPath; }
            if(folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                PuzzleHandle = new Puzzle(folderBrowserDialog1.SelectedPath, solverParameters, logHandle, cancelTokenSource.Token);
            }*/

#warning Only for faster testing !!!
            PuzzleHandle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen\Test\Test3.png", solverParameters, logHandle, cancelTokenSource.Token);
            //PuzzleHandle = new Puzzle(@"..\..\..\Scans\AngryBirds\ScannerOpen", solverParameters, logHandle, cancelTokenSource.Token);
        }

        //**********************************************************************************************************************************************************************************************

        private async void btn_start_solving_Click(object sender, RoutedEventArgs e)
        {
            if(PuzzleHandle == null) { return; }

            cancelTokenSource = new CancellationTokenSource();
            PuzzleHandle.ResetCancelToken(cancelTokenSource.Token);
            
            stopWatchDispatcherTimer.Start();
            StopWatchSolver.Restart();
            try
            {
                await PuzzleHandle.Init();
                await PuzzleHandle.Solve();
            }
            catch (OperationCanceledException) { /* the exceptions are catches inside the methods */ }
            StopWatchSolver.Stop();
            stopWatchDispatcherTimer.Stop();
            logBox1.ScrollToSpecificLogEvent(logBox1.LogEvents.Last());
        }

        //**********************************************************************************************************************************************************************************************

        private void btn_stop_solving_Click(object sender, RoutedEventArgs e)
        {
            if (cancelTokenSource != null) { cancelTokenSource.Cancel(); }
        }

        //**********************************************************************************************************************************************************************************************

        private void btn_info_Click(object sender, RoutedEventArgs e)
        {
            AssemblyInfoHelper_WPF.WindowAssemblyInfo windowAssemblyInfo = new AssemblyInfoHelper_WPF.WindowAssemblyInfo();
            windowAssemblyInfo.ShowDialog();
        }

    }
}
