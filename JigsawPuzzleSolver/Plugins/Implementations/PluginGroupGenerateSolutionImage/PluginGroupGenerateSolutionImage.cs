using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.Core;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupGenerateSolutionImage
{
    /// <summary>
    /// Plugin group base class for solution image generation plugins
    /// </summary>
    [PluginGroupAllowMultipleEnabledPlugins(true)]
    [PluginGroupOrderIndex(5)]
    [PluginName("Generate Solution Image Plugins")]
    public abstract class PluginGroupGenerateSolutionImage : Plugin
    {
        public abstract Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID, List<Piece> Pieces);
    }
}
