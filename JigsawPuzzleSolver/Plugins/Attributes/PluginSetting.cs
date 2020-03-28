using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingDescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public PluginSettingDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingStringAttribute : PluginSettingAttribute
    {
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingBoolAttribute : PluginSettingAttribute
    {
        public string OnLabelText { get; set; }
        public string OffLabelText { get; set; }

        public PluginSettingBoolAttribute(string onLabelText, string offLabelText)
        {
            OnLabelText = onLabelText;
            OffLabelText = offLabelText;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingNumberAttribute : PluginSettingAttribute
    {
        public double Interval { get; set; }
        public double Minimum { get; set; }
        public double Maximum { get; set; }

        public PluginSettingNumberAttribute(double interval, double minimum, double maximum)
        {
            Interval = interval;
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}
