using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    public class PluginGenerateSolutionImageSimple : PluginGenerateSolutionImage
    {
        public PluginGenerateSolutionImageSimple()
        {
            Name = "GenerateSolutionImage Simple";
        }

        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        {
            return null;
        }

    }
}
