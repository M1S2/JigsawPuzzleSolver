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
    public static class PieceRotator_old
    {
        /// <summary>
        /// Find the minimum area rectangle of all pieces and rotate the Mask of the pieces so that this rectangle isn't rotated.
        /// This function only calls RotatePiece for all pieces in the list.
        /// </summary>
        /// <param name="pieces">Pieces to rotate</param>
        public static void RotateAllPieces(List<Piece_old> pieces)
        {
            foreach(Piece_old piece in pieces)
            {
                RotatePiece(piece);
            }
        }

        //**********************************************************************************************************************************************************************************************

        /// <summary>
        /// Find the minimum area rectangle and rotate the Mask of the piece so that this rectangle isn't rotated.
        /// </summary>
        /// <param name="piece">Piece to rotate</param>
        public static void RotatePiece(Piece_old piece)
        {
            VectorOfVectorOfPoint contour = new VectorOfVectorOfPoint();
            CvInvoke.FindContours(piece.Mask, contour, null, RetrType.List, ChainApproxMethod.ChainApproxNone);
            if(contour == null || contour.Size == 0) { return; }

            RotatedRect minAreaRect = CvInvoke.MinAreaRect(contour[0]);
            PointF[] rectVertices = minAreaRect.GetVertices();

#warning MinAreaRect doesn't always fit the piece perfectly (for example for 3 bulbs, 1 hole => unsymmetric pieces)

            CvInvoke.DrawContours(piece.SourceImg, contour, -1, new MCvScalar(255, 0, 0));
            for (int i = 0; i < 4; i++) // draw rotatedRect
            {
                CvInvoke.Line(piece.SourceImg, Point.Round(rectVertices[i]), Point.Round(rectVertices[(i + 1) % 4]), new MCvScalar(0, 255, 0), 1, LineType.AntiAlias, 0);
            }

            /*List<PointF> destVertices = new List<PointF>() { new PointF(0, piece.SourceImg.Height), new PointF(0, 0), new PointF(piece.SourceImg.Width, 0), new PointF(piece.SourceImg.Width, piece.SourceImg.Height) };
            Mat transformMatrix = CvInvoke.GetPerspectiveTransform(rectVertices, destVertices.ToArray());
            CvInvoke.WarpPerspective(piece.SourceImg, piece.SourceImg, transformMatrix, piece.SourceImg.Size);
            CvInvoke.WarpPerspective(piece.Mask, piece.Mask, transformMatrix, piece.Mask.Size);*/

            Mat rotateMatrix = new Mat();
            CvInvoke.GetRotationMatrix2D(new PointF(piece.SourceImg.Width / 2, piece.SourceImg.Height / 2), minAreaRect.Angle, 1, rotateMatrix);
            CvInvoke.WarpAffine(piece.Mask, piece.Mask, rotateMatrix, piece.Mask.Size);
        }

    }
}
