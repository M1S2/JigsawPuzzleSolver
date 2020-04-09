using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using JigsawPuzzleSolver.Plugins.Controls;
using System.IO;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("GenerateSolutionImage Simple")]
    [PluginDescription("Plugin for solution image generation placing pieces side by side")]
    public class PluginGenerateSolutionImageSimple : PluginGroupGenerateSolutionImage
    {
        private int _outputWidthPerPiece;
        [PluginSettingNumber(1, 0, 2000)]
        [PluginSettingDescription("Width of each piece in the output image. Overall Width of the solution image is width per piece times number of pieces in X direction.")]
        public int OutputWidthPerPiece
        {
            get { return _outputWidthPerPiece; }
            set { _outputWidthPerPiece = value; OnPropertyChanged(); }
        }

        private int _outputHeightPerPiece;
        [PluginSettingNumber(1, 0, 2000)]
        [PluginSettingDescription("Height of each piece in the output image. Overall Height of the solution image is height per piece times number of pieces in Y direction.")]
        public int OutputHeightPerPiece
        {
            get { return _outputHeightPerPiece; }
            set { _outputHeightPerPiece = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            OutputWidthPerPiece = 200;
            OutputHeightPerPiece = 200;
        }

        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID, List<Piece> pieces)
        {
            int out_image_width = 0, out_image_height = 0;
            int max_piece_width = 0, max_piece_height = 0;

            for (int i = 0; i < solutionLocations.Size.Width; i++)           // Calculate output image size
            {
                for (int j = 0; j < solutionLocations.Size.Height; j++)
                {
                    int piece_number = solutionLocations[j, i];
                    if (piece_number == -1) { continue; }

                    Bitmap colorImg = pieces[piece_number].PieceImgColor.Bmp;
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

                    Bitmap colorImg = pieces[piece_number].PieceImgColor.Bmp;
                    g.DrawImage(colorImg, i * max_piece_width, j * max_piece_height + 150);
                    Rectangle pieceRect = new Rectangle(i * max_piece_width, j * max_piece_height, max_piece_width, max_piece_height);
                    g.DrawRectangle(redPen, pieceRect);
                    g.DrawString(pieces[piece_number].PieceID + Environment.NewLine + Path.GetFileName(pieces[piece_number].PieceSourceFileName), new Font("Arial", 20), new SolidBrush(Color.Blue), pieceRect, stringFormat);
                    colorImg.Dispose();
                }
            }
            redPen.Dispose();

            Bitmap outImgSmall = outImg.LimitImageSize(solutionLocations.Size.Width * OutputWidthPerPiece, solutionLocations.Size.Height * OutputHeightPerPiece);
            outImg.Dispose();
            return outImgSmall;
        }

    }
}
