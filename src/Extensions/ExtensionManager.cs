using Oxide.Core.Libraries;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Oxide.Core.Extensions
{
    /// <summary>
    /// Responsible for managing all Oxide extensions
    /// </summary>
    public sealed class ExtensionManager
    {
        // All loaded extensions
        private IList<Extension> extensions;

        // The search patterns for extensions
        private const string extSearchPattern = "Oxide.*.dll";

        /// <summary>
        /// Gets the logger to which this extension manager writes
        /// </summary>
        public CompoundLogger Logger { get; private set; }

        // All registered plugin loaders
        private IList<PluginLoader> pluginloaders;

        // All registered libraries
        private IDictionary<string, Library> libraries;

        // All registered watchers
        private IList<PluginChangeWatcher> changewatchers;

        /// <summary>
        /// Initializes a new instance of the ExtensionManager class
        /// </summary>
        public ExtensionManager(CompoundLogger logger)
        {
            // Initialize
            Logger = logger;
            extensions = new List<Extension>();
            pluginloaders = new List<PluginLoader>();
            libraries = new Dictionary<string, Library>();
            changewatchers = new List<PluginChangeWatcher>();
        }

        #region Registering

        /// <summary>
        /// Registers the specified plugin loader
        /// </summary>
        /// <param name="loader"></param>
        public void RegisterPluginLoader(PluginLoader loader) => pluginloaders.Add(loader);

        /// <summary>
        /// Gets all plugin loaders
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => pluginloaders;

        /// <summary>
        /// Registers the specified library
        /// </summary>
        /// <param name="name"></param>
        /// <param name="library"></param>
        public void RegisterLibrary(string name, Library library)
        {
            if (libraries.ContainsKey(name))
            {
                Interface.Oxide.LogError("An extension tried to register an already registered library: " + name);
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
            Library lib;
            return !libraries.TryGetValue(name, out lib) ? null : lib;
        }

        /// <summary>
        /// Registers the specified watcher
        /// </summary>
        /// <param name="watcher"></param>
        public void RegisterPluginChangeWatcher(PluginChangeWatcher watcher) => changewatchers.Add(watcher);

        /// <summary>
        /// Gets all plugin change watchers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginChangeWatcher> GetPluginChangeWatchers() => changewatchers;

        #endregion Registering

        /// <summary>
        /// Loads the extension at the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="forced"></param>
        public void LoadExtension(string filename, bool forced)
        {
            string name = Utility.GetFileNameWithoutExtension(filename);

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

                // Load the assembly
                Assembly assembly = Assembly.Load(data);

                // Search for a type that derives Extension
                Type extType = typeof(Extension);
                Type extensionType = null;
                foreach (Type type in assembly.GetExportedTypes())
                {
                    if (!extType.IsAssignableFrom(type))
                    {
                        continue;
                    }

                    extensionType = type;
                    break;
                }

                if (extensionType == null)
                {
                    Logger.Write(LogType.Error, "Failed to load extension {0} ({1})", name, "Specified assembly does not implement an Extension class");
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
                    Logger.Write(LogType.Info, "Loaded extension {0} v{1} by {2}", extension.Name, extension.Version, extension.Author);
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

            // TODO: Unload any plugins referenceing this extension

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
            List<string> foundCore = new List<string>();
            List<string> foundGame = new List<string>();
            List<string> foundOther = new List<string>();
            string[] coreExtensions = {
                "Oxide.CSharp", "Oxide.JavaScript", "Oxide.Lua", "Oxide.MySql", "Oxide.Python", "Oxide.SQLite", "Oxide.Unity"
            };
            string[] gameExtensions = {
                "Oxide.Blackwake", "Oxide.Blockstorm", "Oxide.FortressCraft", "Oxide.FromTheDepths", "Oxide.GangBeasts", "Oxide.Hurtworld",
                "Oxide.InterstellarRift", "Oxide.MedievalEngineers", "Oxide.Nomad", "Oxide.PlanetExplorers", "Oxide.ReignOfKings",  "Oxide.Rust",
                "Oxide.RustLegacy", "Oxide.SavageLands", "Oxide.SevenDaysToDie", "Oxide.SpaceEngineers", "Oxide.TheForest", "Oxide.Terraria",
                "Oxide.Unturned"
            };
            string[] foundExtensions = Directory.GetFiles(directory, extSearchPattern);

            foreach (string extPath in foundExtensions.Where(e => !e.EndsWith("Oxide.Core.dll") && !e.EndsWith("Oxide.References.dll")))
            {
                if (extPath.Contains("Oxide.Core.") && Array.IndexOf(foundExtensions, extPath.Replace(".Core", "")) != -1)
                {
                    Cleanup.Add(extPath);
                    continue;
                }

                if (extPath.Contains("Oxide.Ext.") && Array.IndexOf(foundExtensions, extPath.Replace(".Ext", "")) != -1)
                {
                    Cleanup.Add(extPath);
                    continue;
                }

                if (extPath.Contains("Oxide.Game."))
                {
                    Cleanup.Add(extPath);
                    continue;
                }

                if (coreExtensions.Contains(extPath.Basename()))
                {
                    foundCore.Add(extPath);
                }
                else if (gameExtensions.Contains(extPath.Basename()))
                {
                    foundGame.Add(extPath);
                }
                else
                {
                    foundOther.Add(extPath);
                }
            }

            foreach (string extPath in foundCore)
            {
                LoadExtension(Path.Combine(directory, extPath), true);
            }

            foreach (string extPath in foundGame)
            {
                LoadExtension(Path.Combine(directory, extPath), true);
            }

            foreach (string extPath in foundOther)
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
