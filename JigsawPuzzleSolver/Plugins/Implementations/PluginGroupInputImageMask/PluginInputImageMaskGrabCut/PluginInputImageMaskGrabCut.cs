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

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public override void ResetPluginSettingsToDefault()
        {
        }

        /// <summary>
        /// Perform GrabCut segmentation on input image to get a mask for the pieces (foreground).
        /// </summary>
        /// <param name="inputImg">Input image for segmentation</param>
        /// <returns>Mask for foreground</returns>
        public override Image<Gray, byte> GetMask(Image<Rgba, byte> inputImg)
        {
            Image<Gray, byte> mask = inputImg.Convert<Rgb, byte>().GrabCut(new Rectangle(1, 1, inputImg.Width - 1, inputImg.Height - 1), 2);
            mask = mask.ThresholdBinary(new Gray(2), new Gray(255));            // Change the mask. All values bigger than 2 get mapped to 255. All values equal or smaller than 2 get mapped to 0.
            return mask;
        }
    }
}
