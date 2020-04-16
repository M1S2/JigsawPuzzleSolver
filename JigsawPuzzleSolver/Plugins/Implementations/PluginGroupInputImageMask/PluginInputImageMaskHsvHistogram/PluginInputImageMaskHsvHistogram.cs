using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using JigsawPuzzleSolver.Plugins.Core;
using LogBox.LogEvents;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupInputImageMask
{
    [PluginName("InputImageMask HSV Histogram")]
    [PluginDescription("Plugin for generating binary mask from input image using HSV histogram")]
    public class PluginInputImageMaskHsvHistogram : PluginGroupInputImageMask
    {
        public override PackIconBase PluginIcon => new PackIconMaterialLight() { Kind = PackIconMaterialLightKind.ChartHistogram };

        //##############################################################################################################################################################################################

        private int _mainHueSegment;
        [PluginSettingNumber(1, 0, 179)]
        [PluginSettingDescription("Is the piece background rather red (0), green (60) or blue (120)?")]
        public int MainHueSegment
        {
            get { return _mainHueSegment; }
            set { _mainHueSegment = value; OnPropertyChanged(); }
        }

        private int _hueDiffHist;
        [PluginSettingNumber(1, 0, 179)]
        [PluginSettingDescription("Allowed Hue range in Histogram")]
        public int HueDiffHist
        {
            get { return _hueDiffHist; }
            set { _hueDiffHist = value; OnPropertyChanged(); }
        }

        private int _hueDiff;
        [PluginSettingNumber(1, 0, 179)]
        [PluginSettingDescription("Allowed Hue range is [Hue - HueDiff , Hue + HueDiff]")]
        public int HueDiff
        {
            get { return _hueDiff; }
            set { _hueDiff = value; OnPropertyChanged(); }
        }

        private int _saturationDiff;
        [PluginSettingNumber(1, 0, 255)]
        [PluginSettingDescription("Allowed Saturation range is [Saturation - SaturationDiff , Saturation + SaturationDiff]")]
        public int SaturationDiff
        {
            get { return _saturationDiff; }
            set { _saturationDiff = value; OnPropertyChanged(); }
        }

        private int _valueDiff;
        [PluginSettingNumber(1, 0, 255)]
        [PluginSettingDescription("Allowed Value range is [Value - ValueDiff , Value + ValueDiff]")]
        public int ValueDiff
        {
            get { return _valueDiff; }
            set { _valueDiff = value; OnPropertyChanged(); }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            MainHueSegment = 90;
            HueDiffHist = 15;
            HueDiff = 20;
            SaturationDiff = 20;
            ValueDiff = 20;
        }

        /// <summary>
        /// Calculate a mask for the pieces. The function calculates a histogram to find the piece background color. 
        /// Everything within a specific HSV range around the piece background color is regarded as foreground. The rest is regarded as background.
        /// </summary>
        /// <param name="inputImg">Color input image</param>
        /// <returns>Mask image</returns>
        /// see: https://docs.opencv.org/2.4/modules/imgproc/doc/histograms.html?highlight=calchist
        public override Image<Gray, byte> GetMask(Image<Rgba, byte> inputImg)
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
                if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                {
                    PluginFactory.LogHandle?.Report(new LogEventImage("Hist H", Utils.DrawHist(histOutH, hbins, 30, 1024, new MCvScalar(255, 0, 0)).Bitmap));
                    PluginFactory.LogHandle?.Report(new LogEventImage("Hist S", Utils.DrawHist(histOutS, sbins, 30, 1024, new MCvScalar(0, 255, 0)).Bitmap));
                    PluginFactory.LogHandle?.Report(new LogEventImage("Hist V", Utils.DrawHist(histOutV, vbins, 30, 1024, new MCvScalar(0, 0, 255)).Bitmap));
                }

                //#warning Use border color
                //int borderHeight = 10;
                //Image<Hsv, byte> borderImg = hsvSourceImg.Copy(new Rectangle(0, hsvSourceImg.Height - borderHeight, hsvSourceImg.Width, borderHeight));
                //MCvScalar meanBorderColorScalar = CvInvoke.Mean(borderImg);
                //Hsv meanBorderColor = new Hsv(meanBorderColorScalar.V0, meanBorderColorScalar.V1, meanBorderColorScalar.V2);
                //if (PuzzleSolverParameters.Instance.SolverShowDebugResults)
                //{
                //    Image<Hsv, byte> borderColorImg = new Image<Hsv, byte>(12, 12);
                //    borderColorImg.SetValue(meanBorderColor);
                //    _logHandle.Report(new LogBox.LogEventImage("HSV Border Color (" + meanBorderColor.Hue + " ; " + meanBorderColor.Satuation + "; " + meanBorderColor.Value + ")", borderColorImg.Bitmap));
                //}


                // Find the peaks in the histograms and use them as piece background color. Black and white areas are ignored.
                Hsv pieceBackgroundColor = new Hsv
                {
                    Hue = Utils.HighestBinValInRange(histOutH, MainHueSegment - HueDiffHist, MainHueSegment + HueDiffHist, 179), //25, 179, 179);
                    Satuation = Utils.HighestBinValInRange(histOutS, 50, 205, 255), //50, 255, 255);
                    Value = Utils.HighestBinValInRange(histOutV, 75, 205, 255) //75, 255, 255);
                };

                histOutH.Dispose();
                histOutS.Dispose();
                histOutV.Dispose();

                // Show the found piece background color
                if (PluginFactory.GetGeneralSettingsPlugin().SolverShowDebugResults)
                {
                    Image<Hsv, byte> pieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    Image<Hsv, byte> lowPieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    Image<Hsv, byte> highPieceBgColorImg = new Image<Hsv, byte>(4, 12);
                    pieceBgColorImg.SetValue(pieceBackgroundColor);
                    lowPieceBgColorImg.SetValue(new Hsv(pieceBackgroundColor.Hue - HueDiff, pieceBackgroundColor.Satuation - SaturationDiff, pieceBackgroundColor.Value - ValueDiff));
                    highPieceBgColorImg.SetValue(new Hsv(pieceBackgroundColor.Hue + HueDiff, pieceBackgroundColor.Satuation + SaturationDiff, pieceBackgroundColor.Value + ValueDiff));

                    PluginFactory.LogHandle?.Report(new LogEventImage("HSV Piece Bg Color (" + pieceBackgroundColor.Hue + " ; " + pieceBackgroundColor.Satuation + "; " + pieceBackgroundColor.Value + ")", Utils.Combine2ImagesHorizontal(Utils.Combine2ImagesHorizontal(lowPieceBgColorImg.Convert<Rgb, byte>(), pieceBgColorImg.Convert<Rgb, byte>(), 0), highPieceBgColorImg.Convert<Rgb, byte>(), 0).Bitmap));

                    pieceBgColorImg.Dispose();
                    lowPieceBgColorImg.Dispose();
                    highPieceBgColorImg.Dispose();
                }

                // do HSV segmentation and keep only the meanColor areas with some hysteresis as pieces
                mask = hsvSourceImg.InRange(new Hsv(pieceBackgroundColor.Hue - HueDiff, pieceBackgroundColor.Satuation - SaturationDiff, pieceBackgroundColor.Value - ValueDiff), new Hsv(pieceBackgroundColor.Hue + HueDiff, pieceBackgroundColor.Satuation + SaturationDiff, pieceBackgroundColor.Value + ValueDiff));

                // close small black gaps with morphological closing operation
                Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
                CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernel, new Point(-1, -1), 5, BorderType.Default, new MCvScalar(0));
            }
            return mask;
        }
    }
}
