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
using JigsawPuzzleSolver.GUI_Elements;
using JigsawPuzzleSolver.Plugins.AbstractClasses;

namespace JigsawPuzzleSolver.Plugins.Controls
{
    /// <summary>
    /// Interaktionslogik für PluginSettingHsvSegmentationColorPicker.xaml
    /// </summary>
    public partial class PluginSettingHsvSegmentationColorPicker : PluginSettingsBaseUserControl
    {
        private ICommand _pickColorCommand;
        public ICommand PickColorCommand
        {
            get
            {
                if (_pickColorCommand == null)
                {
                    _pickColorCommand = new RelayCommand(param =>
                    {
                        // Find PuzzleHandle in MainWindow
                        Puzzle mainWindowPuzzleHandle = null;
                        foreach(Window window in  Application.Current.Windows)
                        {
                            if(window.GetType() == typeof(MainWindow)) { mainWindowPuzzleHandle = ((MainWindow)window).PuzzleHandle; break; }
                        }

                        PieceBackgroundColorPickerWindow colorPickerWindow = new PieceBackgroundColorPickerWindow(mainWindowPuzzleHandle?.PuzzlePiecesFolderPath, (System.Drawing.Color)CustomProp);
                        bool? windowResult = colorPickerWindow.ShowDialog();
                        if (windowResult.HasValue && windowResult.Value == true)
                        {
                            CustomProp = colorPickerWindow.SelectedBackgroundColor;
                        }
                    });
                }
                return _pickColorCommand;
            }
        }

        public PluginSettingHsvSegmentationColorPicker()
        {
            InitializeComponent();
        }
    }
}
