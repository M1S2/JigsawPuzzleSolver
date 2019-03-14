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
using MahApps.Metro;

namespace JigsawPuzzleSolver.WindowTheme
{
    /// <summary>
    /// Interaction logic for WindowThemeUserControl.xaml
    /// </summary>
    public partial class WindowThemeUserControl : UserControl
    {
        /// <summary>
        /// List with all App Themes (Light, Dark)
        /// </summary>
        public List<AppThemeMenuData> AppThemes { get; set; }

        /// <summary>
        /// List with all Accent Colors
        /// </summary>
        public List<AccentColorMenuData> AccentColors { get; set; }

        public WindowThemeUserControl()
        {
            AppThemes = ThemeManager.Themes.GroupBy(x => x.BaseColorScheme).Select(x => x.First()).Select(a => new AppThemeMenuData() { Name = a.BaseColorScheme, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush }).ToList();
            AccentColors = ThemeManager.ColorSchemes.Select(a => new AccentColorMenuData { Name = a.Name, ColorBrush = a.ShowcaseBrush }).ToList();

            InitializeComponent();
        }
    }
}
