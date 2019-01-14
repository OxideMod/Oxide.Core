using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using uMod.Logging;

namespace uMod.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher
    {
        private class QueuedChange
        {
            internal WatcherChangeTypes type;
            internal Libraries.Timer.TimerInstance timer;
        }

        // The file system watcher
        private FileSystemWatcher watcher;

        // The plugin list
        private ICollection<string> watchedPlugins;

        // Changes are buffered briefly to avoid duplicate events
        private Dictionary<string, QueuedChange> changeQueue;

        private Libraries.Timer timers;

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            watchedPlugins = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChange>();
            timers = Interface.uMod.GetLibrary<Libraries.Timer>();

            if (Interface.uMod.Config.Options.PluginWatchers)
            {
                LoadWatcher(directory, filter);
            }
            else
            {
                Interface.uMod.LogWarning("Automatic plugin reloading and unloading has been disabled");
            }
        }

        /// <summary>
        /// Loads the file system watcher
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        private void LoadWatcher(string directory, string filter)
        {
            // Create the watcher
            watcher = new FileSystemWatcher(directory, filter);
            watcher.Changed += watcher_Changed;
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Error += watcher_Error;
            watcher.NotifyFilter = NotifyFilters.LastWrite;
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            GC.KeepAlive(watcher);
        }

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="name"></param>
        public void AddMapping(string name) => watchedPlugins.Add(name);

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapping(string name) => watchedPlugins.Remove(name);

        /// <summary>
        /// Called when the watcher has registered a file system change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileSystemWatcher watcher = (FileSystemWatcher)sender;
            int length = e.FullPath.Length - watcher.Path.Length - Path.GetExtension(e.Name).Length - 1;
            string subPath = e.FullPath.Substring(watcher.Path.Length + 1, length);

            if (!changeQueue.TryGetValue(subPath, out QueuedChange change))
            {
                change = new QueuedChange();
                changeQueue[subPath] = change;
            }
            change.timer?.Destroy();
            change.timer = null;

            switch (e.ChangeType)
            {
                case WatcherChangeTypes.Changed:
                    if (change.type != WatcherChangeTypes.Created)
                    {
                        change.type = WatcherChangeTypes.Changed;
                    }

                    break;

                case WatcherChangeTypes.Created:
                    if (change.type == WatcherChangeTypes.Deleted)
                    {
                        change.type = WatcherChangeTypes.Changed;
                    }
                    else
                    {
                        change.type = WatcherChangeTypes.Created;
                    }

                    break;

                case WatcherChangeTypes.Deleted:
                    if (change.type == WatcherChangeTypes.Created)
                    {
                        changeQueue.Remove(subPath);
                        return;
                    }

                    change.type = WatcherChangeTypes.Deleted;
                    break;
            }

            Interface.uMod.NextTick(() =>
            {
                change.timer?.Destroy();
                change.timer = timers.Once(.2f, () =>
                {
                    change.timer = null;
                    changeQueue.Remove(subPath);
                    if (Regex.Match(subPath, @"include\\", RegexOptions.IgnoreCase).Success)
                    {
                        if (change.type == WatcherChangeTypes.Created || change.type == WatcherChangeTypes.Changed)
                        {
                            FirePluginSourceChanged(subPath);
                        }

                        return;
                    }

                    switch (change.type)
                    {
                        case WatcherChangeTypes.Changed:
                            if (watchedPlugins.Contains(subPath))
                            {
                                FirePluginSourceChanged(subPath);
                            }
                            else
                            {
                                FirePluginAdded(subPath);
                            }

                            break;

                        case WatcherChangeTypes.Created:
                            FirePluginAdded(subPath);
                            break;

                        case WatcherChangeTypes.Deleted:
                            if (watchedPlugins.Contains(subPath))
                            {
                                FirePluginRemoved(subPath);
                            }

                            break;
                    }
                });
            });
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            Interface.uMod.NextTick(() =>
            {
                Interface.uMod.LogError("FSWatcher error: {0}", e.GetException());
                RemoteLogger.Exception("FSWatcher error", e.GetException());
            });
        }
    }
}
