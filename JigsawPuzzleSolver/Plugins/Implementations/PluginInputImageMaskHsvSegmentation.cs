using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using JigsawPuzzleSolver.Plugins.Controls;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("InputImageMask HSV Segmentation")]
    [PluginDescription("Plugin for generating binary mask from input image using HSV segmentation")]
    public class PluginInputImageMaskHsvSegmentation : PluginGroupInputImageMask
    {
        public override PackIconBase PluginIcon => new PackIconMaterial() { Kind = PackIconMaterialKind.Palette };

        //##############################################################################################################################################################################################

        private Color _pieceBackgroundColor;
        [PluginSettingCustomControl(typeof(PluginSettingHsvSegmentationColorPicker))]
        [PluginSettingDescription("Background color of pieces")]
        public Color PieceBackgroundColor
        {
            get { return _pieceBackgroundColor; }
            set { _pieceBackgroundColor = value; OnPropertyChanged(); }
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
            PieceBackgroundColor = Color.Black;
            HueDiff = 20;
            SaturationDiff = 30;
            ValueDiff = 30;
        }

        /// <summary>
        /// Calculate a mask for the pieces using HSV segmentation.
        /// </summary>
        /// <param name="inputImg">Color input image</param>
        /// <returns>Mask image</returns>
        public override Image<Gray, byte> GetMask(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask;
            using (Image<Hsv, byte> hsvSourceImg = inputImg.Convert<Hsv, byte>())
            {
                double h = 0, s = 0, v = 0;
                Utils.ColorToHSV(PieceBackgroundColor, out h, out s, out v);

                mask = hsvSourceImg.InRange(new Hsv(h - HueDiff, s - SaturationDiff, v - ValueDiff), new Hsv(h + HueDiff, s + SaturationDiff, v + ValueDiff));

                // close small black gaps with morphological closing operation
                Mat kernel = CvInvoke.GetStructuringElement(ElementShape.Rectangle, new Size(5, 5), new Point(-1, -1));
                CvInvoke.MorphologyEx(mask, mask, MorphOp.Close, kernel, new Point(-1, -1), 5, BorderType.Default, new MCvScalar(0));
            }
            return mask;
        }
    }
}
