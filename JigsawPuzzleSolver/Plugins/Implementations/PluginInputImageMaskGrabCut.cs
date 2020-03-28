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
    [PluginName("InputImageMask Grab Cut")]
    [PluginDescription("Plugin for generating binary mask from input image using GrabCut algorithm")]
    public class PluginInputImageMaskGrabCut : PluginGroupInputImageMask
    {
        public override Image<Gray, byte> getMask(Image<Rgba, byte> inputImg)
        {
            return null;
        }
    }
}
