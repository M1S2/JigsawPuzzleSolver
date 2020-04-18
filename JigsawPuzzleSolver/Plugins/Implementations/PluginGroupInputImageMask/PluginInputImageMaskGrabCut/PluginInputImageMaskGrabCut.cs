using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.Core;
using MahApps.Metro.IconPacks;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupInputImageMask
{
    [PluginName("InputImageMask Grab Cut")]
    [PluginDescription("Plugin for generating binary mask from input image using GrabCut algorithm")]
    public class PluginInputImageMaskGrabCut : PluginGroupInputImageMask
    {
        public override PackIconBase PluginIcon => new PackIconMaterial() { Kind = PackIconMaterialKind.ContentCut };

        //##############################################################################################################################################################################################

        private int _maxInputImageWidth;
        [PluginSettingNumber(1, 1, 10000)]
        [PluginSettingDescription("The input image width is limited to this value.")]
        public int MaxInputImageWidth
        {
            get { return _maxInputImageWidth; }
            set { _maxInputImageWidth = value; OnPropertyChanged(); }
        }

        private int _maxInputImageHeight;
        [PluginSettingNumber(1, 1, 10000)]
        [PluginSettingDescription("The input image height is limited to this value.")]
        public int MaxInputImageHeight
        {
            get { return _maxInputImageHeight; }
            set { _maxInputImageHeight = value; OnPropertyChanged(); }
        }

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
            MaxInputImageWidth = 1000;
            MaxInputImageHeight = 1000;
        }

        /// <summary>
        /// Perform GrabCut segmentation on input image to get a mask for the pieces (foreground).
        /// </summary>
        /// <param name="inputImg">Input image for segmentation</param>
        /// <returns>Mask for foreground</returns>
        public override Image<Gray, byte> GetMask(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask = null;

            Task task = Task.Run(() =>
            {
                Image<Rgba, byte> tmpInputImg = inputImg.LimitImageSize(MaxInputImageWidth, MaxInputImageHeight);
                mask = tmpInputImg.Convert<Rgb, byte>().GrabCut(new Rectangle(1, 1, tmpInputImg.Width - 1, tmpInputImg.Height - 1), 2);
                tmpInputImg.Dispose();
                mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.
            }, PluginFactory.CancelToken);
            task.Wait(PluginFactory.CancelToken);
            return mask;
        }
    }
}
