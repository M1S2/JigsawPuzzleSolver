using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using JigsawPuzzleSolver.Plugins.AbstractClasses;
using JigsawPuzzleSolver.Plugins.Implementations;
using JigsawPuzzleSolver.Plugins.Attributes;

namespace JigsawPuzzleSolver.Plugins
{
    public static class PluginFactory
    {
        /// <summary>
        /// List with types of abstract plugin group types
        /// </summary>
        public static List<Type> PluginGroupTypes { get; set; } = new List<Type>();

        /// <summary>
        /// List with instances of all available plugins
        /// </summary>
        public static List<Plugin> AvailablePlugins { get; set; } = new List<Plugin>();

        /// <summary>
        /// Static constructor. Fill lists of availabe plugins and plugin group types.
        /// </summary>
        static PluginFactory()
        {
            Assembly executingAssembly = Assembly.GetExecutingAssembly();
            List<Type> pluginTypes = executingAssembly.GetTypes().Where(t => typeof(Plugin).IsAssignableFrom(t) && !t.IsAbstract).ToList();           
            foreach(Type t in pluginTypes)
            {
                Plugin plugin = (Plugin)Activator.CreateInstance(t);
                plugin.PropertyChanged += Plugin_PropertyChanged;
                AvailablePlugins.Add(plugin);
            }
            PluginGroupTypes = executingAssembly.GetTypes().Where(t => typeof(Plugin).IsAssignableFrom(t) && t != typeof(Plugin) && t.IsAbstract).ToList();
            EnsureAllowedNumberOfPluginsPerGroupIsEnabled();
        }


        private static void Plugin_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "IsEnabled")
            {
                Plugin senderPlugin = sender as Plugin;
                if(senderPlugin == null) { return; }

                EnsureAllowedNumberOfPluginsPerGroupIsEnabled(senderPlugin);
            }
        }

        private static bool alreadyInEnsureFunction = false;
        /// <summary>
        /// Ensure that one or more than one (depending on the PluginGroupAllowMultipleEnabledPluginsAttribute) plugin per plugin group is enabled.
        /// </summary>
        /// <param name="currentlyChangedPlugin">The given plugin isn't immediatelly changed back to is old value if possible</param>
        private static void EnsureAllowedNumberOfPluginsPerGroupIsEnabled(Plugin currentlyChangedPlugin = null)
        {
            if (alreadyInEnsureFunction) { return; }
            alreadyInEnsureFunction = true;

            foreach(Type pluginGroupType in PluginGroupTypes)
            {
                List<Plugin> pluginsOfType = GetPluginsOfGroupType(pluginGroupType);
                if(pluginsOfType.Count == 0) { continue; }
                int numberEnabledPlugins = pluginsOfType.Count(p => p.IsEnabled);

                bool allowMultipleEnabled = false;
                object[] customAttributes = pluginGroupType.GetCustomAttributes(false);
                if(customAttributes != null && customAttributes.Length > 0 && customAttributes[0].GetType() == typeof(PluginGroupAllowMultipleEnabledPluginsAttribute))
                {
                    allowMultipleEnabled = ((PluginGroupAllowMultipleEnabledPluginsAttribute)customAttributes[0]).AllowMultipleEnabledPlugins;
                }

                if (numberEnabledPlugins < 1)    // Number of enabled plugins is too small
                {
                    if (pluginsOfType.Count == 1 || currentlyChangedPlugin == null || pluginsOfType[0] != currentlyChangedPlugin)
                    {
                        pluginsOfType[0].IsEnabled = true;
                    }
                    else
                    {
                        pluginsOfType[1].IsEnabled = true;
                    }
                }
                else if(numberEnabledPlugins > 1 && !allowMultipleEnabled)    // exact one plugin is allowed to be enabled. Disable all others.
                {
                    foreach (Plugin plugin in pluginsOfType)
                    {
                        plugin.IsEnabled = (plugin == currentlyChangedPlugin);
                    }
                }
                else
                {
                    // nothing to do here, because either exact one plugin is enabled or exactOne is false (only exact one enabled plugins is allowed)
                }
            }
            alreadyInEnsureFunction = false;
        }

        /// <summary>
        /// Get all available plugins of the given plugin group type.
        /// </summary>
        /// <param name="pluginGroupType">Type of the plugin group</param>
        /// <returns>List of all plugins in this plugin group</returns>
        public static List<Plugin> GetPluginsOfGroupType(Type pluginGroupType)
        {
            return AvailablePlugins.Where(p => pluginGroupType.IsAssignableFrom(p.GetType())).ToList();
        }
        public static List<T> GetPluginsOfGroupType<T>() where T : Plugin
        {
            return GetPluginsOfGroupType(typeof(T)).Cast<T>().ToList();
        }

        /// <summary>
        /// Get all available and enabled plugins of the given plugin group type.
        /// </summary>
        /// <param name="pluginGroupType">Type of the plugin group</param>
        /// <returns>List of all enabled plugins in this plugin group</returns>
        public static List<Plugin> GetEnabledPluginsOfGroupType(Type pluginGroupType)
        {
            return GetPluginsOfGroupType(pluginGroupType).Where(p => p.IsEnabled).ToList();
        }
        public static List<T> GetEnabledPluginsOfGroupType<T>() where T : Plugin
        {
            return GetEnabledPluginsOfGroupType(typeof(T)).Cast<T>().ToList();
        }
    }
}
