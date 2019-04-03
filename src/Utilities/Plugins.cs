using System.Linq;
using uMod.Plugins;

namespace uMod.Utilities
{
    /// <summary>
    /// Utility methods to help with plugin management
    /// </summary>
    public class Plugins
    {
        /// <summary>
        /// Returns if a plugin has been loaded by the specified name
        /// </summary>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public bool Exists(string pluginName) => Interface.uMod.RootPluginManager.GetPlugin(pluginName) != null;

        /// <summary>
        /// Returns the object of a loaded plugin with the specified name
        /// </summary>
        /// <param name="pluginName"></param>
        /// <returns></returns>
        public Plugin Find(string pluginName) => Interface.uMod.RootPluginManager.GetPlugin(pluginName);

        /// <summary>
        /// Calls the specified hook
        /// </summary>
        /// <param name="hookName"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookName, params object[] args) => Interface.Call(hookName, args);

        /// <summary>
        /// Gets an array of all currently loaded plugins
        /// </summary>
        /// <returns></returns>
        public Plugin[] GetAll() => Interface.uMod.RootPluginManager.GetPlugins().ToArray();
    }
}
