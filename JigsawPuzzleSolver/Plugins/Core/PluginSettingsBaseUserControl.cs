using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace JigsawPuzzleSolver.Plugins.Core
{
    /// <summary>
    /// Base Class for custom plugin settings user controls
    /// </summary>
    public class PluginSettingsBaseUserControl : UserControl
    {
        public static readonly DependencyProperty CustomPropProperty = DependencyProperty.Register("CustomProp", typeof(object), typeof(PluginSettingsBaseUserControl));
        /// <summary>
        /// Property that is changed as plugin setting
        /// </summary>
        public object CustomProp
        {
            get { return (object)GetValue(CustomPropProperty); }
            set { SetValue(CustomPropProperty, value); }
        }
    }
}
