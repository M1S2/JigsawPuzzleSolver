using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using JigsawPuzzleSolver.Plugins.Core;

namespace JigsawPuzzleSolver.GUI_Elements
{
    /// <summary>
    /// Interaction logic for SolverControl.xaml
    /// </summary>
    public partial class SolverControl : UserControl, INotifyPropertyChanged
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

        public static readonly DependencyProperty PuzzleHandleDependencyProperty = DependencyProperty.Register("PuzzleHandle", typeof(Puzzle), typeof(SolverControl));
        public Puzzle PuzzleHandle
        {
            get { return (Puzzle)GetValue(PuzzleHandleDependencyProperty); }
            set { SetValue(PuzzleHandleDependencyProperty, value); }
        }

        public static readonly DependencyProperty PuzzleSavingStateDependencyProperty = DependencyProperty.Register("PuzzleSavingState", typeof(PuzzleSavingStates), typeof(SolverControl));
        public PuzzleSavingStates PuzzleSavingState
        {
            get { return (PuzzleSavingStates)GetValue(PuzzleSavingStateDependencyProperty); }
            set { SetValue(PuzzleSavingStateDependencyProperty, value); }
        }

        public static readonly DependencyProperty ScrollLogEntriesDependencyProperty = DependencyProperty.Register("ScrollLogEntries", typeof(bool), typeof(SolverControl));
        public bool ScrollLogEntries
        {
            get { return (bool)GetValue(ScrollLogEntriesDependencyProperty); }
            set { SetValue(ScrollLogEntriesDependencyProperty, value); }
        }

        //##############################################################################################################################################################################################

        private Stopwatch _stopWatchSolver;
        public Stopwatch StopWatchSolver
        {
            get { return _stopWatchSolver; }
            set { _stopWatchSolver = value; OnPropertyChanged(); }
        }

        private DispatcherTimer stopWatchDispatcherTimer;       // This timer is used to notify the GUI that the StopWatchSolver.Elapsed property has changed
        private CancellationTokenSource cancelTokenSource;
        private Task workerTask;

        //##############################################################################################################################################################################################

        private ICommand _startSolvingCommand;
        public ICommand StartSolvingCommand
        {
            get
            {
                if (_startSolvingCommand == null) { _startSolvingCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => this.StartSolving(), param => { return (PuzzleHandle?.IsSolverRunning == false && PuzzleSavingState != PuzzleSavingStates.SAVING && PuzzleSavingState != PuzzleSavingStates.LOADING); }); }
                return _startSolvingCommand;
            }
        }

        private ICommand _stopSolvingCommand;
        public ICommand StopSolvingCommand
        {
            get
            {
                if (_stopSolvingCommand == null) { _stopSolvingCommand = new JigsawPuzzleSolver.GUI_Elements.RelayCommand(param => { cancelTokenSource?.Cancel(); CommandManager.InvalidateRequerySuggested(); }, param => { return (PuzzleHandle?.IsSolverRunning == true && PuzzleSavingState != PuzzleSavingStates.SAVING && PuzzleSavingState != PuzzleSavingStates.LOADING); }); }
                return _stopSolvingCommand;
            }
        }

        //##############################################################################################################################################################################################

        public SolverControl()
        {
            InitializeComponent();
            this.Loaded += SolverControl_Loaded;

            StopWatchSolver = new Stopwatch();
            stopWatchDispatcherTimer = new DispatcherTimer();
            stopWatchDispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 10);
            stopWatchDispatcherTimer.Tick += new EventHandler((obj, e) => { this.OnPropertyChanged("StopWatchSolver"); PuzzleHandle.SolverElapsedTime = new TimeSpan(StopWatchSolver.Elapsed.Ticks); });
        }

        //**********************************************************************************************************************************************************************************************

        void SolverControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            if (window != null) { window.Closed += window_Closed; ; }
        }

        async void window_Closed(object sender, EventArgs e)
        {
            if (cancelTokenSource != null)
            {
                cancelTokenSource.Cancel();
                if(workerTask != null && !workerTask.IsCompleted)
                {
                    ((Window)sender).Hide();
                    //e.Cancel = true;
                    try
                    {
                        await workerTask;
                    }
                    catch (OperationCanceledException) { /* Nothing to do here because we expect the task to cancel */ }
                    cancelTokenSource = null;
                    ((Window)sender).Close();
                }
            }
        }

        //**********************************************************************************************************************************************************************************************

        private async void StartSolving()
        {
            if (PuzzleHandle == null) { return; }

            ScrollLogEntries = false;
            cancelTokenSource = new CancellationTokenSource();
            PuzzleHandle.SetCancelToken(cancelTokenSource.Token);
            PluginFactory.CancelToken = cancelTokenSource.Token;

            stopWatchDispatcherTimer.Start();
            StopWatchSolver.Restart();
            try
            {
                workerTask = PuzzleHandle.Init();
                await workerTask;
                if (PuzzleHandle.CurrentSolverState != PuzzleSolverState.ERROR)
                {
                    workerTask = PuzzleHandle.Solve();
                    await workerTask;
                }
            }
            catch (Exception) { /* the exceptions are catches inside the methods */ }
            StopWatchSolver.Stop();
            stopWatchDispatcherTimer.Stop();
            CommandManager.InvalidateRequerySuggested();
            ScrollLogEntries = true;
        }
    }
}
