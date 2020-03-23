using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    public class PluginGenerateSolutionImageStitching : PluginGenerateSolutionImage
    {
        public PluginGenerateSolutionImageStitching()
        {
            Name = "GenerateSolutionImage Stitching";
        }

        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        {
            return null;
        }
    }
}
