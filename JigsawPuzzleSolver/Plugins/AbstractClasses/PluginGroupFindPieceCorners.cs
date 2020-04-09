using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins.AbstractClasses
{
    /// <summary>
    /// Plugin group base class for piece corner finder plugins
    /// </summary>
    [PluginGroupAllowMultipleEnabledPlugins(false)]
    [PluginGroupOrderIndex(3)]
    [PluginName("Piece Corner Finder Plugins")]
    public abstract class PluginGroupFindPieceCorners : Plugin
    {
        public abstract List<Point> FindCorners(string pieceID, Bitmap pieceImgBw, Bitmap pieceImgColor);
    }
}
