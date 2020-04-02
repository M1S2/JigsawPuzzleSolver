using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins.Controls
{
    /// <summary>
    /// Group Description that uses the PluginNameAttribute value to group items
    /// </summary>
    /// see: https://www.dotnetcurry.com/wpf/1211/wpf-items-control-advanced-topic
    public sealed class PluginGroupGroupDescription : GroupDescription
    {
        public override object GroupNameFromItem(object item, int level, CultureInfo culture)
        {
            Type pluginGroupType = item.GetType().BaseType;
            object nameAttribute = pluginGroupType.GetCustomAttributes(typeof(PluginNameAttribute), false).FirstOrDefault();
            if(nameAttribute == null) { return ""; }
            else { return ((PluginNameAttribute)nameAttribute).Name; }
        }
    }
}
