extern alias References;

using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Oxide.IO;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher, IObserver<FileSystemEvent>
    {
        private class QueuedChange
        {
            internal WatcherChangeTypes type;
            internal Timer.TimerInstance timer;
        }

        // The plugin list
        private ICollection<string> watchedPlugins;
        private Timer timers;
        private readonly string _directory;
        private readonly Regex filter;
        private readonly IFileSystemWatcher watcher;
        private readonly IDisposable subscription;

        public FSWatcher(IFileSystemWatcher watcher, string watchedDirectory, string fileFilter = "*")
        {
            if (!watchedDirectory.StartsWith(watcher.Directory, StringComparison.InvariantCulture))
            {
                throw new ArgumentException($"Path must be begin with {watcher.Directory}", nameof(watchedDirectory));
            }

            if (string.IsNullOrEmpty(fileFilter))
            {
                fileFilter = "*";
            }

            _directory = watchedDirectory;
            fileFilter = Regex.Escape(fileFilter)
                              .Replace("\\*", ".*")
                              .Replace("\\?", ".");
            fileFilter = "^" + fileFilter + "$";
            filter = new Regex(fileFilter, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            this.watcher = watcher;

            watchedPlugins = new HashSet<string>();
            timers = Interface.Oxide.GetLibrary<Timer>();

            if (Interface.Oxide.Config.Options.PluginWatchers)
            {
                subscription = this.watcher.Subscribe(this);
            }
            else
            {
                Interface.Oxide.LogWarning("Automatic plugin reloading and unloading has been disabled");
            }
        }

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter = "*") : this(Interface.Oxide.FileWatcher, directory, filter)
        {
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
        /// Called when the watcher has registered a filesystem change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void watcher_Changed(object sender, FileSystemEvent e)
        {
            string fullPath = Path.Combine(e.Directory, e.Name);
            int length = fullPath.Length - _directory.Length - Path.GetExtension(e.Name).Length - 1;
            string subPath = fullPath.Substring(_directory.Length + 1, length);

            Interface.Oxide.NextTick(() =>
            {
                if (Regex.Match(subPath, @"include\\", RegexOptions.IgnoreCase).Success)
                {
                    if (e.Event == NotifyMask.OnCreated || e.Event == NotifyMask.OnModified)
                    {
                        FirePluginSourceChanged(subPath);
                    }

                    return;
                }

                switch (e.Event)
                {
                    case NotifyMask.OnModified:
                        if (watchedPlugins.Contains(subPath))
                        {
                            FirePluginSourceChanged(subPath);
                        }
                        else
                        {
                            FirePluginAdded(subPath);
                        }
                        break;

                    case NotifyMask.OnCreated:
                        FirePluginAdded(subPath);
                        break;

                    case NotifyMask.OnDeleted:
                        if (watchedPlugins.Contains(subPath))
                        {
                            FirePluginRemoved(subPath);
                        }
                        break;
                }
            });
        }

        public void OnNext(FileSystemEvent value)
        {
            if (!value.Directory.StartsWith(_directory, StringComparison.InvariantCulture) || !filter.IsMatch(value.Name))
            {
                return;
            }

            if ((value.Event & NotifyMask.DirectoryOnly) == NotifyMask.DirectoryOnly)
            {
                return;
            }

#if DEBUG
            Interface.Oxide.LogDebug($"Processing {value.Event}: {Path.Combine(value.Directory, value.Name)}");
#endif
            watcher_Changed(watcher, value);
        }

        public void OnError(Exception error)
        {
            Interface.Oxide.LogError("FSWatcher error: {0}", error);
            RemoteLogger.Exception("FSWatcher error", error);
        }

        public void OnCompleted()
        {
        }
    }
}
