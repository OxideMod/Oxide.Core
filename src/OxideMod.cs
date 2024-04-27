extern alias References;

using Oxide.Core.Configuration;
using Oxide.Core.Extensions;
using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Logging;
using Oxide.Core.Plugins;
using Oxide.Core.Plugins.Watchers;
using Oxide.Core.ServerConsole;
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
using Oxide.DependencyInjection;
using Oxide.DependencyInjection.Resolvers;
using Oxide.Pooling;
using Timer = Oxide.Core.Libraries.Timer;

namespace Oxide.Core
{
    public delegate void NativeDebugCallback(string message);

    /// <summary>
    /// Responsible for core Oxide logic
    /// </summary>
    public sealed class OxideMod
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// The current Oxide version
        /// </summary>
        public static readonly VersionNumber Version = new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

        /// <summary>
        /// The Git Branch of this Oxide.Core build
        /// </summary>
        public static readonly string Branch = Assembly.GetExecutingAssembly().Metadata("GitBranch").FirstOrDefault() ?? "unknown";

        /// <summary>
        /// Gets the main logger
        /// </summary>
        public CompoundLogger RootLogger { get; }

        /// <summary>
        /// Gets the main pluginmanager
        /// </summary>
        public PluginManager RootPluginManager { get; }

        /// <summary>
        /// Gets the data file system
        /// </summary>
        public DataFileSystem DataFileSystem { get; }

        // Various directories
        public string RootDirectory { get; }

        public string ExtensionDirectory { get; }
        public string InstanceDirectory { get; }
        public string PluginDirectory { get; }
        public string ConfigDirectory { get; }
        public string DataDirectory { get; }
        public string LangDirectory { get; }
        public string LogDirectory { get; }

        // Gets the number of seconds since the server started
        public float Now => getTimeSinceStartup();

        /// <summary>
        /// This is true if the server is shutting down
        /// </summary>
        public bool IsShuttingDown { get; private set; }

        // The extension manager
        private ExtensionManager ExtensionManager { get; }

        // The command line
        public CommandLine CommandLine { get; }

        // Various configs
        public OxideConfig Config { get; private set; }

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

        internal bool InitCalled { get; private set; }
        private bool isInitialized;
        public bool HasLoadedCorePlugins { get; private set; }

        public RemoteConsole.RemoteConsole RemoteConsole { get; }
        public ServerConsole.ServerConsole ServerConsole { get; }

        private Stopwatch timer;
        private IPoolFactory PoolFactory { get; }

        private JsonSerializerSettings JsonSettings { get; }
        private IServiceCollection ServiceCollection { get; }

        internal OxideMod(IPoolFactory poolFactory, IServiceCollection serviceCollection)
        {
            PoolFactory = poolFactory;
            ServiceCollection = serviceCollection;
            CommandLine = new CommandLine(Environment.GetCommandLineArgs());

            // Set directories
            ExtensionDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            RootDirectory = Environment.CurrentDirectory;

            if (RootDirectory.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)))
            {
                RootDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            InstanceDirectory = Path.Combine(RootDirectory, "oxide");

            if (CommandLine.HasVariable("oxide.directory"))
            {
                CommandLine.GetArgument("oxide.directory", out string var, out string format);
                if (string.IsNullOrEmpty(var) || CommandLine.HasVariable(var))
                {
                    InstanceDirectory = Path.Combine(RootDirectory, Utility.CleanPath(string.Format(format, CommandLine.GetVariable(var))));
                }
            }

            ConfigDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("config"));
            DataDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("data"));
            LangDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("lang"));
            LogDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("logs"));
            PluginDirectory = Path.Combine(InstanceDirectory, Utility.CleanPath("plugins"));

            // Tools Initialization
            RemoteConsole = new RemoteConsole.RemoteConsole();
            RootLogger = new CompoundLogger();
            Utility.Logger = RootLogger;
            ServerConsole = new ServerConsole.ServerConsole();
            ExtensionManager = new ExtensionManager(RootLogger, PoolFactory.GetArrayProvider<object>(), serviceCollection);
            RootPluginManager = new PluginManager(RootLogger, PoolFactory.GetArrayProvider<object>(), ConfigDirectory);
            DataFileSystem = new DataFileSystem(DataDirectory);
            JsonSettings = new JsonSerializerSettings { Culture = CultureInfo.InvariantCulture };
        }

        /// <summary>
        /// Initializes a new instance of the OxideMod class
        /// </summary>
        public void Load()
        {
            InitCalled = true;
            // Init Debug callback logger
            if (Interface.DebugCallback != null)
            {
                RootLogger.AddLogger(new CallbackLogger(Interface.DebugCallback));
            }

            // Ensure directory creation
            InitDirectory(nameof(InstanceDirectory), InstanceDirectory);
            InitDirectory(nameof(PluginDirectory), PluginDirectory);
            InitDirectory(nameof(ConfigDirectory), ConfigDirectory);
            InitDirectory(nameof(DataDirectory), DataDirectory);
            InitDirectory(nameof(LangDirectory), LangDirectory);
            InitDirectory(nameof(LogDirectory), LogDirectory);
            RootLogger.AddLogger(new RotatingFileLogger(LogDirectory));
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            JsonConvert.DefaultSettings = () => JsonSettings;
            string config = Path.Combine(InstanceDirectory, "oxide.config.json");

            if (File.Exists(config))
            {
                Config = ConfigFile.Load<OxideConfig>(config);
            }
            else
            {
                Config = new OxideConfig(config);
                Config.Save();
                RootLogger.Write(LogType.Info, "Created new Oxide configuration: {0}", config);
            }

            ServiceCollection.AddSingleton(ServerConsole)
                    .AddSingleton(RemoteConsole)
                    .AddSingleton<Logger>(RootLogger)
                    .AddSingleton(CommandLine)
                    .AddSingleton(ExtensionManager)
                    .AddSingleton<IDependencyResolverFactory>(new ResolverFactory(RootLogger))
                    .AddSingleton(DataFileSystem)
                    .AddSingleton(RootPluginManager)
                    .AddSingleton<Covalence>()
                    .AddSingleton<Global>()
                    .AddSingleton<Lang>()
                    .AddSingleton<Permission>()
                    .AddSingleton<Libraries.Plugins>()
                    .AddSingleton<Time>()
                    .AddSingleton<Timer>()
                    .AddSingleton<WebRequests>()
                    .AddSingleton(this)
                    .AddSingleton(Config)
                    .AddSingleton(JsonSettings);

            RegisterLibrarySearchPath(Path.Combine(ExtensionDirectory, IntPtr.Size == 8 ? "x64" : "x86"));

            if (CommandLine.HasVariable("web.ip"))
            {
                Config.Options.WebRequestIP = CommandLine.GetVariable("web.ip");
            }

            if (CommandLine.HasVariable("rcon.port"))
            {
                Config.Rcon.Port = Utility.GetNumbers(CommandLine.GetVariable("rcon.port"));
            }

            if (CommandLine.HasVariable("rcon.password"))
            {
                Config.Rcon.Password = CommandLine.GetVariable("rcon.password");
            }

            RootLogger.Write(LogType.Info, "Loading Oxide Core v{0}...", Version);
            Interface.ServiceProvider
                     .GetRequiredService<IDependencyResolverFactory>()
                     .RegisterServiceResolver<LibraryResolver>()
                     .RegisterServiceResolver<PluginResolver>()
                     .RegisterServiceResolver<ExtensionResolver>()
                     .RegisterServiceResolver<PoolResolver>();

            covalence = Interface.ServiceProvider.GetRequiredService<Covalence>();
            libperm = Interface.ServiceProvider.GetRequiredService<Permission>();
            libtimer = Interface.ServiceProvider.GetRequiredService<Timer>();

            RootLogger.Write(LogType.Info, "Loading extensions...");
            ExtensionManager.LoadAllExtensions(ExtensionDirectory);

            Cleanup.Run();
            covalence.Initialize();
            RemoteConsole.Initialize();

            if (getTimeSinceStartup == null)
            {
                timer = new Stopwatch();
                timer.Start();
                getTimeSinceStartup = () => (float)timer.Elapsed.TotalSeconds;
                RootLogger.Write(LogType.Warning, "A reliable clock is not available, falling back to a clock which may be unreliable on certain hardware");
            }

            foreach (Extension ext in ExtensionManager.GetAllExtensions())
            {
                ext.LoadPluginWatchers(PluginDirectory);
            }

            RootLogger.Write(LogType.Info, "Loading plugins...");
            LoadAllPlugins(true);

            foreach (PluginChangeWatcher watcher in ExtensionManager.GetPluginChangeWatchers())
            {
                watcher.OnPluginSourceChanged += watcher_OnPluginSourceChanged;
                watcher.OnPluginAdded += watcher_OnPluginAdded;
                watcher.OnPluginRemoved += watcher_OnPluginRemoved;
            }
        }

        private void InitDirectory(string name, string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                RootLogger.Write(LogType.Error, "Failed to find {0}", name);
                throw new ArgumentNullException(nameof(path), $"Failed to find {name}");
            }

            if (!Directory.Exists(path))
            {
                try
                {
                    Directory.CreateDirectory(path);
                    RootLogger.Write(LogType.Info, "Created {0}: {1}", name, path);
                }
                catch (Exception e)
                {
                    RootLogger.WriteException($"Failed to create {name}: {path}", e);
                    throw;
                }
            }
        }

        /// <summary>
        /// Gets the library by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        [Obsolete("Use Interface.ServiceProvider.GetService")]
        public T GetLibrary<T>(string name = null) where T : Library => ExtensionManager.GetLibrary(name ?? typeof(T).Name) as T;

        /// <summary>
        /// Gets all loaded extensions
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Extension> GetAllExtensions() => ExtensionManager.GetAllExtensions();

        /// <summary>
        /// Gets an extension by name if it is loaded
        /// </summary>
        /// <param name="name">Extension name</param>
        /// <returns></returns>
        public Extension GetExtension(string name) => ExtensionManager.GetExtension(name);

        /// <summary>
        /// Gets an extension by type if it is present
        /// </summary>
        /// <typeparam name="T">Extension type</typeparam>
        /// <returns></returns>
        public T GetExtension<T>() where T : Extension => ExtensionManager.GetExtension<T>();

        /// <summary>
        /// Gets all present plugin loaders
        /// </summary>
        /// <returns></returns>
        public IEnumerable<PluginLoader> GetPluginLoaders() => ExtensionManager.GetPluginLoaders();

        #region Logging

        /// <summary>
        /// Logs a formatted debug message to the root logger
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public void LogDebug(string format, params object[] args)
        {
            #if DEBUG
            RootLogger.Write(LogType.Debug, format, args);
            #endif
        }

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
            IEnumerable<PluginLoader> loaders = ExtensionManager.GetPluginLoaders().ToArray();

            // Load all core plugins first
            if (!HasLoadedCorePlugins)
            {
                foreach (PluginLoader loader in loaders)
                {
                    foreach (Type type in loader.CorePlugins)
                    {
                        try
                        {
                            Plugin plugin = (Plugin)ActivationUtility.CreateInstance(Interface.ServiceProvider, type);
                            plugin.IsCorePlugin = true;
                            PluginLoaded(plugin);
                        }
                        catch (Exception ex)
                        {
                            RootLogger.WriteException($"Could not load core plugin {type.Name}", ex);
                        }
                    }
                }
                HasLoadedCorePlugins = true;
            }

            // Scan the plugin directory and load all reported plugins
            foreach (PluginLoader loader in loaders)
            {
                foreach (string name in loader.ScanDirectory(PluginDirectory))
                {
                    LoadPlugin(name);
                }
            }

            if (!init)
            {
                return;
            }

            float lastCall = Now;
            foreach (PluginLoader loader in ExtensionManager.GetPluginLoaders())
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
            HashSet<PluginLoader> loaders = new HashSet<PluginLoader>(ExtensionManager.GetPluginLoaders().Where(l => l.ScanDirectory(PluginDirectory).Contains(name)));

            if (loaders.Count == 0)
            {
                // TODO: Fix symlinked plugins unloaded still triggering this
                RootLogger.Write(LogType.Error,"Could not load plugin '{0}' (no plugin found with that file name)", name);
                return false;
            }

            if (loaders.Count > 1)
            {
                RootLogger.Write(LogType.Error,"Could not load plugin '{0}' (multiple plugin with that name)", name);
                return false;
            }

            // Load it and watch for errors
            PluginLoader loader = loaders.First();
            try
            {
                Plugin plugin = loader.Load(PluginDirectory, name);
                if (plugin == null)
                {
                    return true; // Async load
                }

                plugin.Loader = loader;
                PluginLoaded(plugin);
                return true;
            }
            catch (Exception ex)
            {
                RootLogger.WriteException($"Could not load plugin {name}", ex);
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
                CallHook("OnPluginLoaded", plugin);
                RootLogger.Write(LogType.Info,"Loaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
                return true;
            }
            catch (Exception ex)
            {
                if (plugin.Loader != null)
                {
                    plugin.Loader.PluginErrors[plugin.Name] = ex.Message;
                }

                RootLogger.WriteException($"Could not initialize plugin '{plugin.Name} v{plugin.Version}'", ex);
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
            if (plugin == null || (plugin.IsCorePlugin && !IsShuttingDown))
            {
                return false;
            }

            // Let the plugin loader know that this plugin is being unloaded
            PluginLoader loader = ExtensionManager.GetPluginLoaders().SingleOrDefault(l => l.LoadedPlugins.ContainsKey(name));
            loader?.Unloading(plugin);

            // Unload it
            RootPluginManager.RemovePlugin(plugin);

            // Let other plugins know that this plugin has been unloaded
            if (plugin.IsLoaded)
            {
                CallHook("OnPluginUnloaded", plugin);
            }

            plugin.IsLoaded = false;

            RootLogger.Write(LogType.Info,"Unloaded plugin {0} v{1} by {2}", plugin.Title, plugin.Version, plugin.Author);
            return true;
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
            PluginLoader loader = ExtensionManager.GetPluginLoaders().FirstOrDefault(l => l.ScanDirectory(directory).Contains(name));
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
        private void plugin_OnError(Plugin sender, string message) => RootLogger.Write(LogType.Error,"{0} v{1}: {2}", sender.Name, sender.Version, message);

        #endregion Plugin Management

        #region Extension Management

        public bool LoadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                RootLogger.Write(LogType.Error,"Could not load extension '{0}' (file not found)", name);
                return false;
            }

            ExtensionManager.LoadExtension(extPath);
            return true;
        }

        public bool UnloadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                RootLogger.Write(LogType.Error,"Could not unload extension '{0}' (file not found)", name);
                return false;
            }

            ExtensionManager.UnloadExtension(extPath);
            return true;
        }

        public bool ReloadExtension(string name)
        {
            string extFileName = !name.EndsWith(".dll") ? name + ".dll" : name;
            string extPath = Path.Combine(ExtensionDirectory, extFileName);

            if (!File.Exists(extPath))
            {
                RootLogger.Write(LogType.Error,"Could not reload extension '{0}' (file not found)", name);
                return false;
            }

            ExtensionManager.ReloadExtension(extPath);
            return true;
        }

        #endregion Extension Management

        /// <summary>
        /// Calls a hook
        /// </summary>
        /// <param name="hookname"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hookname, params object[] args) => RootPluginManager.CallHook(hookname, args);

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
            return RootPluginManager.CallDeprecatedHook(oldHook, newHook, expireDate, args);
        }

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
                        RootLogger.WriteException("Exception while calling NextTick callback", ex);
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
                    RootLogger.WriteException($"{ex.GetType().Name} while invoke OnFrame in extensions", ex);
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
                IsShuttingDown = true;
                UnloadAllPlugins();

                foreach (Extension extension in ExtensionManager.GetAllExtensions())
                {
                    extension.OnShutdown();
                }

                foreach (string name in ExtensionManager.GetLibraries())
                {
                    ExtensionManager.GetLibrary(name).Shutdown();
                }

                libperm.SaveData();

                RemoteConsole.Shutdown();
                ServerConsole.OnDisable();
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
