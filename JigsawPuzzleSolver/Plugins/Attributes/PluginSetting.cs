using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JigsawPuzzleSolver.Plugins.Attributes
{
    /// <summary>
    /// Base attribute for a plugin settings
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingAttribute : Attribute
    { }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing the description for a plugin setting
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingDescriptionAttribute : Attribute
    {
        public string Description { get; set; }

        public PluginSettingDescriptionAttribute(string description)
        {
            Description = description;
        }
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing the control type for a plugin setting
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingCustomControlAttribute : PluginSettingAttribute
    {
        public Type ControlType { get; set; }

        public PluginSettingCustomControlAttribute(Type controlType)
        {
            ControlType = controlType;
        }
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing one string setting for a plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingStringAttribute : PluginSettingAttribute
    {
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing one bool setting for a plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingBoolAttribute : PluginSettingAttribute
    {
        /// <summary>
        /// On label text for the ToggleSwitch control
        /// </summary>
        public string OnLabelText { get; set; }

        /// <summary>
        /// Off label text for the ToggleSwitch control
        /// </summary>
        public string OffLabelText { get; set; }

        public PluginSettingBoolAttribute(string onLabelText, string offLabelText)
        {
            OnLabelText = onLabelText;
            OffLabelText = offLabelText;
        }
    }

    //##############################################################################################################################################################################################

    /// <summary>
    /// Attribute containing one numeric setting for a plugin
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class PluginSettingNumberAttribute : PluginSettingAttribute
    {
        /// <summary>
        /// Interval value for the NumericUpDown control
        /// </summary>
        public double Interval { get; set; }

        /// <summary>
        /// Minimum value for the NumericUpDown control
        /// </summary>
        public double Minimum { get; set; }

        /// <summary>
        /// Maximum value for the NumericUpDown control
        /// </summary>
        public double Maximum { get; set; }

        public PluginSettingNumberAttribute(double interval, double minimum, double maximum)
        {
            Interval = interval;
            Minimum = minimum;
            Maximum = maximum;
        }
    }
}
