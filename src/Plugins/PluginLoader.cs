using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace uMod.Plugins
{
    /// <summary>
    /// Represents a loader for a certain type of plugin
    /// </summary>
    public abstract class PluginLoader
    {
        /// <summary>
        /// Stores the names of plugins which are currently loading asynchronously
        /// </summary>
        public ConcurrentHashSet<string> LoadingPlugins { get; } = new ConcurrentHashSet<string>();

        /// <summary>
        /// Optional loaded plugin instances used by loaders which need to be notified before a plugin is unloaded
        /// </summary>
        public Dictionary<string, Plugin> LoadedPlugins = new Dictionary<string, Plugin>();

        /// <summary>
        /// Stores the last error a plugin had while loading
        /// </summary>
        public Dictionary<string, string> PluginErrors { get; } = new Dictionary<string, string>();

        /// <summary>
        /// Stores the names of core plugins which should never be unloaded
        /// </summary>
        public virtual Type[] CorePlugins { get; } = new Type[0];

        /// <summary>
        /// Stores the plugin file extension which this loader supports
        /// </summary>
        public virtual string FileExtension { get; }

        /// <summary>
        /// Returns all files based on directory filter and file name pattern
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="directoryFilter"></param>
        /// <param name="filePattern"></param>
        /// <returns></returns>
        public static IEnumerable<FileInfo> GetFiles(DirectoryInfo rootDirectory, string filePattern, Func<DirectoryInfo, bool> directoryFilter)
        {
            foreach (FileInfo matchedFile in rootDirectory.GetFiles(filePattern, SearchOption.TopDirectoryOnly))
            {
                if ((matchedFile.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }

                yield return matchedFile;
            }

            IEnumerable<DirectoryInfo> matchedDirectories = rootDirectory.GetDirectories("*.*", SearchOption.TopDirectoryOnly).Where(directoryFilter);
            foreach (DirectoryInfo directory in matchedDirectories)
            {
                if ((directory.Attributes & FileAttributes.Hidden) == FileAttributes.Hidden)
                {
                    continue;
                }

                foreach (FileInfo matchedFile in GetFiles(directory, filePattern, directoryFilter))
                {
                    yield return matchedFile;
                }
            }
        }

        /// <summary>
        /// Scans the specified directory and returns a set of plugin names for plugins that this loader can load
        /// </summary>
        /// <param name="directory"></param>
        /// <returns></returns>
        public virtual IEnumerable<FileInfo> ScanDirectory(string directory)
        {
            if (FileExtension == null || !Directory.Exists(directory))
            {
                yield break;
            }

            DirectoryInfo rootDirectory = new DirectoryInfo(directory);
            string[] config = Interface.uMod.Config.Options.PluginDirectories;
            string[] ignoredDirectories = { ".git", "bin", "obj", "packages" };
            bool Filter(DirectoryInfo d) => !ignoredDirectories.Contains(d.Name.ToLower()) && config.Contains(d.Name.ToLower()) || config.Contains(d.Parent?.Name.ToLower());
            FileInfo[] files = GetFiles(rootDirectory, "*" + FileExtension, Filter).ToArray();
            foreach (FileInfo file in files)
            {
                yield return file;
            }
        }

        /// <summary>
        /// Loads a plugin given the specified name
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual Plugin Load(string directory, string name)
        {
            if (LoadingPlugins.Contains(name))
            {
                Interface.uMod.LogDebug($"Load requested for plugin which is already loading: {name}");
                return null;
            }

            string filename = Path.Combine(directory, name + FileExtension);
            Plugin plugin = GetPlugin(filename);
            LoadingPlugins.Add(plugin.Name);
            Interface.uMod.NextTick(() => LoadPlugin(plugin));

            return null;
        }

        /// <summary>
        /// Gets a plugin given the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        protected virtual Plugin GetPlugin(string filename) => null;

        /// <summary>
        /// Loads a given plugin
        /// </summary>
        /// <param name="plugin"></param>
        /// <param name="waitingForAccess"></param>
        protected void LoadPlugin(Plugin plugin, bool waitingForAccess = false)
        {
            if (!File.Exists(plugin.Filename))
            {
                LoadingPlugins.Remove(plugin.Name);
                Interface.uMod.LogWarning($"Script no longer exists: {plugin.Name}");
                return;
            }

            try
            {
                plugin.Load();
                Interface.uMod.UnloadPlugin(plugin.Name);
                LoadingPlugins.Remove(plugin.Name);
                Interface.uMod.PluginLoaded(plugin);
            }
            catch (IOException)
            {
                if (!waitingForAccess)
                {
                    Interface.uMod.LogWarning($"Waiting for another application to stop using script: {plugin.Name}");
                }

                Interface.uMod.GetLibrary<Libraries.Timer>().Once(.5f, () => LoadPlugin(plugin, true));
            }
            catch (Exception ex)
            {
                LoadingPlugins.Remove(plugin.Name);
                Interface.uMod.LogException($"Failed to load plugin {plugin.Name}", ex);
            }
        }

        /// <summary>
        /// Reloads a plugin given the specified name, implemented by plugin loaders which support reloading plugins
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public virtual void Reload(string directory, string name)
        {
            Interface.uMod.UnloadPlugin(name);
            Interface.uMod.LoadPlugin(name);
        }

        /// <summary>
        /// Called when a plugin which was loaded by this loader is being unloaded by the plugin manager
        /// </summary>
        /// <param name="plugin"></param>
        public virtual void Unloading(Plugin plugin)
        {
        }
    }
}
