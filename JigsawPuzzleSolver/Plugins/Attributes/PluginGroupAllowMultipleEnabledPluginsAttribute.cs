using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
    /// <summary>
    /// Attribute containing the information, if multiple plugins per plugin group can be enabled. This attribute should only be assigned to plugin groups.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class PluginGroupAllowMultipleEnabledPluginsAttribute : Attribute
    {
        public bool AllowMultipleEnabledPlugins { get; set; }

        public PluginGroupAllowMultipleEnabledPluginsAttribute(bool allowMultipleEnabledPlugins)
        {
            AllowMultipleEnabledPlugins = allowMultipleEnabledPlugins;
        }
    }
}
