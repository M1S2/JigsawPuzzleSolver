using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;

namespace JigsawPuzzleSolver
{
    /// <summary>
    /// Class that is used to extract all pieces from a set of images, store the pieces and solve the puzzle.
    /// </summary>
    /// see: https://github.com/jzeimen/PuzzleSolver/blob/master/PuzzleSolver/puzzle.cpp
    public class Puzzle : ObservableObject
    {       
        public PuzzleSolverParameters SolverParameters { get; private set; }

        public bool Solved { get { return CurrentSolverStep == PuzzleSolverSteps.SOLVED; } }

        private PuzzleSolverSteps _currentSolverStep;
        public PuzzleSolverSteps CurrentSolverStep
        {
            get { return _currentSolverStep; }
            private set
            {
                _currentSolverStep = value;
                OnPropertyChanged();
                OnPropertyChanged("PercentageOfFinishedSolverSteps");
                OnPropertyChanged("NumberOfFinishedSolverSteps");
            }
        }
        
        public int PercentageOfFinishedSolverSteps
        {
            get { return (int)((NumberOfFinishedSolverSteps / (double)NumberOfSolverSteps) * 100); }
        }

        public int NumberOfFinishedSolverSteps
        {
            get { return (int)CurrentSolverStep; }
        }

        public int NumberOfSolverSteps
        {
            get { return Enum.GetNames(typeof(PuzzleSolverSteps)).Count() - 1; }        // -1 because "SOLVED" isn't a real solver step
        }

        private int _currentSolverStepPercentageFinished;
        public int CurrentSolverStepPercentageFinished
        {
            get { return _currentSolverStepPercentageFinished; }
            private set { _currentSolverStepPercentageFinished = value; OnPropertyChanged(); }
        }

        private int _numberPuzzlePieces;
        public int NumberPuzzlePieces
        {
            get { return _numberPuzzlePieces; }
            private set { _numberPuzzlePieces = value; OnPropertyChanged(); }
        }

        private string _puzzlePiecesFolderPath;
        public string PuzzlePiecesFolderPath
        {
            get { return _puzzlePiecesFolderPath; }
            private set { _puzzlePiecesFolderPath = value; OnPropertyChanged(); }
        }
        //##############################################################################################################################################################################################

        private IProgress<LogBox.LogEvent> _logHandle;

        private List<MatchScore> matches = new List<MatchScore>();
        private List<Piece> pieces = new List<Piece>();

        private Matrix<int> solution;
        private Matrix<int> solution_rotations;

        //##############################################################################################################################################################################################

        public Puzzle(string piecesFolderPath, PuzzleSolverParameters solverParameters, IProgress<LogBox.LogEvent> logHandle)
        {
            PuzzlePiecesFolderPath = piecesFolderPath;
            SolverParameters = solverParameters;
            _logHandle = logHandle;
            NumberPuzzlePieces = 0;
        }

        public Puzzle()
        { }

        //##############################################################################################################################################################################################

        /*
        private void extract_pieces(string path, int piece_size, bool needs_filter, int threshold)
        {
            List<Image<Rgb, byte>> color_images_tmp = Utils.GetImagesFromDirectory(path);
            List<Mat> color_images = new List<Mat>();
            foreach(Image<Rgb, byte> img in color_images_tmp) { color_images.Add(img.Mat); }
            
            List<Mat> bw_images = new List<Mat>();
            if (needs_filter)
            {
                List<Mat> blured_images = Utils.MedianBlur(color_images.Select(x => x.ToImage<Rgb, byte>()).ToList(), 5).Select(x => x.Mat).ToList();
                bw_images = Utils.ColorToBw(blured_images.Select(x => x.ToImage<Rgb, byte>()).ToList(), threshold).Select(x => x.Mat).ToList();
            }
            else
            {
                bw_images = Utils.ColorToBw(color_images.Select(x => x.ToImage<Rgb, byte>()).ToList(), threshold).Select(x => x.Mat).ToList();
                Utils.Filter(bw_images, 2);
            }

            //For each input image
            for (int i = 0; i < color_images.Count; i++)
            {
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                
                Mat bw_img_clone = bw_images[i].Clone();    //Need to clone because it will get modified
                CvInvoke.FindContours(bw_img_clone, contours, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
                
                //For each contour in that image
                //TODO: In anticipation of the other TODO's Re-create the b/w image based off of the contour to eliminate noise in the layer mask
                for (int j = 0; j < contours.Size; j++)
                {
                    int bordersize = 15;
                    Rectangle rect = CvInvoke.BoundingRectangle(contours[j]);
                    if (rect.Width < piece_size || rect.Height < piece_size) continue;

                    Mat new_bw = Mat.Zeros(rect.Height + 2 * bordersize, rect.Width + 2 * bordersize, DepthType.Cv8U, 1);
                    VectorOfVectorOfPoint contours_to_draw = new VectorOfVectorOfPoint();
                    contours_to_draw.Push(Utils.TranslateContour(contours[j], bordersize - rect.X, bordersize - rect.Y));
                    CvInvoke.DrawContours(new_bw, contours_to_draw, -1, new MCvScalar(255), 2);

                    _logHandle.Report(new LogBox.LogEventImage("Image #" + i.ToString() + " Contour #" + j.ToString(), new_bw.Bitmap));

                    rect.Width += bordersize * 2;
                    rect.Height += bordersize * 2;
                    rect.X -= bordersize;
                    rect.Y -= bordersize;       

                    Mat mini_color = new Mat(color_images[i], rect);
                    Mat mini_bw = new_bw;
                                             
                    mini_color = mini_color.Clone();        //Create a copy so it can't conflict.
                    mini_bw = mini_bw.Clone();
                    
                    Piece p = new Piece(mini_color.ToImage<Rgb, byte>(), mini_bw.ToImage<Gray, byte>(), SolverParameters);
                    pieces.Add(p);
                }
            }
        }
        */
        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Extract all pieces from the source image. 
        /// </summary>
        /// see: https://docs.opencv.org/trunk/d8/d83/tutorial_py_grabcut.html
        /// see: http://www.emgu.com/forum/viewtopic.php?t=1923
        private void extract_pieces2()
        {
            CurrentSolverStep = PuzzleSolverSteps.INIT_PIECES;
            CurrentSolverStepPercentageFinished = 0;
            _logHandle.Report(new LogBox.LogEventInfo("Extracting Pieces"));
            NumberPuzzlePieces = 0;

            List<Image<Rgb, byte>> color_images = Utils.GetImagesFromDirectory(PuzzlePiecesFolderPath);
            
            if (SolverParameters.PuzzleApplyMedianBlurFilter)
            {
                color_images = Utils.MedianBlur(color_images, 5);
            }
#warning Segment using HSV color model, CvInvoke.InRange, CvInvoke.CvtColor
            List<Image<Gray, byte>> bw_images = Utils.ColorToBw(color_images, 50);

            //For each input image
            for (int i = 0; i < bw_images.Count; i++) //color_images.Count; i++)
            {
                Image<Rgb, byte> sourceImg = new Image<Rgb, byte>(color_images[i].Bitmap);
                Image<Gray, byte> mask = new Image<Gray, byte>(bw_images[i].Bitmap);

                /*int maxWidth = 1000, maxHeight = 1000;
                if (sourceImg.Width > maxWidth || sourceImg.Height > maxHeight)
                {
                    sourceImg = sourceImg.Copy(new Rectangle(new Point(20, 20), Size.Subtract(sourceImg.Size, new Size(20, 20))));      // Used to remove a black line at the top of a scanned image
                    sourceImg = sourceImg.Resize(maxWidth, maxHeight, Inter.Cubic, true);
                }*/

                _logHandle.Report(new LogBox.LogEventImage("Extracting Pieces from source image " + i.ToString() , sourceImg.ToBitmap()));

                //Image<Gray, byte> mask = sourceImg.GrabCut(new Rectangle(1, 1, sourceImg.Width - 1, sourceImg.Height - 1), 20); //10);
                //mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.
                
                CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
                CvBlobs blobs = new CvBlobs();
                blobDetector.Detect(mask, blobs);

                Image<Rgb, byte> sourceImgPiecesMarked = sourceImg.Copy();

                foreach (CvBlob blob in blobs.Values.Where(b => b.BoundingBox.Width >= SolverParameters.PuzzleMinPieceSize && b.BoundingBox.Height >= SolverParameters.PuzzleMinPieceSize))
                {
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
                    catch(Exception)
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

                    Piece p = new Piece(pieceSourceImgMasked, pieceMask, SolverParameters, _logHandle);
                    pieces.Add(p);
                    NumberPuzzlePieces++;

                    pieceSourceImg.Dispose();
                    pieceSourceImgMasked.Dispose();
                    pieceMask.Dispose();
                    pieceSourceImageForeground.Dispose();
                    pieceMaskInverted.Dispose();
                    background.Dispose();
                    pieceSourceImageBackground.Dispose();

                    CurrentSolverStepPercentageFinished = (int)(((i + 1) / (double)bw_images.Count) * 100);
                }

                _logHandle.Report(new LogBox.LogEventImage("Source Img " + i.ToString() + " Pieces", sourceImgPiecesMarked.ToBitmap()));
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
            CurrentSolverStep = PuzzleSolverSteps.COMPARE_EDGES;
            CurrentSolverStepPercentageFinished = 0;
            _logHandle.Report(new LogBox.LogEventInfo("Comparing all edges"));

            matches.Clear();
            int no_edges = (int)pieces.Count * 4;
            int no_compares = (no_edges * (no_edges + 1)) / 2;      // Number of loop runs of the following nested loops 
            int loop_count = 0;

            for (int i = 0; i < no_edges; i++)
            {
                for (int j = i; j < no_edges; j++)
                {
                    loop_count++;
                    if(loop_count > no_compares) { loop_count = no_compares; }
                    CurrentSolverStepPercentageFinished = (int)((loop_count / (double)no_compares) * 100);

                    MatchScore matchScore = new MatchScore();
                    matchScore.PieceIndex1 = i / 4;
                    matchScore.PieceIndex2 = j / 4;
                    matchScore.EdgeIndex1 = i % 4;
                    matchScore.EdgeIndex2 = j % 4;
                    matchScore.score = pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1].Compare2(pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2]);

                    if (matchScore.score <= SolverParameters.PuzzleSolverKeepMatchesThreshold) { matches.Add(matchScore); }
                }
            }

            //double bestMatchScore = matches.Select(m => m.score).Min();
            //matches = matches.Where(m => m.score < SolverParameters.PuzzleSolverKeepBestMatchesFactor * bestMatchScore).ToList();           // Get only the best matches (all scores above or equal 100000000 mean that the edges won't match)
            matches.Sort(new MatchScoreComparer(ScoreOrders.LOWEST_FIRST)); // Sort the matches to get the best scores first. The puzzle is solved by the order of the MatchScores

            if (SolverParameters.SolverShowDebugResults)
            {
                foreach (MatchScore matchScore in matches)
                {
                    _logHandle.Report(new LogBox.LogEventImage("MatchScore " + pieces[matchScore.PieceIndex1].PieceID + "_Edge" + (matchScore.EdgeIndex1).ToString() + " <-->" + pieces[matchScore.PieceIndex2].PieceID + "_Edge" + (matchScore.EdgeIndex2).ToString() + " = " + matchScore.score.ToString(), Utils.Combine2ImagesHorizontal(pieces[matchScore.PieceIndex1].Edges[matchScore.EdgeIndex1].ContourImg, pieces[matchScore.PieceIndex2].Edges[matchScore.EdgeIndex2].ContourImg, 20).ToBitmap()));
                }
            }
        }

        //##############################################################################################################################################################################################

        public async Task Init()
        {
            await Task.Run(() =>
            {
                extract_pieces2();
            });
        }

        //**********************************************************************************************************************************************************************************************

        public async Task Solve()
        {
            await Task.Run(() =>
            {
                compareAllEdges();

                CurrentSolverStep = PuzzleSolverSteps.SOLVE_PUZZLE;
                CurrentSolverStepPercentageFinished = 0;
                PuzzleDisjointSet p = new PuzzleDisjointSet(pieces.Count);

                _logHandle.Report(new LogBox.LogEventInfo("Join Pieces"));

                for (int i = 0; i < matches.Count; i++)
                {
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
                CurrentSolverStep = PuzzleSolverSteps.SOLVED;
                int setNo = 0;
                foreach (Forest jointSet in p.GetJointSets())
                {
                    solution = jointSet.locations;
                    solution_rotations = jointSet.rotations;

                    for (int i = 0; i < solution.Size.Width; i++)
                    {
                        for (int j = 0; j < solution.Size.Height; j++)
                        {
                            int piece_number = solution[j, i];
                            if (piece_number == -1) { continue; }
                            pieces[piece_number].Rotate(4 - solution_rotations[j, i]);
                        }
                    }
                    GenerateSolutionImage2(solution, setNo);
                    setNo++;
                }
            });
        }

        //**********************************************************************************************************************************************************************************************

        #region Generate Solution Image

        private void GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        { 
            if (!Solved) { Solve(); }

            int border = 10;
            float out_image_width = 0, out_image_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    float piece_size_x = (float)Utils.Distance(pieces[piece_number].GetCorner(0), pieces[piece_number].GetCorner(3));
                    float piece_size_y = (float)Utils.Distance(pieces[piece_number].GetCorner(0), pieces[piece_number].GetCorner(1));

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
                    float piece_size_x = (float)Utils.Distance(pieces[piece_number].GetCorner(0), pieces[piece_number].GetCorner(3));
                    float piece_size_y = (float)Utils.Distance(pieces[piece_number].GetCorner(0), pieces[piece_number].GetCorner(1));
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
                    src.Push(pieces[piece_number].GetCorner(0));
                    src.Push(pieces[piece_number].GetCorner(1));
                    src.Push(pieces[piece_number].GetCorner(3));

                    //true means use affine transform
                    Mat a_trans_mat = CvInvoke.EstimateRigidTransform(src, dst, true);

                    Matrix<double> A = new Matrix<double>(a_trans_mat.Rows, a_trans_mat.Cols);
                    a_trans_mat.CopyTo(A);
                    
                    PointF l_r_c = pieces[piece_number].GetCorner(2);       //Lower right corner of each piece

                    //Doing my own matrix multiplication
                    points[i + 1, j + 1] = new PointF((float)(A[0, 0] * l_r_c.X + A[0, 1] * l_r_c.Y + A[0, 2]), (float)(A[1, 0] * l_r_c.X + A[1, 1] * l_r_c.Y + A[1, 2]));

                    Mat layer = new Mat();
                    Mat layer_mask = new Mat();

                    CvInvoke.WarpAffine(pieces[piece_number].Full_color, layer, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Linear, Warp.Default, BorderType.Transparent);
                    CvInvoke.WarpAffine(pieces[piece_number].Bw, layer_mask, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Nearest, Warp.Default, BorderType.Transparent);

                    layer.CopyTo(final_out_image, layer_mask);
                }

                if (failed)
                {
                    _logHandle.Report(new LogBox.LogEventError("Failed to generate solution " + solutionID + " image. Only partial image generated."));
                    break;
                }
            }

            _logHandle.Report(new LogBox.LogEventImage("Solution #" + solutionID.ToString(), final_out_image.Clone().Bitmap));
        }

        //**********************************************************************************************************************************************************************************************

        private void GenerateSolutionImage2(Matrix<int> solutionLocations, int solutionID)
        {
            if (!Solved) { Solve(); }
            
            int out_image_width = 0, out_image_height = 0;
            int max_piece_width = 0, max_piece_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    max_piece_width = Math.Max(max_piece_width, pieces[piece_number].Full_color.Width);
                    max_piece_height = Math.Max(max_piece_height, pieces[piece_number].Full_color.Height);
                }
            }
            out_image_width = max_piece_width * solutionLocations.Size.Width;
            out_image_height = max_piece_height * solutionLocations.Size.Height;

            Bitmap outImg = new Bitmap(out_image_width, out_image_height);
            Graphics g = Graphics.FromImage(outImg);

            for (int i = 0; i < solutionLocations.Size.Width; i++)
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    g.DrawImage(pieces[piece_number].Full_color.Bitmap, i * max_piece_width, j * max_piece_height);
                }
            }

            _logHandle.Report(new LogBox.LogEventImage("Solution #" + solutionID.ToString(), outImg));
        }

        #endregion

    }
}
