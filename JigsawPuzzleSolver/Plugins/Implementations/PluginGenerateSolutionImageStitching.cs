using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;
using Emgu.CV.Util;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("GenerateSolutionImage Stitching")]
    [PluginDescription("Plugin for solution image generation stitching pieces together")]
    public class PluginGenerateSolutionImageStitching : PluginGroupGenerateSolutionImage
    {
        private int _borderAroundSolutionImage;
        [PluginSettingNumber(1, 0, 1000)]
        [PluginSettingDescription("Border around the solution image in pixels in both directions.")]
        public int BorderAroundSolutionImage
        {
            get { return _borderAroundSolutionImage; }
            set { _borderAroundSolutionImage = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            BorderAroundSolutionImage = 10;
        }


#warning Plugin SolutionImage Stitching isn't working correctly. This issue was started to be handled in another branch. 
        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID, List<Piece> pieces)
        {
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
            out_image_width = (out_image_width / solutionLocations.Size.Height) * 1.5f + BorderAroundSolutionImage;
            out_image_height = (out_image_height / solutionLocations.Size.Width) * 1.5f + BorderAroundSolutionImage;

            // Use get affine to map points...
            Image<Rgb, byte> final_out_image = new Image<Rgb, byte>((int)out_image_width, (int)out_image_height);

            PointF[,] points = new PointF[solutionLocations.Size.Width + 1, solutionLocations.Size.Height + 1];
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
                        points[i, j] = new PointF(BorderAroundSolutionImage, BorderAroundSolutionImage);
                    }
                    if (i == 0)
                    {
                        points[i, j + 1] = new PointF(BorderAroundSolutionImage, points[i, j].Y + BorderAroundSolutionImage + piece_size_y); //new PointF(points[i, j].X + border + x_dist, border);
                    }
                    if (j == 0)
                    {
                        points[i + 1, j] = new PointF(points[i, j].X + BorderAroundSolutionImage + piece_size_x, BorderAroundSolutionImage); //new PointF(border, points[i, j].Y + border + y_dist);
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

                    CvInvoke.WarpAffine(new Image<Rgb, byte>(pieces[piece_number].PieceImgColor.Bmp), layer, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Linear, Warp.Default, BorderType.Transparent);
                    CvInvoke.WarpAffine(new Image<Gray, byte>(pieces[piece_number].PieceImgBw.Bmp), layer_mask, a_trans_mat, new Size((int)out_image_width, (int)out_image_height), Inter.Nearest, Warp.Default, BorderType.Transparent);

                    layer.CopyTo(final_out_image, layer_mask);
                }

                if (failed)
                {
                    PluginFactory.LogHandle.Report(new LogBox.LogEvents.LogEventError("Failed to generate solution " + solutionID + " image. Only partial image generated."));
                    break;
                }
            }

            return final_out_image.Clone().Bitmap;
        }
    }
}
