using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.AbstractClasses;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    public class PluginInputImageMaskHsvSegmentation : PluginInputImageMask
    {
        public PluginInputImageMaskHsvSegmentation()
        {
            Name = "InputImageMask HSV Segmentation";
        }

        public override Image<Gray, byte> getMask(Image<Rgba, byte> inputImg)
        {
            return null;
        }
    }
}
