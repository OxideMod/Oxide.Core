extern alias References;

using Oxide.Core.Libraries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
#if !NETSTANDARD
using System.Security.Permissions;
#endif
using System.Text.RegularExpressions;
using Oxide.Core.Logging;
using Oxide.DependencyInjection;
using Oxide.Pooling;
using References::Mono.Unix.Native;
using Syscall = References::Mono.Unix.Native.Syscall;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class FSWatcher : PluginChangeWatcher
    {
        private class QueuedChange
        {
            internal WatcherChangeTypes type;
            internal Timer.TimerInstance timer;
        }

        // The filesystem watcher
        private FileSystemWatcher watcher;

        // The plugin list
        private ICollection<string> watchedPlugins;

        // Changes are buffered briefly to avoid duplicate events
        private Dictionary<string, QueuedChange> changeQueue;

        private Timer timers;

        private Dictionary<string, FileSystemWatcher> m_symlinkWatchers = new Dictionary<string, FileSystemWatcher>();
        private IPoolProvider<StringBuilder> StringPool { get; }
        private Logger Logger { get; }

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            StringPool = Interface.ServiceProvider.GetRequiredService<IPoolFactory>()
                                  .GetProvider<StringBuilder>();
            Logger = Interface.Oxide.RootLogger;
            watchedPlugins = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChange>();
            timers = Interface.ServiceProvider.GetRequiredService<Timer>();

            if (Interface.Oxide.Config.Options.PluginWatchers)
            {
                LoadWatcher(directory, filter);

                // Watch symlinked files
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    foreach (FileInfo fileInfo in new DirectoryInfo(directory).GetFiles(filter))
                    {
                        if (IsFileSymlink(fileInfo.FullName))
                        {
                            LoadWatcherSymlink(fileInfo.FullName);
                        }
                    }
                }
            }
            else
            {
                Logger.Write(LogType.Warning,"Automatic plugin reloading and unloading has been disabled");
            }
        }

        private bool IsFileSymlink(string path)
        {
            return (File.GetAttributes(path) & FileAttributes.ReparsePoint) > 0;
        }


#if !NETSTANDARD
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
#endif
        private void LoadWatcherSymlink(string path)
        {
            StringBuilder str = StringPool.Take();
            try
            {
                int count = Syscall.readlink(path, str);

                if (count == -1)
                {
                    Errno err = Stdlib.GetLastError();

                    switch (err)
                    {
                        case Errno.EINVAL:
                            return;

                        default:
                            throw new IOException($"Unable to process symlink | {err}", (int)err);
                    }
                }

                string realPath = str.ToString(0, count);
                string realDirName = Path.GetDirectoryName(realPath);
                string realFileName = Path.GetFileName(realPath);

                void symlinkTarget_Changed(object sender, FileSystemEventArgs e) => watcher_Changed(sender, e);

                FileSystemWatcher watcher = new FileSystemWatcher(realDirName, realFileName);
                m_symlinkWatchers[path] = watcher;
                watcher.Changed += symlinkTarget_Changed;
                watcher.Created += symlinkTarget_Changed;
                watcher.Deleted += symlinkTarget_Changed;
                watcher.Error += watcher_Error;
                watcher.NotifyFilter = NotifyFilters.LastWrite;
                watcher.IncludeSubdirectories = false;
                watcher.EnableRaisingEvents = true;
            }
            finally
            {
                StringPool.Return(str);
            }

        }

        /// <summary>
        /// Loads the filesystem watcher
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
#if !NETSTANDARD
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
#endif
        private void LoadWatcher(string directory, string filter)
        {
            // Create the watcher
            watcher = new FileSystemWatcher(directory, filter);
            watcher.Changed += watcher_Changed;
            watcher.Created += watcher_Changed;
            watcher.Deleted += watcher_Changed;
            watcher.Error += watcher_Error;
            watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
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
        /// Called when the watcher has registered a filesystem change
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

            Interface.Oxide.NextTick(() =>
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    switch (e.ChangeType)
                    {
                        case WatcherChangeTypes.Created:
                            if (IsFileSymlink(e.FullPath))
                            {
                                LoadWatcherSymlink(e.FullPath);
                            }
                            break;

                        case WatcherChangeTypes.Deleted:
                            if (m_symlinkWatchers.ContainsKey(e.FullPath))
                            {
                                m_symlinkWatchers.TryGetValue(e.FullPath, out FileSystemWatcher symlinkWatcher);
                                symlinkWatcher?.Dispose();
                                m_symlinkWatchers.Remove(e.FullPath);
                            }
                            break;
                    }
                }

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
            Interface.Oxide.NextTick(() =>
            {
                Logger.WriteException("FSWatcher error",e.GetException());
                RemoteLogger.Exception("FSWatcher error", e.GetException());
            });
        }
    }
}
