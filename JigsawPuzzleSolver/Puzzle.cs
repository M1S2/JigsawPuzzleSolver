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
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;
using System.Runtime.Serialization;

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

        [DataMember]
        public PuzzleSolverParameters SolverParameters { get; private set; }

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
            get { return Enum.GetNames(typeof(PuzzleSolverState)).Count() - 2; }        // -2 because "SOLVED" and "UNSOLVED" aren't real solver steps
        }

        public bool IsSolverRunning
        {
            get { return (CurrentSolverState != PuzzleSolverState.SOLVED && CurrentSolverState != PuzzleSolverState.UNSOLVED); }
        }

        private int _currentSolverStepPercentageFinished;
        [DataMember]
        public int CurrentSolverStepPercentageFinished
        {
            get { return _currentSolverStepPercentageFinished; }
            private set { _currentSolverStepPercentageFinished = value; OnPropertyChanged(); }
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

        //**********************************************************************************************************************************************************************************************

        private ObservableDictionary<string, Bitmap> _puzzleSolutionImages = new ObservableDictionary<string, Bitmap>();
        [DataMember]
        public ObservableDictionary<string, Bitmap> PuzzleSolutionImages
        {
            get { return _puzzleSolutionImages; }
            set { _puzzleSolutionImages = value; OnPropertyChanged(); }
        }

        private string _selectedSolutionImageKey;
        [DataMember]
        public string SelectedSolutionImageKey
        {
            get { return _selectedSolutionImageKey; }
            set
            {
                _selectedSolutionImageKey = value;
                OnPropertyChanged();
                if (PuzzleSolutionImages != null) { SelectedSolutionImage = PuzzleSolutionImages[_selectedSolutionImageKey]; }
            }
        }

        private Bitmap _selectedSolutionImage;
        [DataMember]
        public Bitmap SelectedSolutionImage
        {
            get { return _selectedSolutionImage; }
            set { _selectedSolutionImage = value; OnPropertyChanged(); }
        }


        public ObservableCollection<Matrix<int>> Solutions { get; private set; }
        public ObservableCollection<Matrix<int>> SolutionsRotations { get; private set; }
        [DataMember]
        public ObservableCollection<Piece> Pieces { get; private set; }

        //##############################################################################################################################################################################################

        private IProgress<LogBox.LogEvent> _logHandle;
        private CancellationToken _cancelToken;

        [DataMember]
        private List<MatchScore> matches = new List<MatchScore>();

        //##############################################################################################################################################################################################

        public Puzzle(string piecesFolderPath, PuzzleSolverParameters solverParameters, IProgress<LogBox.LogEvent> logHandle, CancellationToken cancelToken)
        {
            PuzzlePiecesFolderPath = piecesFolderPath;
            SolverParameters = solverParameters;
            _logHandle = logHandle;
            _cancelToken = cancelToken;
            NumberPuzzlePieces = 0;
            CurrentSolverState = PuzzleSolverState.UNSOLVED;

            Solutions = new ObservableCollection<Matrix<int>>();
            SolutionsRotations = new ObservableCollection<Matrix<int>>();
            Pieces = new ObservableCollection<Piece>();
        }

        public Puzzle()
        { }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the cancel token by setting it to a new token
        /// </summary>
        /// <param name="newToken">New token obtained by a new CancellationTokenSource</param>
        public void ResetCancelToken(CancellationToken newToken)
        {
            _cancelToken = newToken;
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Extract all pieces from the source image using HSV color space for segmentation. 
        /// </summary>
        private void extract_pieces_HSV_segmentation()
        {
            try
            {
                CurrentSolverState = PuzzleSolverState.INIT_PIECES;
                Piece.NextPieceID = 0;
                CurrentSolverStepPercentageFinished = 0;
                _logHandle.Report(new LogBox.LogEventInfo("Extracting Pieces"));
                NumberPuzzlePieces = 0;

                System.Windows.Application.Current.Dispatcher.Invoke(() => { Pieces.Clear(); });

                List<string> imageExtensions = new List<string>() { ".jpg", ".png", ".bmp", ".tiff" };
                FileAttributes attr = File.GetAttributes(PuzzlePiecesFolderPath);
                List<FileInfo> imageFilesInfo = new List<FileInfo>();
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)      //detect whether its a directory or file
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

                //For each input image
                for (int i = 0; i < imageFilesInfo.Count; i++)
                {
                    Image<Rgb, byte> sourceImg = CvInvoke.Imread(imageFilesInfo[i].FullName).ToImage<Rgb, byte>();
                    CvInvoke.CvtColor(sourceImg, sourceImg, ColorConversion.Bgr2Rgb);               // Images are read in BGR model (not RGB)
                    if (SolverParameters.PuzzleApplyMedianBlurFilter) { CvInvoke.MedianBlur(sourceImg, sourceImg, 5); }

                    Image<Hsv, byte> hsvSourceImg = sourceImg.Clone().Convert<Hsv, byte>();
                    Image<Gray, byte> mask = new Image<Gray, byte>(sourceImg.Size);

                    if (SolverParameters.PuzzleIsInputBackgroundWhite)
                    {
#warning White color values must be adjusted with real scanned image
                        mask = hsvSourceImg.InRange(new Hsv(0, 0, 220), new Hsv(180, 20, 255)).Not();    // white background is defined as the inner region of the top of the HSV color cylinder (hue=0...180, sat=0...20, val=220...255)
                    }
                    else
                    {
                        mask = hsvSourceImg.InRange(new Hsv(0, 0, 0), new Hsv(180, 255, 50)).Not();    // black background is defined as the whole lower base of the HSV color cylinder (hue=0...180, sat=0...255, val=0...50)
                    }

                    _logHandle.Report(new LogBox.LogEventImage("Extracting Pieces from source image " + i.ToString(), sourceImg.ToBitmap()));
                    if (SolverParameters.SolverShowDebugResults) { _logHandle.Report(new LogBox.LogEventImage("Mask " + i.ToString(), mask.ToBitmap())); }

                    CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
                    CvBlobs blobs = new CvBlobs();
                    blobDetector.Detect(mask, blobs);

                    Image<Rgb, byte> sourceImgPiecesMarked = sourceImg.Copy();

                    foreach (CvBlob blob in blobs.Values.Where(b => b.BoundingBox.Width >= SolverParameters.PuzzleMinPieceSize && b.BoundingBox.Height >= SolverParameters.PuzzleMinPieceSize))
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        Rectangle roi = blob.BoundingBox;
                        sourceImgPiecesMarked.Draw(roi, new Rgb(255, 0, 0), 2);

                        Image<Rgb, byte> pieceSourceImg = new Image<Rgb, byte>(sourceImg.Size);
                        Image<Gray, byte> pieceMask = new Image<Gray, byte>(mask.Size);

                        try
                        {
                            if (sourceImg.Height > roi.Height + 2 && sourceImg.Width > roi.Width + 2) { roi.Inflate(1, 1); }
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
                        Image<Rgb, byte> pieceSourceImageForeground = new Image<Rgb, byte>(pieceSourceImg.Size);
                        CvInvoke.BitwiseOr(pieceSourceImg, pieceSourceImg, pieceSourceImageForeground, pieceMask);

                        Image<Gray, byte> pieceMaskInverted = pieceMask.Copy(pieceMask);
                        pieceMaskInverted._Not();
                        Image<Rgb, byte> background = new Image<Rgb, byte>(pieceSourceImg.Size);
                        background.SetValue(new Rgb(255, 255, 255));
                        Image<Rgb, byte> pieceSourceImageBackground = new Image<Rgb, byte>(pieceSourceImg.Size);
                        CvInvoke.BitwiseOr(background, background, pieceSourceImageBackground, pieceMaskInverted);

                        Image<Rgb, byte> pieceSourceImgMasked = new Image<Rgb, byte>(pieceSourceImg.Size);
                        CvInvoke.BitwiseOr(pieceSourceImageForeground, pieceSourceImageBackground, pieceSourceImgMasked);
                        
                        Piece p = new Piece(pieceSourceImgMasked, pieceMask, imageFilesInfo[i].FullName, SolverParameters, _logHandle, _cancelToken);
                        System.Windows.Application.Current.Dispatcher.Invoke(() => { Pieces.Add(p); });
                        NumberPuzzlePieces++;
                    }

                    CurrentSolverStepPercentageFinished = (int)(((i + 1) / (double)imageFilesInfo.Count) * 100);

                    _logHandle.Report(new LogBox.LogEventImage("Source Img " + i.ToString() + " Pieces", sourceImgPiecesMarked.ToBitmap()));
                }
            }
            catch (OperationCanceledException)
            {
                _logHandle.Report(new LogBox.LogEventWarning("The operation was canceled. Step: " + CurrentSolverState.ToString()));
                CurrentSolverState = PuzzleSolverState.UNSOLVED;
            }
            catch (Exception ex)
            {
                _logHandle.Report(new LogBox.LogEventError("The following error occured in step " + CurrentSolverState.ToString() + ":\n" + ex.Message));
                CurrentSolverState = PuzzleSolverState.UNSOLVED;
            }
        }

        //**********************************************************************************************************************************************************************************************

        /*
        /// <summary>
        /// Create an image for all pieces with all edges drawn 
        /// </summary>
        private void print_edges()
        {
            for (int i = 0; i < pieces.Count; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    Mat m = Mat.Zeros(500, 500, DepthType.Cv8U, 1);

                    VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                    VectorOfPoint contour = pieces[i].Edges[j].GetTranslatedContour(200, 0);
                    if(contour.Size == 0) { continue; }
                    contours.Push(contour);

                    CvInvoke.DrawContours(m, contours, -1, new MCvScalar(255), 2);
                    CvInvoke.PutText(m, pieces[i].Edges[j].EdgeType.ToString(), new Point(300, 300), FontFace.HersheyComplexSmall, 2, new MCvScalar(255), 1, LineType.AntiAlias);

                    _logHandle.Report(new LogBox.LogEventImage("Contour " + pieces[i].PieceID + " Edge " + j.ToString(), m.Bitmap));
                }
            }
        }
        */
        //**********************************************************************************************************************************************************************************************

#warning Tried to use parallel loops but seems to be slower ?!
        //ConcurrentDictionary<int, MatchScore> matchesDict = new ConcurrentDictionary<int, MatchScore>();        // Using ConcurrentDictionary because ConcurrentList doesn't exist
        //ConcurrentDictionary<int, int> threadDict = new ConcurrentDictionary<int, int>();           // <ThreadID, NumberExecutedLoops>
        //ParallelOptions parallelOptions = new ParallelOptions();
        //parallelOptions.CancellationToken = _cancelToken;
        //parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
        //Parallel.For(0, no_edges, parallelOptions, (i) => { });
        //if (!threadDict.Keys.Contains(Thread.CurrentThread.ManagedThreadId)) { threadDict.TryAdd(Thread.CurrentThread.ManagedThreadId, 0); }
        //else { threadDict[Thread.CurrentThread.ManagedThreadId]++; }

        /// <summary>
        /// Fill the list with all match scores for all piece edge combinations
        /// </summary>
        private void compareAllEdges()
        {
            try
            {
                CurrentSolverState = PuzzleSolverState.COMPARE_EDGES;
                CurrentSolverStepPercentageFinished = 0;
                _logHandle.Report(new LogBox.LogEventInfo("Comparing all edges"));

                matches.Clear();
                int no_edges = (int)Pieces.Count * 4;
                int no_compares = (no_edges * (no_edges + 1)) / 2;      // Number of loop runs of the following nested loops 
                int loop_count = 0;

                for (int i = 0; i < no_edges; i++)
                {
                    for (int j = i; j < no_edges; j++)
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }
                        
                        loop_count++;
                        if (loop_count > no_compares) { loop_count = no_compares; }
                        CurrentSolverStepPercentageFinished = (int)((loop_count / (double)no_compares) * 100);

                        MatchScore matchScore = new MatchScore();
                        matchScore.PieceIndex1 = i / 4;
                        matchScore.PieceIndex2 = j / 4;
                        matchScore.EdgeIndex1 = i % 4;
                        matchScore.EdgeIndex2 = j % 4;
                        matchScore.score = Pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1].Compare(Pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2]);

                        if (matchScore.score <= SolverParameters.PuzzleSolverKeepMatchesThreshold)  // Keep only the best matches (all scores above or equal 100000000 mean that the edges won't match)
                        {
                            matches.Add(matchScore);
                        }
                    }
                }
                
                matches.Sort(new MatchScoreComparer(ScoreOrders.LOWEST_FIRST)); // Sort the matches to get the best scores first. The puzzle is solved by the order of the MatchScores

                if (SolverParameters.SolverShowDebugResults)
                {
                    foreach (MatchScore matchScore in matches)
                    {
                        _logHandle.Report(new LogBox.LogEventImage("MatchScore " + Pieces[matchScore.PieceIndex1].PieceID + "_Edge" + (matchScore.EdgeIndex1).ToString() + " <-->" + Pieces[matchScore.PieceIndex2].PieceID + "_Edge" + (matchScore.EdgeIndex2).ToString() + " = " + matchScore.score.ToString(), Utils.Combine2ImagesHorizontal(Pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1].ContourImg, Pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2].ContourImg, 20).ToBitmap()));
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                throw ex;
            }
        }

        //##############################################################################################################################################################################################

        public async Task Init()
        {
            await Task.Run(() =>
            {
                extract_pieces_HSV_segmentation();
            }, _cancelToken);
        }

        //**********************************************************************************************************************************************************************************************

        public async Task Solve()
        {
            await Task.Run(() =>
            {
                try
                {
                    compareAllEdges();

                    CurrentSolverState = PuzzleSolverState.SOLVE_PUZZLE;
                    CurrentSolverStepPercentageFinished = 0;
                    PuzzleDisjointSet p = new PuzzleDisjointSet(Pieces.Count);

                    _logHandle.Report(new LogBox.LogEventInfo("Join Pieces"));

                    for (int i = 0; i < matches.Count; i++)
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        CurrentSolverStepPercentageFinished = (int)((i / (double)matches.Count) * 100);
                        if (p.InOneSet()) { break; }

                        int p1 = matches[i].PieceIndex1;
                        int e1 = matches[i].EdgeIndex1;
                        int p2 = matches[i].PieceIndex2;
                        int e2 = matches[i].EdgeIndex2;

                        p.JoinSets(p1, p2, e1, e2);
                    }

                    _logHandle.Report(new LogBox.LogEventInfo("Possible solution found " + (p.InOneSet() ? "(one set)." : "(" + p.SetCount.ToString() + " sets)")));
                    CurrentSolverStepPercentageFinished = 100;
                    CurrentSolverState = PuzzleSolverState.SOLVED;
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
                                Pieces[piece_number].SolutionID = "Solution #" + setNo;
                            }
                        }
                        Solutions.Add(solution);
                        SolutionsRotations.Add(solution_rotations);

                        Bitmap solutionImg = GenerateSolutionImage2(solution, setNo);
                        if(PuzzleSolutionImages == null) { PuzzleSolutionImages = new ObservableDictionary<string, Bitmap>(); }

                        System.Windows.Application.Current.Dispatcher.Invoke(() => { PuzzleSolutionImages.Add("Solution #" + setNo.ToString(), solutionImg); });

                        _logHandle.Report(new LogBox.LogEventImage("Solution #" + setNo.ToString(), solutionImg));
                        setNo++;
                    }
                    SelectedSolutionImageKey = PuzzleSolutionImages.Keys.First();
                }
                catch (OperationCanceledException)
                {
                    _logHandle.Report(new LogBox.LogEventWarning("The operation was canceled. Step: " + CurrentSolverState.ToString()));
                    CurrentSolverState = PuzzleSolverState.UNSOLVED;
                }
                catch (Exception ex)
                {
                    _logHandle.Report(new LogBox.LogEventError("The following error occured in step " + CurrentSolverState.ToString() + ":\n" + ex.Message));
                    CurrentSolverState = PuzzleSolverState.UNSOLVED;
                }
            }, _cancelToken);
        }

        //**********************************************************************************************************************************************************************************************

        #region Generate Solution Image

        private Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        { 
            if (!Solved) { return null; }

            int border = 10;
            float out_image_width = 0, out_image_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    float piece_size_x = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(3));
                    float piece_size_y = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(1));

                    out_image_width += piece_size_x;
                    out_image_height += piece_size_y;
                }
            }
            out_image_width = (out_image_width / solutionLocations.Size.Height) * 1.5f + border;
            out_image_height = (out_image_height / solutionLocations.Size.Width) * 1.5f + border;
            
            // Use get affine to map points...
            Image<Rgb, byte> final_out_image = new Image<Rgb, byte>((int)out_image_width, (int)out_image_height);
            
            PointF[,] points = new PointF[solutionLocations.Size.Width + 1, solutionLocations.Size.Height +1];
            bool failed = false;
            
            for (int i = 0; i < solutionLocations.Size.Width; i++)
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];

                    if (piece_number == -1)
                    {
                        failed = true;
                        break;
                    }
                    float piece_size_x = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(3));
                    float piece_size_y = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(1));
                    VectorOfPointF src = new VectorOfPointF();
                    VectorOfPointF dst = new VectorOfPointF();

                    if (i == 0 && j == 0)
                    {
                        points[i, j] = new PointF(border, border);
                    }
                    if (i == 0)
                    {
                        points[i, j + 1] = new PointF(border, points[i, j].Y + border + piece_size_y); //new PointF(points[i, j].X + border + x_dist, border);
                    }
                    if (j == 0)
                    {
                        points[i + 1, j] = new PointF(points[i, j].X + border + piece_size_x, border); //new PointF(border, points[i, j].Y + border + y_dist);
                    }

                    dst.Push(points[i, j]);
                    //dst.Push(points[i + 1, j]);
                    //dst.Push(points[i, j + 1]);
                    dst.Push(points[i, j + 1]);
                    dst.Push(points[i + 1, j]);
                    src.Push(Pieces[piece_number].GetCorner(0));
                    src.Push(Pieces[piece_number].GetCorner(1));
                    src.Push(Pieces[piece_number].GetCorner(3));

                    //true means use affine transform
                    Mat a_trans_mat = CvInvoke.EstimateRigidTransform(src, dst, true);

                    Matrix<double> A = new Matrix<double>(a_trans_mat.Rows, a_trans_mat.Cols);
                    a_trans_mat.CopyTo(A);
                    
                    PointF l_r_c = Pieces[piece_number].GetCorner(2);       //Lower right corner of each piece

                    //Doing my own matrix multiplication
                    points[i + 1, j + 1] = new PointF((float)(A[0, 0] * l_r_c.X + A[0, 1] * l_r_c.Y + A[0, 2]), (float)(A[1, 0] * l_r_c.X + A[1, 1] * l_r_c.Y + A[1, 2]));

                    Mat layer = new Mat();
                    Mat layer_mask = new Mat();

                    CvInvoke.WarpAffine(Pieces[piece_number].PieceImgColor, layer, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Linear, Warp.Default, BorderType.Transparent);
                    CvInvoke.WarpAffine(Pieces[piece_number].PieceImgBw, layer_mask, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Nearest, Warp.Default, BorderType.Transparent);

                    layer.CopyTo(final_out_image, layer_mask);
                }

                if (failed)
                {
                    _logHandle.Report(new LogBox.LogEventError("Failed to generate solution " + solutionID + " image. Only partial image generated."));
                    break;
                }
            }

            return final_out_image.Clone().Bitmap;
        }

        //**********************************************************************************************************************************************************************************************

        private Bitmap GenerateSolutionImage2(Matrix<int> solutionLocations, int solutionID)
        {
#warning Add PieceIDs to solution image !!!

            if (!Solved) { return null; }
            
            int out_image_width = 0, out_image_height = 0;
            int max_piece_width = 0, max_piece_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    max_piece_width = Math.Max(max_piece_width, Pieces[piece_number].PieceImgColor.Width);
                    max_piece_height = Math.Max(max_piece_height, Pieces[piece_number].PieceImgColor.Height);
                }
            }
            max_piece_height += 150;
            out_image_width = max_piece_width * solutionLocations.Size.Width;
            out_image_height = max_piece_height * solutionLocations.Size.Height;

            Bitmap outImg = new Bitmap(out_image_width, out_image_height);
            Graphics g = Graphics.FromImage(outImg);
            g.Clear(Color.White);

            for (int i = 0; i < solutionLocations.Size.Width; i++)
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    g.DrawImage(Pieces[piece_number].PieceImgColor.Bitmap, i * max_piece_width, j * max_piece_height + 150);
                    Rectangle pieceRect = new Rectangle(i * max_piece_width, j * max_piece_height, max_piece_width, max_piece_height);
                    g.DrawRectangle(new Pen(Color.Red, 4), pieceRect);
                    StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };
                    g.DrawString(Pieces[piece_number].PieceID + Environment.NewLine + Path.GetFileName(Pieces[piece_number].PieceSourceFileName), new Font("Arial", 40), new SolidBrush(Color.Blue), pieceRect, stringFormat);
                }
            }

            return outImg;
        }

        #endregion

    }
}
