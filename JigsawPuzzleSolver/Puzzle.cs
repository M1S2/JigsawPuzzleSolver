using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
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
    public class Puzzle
    {
        private int threshold;
        private bool solved;
        private int piece_size;

        private List<MatchScore> matches = new List<MatchScore>();
        private List<Piece> pieces = new List<Piece>();

        private Matrix<int> solution;
        private Matrix<int> solution_rotations;

        //##############################################################################################################################################################################################

        public Puzzle(string path, int estimated_piece_size, int thresh, bool filter = true)
        {
            threshold = thresh;
            piece_size = estimated_piece_size;
            extract_pieces2(path);
            solved = false;
            // print_edges();
        }

        //##############################################################################################################################################################################################

#warning OBSOLETE
        private void extract_pieces(string path, bool needs_filter)
        {
            List<Image<Rgb, byte>> color_images_tmp = Utils.GetImagesFromDirectory(path);
            List<Mat> color_images = new List<Mat>();
            foreach(Image<Rgb, byte> img in color_images_tmp) { color_images.Add(img.Mat); }
            
            List<Mat> bw_images = new List<Mat>();
            if (needs_filter)
            {
                List<Mat> blured_images = Utils.MedianBlur(color_images, 5);
                bw_images = Utils.ColorToBw(blured_images, threshold);
            }
            else
            {
                bw_images = Utils.ColorToBw(color_images, threshold);
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

                    ProcessedImagesStorage.AddImage("Image #" + i.ToString() + " Contour #" + j.ToString(), new_bw.Bitmap);

                    rect.Width += bordersize * 2;
                    rect.Height += bordersize * 2;
                    rect.X -= bordersize;
                    rect.Y -= bordersize;       

                    Mat mini_color = new Mat(color_images[i], rect);
                    Mat mini_bw = new_bw;
                                             
                    mini_color = mini_color.Clone();        //Create a copy so it can't conflict.
                    mini_bw = mini_bw.Clone();
                    
                    Piece p = new Piece(mini_color.ToImage<Rgb, byte>(), mini_bw.ToImage<Gray, byte>(), piece_size);
                    pieces.Add(p);
                }
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Extract all pieces from the source image. 
        /// </summary>
        /// <param name="path">Path to the image in which to search for the pieces</param>
        /// see: https://docs.opencv.org/trunk/d8/d83/tutorial_py_grabcut.html
        /// see: http://www.emgu.com/forum/viewtopic.php?t=1923
        private void extract_pieces2(string path)
        {
            List<Image<Rgb, byte>> color_images = Utils.GetImagesFromDirectory(path);

            //For each input image
            for (int i = 0; i < color_images.Count; i++)
            {
                Image<Rgb, byte> sourceImg = new Image<Rgb, byte>(color_images[i].Bitmap);

                int maxWidth = 1000, maxHeight = 1000;
                if (sourceImg.Width > maxWidth || sourceImg.Height > maxHeight)
                {
                    sourceImg = sourceImg.Copy(new Rectangle(new Point(20, 20), Size.Subtract(sourceImg.Size, new Size(20, 20))));      // Used to remove a black line at the top of a scanned image
                    sourceImg = sourceImg.Resize(maxWidth, maxHeight, Inter.Cubic, true);
                }

                ProcessedImagesStorage.AddImage("Source Img " + i.ToString() , sourceImg.ToBitmap());
                
                Image<Gray, byte> mask = sourceImg.GrabCut(new Rectangle(1, 1, sourceImg.Width - 1, sourceImg.Height - 1), 20); //10);
                mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.

                CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
                CvBlobs blobs = new CvBlobs();
                blobDetector.Detect(mask, blobs);

                Image<Rgb, byte> sourceImgPiecesMarked = sourceImg.Copy();

                foreach (CvBlob blob in blobs.Values.Where(b => b.Area > 10))
                {
                    Rectangle roi = blob.BoundingBox;
                    sourceImgPiecesMarked.Draw(roi, new Rgb(255, 0, 0), 2);

                    if (sourceImg.Height > roi.Height + 2 && sourceImg.Width > roi.Width + 2) { roi.Inflate(1, 1); }
                    Image<Rgb, byte> pieceSourceImg = sourceImg.Copy(roi);
                    Image<Gray, byte> pieceMask = mask.Copy(roi);

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

                    Piece p = new Piece(pieceSourceImgMasked, pieceMask, piece_size);
                    pieces.Add(p);
                }

                ProcessedImagesStorage.AddImage("Source Img " + i.ToString() + " Pieces", sourceImgPiecesMarked.ToBitmap());
            }
        }

        //**********************************************************************************************************************************************************************************************

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

                    VectorOfVectorOfPointF contours = new VectorOfVectorOfPointF();
                    contours.Push(pieces[i].Edges[j].GetTranslatedContour(200, 0));

                    CvInvoke.DrawContours(m, contours, -1, new MCvScalar(255), 2);
                    CvInvoke.PutText(m, pieces[i].Edges[j].EdgeType.ToString(), new Point(300, 300), FontFace.HersheyComplexSmall, 0.8, new MCvScalar(255), 1, LineType.AntiAlias);

                    ProcessedImagesStorage.AddImage("Contour" + i.ToString() + "_" + j.ToString(), m.Bitmap);
                }
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Fill the list with all match scores for all piece edge combinations
        /// </summary>
        private void fill_costs()
        {
            int no_edges = (int)pieces.Count * 4;

            for (int i = 0; i < no_edges; i++)
            {
                for (int j = i; j < no_edges; j++)
                {
                    MatchScore score = new MatchScore();
                    score.edgeIndex1 = i;
                    score.edgeIndex2 = j;
                    score.score = pieces[i / 4].Edges[i % 4].Compare2(pieces[j / 4].Edges[j % 4]);
                    {
                        matches.Add(score);
                    }
                }
            }
            matches.Sort(new MatchScoreComparer());
        }

        //##############################################################################################################################################################################################

        public void solve()
        {
            throw new NotImplementedException();
        }

        //**********************************************************************************************************************************************************************************************

        public void save_image(string filepath)
        {
            throw new NotImplementedException();
        }
        
    }
}
