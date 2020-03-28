using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("InputImageMask HSV Histogram")]
    [PluginDescription("Plugin for generating binary mask from input image using HSV histogram")]
    public class PluginInputImageMaskHsvHistogram : PluginGroupInputImageMask
    {
        public override Image<Gray, byte> getMask(Image<Rgba, byte> inputImg)
        {
            return null;
        }
    }
}
