using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;

namespace JigsawPuzzleSolver.Plugins.AbstractClasses
{
    public abstract class PluginGenerateSolutionImage : Plugin
    {
        public abstract Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID);
    }
}
