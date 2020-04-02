using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
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
}
