using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using MahApps.Metro;
using JigsawPuzzleSolver.GUI_Elements;

namespace JigsawPuzzleSolver.WindowTheme
{
    /// <summary>
    /// Menu entry data for the Accent color menu.
    /// </summary>
    /// see: https://github.com/MahApps/MahApps.Metro/blob/develop/src/MahApps.Metro.Samples/MahApps.Metro.Demo/MainWindowViewModel.cs
    public class AccentColorMenuData : INotifyPropertyChanged
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

        private string _name;
        /// <summary>
        /// Menu entry name
        /// </summary>
        public string Name
        {
            get { return _name; }
            set { _name = value;  OnPropertyChanged(); }
        }

        private Brush _borderColorBrush;
        /// <summary>
        /// Menu entry border color brush
        /// </summary>
        public Brush BorderColorBrush
        {
            get { return _borderColorBrush; }
            set { _borderColorBrush = value;  OnPropertyChanged(); }
        }

        private Brush _colorBrush;
        /// <summary>
        /// Menu entry color brush
        /// </summary>
        public Brush ColorBrush
        {
            get { return _colorBrush; }
            set { _colorBrush = value; OnPropertyChanged(); }
        }

        private bool _isSelectedItem;
        /// <summary>
        /// True if this is the currently selected theme or accent
        /// </summary>
        public bool IsSelectedItem
        {
            get { return _isSelectedItem; }
            set { _isSelectedItem = value; OnPropertyChanged(); }
        }

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
