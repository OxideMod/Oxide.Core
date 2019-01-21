using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using uMod.Libraries;
using uMod.Logging;
using uMod.Plugins;
using uMod.Plugins.Watchers;

namespace uMod.Extensions
{
    /// <summary>
    /// Responsible for managing all uMod extensions
    /// </summary>
    public sealed class ExtensionManager
    {
        // All loaded extensions
        private IList<Extension> extensions;

        // The search patterns for extensions
        private const string extSearchPattern = "uMod.*.dll";

        /// <summary>
        /// Gets the logger to which this extension manager writes
        /// </summary>
        public CompoundLogger Logger { get; private set; }

        // All registered plugin loaders
        private IList<PluginLoader> pluginLoaders;

        // All registered libraries
        private IDictionary<string, Library> libraries;

        // All registered watchers
        private IList<ChangeWatcher> changeWatchers;

        /// <summary>
        /// Initializes a new instance of the ExtensionManager class
        /// </summary>
        public ExtensionManager(CompoundLogger logger)
        {
            // Initialize
            Logger = logger;
            extensions = new List<Extension>();
            pluginLoaders = new List<PluginLoader>();
            libraries = new Dictionary<string, Library>();
            changeWatchers = new List<ChangeWatcher>();
        }

        #region Registering

        /// <summary>
        /// Registers the specified plugin loader
        /// </summary>
        /// <param name="loader"></param>
        public void RegisterPluginLoader(PluginLoader loader) => pluginLoaders.Add(loader);

        /// <summary>
        /// Gets all plugin loaders
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => pluginLoaders;

        /// <summary>
        /// Registers the specified library
        /// </summary>
        /// <param name="name"></param>
        /// <param name="library"></param>
        public void RegisterLibrary(string name, Library library)
        {
            if (libraries.ContainsKey(name))
            {
                Interface.uMod.LogError($"An extension tried to register an already registered library: {name}");
            }
            else
            {
                libraries[name] = library;
            }
        }

        /// <summary>
        /// Gets all library names
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetLibraries() => libraries.Keys;

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Library GetLibrary(string name)
        {
            return !libraries.TryGetValue(name, out Library lib) ? null : lib;
        }

        /// <summary>
        /// Registers the specified watcher
        /// </summary>
        /// <param name="watcher"></param>
        public void RegisterChangeWatcher(ChangeWatcher watcher) => changeWatchers.Add(watcher);

        /// <summary>
        /// Gets all plugin change watchers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<ChangeWatcher> GetChangeWatchers() => changeWatchers;

        #endregion Registering

        /// <summary>
        /// Loads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="forced"></param>
        public void LoadExtension(string filename, bool forced)
        {
            string name = Utility.GetFileNameWithoutExtension(filename);
            string pdbFileName = filename.Replace(".dll", "") + ".pdb";

            // Check if the extension is already loaded
            if (extensions.Any(x => x.Filename == filename))
            {
                Logger.Write(LogType.Error, $"Failed to load extension '{name}': extension already loaded.");
                return;
            }

            try
            {
                // Read the assembly from file
                byte[] data = File.ReadAllBytes(filename);
                Assembly assembly;

                if (File.Exists(pdbFileName))
                {
                    // Read debug information from file
                    byte[] pdbData = File.ReadAllBytes(pdbFileName);

                    // Load the assembly with debug data
                    assembly = Assembly.Load(data, pdbData);
                }
                else
                {
                    // Load the assembly
                    assembly = Assembly.Load(data);
                }

                // Search for a type that derives Extension
                Type extType = typeof(Extension);
                Type extensionType = null;
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (extType.IsAssignableFrom(type))
                    {
                        extensionType = type;
                        break;
                    }
                }

                if (extensionType == null)
                {
                    Logger.Write(LogType.Error, $"Failed to load extension {name} (Specified assembly does not implement an Extension class)");
                    return;
                }

                // Create and register the extension
                Extension extension = Activator.CreateInstance(extensionType, this) as Extension;
                if (extension != null)
                {
                    if (!forced)
                    {
                        if (extension.IsCoreExtension || extension.IsGameExtension)
                        {
                            Logger.Write(LogType.Error, $"Failed to load extension '{name}': you may not hotload Core or Game extensions.");
                            return;
                        }

                        if (!extension.SupportsReloading)
                        {
                            Logger.Write(LogType.Error, $"Failed to load extension '{name}': this extension does not support reloading.");
                            return;
                        }
                    }

                    extension.Filename = filename;

                    extension.Load();
                    extensions.Add(extension);

                    // Log extension loaded
                    Logger.Write(LogType.Info, $"Loaded extension {extension.Name} v{extension.Version} by {extension.Author}");
                }
            }
            catch (Exception ex)
            {
                Logger.WriteException($"Failed to load extension {name}", ex);
                RemoteLogger.Exception($"Failed to load extension {name}", ex);
            }
        }

        /// <summary>
        /// Unloads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        public void UnloadExtension(string filename)
        {
            string name = Utility.GetFileNameWithoutExtension(filename);

            // Find the extension
            Extension extension = extensions.SingleOrDefault(x => x.Filename == filename);
            if (extension == null)
            {
                Logger.Write(LogType.Error, $"Failed to unload extension '{name}': extension not loaded.");
                return;
            }

            // Check if it's a Core or Game extension
            if (extension.IsCoreExtension || extension.IsGameExtension)
            {
                Logger.Write(LogType.Error, $"Failed to unload extension '{name}': you may not unload Core or Game extensions.");
                return;
            }

            // Check if the extension supports reloading
            if (!extension.SupportsReloading)
            {
                Logger.Write(LogType.Error, $"Failed to unload extension '{name}': this extension doesn't support reloading.");
                return;
            }

            // TODO: Unload any plugins referencing this extension

            // Unload it
            extension.Unload();
            extensions.Remove(extension);

            // Log extension unloaded
            Logger.Write(LogType.Info, $"Unloaded extension {extension.Name} v{extension.Version} by {extension.Author}");
        }

        /// <summary>
        /// Reloads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        public void ReloadExtension(string filename)
        {
            string name = Utility.GetFileNameWithoutExtension(filename);

            // Find the extension
            Extension extension = extensions.SingleOrDefault(x => Utility.GetFileNameWithoutExtension(x.Filename) == name);

            // If the extension isn't already loaded, load it
            if (extension == null)
            {
                LoadExtension(filename, false);
                return;
            }

            // Check if it's a Core or Game extension
            if (extension.IsCoreExtension || extension.IsGameExtension)
            {
                Logger.Write(LogType.Error, $"Failed to unload extension '{name}': you may not unload Core or Game extensions.");
                return;
            }

            // Check if the extension supports reloading
            if (!extension.SupportsReloading)
            {
                Logger.Write(LogType.Error, $"Failed to reload extension '{name}': this extension doesn't support reloading.");
                return;
            }

            UnloadExtension(filename);

            LoadExtension(filename, false);
        }

        /// <summary>
        /// Loads all extensions in the given directory
        /// </summary>
        /// <param name="directory"></param>
        public void LoadAllExtensions(string directory)
        {
            string[] foundExtensions = Directory.GetFiles(directory, extSearchPattern);
            foreach (string extPath in foundExtensions.Where(e => !e.Equals("uMod.dll") && !e.Equals("uMod.References.dll")))
            {
                LoadExtension(Path.Combine(directory, extPath), true);
            }

            foreach (Extension ext in extensions.ToArray())
            {
                try
                {
                    ext.OnModLoad();
                }
                catch (Exception ex)
                {
                    extensions.Remove(ext);
                    Logger.WriteException($"Failed OnModLoad extension {ext.Name} v{ext.Version}", ex);
                    RemoteLogger.Exception($"Failed OnModLoad extension {ext.Name} v{ext.Version}", ex);
                }
            }
        }

        /// <summary>
        /// Gets all currently loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions() => extensions;

        /// <summary>
        /// Returns if an extension by the given name is present
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool IsExtensionPresent(string name) => extensions.Any(e => e.Name == name);

        /// <summary>
        /// Gets the extension by the given name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Extension GetExtension(string name)
        {
            try
            {
                return extensions.Single(e => e.Name == name);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
