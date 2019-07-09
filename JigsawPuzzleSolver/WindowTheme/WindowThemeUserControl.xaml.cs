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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MahApps.Metro;

namespace JigsawPuzzleSolver.WindowTheme
{
    /// <summary>
    /// Interaction logic for WindowThemeUserControl.xaml
    /// To use this control, create two application settings "AppTheme" and "AppAccent" to store the current configuration (strings).
    /// </summary>
    public partial class WindowThemeUserControl : UserControl, INotifyPropertyChanged 
    {
        #region INotifyPropertyChanged implementation
        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        [field: NonSerialized]
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// This method is called by the Set accessor of each property. The CallerMemberName attribute that is applied to the optional propertyName parameter causes the property name of the caller to be substituted as an argument.
        /// </summary>
        /// <param name="propertyName">Name of the property that is changed</param>
        /// see: https://docs.microsoft.com/de-de/dotnet/framework/winforms/how-to-implement-the-inotifypropertychanged-interface
        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion

        //##############################################################################################################################################################################################

        /// <summary>
        /// List with all App Themes (Light, Dark)
        /// </summary>
        public List<AppThemeMenuData> AppThemes { get; set; }

        /// <summary>
        /// List with all Accent Colors
        /// </summary>
        public List<AccentColorMenuData> AccentColors { get; set; }

        public AppThemeMenuData CurrentAppTheme => AppThemes.Where(t => t.Name == ThemeManager.DetectTheme().BaseColorScheme).FirstOrDefault();
        public AccentColorMenuData CurrentAppAccent => AccentColors.Where(a => a.Name == ThemeManager.DetectTheme().ColorScheme).FirstOrDefault();

        public WindowThemeUserControl()
        {
            // Load all available themes and accents
            AppThemes = ThemeManager.Themes.GroupBy(x => x.BaseColorScheme).Select(x => x.First()).Select(a => new AppThemeMenuData() { Name = a.BaseColorScheme, BorderColorBrush = a.Resources["BlackColorBrush"] as Brush, ColorBrush = a.Resources["WhiteColorBrush"] as Brush }).ToList();
            AccentColors = ThemeManager.ColorSchemes.Select(a => new AccentColorMenuData { Name = a.Name, ColorBrush = a.ShowcaseBrush }).ToList();

            InitializeComponent();
            ThemeManager.IsThemeChanged += ThemeManager_IsThemeChanged;
            this.Loaded += WindowThemeUserControl_Loaded;
        }

        /// <summary>
        /// Update the selected item property for each theme/accent when the theme/accent is changed
        /// </summary>
        private void ThemeManager_IsThemeChanged(object sender, OnThemeChangedEventArgs e)
        {
            OnPropertyChanged("CurrentAppTheme");
            OnPropertyChanged("CurrentAppAccent");

            AppThemes.ForEach(t => t.IsSelectedItem = (t == CurrentAppTheme));
            AccentColors.ForEach(a => a.IsSelectedItem = (a == CurrentAppAccent));
        }

        private void WindowThemeUserControl_Loaded(object sender, RoutedEventArgs e)
        {
            Window window = Window.GetWindow(this);
            window.Closing += window_Closing;
            
            // Get the saved theme and accent
            AppThemeMenuData savedTheme = AppThemes.Where(t => t.Name == Properties.Settings.Default.AppTheme).FirstOrDefault();
            AccentColorMenuData savedAccent = AccentColors.Where(a => a.Name == Properties.Settings.Default.AppAccent).FirstOrDefault();

            // Change to the saved theme and accent
            savedTheme?.ChangeAccentCommand.Execute(null);
            savedAccent?.ChangeAccentCommand.Execute(null);
        }

        /// <summary>
        /// Save the current theme and accent to the application settings
        /// </summary>
        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.AppTheme = ThemeManager.DetectTheme().BaseColorScheme;
            Properties.Settings.Default.AppAccent = ThemeManager.DetectTheme().ColorScheme;
            Properties.Settings.Default.Save();
        }
    }
}
