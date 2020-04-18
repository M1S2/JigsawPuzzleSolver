using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Core
{
    /// <summary>
    /// Attribute containing the name for a plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginNameAttribute : Attribute
    {
        public string Name { get; set; }

        public PluginNameAttribute(string name)
        {
            Name = name;
        }
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing the description for a plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class PluginDescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public PluginDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute that should be set by developers to plugins that should be marked as favorite (e.g. because they work the best).
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    class PluginIsDevFavoriteAttribute : Attribute
    {
    }

    //##############################################################################################################################################################################################

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

    //##############################################################################################################################################################################################

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
