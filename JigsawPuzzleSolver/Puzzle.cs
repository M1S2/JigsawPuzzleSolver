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
        private ObservableCollection<ImageGallery.ImageDescribed> _puzzleSolutionImages;
        [DataMember]
        public ObservableCollection<ImageGallery.ImageDescribed> PuzzleSolutionImages
        {
            get { return _puzzleSolutionImages; }
            private set { _puzzleSolutionImages = value; OnPropertyChanged(); }
        }

        //**********************************************************************************************************************************************************************************************

        private static object _inputImgListLock = new object();
        private ObservableCollection<ImageGallery.ImageDescribed> _inputImages;
        [DataMember]
        public ObservableCollection<ImageGallery.ImageDescribed> InputImages
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
                if(Solutions == null) { Solutions = new ObservableCollection<Matrix<int>>(); }
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

            InputImages = new ObservableCollection<ImageGallery.ImageDescribed>();
            BindingOperations.EnableCollectionSynchronization(InputImages, _inputImgListLock);

            PuzzleSolutionImages = new ObservableCollection<ImageGallery.ImageDescribed>();
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
        
        /*
        /// <summary>
        /// Perform GrabCut segmentation on input image to get a mask for the pieces (foreground).
        /// </summary>
        /// <param name="inputImg">Input image for segmentation</param>
        /// <returns>Mask for foreground</returns>
        private Image<Gray, byte> getMaskGrabCut(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask = inputImg.Convert<Rgb, byte>().GrabCut(new Rectangle(1, 1, inputImg.Width - 1, inputImg.Height - 1), 2);
            mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.
            return mask;
        }*/

/*
        /// <summary>
        /// Calculate a mask for the pieces. Works with white or black background (depending on PuzzleSolverParameters.PuzzleIsInputBackgroundWhite).
        /// </summary>
        /// <param name="inputImg">Color input image</param>
        /// <returns>Mask image</returns>
        private Image<Gray, byte> getMaskHsvSegmentation(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask;

            using (Image<Hsv, byte> hsvSourceImg = inputImg.Convert<Hsv, byte>())
            {
                Image<Gray, byte> maskInverted;
                if (PuzzleSolverParameters.Instance.PuzzleIsInputBackgroundWhite)
                {
#warning White color values must be adjusted with real scanned image
                    maskInverted = hsvSourceImg.InRange(new Hsv(0, 0, 220), new Hsv(180, 20, 255));    // white background is defined as the inner region of the top of the HSV color cylinder (hue=0...180, sat=0...20, val=220...255)
                }
                else
                {
                    maskInverted = hsvSourceImg.InRange(new Hsv(0, 0, 0), new Hsv(180, 255, 50));    // black background is defined as the whole lower base of the HSV color cylinder (hue=0...180, sat=0...255, val=0...50)
                }

                mask = maskInverted.Not();
                maskInverted.Dispose();
            }
            return mask;
        }*/

        /// <summary>
        /// Calculate a mask for the pieces. The function calculates a histogram to find the piece background color. 
        /// Everything within a specific HSV range around the piece background color is regarded as foreground. The rest is regarded as background.
        /// </summary>
        /// <param name="inputImg">Color input image</param>
        /// <returns>Mask image</returns>
        /// see: https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html?highlight=calchist
        private Image<Gray, byte> getMaskHsvSegmentationHistogram(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask;

            using (Image<Hsv, byte> hsvSourceImg = inputImg.Convert<Hsv, byte>())       //Convert input image to HSV color space
            {
                Mat hsvImgMat = new Mat();
                hsvSourceImg.Mat.ConvertTo(hsvImgMat, DepthType.Cv32F);
                VectorOfMat vm = new VectorOfMat(hsvImgMat);

                // Calculate histograms for each channel of the HSV image (H, S, V)
                Mat histOutH = new Mat(), histOutS = new Mat(), histOutV = new Mat();
                int hbins = 32, sbins = 32, vbins = 32;
                CvInvoke.CalcHist(vm, new int[] { 0 }, new Mat(), histOutH, new int[] { hbins }, new float[] { 0, 179 }, false);
                CvInvoke.CalcHist(vm, new int[] { 1 }, new Mat(), histOutS, new int[] { sbins }, new float[] { 0, 255 }, false);
                CvInvoke.CalcHist(vm, new int[] { 2 }, new Mat(), histOutV, new int[] { vbins }, new float[] { 0, 255 }, false);

                hsvImgMat.Dispose();
                vm.Dispose();

                // Draw the histograms for debugging purposes
                if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
                {
                    _logHandle.Report(new LogEventImage("Hist H", Utils.DrawHist(histOutH, hbins, 30, 1024, new MCvScalar(255, 0, 0)).Bitmap));
                    _logHandle.Report(new LogEventImage("Hist S", Utils.DrawHist(histOutS, sbins, 30, 1024, new MCvScalar(0, 255, 0)).Bitmap));
                    _logHandle.Report(new LogEventImage("Hist V", Utils.DrawHist(histOutV, vbins, 30, 1024, new MCvScalar(0, 0, 255)).Bitmap));
                }

                //#warning Use border color
                //                int borderHeight = 10;
                //                Image<Hsv, byte> borderImg = hsvSourceImg.Copy(new Rectangle(0, hsvSourceImg.Height - borderHeight, hsvSourceImg.Width, borderHeight));
                //                MCvScalar meanBorderColorScalar = CvInvoke.Mean(borderImg);
                //                Hsv meanBorderColor = new Hsv(meanBorderColorScalar.V0, meanBorderColorScalar.V1, meanBorderColorScalar.V2);
                //                if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
                //                {
                //                    Image<Hsv, byte> borderColorImg = new Image<Hsv, byte>(12, 12);
                //                    borderColorImg.SetValue(meanBorderColor);
                //                    _logHandle.Report(new LogBox.LogEventImage("HSV Border Color (" + meanBorderColor.Hue + " ; " + meanBorderColor.Satuation + "; " + meanBorderColor.Value + ")", borderColorImg.Bitmap));
                //                }

#warning Make this to settings (mainHueSegment)
                int mainHueSegment = 90;    // Is the piece background rather red (0), green (60) or blue (120) ? 
                int hDiffHist = 15;
                int hDiff = 15; //20;
                int sDiff = 15; //20; //40;
                int vDiff = 15; //20; //40;

                // Find the peaks in the histograms and use them as piece background color. Black and white areas are ignored.
                Hsv pieceBackgroundColor = new Hsv();
                pieceBackgroundColor.Hue = Utils.HighestBinValInRange(histOutH, mainHueSegment - hDiffHist, mainHueSegment + hDiffHist, 179); //25, 179, 179);
                pieceBackgroundColor.Satuation = Utils.HighestBinValInRange(histOutS, 50, 205, 255); //50, 255, 255);
                pieceBackgroundColor.Value = Utils.HighestBinValInRange(histOutV, 75, 205, 255); //75, 255, 255);

                histOutH.Dispose();
                histOutS.Dispose();
                histOutV.Dispose();

                // Show the found piece background color
                if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
                {
                    Image<Hsv, byte> pieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    Image<Hsv, byte> lowPieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    Image<Hsv, byte> highPieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    pieceBgColorImg.SetValue(pieceBackgroundColor);
                    lowPieceBgColorImg.SetValue(new Hsv(pieceBackgroundColor.Hue - hDiff, pieceBackgroundColor.Satuation - sDiff, pieceBackgroundColor.Value - vDiff));
                    highPieceBgColorImg.SetValue(new Hsv(pieceBackgroundColor.Hue + hDiff, pieceBackgroundColor.Satuation + sDiff, pieceBackgroundColor.Value + vDiff));

                    _logHandle.Report(new LogEventImage("HSV Piece Bg Color (" + pieceBackgroundColor.Hue + " ; " + pieceBackgroundColor.Satuation + "; " + pieceBackgroundColor.Value + ")", Utils.Combine2ImagesHorizontal(Utils.Combine2ImagesHorizontal(lowPieceBgColorImg.Convert<Rgb, byte>(), pieceBgColorImg.Convert<Rgb, byte>(), 0), highPieceBgColorImg.Convert<Rgb, byte>(), 0).Bitmap));

                    pieceBgColorImg.Dispose();
                    lowPieceBgColorImg.Dispose();
                    highPieceBgColorImg.Dispose();
                }

                // do HSV segmentation and keep only the meanColor areas with some hysteresis as pieces
                mask = hsvSourceImg.InRange(new Hsv(pieceBackgroundColor.Hue - hDiff, pieceBackgroundColor.Satuation - sDiff, pieceBackgroundColor.Value - vDiff), new Hsv(pieceBackgroundColor.Hue + hDiff, pieceBackgroundColor.Satuation + sDiff, pieceBackgroundColor.Value + vDiff));

                // close small black gaps with morphological closing operation
                Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
                CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernel, new Point(-1, -1), 5, BorderType.Default, new MCvScalar(0));
            }
            return mask;
        }

        //**********************************************************************************************************************************************************************************************

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
                _logHandle.Report(new LogEventInfo("Extracting Pieces"));
                NumberPuzzlePieces = 0;

                Pieces.Clear();
                InputImages.Clear();

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

                int loopCount = 0;

                //For each input image
                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = _cancelToken;
                parallelOptions.MaxDegreeOfParallelism = (PuzzleSolverParameters.Instance.UseParallelLoops ? Environment.ProcessorCount : 1);
                Parallel.For(0, imageFilesInfo.Count, parallelOptions, (i) =>
                {
                    //_logHandle.Report(new LogEventInfo("!!!MEMORY: Before sourceImg read " + (System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1000000).ToString()));

                    using (Image<Rgba, byte> sourceImg = new Image<Rgba, byte>(imageFilesInfo[i].FullName)) //.LimitImageSize(1000, 1000))
                    {
                        //CvInvoke.CvtColor(sourceImg, sourceImg, ColorConversion.Bgr2Rgba);               // Images are read in BGR model (not RGB)
                        CvInvoke.MedianBlur(sourceImg, sourceImg, 5);

                        using (Image<Gray, byte> mask = getMaskHsvSegmentationHistogram(sourceImg))    //getMaskGrabCut(sourceImg))
                        {
                            _logHandle.Report(new LogEventInfo("Extracting Pieces from source image " + i.ToString()));
                            if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
                            {
                                _logHandle.Report(new LogEventImage("Source image " + i.ToString(), sourceImg.Bitmap));
                                _logHandle.Report(new LogEventImage("Mask " + i.ToString(), mask.Bitmap));
                            }

                            CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
                            CvBlobs blobs = new CvBlobs();
                            blobDetector.Detect(mask, blobs);

                            foreach (CvBlob blob in blobs.Values.Where(b => b.BoundingBox.Width >= PuzzleSolverParameters.Instance.PuzzleMinPieceSize && b.BoundingBox.Height >= PuzzleSolverParameters.Instance.PuzzleMinPieceSize))
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

                            _logHandle.Report(new LogEventImage("Source Img " + i.ToString() + " Pieces", sourceImg.Bitmap));
#warning Possible Memory Leak in ImageGallery ?!
                            InputImages.Add(new ImageGallery.ImageDescribed(Path.GetFileName(imageFilesInfo[i].FullName), (Bitmap)sourceImg.LimitImageSize(1000, 1000).Bitmap.Clone()));
                            blobs.Dispose();
                            blobDetector.Dispose();
                            GC.Collect();
                        }
                    }
                    
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    _logHandle.Report(new LogEventInfo("!!!MEMORY: After sourceImg read " + (System.Diagnostics.Process.GetCurrentProcess().PrivateMemorySize64 / 1000000).ToString()));
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

                ParallelOptions parallelOptions = new ParallelOptions();
                parallelOptions.CancellationToken = _cancelToken;
                parallelOptions.MaxDegreeOfParallelism = (PuzzleSolverParameters.Instance.UseParallelLoops ? Environment.ProcessorCount : 1);
                Parallel.For(0, no_edges, parallelOptions, (i) =>
                {
                    Parallel.For(i, no_edges, parallelOptions, (j) =>
                    {
                        if (_cancelToken.IsCancellationRequested) { _cancelToken.ThrowIfCancellationRequested(); }

                        loop_count++;
                        if (loop_count > no_compares) { loop_count = no_compares; }
                        CurrentSolverStepPercentageFinished = (loop_count / (double)no_compares) * 100;

                        MatchScore matchScore = new MatchScore();
                        matchScore.PieceIndex1 = i / 4;
                        matchScore.PieceIndex2 = j / 4;
                        matchScore.EdgeIndex1 = i % 4;
                        matchScore.EdgeIndex2 = j % 4;

                        Edge edge1 = Pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1];
                        Edge edge2 = Pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2];
                        if (edge1 == null || edge2 == null) { matchScore.score = 400000000; }
                        else { matchScore.score = edge1.Compare(edge2); }

                        if (matchScore.score <= PuzzleSolverParameters.Instance.PuzzleSolverKeepMatchesThreshold)  // Keep only the best matches (all scores above or equal 100000000 mean that the edges won't match)
                        {
                            matchesDict.TryAdd(matchesDict.Count, matchScore);
                        }
                    });
                });

                matches = matchesDict.Select(m => m.Value).ToList();

                matches.Sort(new MatchScoreComparer(ScoreOrders.LOWEST_FIRST)); // Sort the matches to get the best scores first. The puzzle is solved by the order of the MatchScores

                if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
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
                    PuzzleSolutionImages.Clear();

                    compareAllEdges();

                    CurrentSolverState = PuzzleSolverState.SOLVE_PUZZLE;
                    CurrentSolverStepPercentageFinished = 0;
                    PuzzleDisjointSet p = new PuzzleDisjointSet(Pieces.Count);

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

                    _logHandle.Report(new LogEventInfo("Possible solution found " + (p.InOneSet() ? "(one set)." : "(" + p.SetCount.ToString() + " sets)")));
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

                        Bitmap solutionImg = GenerateSolutionImage2(solution, setNo);
                        PuzzleSolutionImages.Add(new ImageGallery.ImageDescribed("Solution #" + setNo.ToString(), solutionImg));

                        _logHandle.Report(new LogEventImage("Solution #" + setNo.ToString(), solutionImg));
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

        //**********************************************************************************************************************************************************************************************

#region Generate Solution Image

        //private Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        //{ 
        //    if (!Solved) { return null; }

        //    int border = 10;
        //    float out_image_width = 0, out_image_height = 0;

        //    for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
        //    {
        //        for (int j = 0; j < solutionLocations.Size.Height; j++)
        //        {
        //            int piece_number = solutionLocations[j, i];
        //            if (piece_number == -1) { continue; }

        //            float piece_size_x = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(3));
        //            float piece_size_y = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(1));

        //            out_image_width += piece_size_x;
        //            out_image_height += piece_size_y;
        //        }
        //    }
        //    out_image_width = (out_image_width / solutionLocations.Size.Height) * 1.5f + border;
        //    out_image_height = (out_image_height / solutionLocations.Size.Width) * 1.5f + border;
            
        //    // Use get affine to map points...
        //    Image<Rgb, byte> final_out_image = new Image<Rgb, byte>((int)out_image_width, (int)out_image_height);
            
        //    PointF[,] points = new PointF[solutionLocations.Size.Width + 1, solutionLocations.Size.Height +1];
        //    bool failed = false;
            
        //    for (int i = 0; i < solutionLocations.Size.Width; i++)
        //    {
        //        for (int j = 0; j < solutionLocations.Size.Height; j++)
        //        {
        //            int piece_number = solutionLocations[j, i];

        //            if (piece_number == -1)
        //            {
        //                failed = true;
        //                break;
        //            }
        //            float piece_size_x = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(3));
        //            float piece_size_y = (float)Utils.Distance(Pieces[piece_number].GetCorner(0), Pieces[piece_number].GetCorner(1));
        //            VectorOfPointF src = new VectorOfPointF();
        //            VectorOfPointF dst = new VectorOfPointF();

        //            if (i == 0 && j == 0)
        //            {
        //                points[i, j] = new PointF(border, border);
        //            }
        //            if (i == 0)
        //            {
        //                points[i, j + 1] = new PointF(border, points[i, j].Y + border + piece_size_y); //new PointF(points[i, j].X + border + x_dist, border);
        //            }
        //            if (j == 0)
        //            {
        //                points[i + 1, j] = new PointF(points[i, j].X + border + piece_size_x, border); //new PointF(border, points[i, j].Y + border + y_dist);
        //            }

        //            dst.Push(points[i, j]);
        //            //dst.Push(points[i + 1, j]);
        //            //dst.Push(points[i, j + 1]);
        //            dst.Push(points[i, j + 1]);
        //            dst.Push(points[i + 1, j]);
        //            src.Push(Pieces[piece_number].GetCorner(0));
        //            src.Push(Pieces[piece_number].GetCorner(1));
        //            src.Push(Pieces[piece_number].GetCorner(3));

        //            //true means use affine transform
        //            Mat a_trans_mat = CvInvoke.EstimateRigidTransform(src, dst, true);

        //            Matrix<double> A = new Matrix<double>(a_trans_mat.Rows, a_trans_mat.Cols);
        //            a_trans_mat.CopyTo(A);
                    
        //            PointF l_r_c = Pieces[piece_number].GetCorner(2);       //Lower right corner of each piece

        //            //Doing my own matrix multiplication
        //            points[i + 1, j + 1] = new PointF((float)(A[0, 0] * l_r_c.X + A[0, 1] * l_r_c.Y + A[0, 2]), (float)(A[1, 0] * l_r_c.X + A[1, 1] * l_r_c.Y + A[1, 2]));

        //            Mat layer = new Mat();
        //            Mat layer_mask = new Mat();

        //            CvInvoke.WarpAffine(new Image<Rgb, byte>(Pieces[piece_number].PieceImgColor), layer, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Linear, Warp.Default, BorderType.Transparent);
        //            CvInvoke.WarpAffine(new Image<Gray, byte>(Pieces[piece_number].PieceImgBw), layer_mask, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Nearest, Warp.Default, BorderType.Transparent);

        //            layer.CopyTo(final_out_image, layer_mask);
        //        }

        //        if (failed)
        //        {
        //            _logHandle.Report(new LogBox.LogEventError("Failed to generate solution " + solutionID + " image. Only partial image generated."));
        //            break;
        //        }
        //    }

        //    return final_out_image.Clone().Bitmap;
        //}

        //**********************************************************************************************************************************************************************************************

        private Bitmap GenerateSolutionImage2(Matrix<int> solutionLocations, int solutionID)
        {
            if (!Solved) { return null; }
            
            int out_image_width = 0, out_image_height = 0;
            int max_piece_width = 0, max_piece_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    Bitmap colorImg = Pieces[piece_number].PieceImgColor.Bmp;
                    max_piece_width = Math.Max(max_piece_width, colorImg.Width);
                    max_piece_height = Math.Max(max_piece_height, colorImg.Height);
                    colorImg.Dispose();
                }
            }
            max_piece_height += 150;
            out_image_width = max_piece_width * solutionLocations.Size.Width;
            out_image_height = max_piece_height * solutionLocations.Size.Height;

            Bitmap outImg = new Bitmap(out_image_width, out_image_height);
            Graphics g = Graphics.FromImage(outImg);
            g.Clear(Color.White);
            Pen redPen = new Pen(Color.Red, 4);
            StringFormat stringFormat = new StringFormat() { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Near };

            for (int i = 0; i < solutionLocations.Size.Width; i++)
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    Bitmap colorImg = Pieces[piece_number].PieceImgColor.Bmp;
                    g.DrawImage(colorImg, i * max_piece_width, j * max_piece_height + 150);
                    Rectangle pieceRect = new Rectangle(i * max_piece_width, j * max_piece_height, max_piece_width, max_piece_height);
                    g.DrawRectangle(redPen, pieceRect);
                    g.DrawString(Pieces[piece_number].PieceID + Environment.NewLine + Path.GetFileName(Pieces[piece_number].PieceSourceFileName), new Font("Arial", 40), new SolidBrush(Color.Blue), pieceRect, stringFormat);
                    colorImg.Dispose();
                }
            }
            redPen.Dispose();
            
            return outImg;
        }

#endregion

    }
}
