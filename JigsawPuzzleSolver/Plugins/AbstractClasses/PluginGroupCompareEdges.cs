using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins.AbstractClasses
{
    /// <summary>
    /// Plugin group base class for edge comparison plugins
    /// </summary>
    [PluginGroupAllowMultipleEnabledPlugins(false)]
    [PluginGroupOrderIndex(4)]
    [PluginName("Compare Edges Plugins")]
    public abstract class PluginGroupCompareEdges : Plugin
    {
        public abstract double CompareEdges(Edge edge1, Edge edge2);
    }
}
