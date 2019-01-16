using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using uMod.Configuration;
using uMod.Libraries;
using uMod.Libraries.Universal;

namespace uMod.Plugins
{
    public delegate void PluginError(Plugin sender, string message);

    public class PluginManagerEvent : Event<Plugin, PluginManager> { }

    /// <summary>
    /// Represents a single plugin
    /// </summary>
    public abstract class Plugin
    {
        public static implicit operator bool(Plugin plugin) => plugin != null;

        public static bool operator !(Plugin plugin) => !(bool)plugin;

        /// <summary>
        /// Gets the source file name, if any
        /// </summary>
        public string Filename { get; protected set; }

        /// <summary>
        /// Gets the internal name of this plugin
        /// </summary>
        private string name;

        public string Name
        {
            get => name;
            set
            {
                if (string.IsNullOrEmpty(Name) || name == GetType().Name)
                {
                    name = value;
                }
            }
        }

        /// <summary>
        /// Gets the user-friendly title of this plugin
        /// </summary>
        public string Title { get; protected set; }

        /// <summary>
        /// Gets the description of this plugin
        /// </summary>
        public string Description { get; protected set; }

        /// <summary>
        /// Gets the author of this plugin
        /// </summary>
        public string Author { get; protected set; }

        /// <summary>
        /// Gets the version of this plugin
        /// </summary>
        public VersionNumber Version { get; protected set; }

        /// <summary>
        /// Gets the plugin manager responsible for this plugin
        /// </summary>
        public PluginManager Manager { get; private set; }

        /// <summary>
        /// Gets if this plugin has a config file or not
        /// </summary>
        public bool HasConfig { get; protected set; }

        /// <summary>
        /// Gets if this plugin has a lang file or not
        /// </summary>
        public bool HasMessages { get; protected set; }

        /// <summary>
        /// Gets if this plugin should never be unloaded
        /// </summary>
        private bool isCorePlugin;

        public bool IsCorePlugin
        {
            get => isCorePlugin;
            set
            {
                if (!Interface.uMod.HasLoadedCorePlugins)
                {
                    isCorePlugin = value;
                }
            }
        }

        /// <summary>
        /// Gets the PluginLoader which loaded this plugin
        /// </summary>
        public PluginLoader Loader { get; set; }

        /// <summary>
        /// Gets the object associated with this plugin
        /// </summary>
        public virtual object Object => this;

        /// <summary>
        /// Gets the config file in use by this plugin
        /// </summary>
        public DynamicConfigFile Config { get; private set; }

        /// <summary>
        /// Called when this plugin has raised an error
        /// </summary>
        public event PluginError OnError;

        /// <summary>
        /// Called when this plugin was added to a manager
        /// </summary>
        public PluginManagerEvent OnAddedToManager = new PluginManagerEvent();

        /// <summary>
        /// Called when this plugin was removed from a manager
        /// </summary>
        public PluginManagerEvent OnRemovedFromManager = new PluginManagerEvent();

        /// <summary>
        /// Has this plugins Init/Loaded hook been called
        /// </summary>
        public bool IsLoaded { get; internal set; }

        /// <summary>
        /// Gets or sets the total hook time
        /// </summary>
        /// <value>The total hook time.</value>
        public double TotalHookTime { get; internal set; }

        // Used to measure time spent in this plugin
        private Stopwatch trackStopwatch = new Stopwatch();
        private Stopwatch stopwatch = new Stopwatch();
        //private float trackStartAt;
        private float averageAt;
        private double sum;
        private int preHookGcCount;

        // The depth of hook call nesting
        protected int nestcount;

        private class CommandInfo
        {
            public readonly string[] Names;
            public readonly string[] PermissionsRequired;
            public readonly CommandCallback Callback;

            public CommandInfo(string[] names, string[] perms, CommandCallback callback)
            {
                Names = names;
                PermissionsRequired = perms;
                Callback = callback;
            }
        }

        private IDictionary<string, CommandInfo> commandInfos;

        private Permission permission = Interface.uMod.GetLibrary<Permission>();

        /// <summary>
        /// Initializes an empty version of the Plugin class
        /// </summary>
        protected Plugin()
        {
            Name = GetType().Name;
            Title = Name.Humanize();
            Author = "Unnamed";
            Version = new VersionNumber(1, 0, 0);
            commandInfos = new Dictionary<string, CommandInfo>();
        }

        /// <summary>
        /// Subscribes this plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        protected void Subscribe(string hook) => Manager.SubscribeToHook(hook, this);

        /// <summary>
        /// Unsubscribes this plugin to the specified hook
        /// </summary>
        /// <param name="hook"></param>
        protected void Unsubscribe(string hook) => Manager.UnsubscribeToHook(hook, this);

        /// <summary>
        /// Called when this plugin has been added to the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleAddedToManager(PluginManager manager)
        {
            Manager = manager;
            if (HasConfig)
            {
                LoadConfig();
            }

            if (HasMessages)
            {
                LoadDefaultMessages();
            }

            OnAddedToManager?.Invoke(this, manager);
            RegisterWithUniversal();
        }

        /// <summary>
        /// Called when this plugin has been removed from the specified manager
        /// </summary>
        /// <param name="manager"></param>
        public virtual void HandleRemovedFromManager(PluginManager manager)
        {
            UnregisterWithUniversal();
            if (Manager == manager)
            {
                Manager = null;
            }

            OnRemovedFromManager?.Invoke(this, manager);
        }

        /// <summary>
        /// Called when this plugin is loading
        /// </summary>
        public virtual void Load()
        {
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object CallHook(string hook, params object[] args)
        {
            float startedAt = 0f;
            if (!IsCorePlugin && nestcount == 0)
            {
                preHookGcCount = GC.CollectionCount(0);
                startedAt = Interface.uMod.Now;
                stopwatch.Start();
                if (averageAt < 1)
                {
                    averageAt = startedAt;
                }
            }
            TrackStart();
            nestcount++;
            try
            {
                return OnCallHook(hook, args);
            }
            catch (Exception ex)
            {
                Interface.uMod.LogException($"Failed to call hook '{hook}' on plugin '{Name} v{Version}'", ex); // TODO: Localization
                return null;
            }
            finally
            {
                nestcount--;
                TrackEnd();
                if (startedAt > 0)
                {
                    stopwatch.Stop();
                    double duration = stopwatch.Elapsed.TotalSeconds;
                    if (duration > 0.1)
                    {
                        string suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                        Interface.uMod.LogWarning($"Calling '{hook}' on '{Name} v{Version}' took {duration * 1000:0}ms{suffix}");
                    }
                    stopwatch.Reset();
                    double total = sum + duration;
                    double endedAt = startedAt + duration;
                    if (endedAt - averageAt > 10)
                    {
                        total /= endedAt - averageAt;
                        if (total > 0.1)
                        {
                            string suffix = preHookGcCount == GC.CollectionCount(0) ? string.Empty : " [GARBAGE COLLECT]";
                            Interface.uMod.LogWarning($"Calling '{hook}' on '{Name} v{Version}' took average {sum * 1000:0}ms{suffix}");
                        }
                        sum = 0;
                        averageAt = 0;
                    }
                    else
                    {
                        sum = total;
                    }
                }
            }
        }

        /// <summary>
        /// Calls a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object Call(string hook, params object[] args) => CallHook(hook, args);

        /// <summary>
        /// Calls a hook on this plugin and converts the return value to the specified type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public T Call<T>(string hook, params object[] args) => (T)Convert.ChangeType(CallHook(hook, args), typeof(T));

        /// <summary>
        /// Called when it's time to run a hook on this plugin
        /// </summary>
        /// <param name="hook"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected abstract object OnCallHook(string hook, object[] args);

        /// <summary>
        /// Raises an error on this plugin
        /// </summary>
        /// <param name="message"></param>
        public void RaiseError(string message) => OnError?.Invoke(this, message);

        public void TrackStart()
        {
            if (!IsCorePlugin && nestcount <= 0)
            {
                Stopwatch stopwatch = trackStopwatch;
                if (!stopwatch.IsRunning)
                {
                    stopwatch.Start();
                }
            }
        }

        public void TrackEnd()
        {
            if (!IsCorePlugin && nestcount <= 0)
            {
                Stopwatch stopwatch = trackStopwatch;
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                    TotalHookTime += stopwatch.Elapsed.TotalSeconds;
                    stopwatch.Reset();
                }
            }
        }

        #region Config

        /// <summary>
        /// Loads the config file for this plugin
        /// </summary>
        protected virtual void LoadConfig()
        {
            Config = new DynamicConfigFile(Path.Combine(Manager.ConfigPath, $"{Name}.json"));
            if (!Config.Exists())
            {
                LoadDefaultConfig();
                SaveConfig();
            }
            try
            {
                Config.Load();
            }
            catch (Exception ex)
            {
                RaiseError($"Failed to load config file (is the config file corrupt?) ({ex.Message})");
            }
        }

        /// <summary>
        /// Populates the config with default settings
        /// </summary>
        protected virtual void LoadDefaultConfig() => CallHook("LoadDefaultConfig", null);

        /// <summary>
        /// Saves the config file for this plugin
        /// </summary>
        protected virtual void SaveConfig()
        {
            if (Config != null)
            {
                try
                {
                    Config.Save();
                }
                catch (Exception ex)
                {
                    RaiseError($"Failed to save config file (does the config have illegal objects in it?) ({ex.Message})");
                }
            }
        }

        #endregion Config

        #region Lang

        /// <summary>
        /// Populates the lang file(s) with default messages
        /// </summary>
        protected virtual void LoadDefaultMessages() => CallHook("LoadDefaultMessages", null);

        #endregion Lang

        #region Universal

        [Obsolete("Use AddUniversalCommand instead")]
        public void AddCovalenceCommand(string command, string callback, string perm = null)
        {
            AddUniversalCommand(command, callback, perm);
        }

        public void AddUniversalCommand(string command, string callback, string perm = null)
        {
            AddUniversalCommand(new[] { command }, callback, string.IsNullOrEmpty(perm) ? null : new[] { perm });
        }

        [Obsolete("Use AddUniversalCommand instead")]
        public void AddCovalenceCommand(string[] commands, string callback, string perm)
        {
            AddUniversalCommand(commands, callback, perm);
        }

        public void AddUniversalCommand(string[] commands, string callback, string perm)
        {
            AddUniversalCommand(commands, callback, string.IsNullOrEmpty(perm) ? null : new[] { perm });
        }

        [Obsolete("Use AddUniversalCommand instead")]
        public void AddCovalenceCommand(string[] commands, string callback, string[] perms = null)
        {
            AddUniversalCommand(commands, callback, perms);
        }

        public void AddUniversalCommand(string[] commands, string callback, string[] perms = null)
        {
            AddUniversalCommand(commands, perms, (caller, command, args) =>
            {
                CallHook(callback, caller, command, args);
                return true;
            });

            Universal universal = Interface.uMod.GetLibrary<Universal>();
            foreach (string command in commands)
            {
                universal.RegisterCommand(command, this, UniversalCommandCallback);
            }
        }

        [Obsolete("Use AddUniversalCommand instead")]
        protected void AddCovalenceCommand(string[] commands, string[] perms, CommandCallback callback)
        {
            AddUniversalCommand(commands, perms, callback);
        }

        protected void AddUniversalCommand(string[] commands, string[] perms, CommandCallback callback)
        {
            foreach (string cmdName in commands)
            {
                if (commandInfos.ContainsKey(cmdName.ToLowerInvariant()))
                {
                    Interface.uMod.LogWarning("Universal command alias already exists: {0}", cmdName);
                    continue;
                }

                commandInfos.Add(cmdName.ToLowerInvariant(), new CommandInfo(commands, perms, callback));
            }

            if (perms != null)
            {
                foreach (string perm in perms)
                {
                    if (!permission.PermissionExists(perm))
                    {
                        permission.RegisterPermission(perm, this);
                    }
                }
            }
        }

        private void RegisterWithUniversal()
        {
            Universal universal = Interface.uMod.GetLibrary<Universal>();
            foreach (KeyValuePair<string, CommandInfo> pair in commandInfos)
            {
                universal.RegisterCommand(pair.Key, this, UniversalCommandCallback);
            }
        }

        private bool UniversalCommandCallback(IPlayer caller, string cmd, string[] args)
        {
            if (commandInfos.TryGetValue(cmd, out CommandInfo cmdInfo))
            {
                if (caller == null)
                {
                    Interface.uMod.LogWarning("Plugin.UniversalCommandCallback received null as the caller (bad game universal bindings?)"); // TODO: Localization
                    return false;
                }

                if (cmdInfo.PermissionsRequired != null)
                {
                    foreach (string perm in cmdInfo.PermissionsRequired)
                    {
                        if (!caller.HasPermission(perm) && !caller.IsServer && (!caller.IsAdmin || !IsCorePlugin))
                        {
                            caller.Message($"You don't have permission to use the command '{cmd}'!"); // TODO: Localization
                            return true;
                        }
                    }
                }

                cmdInfo.Callback(caller, cmd, args);
                return true;
            }

            return false;
        }

        private void UnregisterWithUniversal()
        {
            Universal universal = Interface.uMod.GetLibrary<Universal>();
            foreach (KeyValuePair<string, CommandInfo> pair in commandInfos)
            {
                universal.UnregisterCommand(pair.Key, this);
            }
        }

        #endregion Universal
    }
}
