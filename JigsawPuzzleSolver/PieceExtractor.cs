using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;
using Emgu.CV.Cvb;

namespace JigsawPuzzleSolver
{
    public static class PieceExtractor
    {
        /// <summary>
        /// Extract all pieces from the source image. 
        /// </summary>
        /// <param name="sourceImgPath">Path to the image in which to search for the pieces</param>
        /// <returns>List with all found pieces</returns>
        /// see: https://docs.opencv.org/trunk/d8/d83/tutorial_py_grabcut.html
        /// see: http://www.emgu.com/forum/viewtopic.php?t=1923
        public static PieceCollection ExtractPieces(string sourceImgPath)
        {
            Image<Rgb, byte> sourceImg = new Image<Rgb, byte>(sourceImgPath);

            int maxWidth = 1000, maxHeight = 1000;
            if (sourceImg.Width > maxWidth || sourceImg.Height > maxHeight)
            {
                sourceImg = sourceImg.Copy(new Rectangle(new Point(20, 20), Size.Subtract(sourceImg.Size, new Size(20, 20))));      // Used to remove a black line at the top of a scanned image
                sourceImg = sourceImg.Resize(maxWidth, maxHeight, Inter.Cubic, true);
            }

            ProcessedImagesStorage.AddImage("Source Img \"" + Path.GetFileName(sourceImgPath) + "\"", sourceImg.ToBitmap());

            PieceCollection pieces = new PieceCollection();

            Image<Gray, byte> mask = sourceImg.GrabCut(new Rectangle(1, 1, sourceImg.Width - 1, sourceImg.Height - 1), 20); //10);
            mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.

            /*VectorOfVectorOfPoint contour = new VectorOfVectorOfPoint();       // Draw contours in sourceImg. Only for visualization !
            CvInvoke.FindContours(mask, contour, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            CvInvoke.DrawContours(sourceImg, contour, -1, new MCvScalar(255, 0, 0));*/

            CvBlobDetector blobDetector = new CvBlobDetector();                 // Find all blobs in the mask image, extract them and add them to the list of pieces
            CvBlobs blobs = new CvBlobs();
            blobDetector.Detect(mask, blobs);

            Image<Rgb, byte> sourceImgPiecesMarked = sourceImg.Copy();

            foreach (CvBlob blob in blobs.Values.Where(b => b.Area > 10))
            {
                Rectangle roi = blob.BoundingBox;
                sourceImgPiecesMarked.Draw(roi, new Rgb(255, 0, 0), 2);

                //if (sourceImg.Height > roi.Height + 2 && sourceImg.Width > roi.Width + 2) { roi.Inflate(1, 1); }
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

                Piece piece = new Piece(pieceSourceImgMasked, pieceMask);
                piece.OriginImageName = Path.GetFileName(sourceImgPath);
                pieces.Add(piece);
            }

            ProcessedImagesStorage.AddImage("Source Img \"" + System.IO.Path.GetFileName(sourceImgPath) + "\" Pieces", sourceImgPiecesMarked.ToBitmap());

            return pieces;
        }

    }
}
