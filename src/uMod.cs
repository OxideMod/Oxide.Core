extern alias References;

using References::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using uMod.Configuration;
using uMod.Extensions;
using uMod.Libraries;
using uMod.Libraries.Covalence;
using uMod.Logging;
using uMod.Plugins;
using uMod.Plugins.Watchers;
using uMod.ServerConsole;
using Timer = uMod.Libraries.Timer;

namespace uMod
{
    public delegate void NativeDebugCallback(string message);

    /// <summary>
    /// Responsible for core uMod logic
    /// </summary>
    public sealed class uMod
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The current uMod version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

        /// <summary>
        /// Gets the main logger
        /// </summary>
        public CompoundLogger RootLogger { get; private set; }

        /// <summary>
        /// Gets the main plugin manager
        /// </summary>
        public PluginManager RootPluginManager { get; private set; }

        /// <summary>
        /// Gets the data file system
        /// </summary>
        public DataFileSystem DataFileSystem { get; private set; }

        public string RootDirectory { get; private set; }
        public string ExtensionDirectory { get; private set; }
        public string InstanceDirectory { get; private set; }
        public string PluginDirectory { get; private set; }
        public string ConfigDirectory { get; private set; }
        public string DataDirectory { get; private set; }
        public string LangDirectory { get; private set; }
        public string LogDirectory { get; private set; }

        /// <summary>
        /// Gets the number of seconds since the server started
        /// </summary>
        public float Now => getTimeSinceStartup();

        /// <summary>
        /// This is true if the server is shutting down
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        // The command line
        public CommandLine CommandLine;

        // Various configs
        public uModConfig Config { get; private set; }

        // The extension manager
        private ExtensionManager extensionManager;

        // The .cs plugin loader
        private CSharpPluginLoader coreLoader;

        // Various libraries
        private Covalence covalence;
        private Permission libperm;
        private Timer libtimer;

        // Extension implemented delegates
        private Func<float> getTimeSinceStartup;

        // Thread safe NextTick callback queue
        private List<Action> nextTickQueue = new List<Action>();
        private List<Action> lastTickQueue = new List<Action>();
        private readonly object nextTickLock = new object();

        // Allow extensions to register a method to be called every frame
        private Action<float> onFrame;

        private readonly NativeDebugCallback debugCallback;
        private Stopwatch timer;
        private bool isInitialized;

        public bool HasLoadedCorePlugins { get; private set; }
        public RemoteConsole.RemoteConsole RemoteConsole;
        public ServerConsole.ServerConsole ServerConsole;

        public uMod(NativeDebugCallback debugCallback)
        {
            this.debugCallback = debugCallback;
        }

        /// <summary>
        /// Initializes a new instance of the uMod class
        /// </summary>
        public void Load()
        {
            // Set the root directory, where the server is installed
            RootDirectory = Environment.CurrentDirectory;
            if (RootDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
            {
                RootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (RootDirectory == null)
            {
                throw new Exception("Could not identify root directory");
            }

            // Set the instance directory, where uMod content will be
            InstanceDirectory = Path.Combine(RootDirectory, "umod");

            // Move files from "oxide" directory to "umod" directory
            string oxideDirectory = Path.Combine(RootDirectory, "oxide");
            if (Directory.Exists(oxideDirectory) && !Directory.Exists(InstanceDirectory))
            {
                Directory.Move(oxideDirectory, InstanceDirectory);
            }

            // Set culture settings for thread and JSON handling
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            JsonConvert.DefaultSettings = () => new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture };

            // Parse command-line to set instance directory
            CommandLine = new CommandLine(Environment.GetCommandLineArgs());
            if (CommandLine.HasVariable("umod.directory") || CommandLine.HasVariable("oxide.directory"))
            {
                string instanceDirectory, format;
                CommandLine.GetArgument("umod.directory", out instanceDirectory, out format);

                if (string.IsNullOrEmpty(instanceDirectory) && CommandLine.HasVariable("oxide.directory"))
                {
                    CommandLine.GetArgument("oxide.directory", out instanceDirectory, out format);
                    LogWarning("oxide.directory in command-line is deprecated, please use umod.directory instead");
                }

                if (string.IsNullOrEmpty(instanceDirectory) || CommandLine.HasVariable(instanceDirectory))
                {
                    InstanceDirectory = Path.Combine(RootDirectory, Utility.CleanPath(string.Format(format, CommandLine.GetVariable(instanceDirectory))));
                }
            }

            // Set and create core directories, if needed
            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (ExtensionDirectory == null || !Directory.Exists(ExtensionDirectory))
            {
                throw new Exception("Could not identify extension directory");
            }
            if (!Directory.Exists(InstanceDirectory))
            {
                Directory.CreateDirectory(InstanceDirectory);
            }
            ConfigDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("config"));
            if (!Directory.Exists(ConfigDirectory))
            {
                Directory.CreateDirectory(ConfigDirectory);
            }
            DataDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("data"));
            if (!Directory.Exists(DataDirectory))
            {
                Directory.CreateDirectory(DataDirectory);
            }
            LangDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("lang"));
            if (!Directory.Exists(LangDirectory))
            {
                Directory.CreateDirectory(LangDirectory);
            }
            LogDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("logs"));
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }
            PluginDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("plugins"));
            if (!Directory.Exists(PluginDirectory))
            {
                Directory.CreateDirectory(PluginDirectory);
            }

            // Register the library search path for dependencies
            RegisterLibrarySearchPath(Path.Combine(ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86"));

            // Load core configuration file
            string config = Path.Combine(InstanceDirectory, "umod.config.json"); // TODO: Rename existing oxide.config.json if exists
            if (File.Exists(config))
            {
                Config = ConfigFile.Load<uModConfig>(config);
            }
            else
            {
                Config = new uModConfig(config);
                Config.Save();
            }

            // Check for and set configuration options from command-line
            if (CommandLine.HasVariable("nolog"))
            {
                LogWarning("Usage of the 'nolog' variable will prevent logging");
            }
            if (CommandLine.HasVariable("rcon.port"))
            {
                Config.Rcon.Port = Utility.GetNumbers(CommandLine.GetVariable("rcon.port"));
            }
            if (CommandLine.HasVariable("rcon.password"))
            {
                Config.Rcon.Password = CommandLine.GetVariable("rcon.password");
            }

            // Add core logger
            RootLogger = new CompoundLogger();
            RootLogger.AddLogger(new RotatingFileLogger { Directory = LogDirectory });
            if (debugCallback != null)
            {
                RootLogger.AddLogger(new CallbackLogger(debugCallback));
            }

            // Setup core managers and data file system
            LogInfo($"Loading uMod v{Version}...");
            RootPluginManager = new PluginManager(RootLogger) { ConfigPath = ConfigDirectory };
            extensionManager = new ExtensionManager(RootLogger);
            DataFileSystem = new DataFileSystem(DataDirectory);

            // Setup configuration and DLL mapping for references
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                string configPath = Path.Combine(ExtensionDirectory, "Oxide.References.dll.config");
                if (!File.Exists(configPath))
                {
                    File.WriteAllText(configPath,
                        $"<configuration>\n<dllmap dll=\"MonoPosixHelper\" target=\"{ExtensionDirectory}/x86/libMonoPosixHelper.so\" os=\"!windows,osx\" wordsize=\"32\" />\n" +
                        $"<dllmap dll=\"MonoPosixHelper\" target=\"{ExtensionDirectory}/x64/libMonoPosixHelper.so\" os=\"!windows,osx\" wordsize=\"64\" />\n</configuration>");
                }
            }

            // Register libraries (these are going to get replaced soon)
            extensionManager.RegisterLibrary("Covalence", covalence = new Covalence());
            extensionManager.RegisterLibrary("Global", new Global());
            extensionManager.RegisterLibrary("Lang", new Lang());
            extensionManager.RegisterLibrary("Permission", libperm = new Permission());
            extensionManager.RegisterLibrary("Timer", libtimer = new Timer());
            extensionManager.RegisterLibrary("WebRequests", new WebRequests());

            // Load all extensions
            LogInfo("Loading extensions...");
            extensionManager.LoadAllExtensions(ExtensionDirectory);

            // Run cleanup of old files and initialize universal API
            Cleanup.Run();
            covalence.Initialize();

            // Initialize custom console
            RemoteConsole = new RemoteConsole.RemoteConsole();
            RemoteConsole?.Initalize();

            // Check for a reliable clock, else set primitive timer
            if (getTimeSinceStartup == null)
            {
                timer = new Stopwatch();
                timer.Start();
                getTimeSinceStartup = () => (float)timer.Elapsed.TotalSeconds;
                LogWarning("A reliable clock is not available, falling back to a clock which may be unreliable on certain hardware");
            }

            // Register .cs plugin watcher
            FSWatcher coreWatcher = new FSWatcher(PluginDirectory, "*.cs");
            extensionManager.RegisterPluginChangeWatcher(coreWatcher);

            // Register .cs plugin loader
            coreLoader = new CSharpPluginLoader(coreWatcher);
            extensionManager.RegisterPluginLoader(coreLoader);
            coreLoader.AddReferences();

            // Load all plugin watchers for extensions
            foreach (Extension ext in extensionManager.GetAllExtensions())
            {
                ext.LoadPluginWatchers(PluginDirectory);
            }

            // Load all plugins
            LogInfo("Loading plugins...");
            LoadAllPlugins(true);

            // Setup events for all plugin changes watchers
            foreach (PluginChangeWatcher watcher in extensionManager.GetPluginChangeWatchers())
            {
                watcher.OnPluginSourceChanged += watcher_OnPluginSourceChanged;
                watcher.OnPluginAdded += watcher_OnPluginAdded;
                watcher.OnPluginRemoved += watcher_OnPluginRemoved;
            }
        }

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public T GetLibrary<T>(string name = null) where T : Library => extensionManager.GetLibrary(name ?? typeof(T).Name) as T;

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions() => extensionManager.GetAllExtensions();

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => extensionManager.GetPluginLoaders();

        #region Logging

        /// <summary>
        /// Logs a formatted debug message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogDebug(string format, params object[] args) => RootLogger.Write(LogType.Warning, $"[DEBUG] {format}", args);

        /// <summary>
        /// Logs a formatted error message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogError(string format, params object[] args) => RootLogger.Write(LogType.Error, format, args);

        /// <summary>
        /// Logs an exception to the root logger
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        /// <returns></returns>
        public void LogException(string message, Exception ex) => RootLogger.WriteException(message, ex);

        /// <summary>
        /// Logs a formatted info message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogInfo(string format, params object[] args) => RootLogger.Write(LogType.Info, format, args);

        /// <summary>
        /// Logs a formatted warning message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogWarning(string format, params object[] args) => RootLogger.Write(LogType.Warning, format, args);

        #endregion Logging

        #region Plugin Management

        /// <summary>
        /// Scans for all available plugins and attempts to load them
        /// </summary>
        public void LoadAllPlugins(bool init = false)
        {
            IEnumerable<PluginLoader> loaders = extensionManager.GetPluginLoaders().ToArray();

            // Load all core plugins first
            if (!HasLoadedCorePlugins)
            {
                foreach (PluginLoader loader in loaders)
                {
                    foreach (Type type in loader.CorePlugins)
                    {
                        try
                        {
                            Plugin plugin = (Plugin)Activator.CreateInstance(type);
                            plugin.IsCorePlugin = true;
                            PluginLoaded(plugin);
                        }
                        catch (Exception ex)
                        {
                            LogException($"Could not load core plugin {type.Name}", ex);
                        }
                    }
                }
                HasLoadedCorePlugins = true;
            }

            // Scan the plugin directory and load all reported plugins
            foreach (PluginLoader loader in loaders)
            {
                foreach (FileInfo file in loader.ScanDirectory(PluginDirectory))
                {
                    LoadPlugin(Utility.GetFileNameWithoutExtension(file.Name));
                }
            }

            if (init)
            {
                float lastCall = Now;
                foreach (PluginLoader loader in extensionManager.GetPluginLoaders())
                {
                    // Wait until all async plugins have finished loading
                    while (loader.LoadingPlugins.Count > 0)
                    {
                        Thread.Sleep(25);
                        OnFrame(Now - lastCall);
                        lastCall = Now;
                    }
                }

                isInitialized = true;
            }
        }

        /// <summary>
        /// Unloads all plugins
        /// </summary>
        public void UnloadAllPlugins(IList<string> skip = null)
        {
            foreach (Plugin plugin in RootPluginManager.GetPlugins().Where(p => !p.IsCorePlugin && (skip == null || !skip.Contains(p.Name))).ToArray())
            {
                UnloadPlugin(plugin.Name);
            }
        }

        /// <summary>
        /// Reloads all plugins
        /// </summary>
        public void ReloadAllPlugins(IList<string> skip = null)
        {
            foreach (Plugin plugin in RootPluginManager.GetPlugins().Where(p => !p.IsCorePlugin && (skip == null || !skip.Contains(p.Name))).ToArray())
            {
                ReloadPlugin(plugin.Name);
            }
        }

        /// <summary>
        /// Loads a plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool LoadPlugin(string name)
        {
            // Check if the plugin is already loaded
            if (RootPluginManager.GetPlugin(name) != null)
            {
                return false;
            }

            // Find all plugin loaders that lay claim to the name
            HashSet<PluginLoader> loaders = new HashSet<PluginLoader>(extensionManager.GetPluginLoaders().Where(l => l.ScanDirectory(PluginDirectory).Any(f => f.Name.StartsWith(name))));
            if (loaders.Count == 0)
            {
                // TODO: Fix symlinked plugins unloaded still triggering this
                LogError($"Could not load plugin '{name}' (no plugin found with that file name)");
                return false;
            }

            if (loaders.Count > 1)
            {
                LogError($"Could not load plugin '{name}' (multiple plugin with that name)");
                return false;
            }

            // Load it and watch for errors
            PluginLoader loader = loaders.First();

            // Get all plugin file info to load
            FileInfo pluginFileInfo = loader.ScanDirectory(PluginDirectory).First(f => f.Name.StartsWith(name));

            // Try to load plugin file
            try
            {
                Plugin plugin = loader.Load(pluginFileInfo.DirectoryName, name);
                if (plugin != null)
                {
                    plugin.Loader = loader;
                    PluginLoaded(plugin);
                    return true;
                }

                return true; // Async load
            }
            catch (Exception ex)
            {
                LogException($"Could not load plugin {name}", ex);
                return false;
            }
        }

        public bool PluginLoaded(Plugin plugin)
        {
            plugin.OnError += plugin_OnError;
            try
            {
                plugin.Loader?.PluginErrors.Remove(plugin.Name);
                RootPluginManager.AddPlugin(plugin);
                if (plugin.Loader != null)
                {
                    if (plugin.Loader.PluginErrors.ContainsKey(plugin.Name))
                    {
                        UnloadPlugin(plugin.Name);
                        return false;
                    }
                }

                plugin.IsLoaded = true;
                LogInfo("Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);

                // Let plugins know
                CallHook("OnPluginLoaded", plugin);
                return true;
            }
            catch (Exception ex)
            {
                if (plugin.Loader != null)
                {
                    plugin.Loader.PluginErrors[plugin.Name] = ex.Message;
                }

                LogException($"Could not initialize plugin '{plugin.Name} v{plugin.Version}'", ex);
                return false;
            }
        }

        /// <summary>
        /// Unloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool UnloadPlugin(string name)
        {
            // Get the plugin
            Plugin plugin = RootPluginManager.GetPlugin(name);
            if (plugin != null)
            {
                PluginLoader loader = extensionManager.GetPluginLoaders().SingleOrDefault(l => l.LoadedPlugins.ContainsKey(name));
                loader?.Unloading(plugin);

                // Unload the plugin
                RootPluginManager.RemovePlugin(plugin);

                // Let plugins know
                if (plugin.IsLoaded)
                {
                    CallHook("OnPluginUnloaded", plugin);
                }

                plugin.IsLoaded = false;
                LogInfo("Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
                return true;
            }

            // Let the plugin loader know that this plugin is being unloaded
            return false;
        }

        /// <summary>
        /// Reloads the plugin by the given name
        /// </summary>
        /// <param name="name"></param>
        public bool ReloadPlugin(string name)
        {
            bool isNested = false;
            string directory = PluginDirectory;
            if (name.Contains("\\"))
            {
                isNested = true;
                string subPath = Path.GetDirectoryName(name);
                if (subPath != null)
                {
                    directory = Path.Combine(directory, subPath);
                    name = name.Substring(subPath.Length + 1);
                }
            }

            PluginLoader loader = extensionManager.GetPluginLoaders().FirstOrDefault(l => l.ScanDirectory(directory).Any(f => f.Name.StartsWith(name)));
            if (loader != null)
            {
                loader.Reload(directory, name);
                return true;
            }

            if (isNested)
            {
                return false;
            }

            UnloadPlugin(name);
            LoadPlugin(name);
            return true;
        }

        /// <summary>
        /// Called when a plugin has raised an error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void plugin_OnError(Plugin sender, string message) => LogError("{0} v{1}: {2}", sender.Name, sender.Version, message);

        #endregion Plugin Management

        #region Extension Management

        public bool LoadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                LogError($"Could not load extension '{name}' (file not found)");
                return false;
            }

            extensionManager.LoadExtension(extPath, false);
            return true;
        }

        public bool UnloadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                LogError($"Could not unload extension '{name}' (file not found)");
                return false;
            }

            extensionManager.UnloadExtension(extPath);
            return true;
        }

        public bool ReloadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                LogError($"Could not reload extension '{name}' (file not found)");
                return false;
            }

            extensionManager.ReloadExtension(extPath);
            return true;
        }

        #endregion Extension Management

        #region Hook Calling

        /// <summary>
        /// Calls a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args) => RootPluginManager?.CallHook(hookname, args);

        /// <summary>
        /// Calls a deprecated hook and prints a warning
        /// </summary>
        /// <param name="oldHook"></param>
        /// <param name="newHook"></param>
        /// <param name="expireDate"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallDeprecatedHook(string oldHook, string newHook, DateTime expireDate, params object[] args)
        {
            return RootPluginManager?.CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

        #endregion Hook Calling

        /// <summary>
        /// Queues a callback to be called in the next server frame
        /// </summary>
        /// <param name="callback"></param>
        public void NextTick(Action callback)
        {
            lock (nextTickLock)
            {
                nextTickQueue.Add(callback);
            }
        }

        /// <summary>
        /// Registers a callback which will be called every server frame
        /// </summary>
        /// <param name="callback"></param>
        public void OnFrame(Action<float> callback) => onFrame += callback;

        /// <summary>
        /// Called every server frame, implemented by an engine-specific extension
        /// </summary>
        public void OnFrame(float delta)
        {
            // Call any callbacks queued for this frame
            if (nextTickQueue.Count > 0)
            {
                List<Action> queued;
                lock (nextTickLock)
                {
                    queued = nextTickQueue;
                    nextTickQueue = lastTickQueue;
                    lastTickQueue = queued;
                }
                for (int i = 0; i < queued.Count; i++)
                {
                    try
                    {
                        queued[i]();
                    }
                    catch (Exception ex)
                    {
                        LogException("Exception while calling NextTick callback", ex);
                    }
                }
                queued.Clear();
            }

            // Update libraries
            libtimer.Update(delta);

            // Don't update plugin watchers or call OnFrame in plugins until servers starts ticking
            if (isInitialized)
            {
                ServerConsole?.Update();

                // Update extensions
                try
                {
                    onFrame?.Invoke(delta);
                }
                catch (Exception ex)
                {
                    LogException($"{ex.GetType().Name} while invoke OnFrame in extensions", ex);
                }

                // Call OnFrame hook in plugins
                foreach (KeyValuePair<string, Plugin> kv in coreLoader.LoadedPlugins)
                {
                    CSharpPlugin plugin = kv.Value as CSharpPlugin;
                    if (plugin != null && plugin.HookedOnFrame)
                    {
                        plugin.CallHook("OnFrame", delta);
                    }
                }
            }
        }

        /// <summary>
        /// Called when the server is saving
        /// </summary>
        public void OnSave() => libperm.SaveData();

        /// <summary>
        /// Called when the server is shutting down
        /// </summary>
        public void OnShutdown()
        {
            if (!IsShuttingDown)
            {
                libperm.SaveData();
                IsShuttingDown = true;
                UnloadAllPlugins();

                foreach (Extension extension in extensionManager.GetAllExtensions())
                {
                    extension.OnShutdown();
                }

                foreach (string name in extensionManager.GetLibraries())
                {
                    extensionManager.GetLibrary(name).Shutdown();
                }

                RemoteConsole?.Shutdown();
                ServerConsole?.OnDisable();
                RootLogger.Shutdown();
            }
        }

        /// <summary>
        /// Called by an engine-specific extension to register the engine clock
        /// </summary>
        /// <param name="method"></param>
        public void RegisterEngineClock(Func<float> method) => getTimeSinceStartup = method;

        public bool CheckConsole(bool force = false) => ConsoleWindow.Check(force) && Config.Console.Enabled;

        public bool EnableConsole(bool force = false)
        {
            if (CheckConsole(force))
            {
                ServerConsole = new ServerConsole.ServerConsole();
                ServerConsole.OnEnable();
                return true;
            }

            return false;
        }

        #region Plugin Change Watchers

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginSourceChanged(string name) => ReloadPlugin(name);

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginAdded(string name) => LoadPlugin(name);

        /// <summary>
        /// Called when a plugin watcher has reported a change in a plugin source
        /// </summary>
        /// <param name="name"></param>
        private void watcher_OnPluginRemoved(string name) => UnloadPlugin(name);

        #endregion Plugin Change Watchers

        #region Library Paths

        private static void RegisterLibrarySearchPath(string path)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                    string newPath = string.IsNullOrEmpty(currentPath) ? path : currentPath + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("PATH", newPath);
                    SetDllDirectory(path);
                    break;

                case PlatformID.Unix:
                case PlatformID.MacOSX:
                    string currentLdLibraryPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? string.Empty;
                    string newLdLibraryPath = string.IsNullOrEmpty(currentLdLibraryPath) ? path : currentLdLibraryPath + Path.PathSeparator + path;
                    Environment.SetEnvironmentVariable("LD_LIBRARY_PATH", newLdLibraryPath);
                    break;
            }
        }

        [DllImport("kernel32", SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        #endregion Library Paths
    }
}
