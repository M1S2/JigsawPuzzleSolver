using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
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
