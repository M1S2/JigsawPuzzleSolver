using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Reflection;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using MahApps.Metro.Controls;

namespace JigsawPuzzleSolver.Plugins.Controls
{
    /// <summary>
    /// Interaktionslogik für PluginConfigurator.xaml
    /// </summary>
    public partial class PluginConfigurator : UserControl
    {
        public PluginConfigurator()
        {
            InitializeComponent();
        }

        private void ContentControl_Loaded(object sender, RoutedEventArgs e)
        {
            ContentControl contentCtrl = (ContentControl)sender;
            Plugin plugin = contentCtrl.Tag as Plugin;
            contentCtrl.Content = BuildSettingsControlForPlugin(plugin);
        }

        private UIElement BuildSettingsControlForPlugin(Plugin plugin)
        {
            List<PropertyInfo> propInfos = plugin.GetType().GetProperties().Where(p => p.GetCustomAttributes().Any(a => typeof(PluginSettingAttribute).IsAssignableFrom(a.GetType()))).ToList(); 
            if(propInfos.Count == 0) { return null; }

            Expander settingsExpander = new Expander() { Header = "Settings", Margin = new Thickness(0, 10, 0, 0), IsExpanded = true, Background = (Brush)this.FindResource("GrayBrush4") };
            StackPanel settingsPanel = new StackPanel() { Orientation = Orientation.Vertical };
            foreach (PropertyInfo propInfo in propInfos)
            {
                DockPanel singleSettingPanel = new DockPanel() { LastChildFill = true, Margin = new Thickness(0, 5, 0, 5) };
                singleSettingPanel.Children.Add(new TextBlock(new Run(propInfo.Name)) { Width = 100, VerticalAlignment = VerticalAlignment.Center });

                UIElement singleSettingControl = null;
                Binding binding = new Binding(propInfo.Name);

                if (propInfo.PropertyType == typeof(string) || propInfo.PropertyType == typeof(String))
                {
                    singleSettingControl = new TextBox();
                    BindingOperations.SetBinding(singleSettingControl, TextBox.TextProperty, binding);
                }
                else if (propInfo.PropertyType.IsNumeric())
                {
                    PluginSettingNumberAttribute attribute = propInfo.GetCustomAttribute<PluginSettingNumberAttribute>();

                    if (attribute != null) { singleSettingControl = new NumericUpDown() { Minimum = attribute.Minimum, Maximum = attribute.Maximum, Interval = attribute.Interval }; }
                    else { singleSettingControl = new NumericUpDown(); }
                    BindingOperations.SetBinding(singleSettingControl, NumericUpDown.ValueProperty, binding);
                }
                else if (propInfo.PropertyType == typeof(bool) || propInfo.PropertyType == typeof(Boolean))
                {
                    PluginSettingBoolAttribute attribute = propInfo.GetCustomAttribute<PluginSettingBoolAttribute>();

                    singleSettingControl = new ToggleSwitch() { Style = (Style)this.FindResource("MahApps.Metro.Styles.ToggleSwitch.Win10"), HorizontalAlignment = HorizontalAlignment.Right, OnLabel = attribute?.OnLabelText, OffLabel = attribute?.OffLabelText };
                    BindingOperations.SetBinding(singleSettingControl, CheckBox.IsCheckedProperty, binding);
                }

                if (singleSettingControl != null)
                {
                    singleSettingPanel.Children.Add(singleSettingControl);

                    PluginSettingDescriptionAttribute attribute = propInfo.GetCustomAttribute<PluginSettingDescriptionAttribute>();
                    if (attribute != null) { singleSettingPanel.ToolTip = new TextBlock(new Run(attribute?.Description)); }

                    settingsPanel.Children.Add(singleSettingPanel);
                }
            }
            settingsExpander.Content = settingsPanel;
            return settingsExpander;
        }

    }
}
