using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
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
}
