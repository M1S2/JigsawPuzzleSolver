using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace JigsawPuzzleSolver.Plugins.AbstractClasses
{
    /// <summary>
    /// Base class for all plugins
    /// </summary>
    [DataContract]
    public abstract class Plugin : SaveableObject<Plugin>, INotifyPropertyChanged
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

        private bool _isEnabled;
        /// <summary>
        /// Is the plugin enabled or not
        /// </summary>
        [DataMember]
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set
            {
                bool tmp_is_enabled = _isEnabled;
                _isEnabled = value;
                if (tmp_is_enabled != _isEnabled) { OnPropertyChanged(); }
            }
        }

        /// <summary>
        /// true, if more than one plugin is in the corresponding plugin group
        /// </summary>
        public bool MoreThanOnePluginInPluginGroup
        {
            get
            {
                Type pluginGroupType = this.GetType().BaseType;
                return PluginFactory.GetPluginsOfGroupType(pluginGroupType).Count > 1;
            }
        }

        //##############################################################################################################################################################################################

        /// <summary>
        /// Reset the plugin settings to default values
        /// </summary>
        public abstract void ResetPluginSettingsToDefault();
    }
}
