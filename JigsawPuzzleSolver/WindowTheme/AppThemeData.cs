using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using MahApps.Metro;
using System.Windows.Input;
using JigsawPuzzleSolver.GUI_Elements;

namespace JigsawPuzzleSolver.WindowTheme
{
    /// <summary>
    /// Menu entry data for the Accent color menu.
    /// </summary>
    /// see: https://github.com/MahApps/MahApps.Metro/blob/develop/src/MahApps.Metro.Samples/MahApps.Metro.Demo/MainWindowViewModel.cs
    public class AccentColorMenuData
    {
        /// <summary>
        /// Menu entry name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Menu entry border color brush
        /// </summary>
        public Brush BorderColorBrush { get; set; }

        /// <summary>
        /// Menu entry color brush
        /// </summary>
        public Brush ColorBrush { get; set; }

        private ICommand changeAccentCommand;
        /// <summary>
        /// Command to change the accent color or theme
        /// </summary>
        public ICommand ChangeAccentCommand
        {
            get { return this.changeAccentCommand ?? (changeAccentCommand = new RelayCommand(x => this.DoChangeTheme(), x => true)); }
        }

        /// <summary>
        /// Change the Accent color
        /// </summary>
        protected virtual void DoChangeTheme()
        {
            ThemeManager.ChangeThemeColorScheme(Application.Current, this.Name);
        }
    }

    /// <summary>
    /// Menu entry data for the Theme menu.
    /// </summary>
    /// see: https://github.com/MahApps/MahApps.Metro/blob/develop/src/MahApps.Metro.Samples/MahApps.Metro.Demo/MainWindowViewModel.cs
    public class AppThemeMenuData : AccentColorMenuData
    {
        /// <summary>
        /// Change the Theme
        /// </summary>
        protected override void DoChangeTheme()
        {
            ThemeManager.ChangeThemeBaseColor(Application.Current, this.Name);
        }
    }
}
