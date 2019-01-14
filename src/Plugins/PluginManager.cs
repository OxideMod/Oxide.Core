using System;
using System.Collections.Generic;
using uMod.Logging;
using uMod.Utilities;

namespace uMod.Plugins
{
    public delegate void PluginEvent(Plugin plugin);

    /// <summary>
    /// Manages a set of plugins
    /// </summary>
    public sealed class PluginManager
    {
        /// <summary>
        /// Gets the logger to which this plugin manager writes
        /// </summary>
        public Logger Logger { get; private set; }

        /// <summary>
        /// Gets or sets the path for plugin configs
        /// </summary>
        public string ConfigPath { get; set; }

        /// <summary>
        /// Called when a plugin has been added
        /// </summary>
        public event PluginEvent OnPluginAdded;

        /// <summary>
        /// Called when a plugin has been removed
        /// </summary>
        public event PluginEvent OnPluginRemoved;

        // All loaded plugins
        private readonly IDictionary<string, Plugin> loadedPlugins;

        // All hook subscriptions
        private readonly IDictionary<string, IList<Plugin>> hookSubscriptions;

        // Stores the last time a deprecation warning was printed for a specific hook
        private readonly Dictionary<string, float> lastDeprecatedWarningAt = new Dictionary<string, float>();

        // Re-usable conflict list used for hook calls
        private readonly List<string> hookConflicts = new List<string>();

        /// <summary>
        /// Initializes a new instance of the PluginManager class
        /// </summary>
        public PluginManager(Logger logger)
        {
            // Initialize
            loadedPlugins = new Dictionary<string, Plugin>();
            hookSubscriptions = new Dictionary<string, IList<Plugin>>();
            Logger = logger;
        }

        /// <summary>
        /// Adds a plugin to this manager
        /// </summary>
        /// <param name="plugin"></param>
        public bool AddPlugin(Plugin plugin)
        {
            if (!loadedPlugins.ContainsKey(plugin.Name))
            {
                loadedPlugins.Add(plugin.Name, plugin);
                plugin.HandleAddedToManager(this);
                OnPluginAdded?.Invoke(plugin);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Removes a plugin from this manager
        /// </summary>
        /// <param name="plugin"></param>
        /// <returns></returns>
        public bool RemovePlugin(Plugin plugin)
        {
            if (loadedPlugins.ContainsKey(plugin.Name))
            {
                loadedPlugins.Remove(plugin.Name);
                foreach (IList<Plugin> list in hookSubscriptions.Values)
                {
                    if (list.Contains(plugin))
                    {
                        list.Remove(plugin);
                    }
                }
                plugin.HandleRemovedFromManager(this);
                OnPluginRemoved?.Invoke(plugin);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets a plugin by name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Plugin GetPlugin(string name)
        {
            return loadedPlugins.TryGetValue(name, out Plugin plugin) ? plugin : null;
        }

        /// <summary>
        /// Gets all plugins managed by this manager
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Plugin> GetPlugins() => loadedPlugins.Values;

        /// <summary>
        /// Subscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="plugin"></param>
        internal void SubscribeToHook(string hook, Plugin plugin)
        {
            if (loadedPlugins.ContainsKey(plugin.Name) && (plugin.IsCorePlugin || !hook.StartsWith("I")))
            {
                if (!hookSubscriptions.TryGetValue(hook, out IList<Plugin> sublist))
                {
                    sublist = new List<Plugin>();
                    hookSubscriptions.Add(hook, sublist);
                }

                if (!sublist.Contains(plugin))
                {
                    sublist.Add(plugin);
                }
            }
        }

        /// <summary>
        /// Unsubscribes the specified plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="plugin"></param>
        internal void UnsubscribeToHook(string hook, Plugin plugin)
        {
            if (loadedPlugins.ContainsKey(plugin.Name) && (plugin.IsCorePlugin || !hook.StartsWith("I")))
            {
                if (hookSubscriptions.TryGetValue(hook, out IList<Plugin> sublist) && sublist.Contains(plugin))
                {
                    sublist.Remove(plugin);
                }
            }
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hook, params object[] args)
        {
            // Locate the sublist
            if (!hookSubscriptions.TryGetValue(hook, out IList<Plugin> plugins) || plugins.Count == 0)
            {
                return null;
            }

            // Loop each item
            object[] values = ArrayPool.Get(plugins.Count);
            int returnCount = 0;
            object finalValue = null;
            Plugin finalPlugin = null;
            for (int i = 0; i < plugins.Count; i++)
            {
                // Call the hook
                object value = plugins[i].CallHook(hook, args);
                if (value != null)
                {
                    values[i] = value;
                    finalValue = value;
                    finalPlugin = plugins[i];
                    returnCount++;
                }
            }

            // Return null if no return value
            if (returnCount == 0)
            {
                ArrayPool.Free(values);
                return null;
            }

            // Only show conflict warnings if return has value and is a "Can" hook
            if (returnCount > 1 && finalValue != null && hook.StartsWith("Can"))
            {
                // Notify log of hook conflict
                hookConflicts.Clear();
                for (int i = 0; i < plugins.Count; i++)
                {
                    object value = values[i];
                    if (value != null)
                    {
                        if (value.GetType().IsValueType)
                        {
                            if (!values[i].Equals(finalValue))
                            {
                                hookConflicts.Add($"{plugins[i].Name} - {value} ({value.GetType().Name})");
                            }
                        }
                        else
                        {
                            if (values[i] != finalValue)
                            {
                                hookConflicts.Add($"{plugins[i].Name} - {value} ({value.GetType().Name})");
                            }
                        }
                    }
                }
                if (hookConflicts.Count > 0)
                {
                    hookConflicts.Add($"{finalPlugin.Name} ({finalValue} ({finalValue.GetType().Name}))");
                    Logger.Write(LogType.Warning, "Calling hook {0} resulted in a conflict between the following plugins: {1}", hook, string.Join(", ", hookConflicts.ToArray()));
                }
            }
            ArrayPool.Free(values);

            return finalValue;
        }

        /// <summary>
        /// Calls a hook on all plugins of this manager and prints a deprecation warning
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            if (!hookSubscriptions.TryGetValue(oldHook, out IList<Plugin> plugins) || plugins.Count == 0 || expireDate < DateTime.Now)
            {
                return null;
            }

            float now = Interface.uMod.Now;
            if (!lastDeprecatedWarningAt.TryGetValue(oldHook, out float lastWarningAt) || now - lastWarningAt > 300f)
            {
                Plugin plugin = plugins[0];
                lastDeprecatedWarningAt[oldHook] = now;
                Interface.uMod.LogWarning($"'{plugin.Name} v{plugin.Version}' is using obsolete hook '{oldHook}', which will stop working on {expireDate:D}. Please ask the author to update to '{newHook}'");
            }

            return CallHook(oldHook, args);
        }
    }
}
