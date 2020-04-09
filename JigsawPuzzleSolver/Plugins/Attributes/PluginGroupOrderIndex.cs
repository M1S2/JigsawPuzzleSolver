using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
    /// <summary>
    /// Attribute containing the order index of the plugin group. This is used to order the plugin groups in the GUI. This attribute should only be assigned to plugin groups.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class PluginGroupOrderIndex : Attribute
    {
        public int OrderIndex { get; set; }

        public PluginGroupOrderIndex(int orderIndex)
        {
            OrderIndex = orderIndex;
        }
    }
}
