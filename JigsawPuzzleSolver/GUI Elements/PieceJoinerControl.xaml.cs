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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Drawing;

namespace JigsawPuzzleSolver.GUI_Elements
{
    /// <summary>
    /// Interaction logic for PieceJoinerControl.xaml
    /// </summary>
    public partial class PieceJoinerControl : UserControl, INotifyPropertyChanged
    {
        #region INotifyPropertyChanged implementation
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
        #endregion

        //##############################################################################################################################################################################################

        public static readonly DependencyProperty PuzzleHandleDependencyProperty = DependencyProperty.Register("PuzzleHandle", typeof(Puzzle), typeof(PieceJoinerControl), new PropertyMetadata(OnPuzzleHandleChanged));
        public Puzzle PuzzleHandle
        {
            get { return (Puzzle)GetValue(PuzzleHandleDependencyProperty); }
            set { SetValue(PuzzleHandleDependencyProperty, value); }
        }

        public static void OnPuzzleHandleChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            PieceJoinerControl c = sender as PieceJoinerControl;
            c.PuzzleHandle.PropertyChanged += c.PuzzleHandle_PropertyChanged;
            c.RecalculatePieceJoiningOrder();
            c.OnPropertyChanged("PercentageJoiningFinished");
        }

        private void PuzzleHandle_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!this.Dispatcher.CheckAccess())
            {
                this.Dispatcher.Invoke(new Action<object, PropertyChangedEventArgs>(PuzzleHandle_PropertyChanged), sender, e);
                return;
            }

            switch(e.PropertyName)
            {
                case "Solutions": RecalculatePieceJoiningOrder(); break;
                case "NumberJoinedPieces": OnPropertyChanged("PercentageJoiningFinished"); break;
                case "CurrentSolutionNumber":
                case "CurrentSolutionPieceIndex":
                    OnPropertyChanged("CurrentPiece");
                    break;
            }
        }

        //**********************************************************************************************************************************************************************************************

        public int NumberPiecesInSolution
        {
            get { return PuzzleHandle.Pieces.Where(p => p.SolutionID == PuzzleHandle.CurrentSolutionNumber).Count(); }
        }

        public int NumberSolutions
        {
            get { return ((PuzzleHandle == null || PuzzleHandle.Solutions == null) ? 0 : PuzzleHandle.Solutions.Count); }
        }
        
        private Piece _currentPiece;
        public Piece CurrentPiece
        {
            get { return _currentPiece; }
            set
            {
                if (CurrentPiece != value)
                {
                    PreviousPiece = _currentPiece;
                    _currentPiece = value;
                    OnPropertyChanged();
                    OnPropertyChanged("PreviousToCurrentPieceDistance");
                    OnPropertyChanged("CurrentPieceSourceImage");
                    GetSurroundingPieces();
                }
            }
        }
        
        public Bitmap CurrentPieceSourceImage
        {
            get
            {
                if(PuzzleHandle == null || PuzzleHandle.InputImages == null || PuzzleHandle.InputImages.Count == 0 || CurrentPiece == null) { return null; }

                Bitmap srcImg = (Bitmap)PuzzleHandle?.InputImages.Where(p => p.Description == System.IO.Path.GetFileName(CurrentPiece?.PieceSourceFileName))?.First().Img.Clone();
                if(srcImg != null)
                {
                    using (Graphics srcGraphics = Graphics.FromImage(srcImg))
                    {
                        srcGraphics.DrawRectangle(new System.Drawing.Pen(System.Drawing.Color.Lime, 10), new System.Drawing.Rectangle(CurrentPiece.PieceSourceFileLocation, CurrentPiece.PieceSize));
                    }
                }
                return srcImg;
            }
        }

        private Piece _previousPiece;
        public Piece PreviousPiece
        {
            get { return _previousPiece; }
            private set { _previousPiece = value; OnPropertyChanged(); OnPropertyChanged("PreviousToCurrentPieceDistance"); }
        }

        public double PercentageJoiningFinished
        {
            get { return ((PuzzleHandle == null || PuzzleHandle.Pieces == null) ? 0 : ((PuzzleHandle.NumberJoinedPieces / (double)(PuzzleHandle?.Pieces.Count - 1)) * 100)); }
        }
        
        private ObservableCollection<Piece> _surroundingPieces;
        /// <summary>
        /// A list that is containing the pieces that are surrounding the current piece. [LeftTop, Top, RightTop, Left, Middle (current piece), Right, LeftBottom, Bottom, RightBottom]
        /// </summary>
        public ObservableCollection<Piece> SurroundingPieces
        {
            get { return _surroundingPieces; }
            private set { _surroundingPieces = value; OnPropertyChanged(); }
        }

        public System.Drawing.Point PreviousToCurrentPieceDistance
        {
            get
            {
                if (CurrentPiece == null || PreviousPiece == null) { return System.Drawing.Point.Empty; }
                else { return System.Drawing.Point.Subtract(CurrentPiece.SolutionLocation, new System.Drawing.Size(PreviousPiece.SolutionLocation)); }
            }
        }

        //**********************************************************************************************************************************************************************************************

        private Dictionary<int, List<Piece>> OrderedPieces { get; set; }

        //##############################################################################################################################################################################################

        private ICommand _previousPieceCommand;
        public ICommand PreviousPieceCommand
        {
            get
            {
                if (_previousPieceCommand == null) { _previousPieceCommand = new RelayCommand(param => JoinPreviousPiece(), param => { return (PuzzleHandle?.IsSolverRunning == false && (PuzzleHandle.CurrentSolutionPieceIndex > 0 || PuzzleHandle.CurrentSolutionNumber > 0)); }); }
                return _previousPieceCommand;
            }
        }

        private ICommand _nextPieceCommand;
        public ICommand NextPieceCommand
        {
            get
            {
                if (_nextPieceCommand == null) { _nextPieceCommand = new RelayCommand(param => JoinNextPiece(), param => { return (PuzzleHandle?.IsSolverRunning == false && (PuzzleHandle.CurrentSolutionPieceIndex < NumberPiecesInSolution - 1 || PuzzleHandle.CurrentSolutionNumber < NumberSolutions - 1)); }); }
                return _nextPieceCommand;
            }
        }

        //##############################################################################################################################################################################################

        public PieceJoinerControl()
        {
            InitializeComponent();
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// This function is called whenever a new solution is added to the Puzzle. It orders the pieces in the solution by their placement order and adds the ordered pieces to the OrderedPieces dictionary.
        /// </summary>
        private void RecalculatePieceJoiningOrder()
        {
            OrderedPieces = new Dictionary<int, List<Piece>>();
            if(NumberSolutions == 0)
            {
                CurrentPiece = null;
                return;
            }

            for (int numSolution = 0; numSolution < NumberSolutions; numSolution++)
            {
                List<Piece> orderedSolutionPieces = new List<Piece>();
                for (int i = 0; i < PuzzleHandle.Solutions[numSolution].Rows; i++)
                {
                    for (int j = 0; j < PuzzleHandle.Solutions[numSolution].Cols; j++)
                    {
                        int pieceNumber = PuzzleHandle.Solutions[numSolution][i, j];
                        if (pieceNumber == -1) { continue; }
                        orderedSolutionPieces.Add(PuzzleHandle.Pieces[pieceNumber]);
                    }
                }

                if (OrderedPieces.ContainsKey(numSolution)) { OrderedPieces[numSolution] = orderedSolutionPieces; }
                else { OrderedPieces.Add(numSolution, orderedSolutionPieces); }
            }
            CurrentPiece = OrderedPieces[PuzzleHandle.CurrentSolutionNumber][PuzzleHandle.CurrentSolutionPieceIndex];
        }

        //**********************************************************************************************************************************************************************************************

        private void GetSurroundingPieces()
        {
            ObservableCollection<Piece> surroundingPieces = new ObservableCollection<Piece>();
            if(CurrentPiece == null) { SurroundingPieces = null; return; }

            for (int y = CurrentPiece.SolutionLocation.Y - 1; y <= (CurrentPiece.SolutionLocation.Y + 1); y++)
            {
                for (int x = CurrentPiece.SolutionLocation.X - 1; x <= (CurrentPiece.SolutionLocation.X + 1); x++)
                {
                    if (x < 0 || y < 0 || x >= PuzzleHandle.Solutions[PuzzleHandle.CurrentSolutionNumber].Cols || y >= PuzzleHandle.Solutions[PuzzleHandle.CurrentSolutionNumber].Rows)
                    {
                        surroundingPieces.Add(null);
                        continue;
                    }

                    int piece_number = PuzzleHandle.Solutions[PuzzleHandle.CurrentSolutionNumber][y, x];
                    if (piece_number == -1) { surroundingPieces.Add(null); continue; }

                    surroundingPieces.Add(PuzzleHandle.Pieces[piece_number]);
                }
            }
            SurroundingPieces = surroundingPieces;
        }

        //**********************************************************************************************************************************************************************************************

        private void JoinPreviousPiece()
        {
            bool isPreviousSolution = false;
            if (PuzzleHandle.CurrentSolutionPieceIndex > 0)
            {
                PuzzleHandle.CurrentSolutionPieceIndex--;
            }
            else if (PuzzleHandle.CurrentSolutionNumber > 0)
            {
                PuzzleHandle.CurrentSolutionNumber--;
                PuzzleHandle.CurrentSolutionPieceIndex = NumberPiecesInSolution - 1;
                isPreviousSolution = true;
            }
            CurrentPiece = OrderedPieces[PuzzleHandle.CurrentSolutionNumber][PuzzleHandle.CurrentSolutionPieceIndex];
            if (isPreviousSolution) { PreviousPiece = null; }
            PuzzleHandle.NumberJoinedPieces--;
        }

        private void JoinNextPiece()
        {
            bool isNextSolution = false;
            if (PuzzleHandle.CurrentSolutionPieceIndex < NumberPiecesInSolution - 1)
            {
                PuzzleHandle.CurrentSolutionPieceIndex++;
            }
            else if(PuzzleHandle.CurrentSolutionNumber < NumberSolutions - 1)
            {
                PuzzleHandle.CurrentSolutionPieceIndex = 0;
                PuzzleHandle.CurrentSolutionNumber++;
                isNextSolution = true;
            }
            CurrentPiece = OrderedPieces[PuzzleHandle.CurrentSolutionNumber][PuzzleHandle.CurrentSolutionPieceIndex];
            if (isNextSolution) { PreviousPiece = null; }
            PuzzleHandle.NumberJoinedPieces++;
        }

    }
}
