using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.Core;

namespace JigsawPuzzleSolver.Plugins.Implementations.GroupInputImageMask
{
    /// <summary>
    /// Plugin group base class for input image mask generation plugins
    /// </summary>
    [PluginGroupAllowMultipleEnabledPlugins(false)]
    [PluginGroupOrderIndex(2)]
    [PluginName("Input Image Mask Plugins")]
    public abstract class PluginGroupInputImageMask : Plugin
    {
        public abstract Image<Gray, byte> GetMask(Image<Rgba, byte> inputImg);
    }
}
