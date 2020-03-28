using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("GenerateSolutionImage Stitching")]
    [PluginDescription("Plugin for solution image generation stitching pieces together")]
    public class PluginGenerateSolutionImageStitching : PluginGroupGenerateSolutionImage
    {
        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        {
            return null;
        }
    }
}
