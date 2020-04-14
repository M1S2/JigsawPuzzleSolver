using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System.Windows.Data;
using System.Runtime.Serialization;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;
using LogBox.LogEvents;
using ImageGallery.LocalDriveBitmaps;
using JigsawPuzzleSolver.Plugins;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Class that is used to extract all pieces from a set of images, store the pieces and solve the puzzle.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/puzzle.cpp
    [DataContract]
    public class Puzzle : SaveableObject<Puzzle>, INotifyPropertyChanged
    {
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

        public bool Solved
        {
            get { return CurrentSolverState == PuzzleSolverState.SOLVED; }
        }

        private PuzzleSolverState _currentSolverState;
        [DataMember]
        public PuzzleSolverState CurrentSolverState
        {
            get { return _currentSolverState; }
            private set
            {
                _currentSolverState = value;
                OnPropertyChanged();
                OnPropertyChanged("PercentageOfFinishedSolverSteps");
                OnPropertyChanged("NumberOfFinishedSolverSteps");
                OnPropertyChanged("IsSolverRunning");
                OnPropertyChanged("Solved");
            }
        }

        public int PercentageOfFinishedSolverSteps
        {
            get { return (int)((NumberOfFinishedSolverSteps / (double)NumberOfSolverSteps) * 100); }
        }

        public int NumberOfFinishedSolverSteps
        {
            get { return ((int)CurrentSolverState < 0 ? 0 : (int)CurrentSolverState); }
        }

        public int NumberOfSolverSteps
        {
            get { return Enum.GetNames(typeof(PuzzleSolverState)).Count() - 3; }        // -2 because "SOLVED", "UNSOLVED" and "ERROR" aren't real solver steps
        }

        public bool IsSolverRunning
        {
            get { return (CurrentSolverState != PuzzleSolverState.SOLVED && CurrentSolverState != PuzzleSolverState.UNSOLVED && CurrentSolverState != PuzzleSolverState.ERROR); }
        }

        private double _currentSolverStepPercentageFinished;
        [DataMember]
        public double CurrentSolverStepPercentageFinished
        {
            get { return _currentSolverStepPercentageFinished; }
            private set { _currentSolverStepPercentageFinished = value; OnPropertyChanged(); }
        }

        private TimeSpan _solverElapsedTime;
        [DataMember]
        public TimeSpan SolverElapsedTime
        {
            get { return _solverElapsedTime; }
            set { _solverElapsedTime = value; OnPropertyChanged(); }
        }

        private int _numberPuzzlePieces;
        [DataMember]
        public int NumberPuzzlePieces
        {
            get { return _numberPuzzlePieces; }
            private set { _numberPuzzlePieces = value; OnPropertyChanged(); }
        }

        private string _puzzlePiecesFolderPath;
        [DataMember]
        public string PuzzlePiecesFolderPath
        {
            get { return _puzzlePiecesFolderPath; }
            private set { _puzzlePiecesFolderPath = value; OnPropertyChanged(); }
        }

        private string _puzzleXMLOutputPath;
        [DataMember]
        public string PuzzleXMLOutputPath
        {
            get { return _puzzleXMLOutputPath; }
            set { _puzzleXMLOutputPath = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private static object _puzzleSolutionImgListLock = new object();
        private ObservableCollection<ImageDescribedLight> _puzzleSolutionImages;
        [DataMember]
        public ObservableCollection<ImageDescribedLight> PuzzleSolutionImages
        {
            get { return _puzzleSolutionImages; }
            private set { _puzzleSolutionImages = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private static object _inputImgListLock = new object();
        private ObservableCollection<ImageDescribedLight> _inputImages;
        [DataMember]
        public ObservableCollection<ImageDescribedLight> InputImages
        {
            get { return _inputImages; }
            private set { _inputImages = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private readonly object _piecesLock = new object();
        private ObservableCollection<Piece> _pieces;
        [DataMember]
        public ObservableCollection<Piece> Pieces
        {
            get { return _pieces; }
            private set { _pieces = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        public int NumberOfSolutions { get { return Solutions == null ? 0 : Solutions.Count; } }
        public ObservableCollection<Matrix<int>> Solutions { get; private set; }
        public ObservableCollection<Matrix<int>> SolutionsRotations { get; private set; }

        private void Solutions_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { OnPropertyChanged("Solutions"); }
        private void SolutionsRotations_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e) { OnPropertyChanged("SolutionsRotations"); }

        //**********************************************************************************************************************************************************************************************

        private int _currentSolutionNumber;
        [DataMember]
        public int CurrentSolutionNumber
        {
            get { return _currentSolutionNumber; }
            set { _currentSolutionNumber = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private int _currentSolutionPieceIndex;
        [DataMember]
        public int CurrentSolutionPieceIndex
        {
            get { return _currentSolutionPieceIndex; }
            set { _currentSolutionPieceIndex = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private int _numberJoinedPieces;
        [DataMember]
        public int NumberJoinedPieces
        {
            get { return _numberJoinedPieces; }
            set { _numberJoinedPieces = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Don't use this property. They are only for serializing and deserializing an Emgu Matrix.
        /// </summary>
        [DataMember]
        private List<int[][]> SolutionsData
        {
            get { return Solutions.Select(s => Utils.FlattenMultidimArray(s.Data)).ToList(); }
            set
            {
                if (Solutions == null) { Solutions = new ObservableCollection<Matrix<int>>(); }
                Solutions.Clear();
                foreach (int[][] solution in value) { Solutions.Add(new Matrix<int>(Utils.DeFlattenMultidimArray(solution))); }
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Don't use this property. They are only for serializing and deserializing an Emgu Matrix.
        /// </summary>
        [DataMember]
        private List<int[][]> SolutionsRotationsData
        {
            get { return SolutionsRotations.Select(s => Utils.FlattenMultidimArray(s.Data)).ToList(); }
            set
            {
                if (SolutionsRotations == null) { SolutionsRotations = new ObservableCollection<Matrix<int>>(); }
                SolutionsRotations.Clear();
                foreach (int[][] solutionRotation in value) { SolutionsRotations.Add(new Matrix<int>(Utils.DeFlattenMultidimArray(solutionRotation))); }
            }
        }

        //##############################################################################################################################################################################################

        private IProgress<LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        private List<MatchScore> matches = new List<MatchScore>();

        //##############################################################################################################################################################################################

        public Puzzle(string piecesFolderPath, IProgress<LogEvent> logHandle)
        {
            PuzzlePiecesFolderPath = Path.GetFullPath(piecesFolderPath);

            FileAttributes attr = File.GetAttributes(PuzzlePiecesFolderPath);
            if ((attr & FileAttributes.Directory) == FileAttributes.Directory)      //detect whether its a directory or file
            {
                PuzzleXMLOutputPath = PuzzlePiecesFolderPath + "\\SavedPuzzle_" + new DirectoryInfo(PuzzlePiecesFolderPath).Name + ".xml";
            }
            else
            {
                PuzzleXMLOutputPath = Path.GetDirectoryName(PuzzlePiecesFolderPath) + "\\SavedPuzzle_" + Path.GetFileNameWithoutExtension(PuzzlePiecesFolderPath) + ".xml";
            }

            _logHandle = logHandle;
            NumberPuzzlePieces = 0;
            CurrentSolverState = PuzzleSolverState.UNSOLVED;

            Solutions = new ObservableCollection<Matrix<int>>();
            Solutions.CollectionChanged += Solutions_CollectionChanged;
            SolutionsRotations = new ObservableCollection<Matrix<int>>();
            SolutionsRotations.CollectionChanged += SolutionsRotations_CollectionChanged;

            InputImages = new ObservableCollection<ImageDescribedLight>();
            BindingOperations.EnableCollectionSynchronization(InputImages, _inputImgListLock);

            PuzzleSolutionImages = new ObservableCollection<ImageDescribedLight>();
            BindingOperations.EnableCollectionSynchronization(PuzzleSolutionImages, _puzzleSolutionImgListLock);

            Pieces = new ObservableCollection<Piece>();
            BindingOperations.EnableCollectionSynchronization(Pieces, _piecesLock);
        }

        public Puzzle()
        { }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Set the cancel token
        /// </summary>
        /// <param name="newToken">New token obtained by a new CancellationTokenSource</param>
        public void SetCancelToken(CancellationToken newToken)
        {
            _cancelToken = newToken;
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Extract all pieces from the source image. 
        /// </summary>
        private void extract_pieces()
        {
            try
            {
                CurrentSolverState = PuzzleSolverState.INIT_PIECES;
                Piece.NextPieceID = 0;
                CurrentSolverStepPercentageFinished = 0;
                _logHandle.Report(new LogEventInfo("Extracting Pieces"));
                NumberPuzzlePieces = 0;

                Pieces.Clear();
                InputImages.Clear();

                List<string> imageExtensions = new List<string>() { ".jpg", ".png", ".bmp", ".tiff" };
                FileAttributes attr = File.GetAttributes(PuzzlePiecesFolderPath);
                List<FileInfo> imageFilesInfo = new List<FileInfo>();
                if (attr.HasFlag(FileAttributes.Directory))      //detect whether its a directory or file
                {
                    DirectoryInfo folderInfo = new DirectoryInfo(PuzzlePiecesFolderPath);
                    imageFilesInfo = folderInfo.GetFiles().ToList();
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(PuzzlePiecesFolderPath);
                    imageFilesInfo.Add(fileInfo);
                }

                imageFilesInfo = imageFilesInfo.Where(f => imageExtensions.Contains(f.Extension)).ToList();

                int loopCount = 0;

                ParallelOptions parallelOptions = new ParallelOptions
                {
                    CancellationToken = _cancelToken,
                    MaxDegreeOfParallelism = (PluginFactory.GetGeneralSettingsPlugin().UseParallelLoops ? Environment.ProcessorCount : 1)
                };
                //For each input image
                Parallel.For(0, imageFilesInfo.Count, parallelOptions, (i) =>
                {
                    using (Image<Rgba, byte> sourceImg = new Image<Rgba, byte>(imageFilesInfo[i].FullName)) //.LimitImageSize(1000, 1000))
                    {
                        CvInvoke.MedianBlur(sourceImg, sourceImg, 5);
                        
                        // Get the (first) enabled Plugin for input image mask generation
                        PluginGroupInputImageMask pluginInputImageMask = PluginFactory.GetEnabledPluginsOfGroupType<PluginGroupInputImageMask>().FirstOrDefault();
                        
                        using (Image<Gray, byte> mask = pluginInputImageMask.GetMask(sourceImg))
                        {
                            _logHandle.Report(new LogEventInfo("Extracting Pieces from source image " + i.ToString()));
                            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                            {
                                _logHandle.Report(new LogEventImage("Source image " + i.ToString(), sourceImg.Bitmap));
                                _logHandle.Report(new LogEventImage("Mask " + i.ToString(), mask.Bitmap));
                            }

                            CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
                            CvBlobs blobs = new CvBlobs();
                            blobDetector.Detect(mask, blobs);

                            foreach (CvBlob blob in blobs.Values.Where(b => b.BoundingBox.Width >= PluginFactory.GetGeneralSettingsPlugin().PuzzleMinPieceSize && b.BoundingBox.Height >= PluginFactory.GetGeneralSettingsPlugin().PuzzleMinPieceSize))
                            {
                                if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                                Rectangle roi = blob.BoundingBox;

                                Image<Rgba, byte> pieceSourceImg;
                                Image<Gray, byte> pieceMask;

                                try
                                {
                                    if (sourceImg.Height > roi.Height + 4 && sourceImg.Width > roi.Width + 4) { roi.Inflate(2, 2); }
                                    pieceSourceImg = sourceImg.Copy(roi);
                                    pieceMask = mask.Copy(roi);
                                }
                                catch (Exception)
                                {
                                    roi = blob.BoundingBox;
                                    pieceSourceImg = sourceImg.Copy(roi);
                                    pieceMask = mask.Copy(roi);
                                }

                                // Mask out background of piece
                                Image<Rgba, byte> pieceSourceImageForeground = new Image<Rgba, byte>(pieceSourceImg.Size);
                                CvInvoke.BitwiseOr(pieceSourceImg, pieceSourceImg, pieceSourceImageForeground, pieceMask);

                                Image<Gray, byte> pieceMaskInverted = pieceMask.Copy(pieceMask);
                                pieceMaskInverted._Not();
                                Image<Rgba, byte> background = new Image<Rgba, byte>(pieceSourceImg.Size);
                                background.SetValue(new Rgba(255, 255, 255, 0));
                                Image<Rgba, byte> pieceSourceImageBackground = new Image<Rgba, byte>(pieceSourceImg.Size);
                                CvInvoke.BitwiseOr(background, background, pieceSourceImageBackground, pieceMaskInverted);

                                Image<Rgba, byte> pieceSourceImgMasked = new Image<Rgba, byte>(pieceSourceImg.Size);
                                CvInvoke.BitwiseOr(pieceSourceImageForeground, pieceSourceImageBackground, pieceSourceImgMasked);

                                Piece p = new Piece(pieceSourceImgMasked, pieceMask, imageFilesInfo[i].FullName, roi.Location, _logHandle, _cancelToken);
                                lock (_piecesLock) { Pieces.Add(p); }

                                sourceImg.Draw(roi, new Rgba(255, 0, 0, 1), 2);
                                int baseLine = 0;
                                Size textSize = CvInvoke.GetTextSize(p.PieceID.Replace("Piece", ""), FontFace.HersheyDuplex, 3, 2, ref baseLine);
                                CvInvoke.PutText(sourceImg, p.PieceID.Replace("Piece", ""), Point.Add(roi.Location, new Size(0, textSize.Height + 10)), FontFace.HersheyDuplex, 3, new MCvScalar(255, 0, 0), 2);

                                NumberPuzzlePieces++;

                                pieceSourceImg.Dispose();
                                pieceMask.Dispose();
                                pieceSourceImageForeground.Dispose();
                                pieceMaskInverted.Dispose();
                                background.Dispose();
                                pieceSourceImageBackground.Dispose();
                                pieceSourceImgMasked.Dispose();

                                GC.Collect();
                            }

                            Interlocked.Add(ref loopCount, 1);
                            CurrentSolverStepPercentageFinished = (loopCount / (double)imageFilesInfo.Count) * 100;

                            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults) { _logHandle.Report(new LogEventImage("Source Img " + i.ToString() + " Pieces", sourceImg.Bitmap)); }
                            InputImages.Add(new ImageDescribedLight(Path.GetFileName(imageFilesInfo[i].FullName), PuzzlePiecesFolderPath + @"\Results\InputImagesMarked\" + Path.GetFileName(imageFilesInfo[i].FullName), sourceImg.Bitmap)); //sourceImg.LimitImageSize(1000, 1000).Bitmap));
                            blobs.Dispose();
                            blobDetector.Dispose();
                            GC.Collect();
                        }
                    }

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                });

                Pieces.Sort(p => ((Piece)p).PieceIndex, null);
            }
            catch (OperationCanceledException)
            {
                _logHandle.Report(new LogEventWarning("The operation was canceled. Step: " + CurrentSolverState.ToString()));
                CurrentSolverState = PuzzleSolverState.UNSOLVED;
            }
            catch (Exception ex)
            {
                _logHandle.Report(new LogEventError("The following error occured in step " + CurrentSolverState.ToString() + ":\n" + ex.Message));
                CurrentSolverState = PuzzleSolverState.ERROR;
                CurrentSolverStepPercentageFinished = 100;
            }
        }
        
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Fill the list with all match scores for all piece edge combinations
        /// </summary>
        private void compareAllEdges()
        {
            try
            {
                CurrentSolverState = PuzzleSolverState.COMPARE_EDGES;
                CurrentSolverStepPercentageFinished = 0;
                _logHandle.Report(new LogEventInfo("Comparing all edges"));

                matches.Clear();
                int no_edges = (int)Pieces.Count * 4;
                int no_compares = (no_edges * (no_edges + 1)) / 2;      // Number of loop runs of the following nested loops 
                int loop_count = 0;

                ConcurrentDictionary<int, MatchScore> matchesDict = new ConcurrentDictionary<int, MatchScore>();        // Using ConcurrentDictionary because ConcurrentList doesn't exist

                // Get the (first) enabled Plugin for edge comparison
                PluginGroupCompareEdges pluginCompareEdges = PluginFactory.GetEnabledPluginsOfGroupType<PluginGroupCompareEdges>().FirstOrDefault();

                ParallelOptions parallelOptions = new ParallelOptions
                {
                    CancellationToken = _cancelToken,
                    MaxDegreeOfParallelism = (PluginFactory.GetGeneralSettingsPlugin().UseParallelLoops ? Environment.ProcessorCount : 1)
                };
                Parallel.For(0, no_edges, parallelOptions, (i) =>
                {
                    Parallel.For(i, no_edges, parallelOptions, (j) =>
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        loop_count++;
                        if (loop_count > no_compares) { loop_count = no_compares; }
                        CurrentSolverStepPercentageFinished = (loop_count / (double)no_compares) * 100;

                        MatchScore matchScore = new MatchScore
                        {
                            PieceIndex1 = i / 4,
                            PieceIndex2 = j / 4,
                            EdgeIndex1 = i % 4,
                            EdgeIndex2 = j % 4
                        };

                        Edge edge1 = Pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1];
                        Edge edge2 = Pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2];
                        if (edge1 == null || edge2 == null) { matchScore.score = 400000000; }
                        else { matchScore.score = pluginCompareEdges.CompareEdges(edge1, edge2); }

                        if (matchScore.score <= PluginFactory.GetGeneralSettingsPlugin().PuzzleSolverKeepMatchesThreshold)  // Keep only the best matches (all scores above or equal 100000000 mean that the edges won't match)
                        {
                            matchesDict.TryAdd(matchesDict.Count, matchScore);
                        }
                    });
                });

                matches = matchesDict.Select(m => m.Value).ToList();

                matches.Sort(new MatchScoreComparer(ScoreOrders.LOWEST_FIRST)); // Sort the matches to get the best scores first. The puzzle is solved by the order of the MatchScores

                if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                {
                    foreach (MatchScore matchScore in matches)
                    {
                        Bitmap contourImg1 = Pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1].ContourImg.Bmp;
                        Bitmap contourImg2 = Pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2].ContourImg.Bmp;
                        _logHandle.Report(new LogEventImage("MatchScore " + Pieces[matchScore.PieceIndex1].PieceID + "_Edge" + (matchScore.EdgeIndex1).ToString() + " <-->" + Pieces[matchScore.PieceIndex2].PieceID + "_Edge" + (matchScore.EdgeIndex2).ToString() + " = " + matchScore.score.ToString(), Utils.Combine2ImagesHorizontal(contourImg1, contourImg2, 20)));

                        contourImg1.Dispose();
                        contourImg2.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        //##############################################################################################################################################################################################

        public async Task Init()
        {
            await Task.Run(() =>
            {
                _logHandle.Report(new LogEventInfo("Puzzle solving started."));
                extract_pieces();
            }, _cancelToken);
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Check if the joining of the pieces in the given newSet is correct
        /// </summary>
        /// <param name="newSet">Forest to check</param>
        /// <returns>true if set is valid, otherwise false</returns>
        public bool JoinValidationFunction(Forest newSet)
        {
            /* check horizontal combinations, e.g.:
             *  1 ↔ 2 ↔ 3
             *  4 ↔ 5 ↔ 6
             *  7 ↔ 8 ↔ 9    */
            for (int r = 0; r<newSet.locations.Rows; r++)
            {
                for (int c = 0; c < newSet.locations.Cols - 1; c++)
                {
                    int pl_index = newSet.locations[r, c];
                    int pr_index = newSet.locations[r, c + 1];
                    if (pl_index != -1 && pr_index != -1)
                    {
                        Edge el = Pieces[pr_index].Edges[Utils.ModuloWithNegative(0 - newSet.rotations[r, c + 1], 4)];   //Get left edge of piece right
                        Edge er = Pieces[pl_index].Edges[Utils.ModuloWithNegative(2 - newSet.rotations[r, c], 4)];       //Get right edge of piece left
                        
                        if(el.EdgeType == er.EdgeType || el.EdgeType == EdgeTypes.LINE || er.EdgeType == EdgeTypes.LINE)
                        {
                            return false;
                        }
                    }
                }
            }

            /* check vertical combinations, e.g.:
             *  1  2  3
             *  ↕  ↕  ↕
             *  4  5  6
             *  ↕  ↕  ↕
             *  7  8  9      */
            for (int c = 0; c < newSet.locations.Cols; c++)
            {
                for (int r = 0; r < newSet.locations.Rows - 1; r++)
                {
                    int pt_index = newSet.locations[r, c];
                    int pb_index = newSet.locations[r + 1, c];
                    if (pt_index != -1 && pb_index != -1)
                    {
                        Edge et = Pieces[pb_index].Edges[Utils.ModuloWithNegative(3 - newSet.rotations[r + 1, c], 4)];   //Get top edge of piece bottom
                        Edge eb = Pieces[pt_index].Edges[Utils.ModuloWithNegative(1 - newSet.rotations[r, c], 4)];       //Get bottom edge of piece top

                        if (et.EdgeType == eb.EdgeType || et.EdgeType == EdgeTypes.LINE || eb.EdgeType == EdgeTypes.LINE)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        //**********************************************************************************************************************************************************************************************

        public async Task Solve()
        {
            await Task.Run(() =>
            {
                try
                {
                    PuzzleSolutionImages.Clear();

                    compareAllEdges();

                    CurrentSolverState = PuzzleSolverState.SOLVE_PUZZLE;
                    CurrentSolverStepPercentageFinished = 0;
                    PuzzleDisjointSet p = new PuzzleDisjointSet(Pieces.Count);
                    p.JoinValidation = JoinValidationFunction;

                    _logHandle.Report(new LogEventInfo("Join Pieces"));

                    for (int i = 0; i < matches.Count; i++)
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        CurrentSolverStepPercentageFinished = (i / (double)matches.Count) * 100;
                        if (p.InOneSet()) { break; }

                        int p1 = matches[i].PieceIndex1;
                        int e1 = matches[i].EdgeIndex1;
                        int p2 = matches[i].PieceIndex2;
                        int e2 = matches[i].EdgeIndex2;

                        p.JoinSets(p1, p2, e1, e2);
                    }
                    
                    _logHandle.Report(new LogEventInfo("Possible solution found (" + p.SetCount.ToString() + " solutions)"));
                    CurrentSolverStepPercentageFinished = 100;
                    CurrentSolverState = PuzzleSolverState.SOLVED;
                    CurrentSolutionNumber = 0;
                    CurrentSolutionPieceIndex = 0;
                    int setNo = 0;
                    foreach (Forest jointSet in p.GetJointSets())
                    {
                        Matrix<int> solution = jointSet.locations;
                        Matrix<int> solution_rotations = jointSet.rotations;

                        for (int i = 0; i < solution.Size.Width; i++)
                        {
                            for (int j = 0; j < solution.Size.Height; j++)
                            {
                                int piece_number = solution[j, i];
                                if (piece_number == -1) { continue; }
                                Pieces[piece_number].Rotate(4 - solution_rotations[j, i]);

                                Pieces[piece_number].SolutionRotation = solution_rotations[j, i] * 90;
                                Pieces[piece_number].SolutionLocation = new Point(i, j);
                                Pieces[piece_number].SolutionID = setNo;
                            }
                        }
                        Solutions.Add(solution);
                        SolutionsRotations.Add(solution_rotations);

                        // Get the enabled Plugins for solution image generation
                        List<PluginGroupGenerateSolutionImage> pluginsGenerateSolutionImage = PluginFactory.GetEnabledPluginsOfGroupType<PluginGroupGenerateSolutionImage>();

                        foreach (PluginGroupGenerateSolutionImage plugin in pluginsGenerateSolutionImage)
                        {
                            PluginNameAttribute nameAttribute = plugin.GetType().GetCustomAttributes(false).Where(a => a.GetType() == typeof(PluginNameAttribute)).FirstOrDefault() as PluginNameAttribute;

                            Bitmap solutionImg = plugin.GenerateSolutionImage(solution, setNo, Pieces.ToList());
                            PuzzleSolutionImages.Add(new ImageDescribedLight("Solution #" + setNo.ToString() + " (" + nameAttribute.Name + ")", PuzzlePiecesFolderPath + @"\Results\Solutions\Solution#" + setNo.ToString() + ".png", solutionImg));

                            if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults) { _logHandle.Report(new LogEventImage("Solution #" + setNo.ToString() + " (" + nameAttribute.Name + ")", solutionImg)); }
                            solutionImg.Dispose();
                        }
                        setNo++;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logHandle.Report(new LogEventWarning("The operation was canceled. Step: " + CurrentSolverState.ToString()));
                    CurrentSolverState = PuzzleSolverState.UNSOLVED;
                }
                catch (Exception ex)
                {
                    _logHandle.Report(new LogEventError("The following error occured in step " + CurrentSolverState.ToString() + ":\n" + ex.Message));
                    CurrentSolverState = PuzzleSolverState.ERROR;
                    CurrentSolverStepPercentageFinished = 100;
                }
            }, _cancelToken);
        }

    }
}
