using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Emgu.CV;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Attributes;
using JigsawPuzzleSolver.Plugins.Controls;

namespace JigsawPuzzleSolver.Plugins.Implementations
{
    [PluginName("GenerateSolutionImage Simple")]
    [PluginDescription("Plugin for solution image generation placing pieces side by side")]
    public class PluginGenerateSolutionImageSimple : PluginGroupGenerateSolutionImage
    {
        [PluginSettingString]
        [PluginSettingDescription("Test setting No. 1")]
        public string TestSetting1 { get; set; }

        [PluginSettingBool("Enabled", "Disabled")]
        [PluginSettingDescription("Test setting No. 2")]
        public bool TestSetting2 { get; set; }

        [PluginSettingNumber(0.1, 0, 5)]
        [PluginSettingDescription("Test setting No. 3")]
        public double TestSetting3 { get; set; }

        [PluginSettingCustomControl(typeof(PluginSettingHsvSegmentationColorPicker))]
        [PluginSettingDescription("Test setting No. 4")]
        public Color TestSetting4 { get; set; }

        public override Bitmap GenerateSolutionImage(Matrix<int> solutionLocations, int solutionID)
        {
            return null;
        }

    }
}
