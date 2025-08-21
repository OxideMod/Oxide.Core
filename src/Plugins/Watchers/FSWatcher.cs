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

        /// <summary>
        /// Initializes a new instance of the FSWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public FSWatcher(string directory, string filter)
        {
            StringPool = Interface.Oxide.PoolFactory.GetProvider<StringBuilder>();
            watchedPlugins = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChange>();
            timers = Interface.Oxide.GetLibrary<Timer>();

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
                Interface.Oxide.LogWarning("Automatic plugin reloading and unloading has been disabled");
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
            str.Capacity = 4096;
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
        private readonly object changeQueueLock = new object();

        private void watcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
#if DEBUG
                Interface.Oxide.LogInfo("[FSWatcher] Event received: {0} {1}", e.ChangeType, e.FullPath);
#endif

                FileSystemWatcher watcher = (FileSystemWatcher)sender;
                int length = e.FullPath.Length - watcher.Path.Length - Path.GetExtension(e.Name).Length - 1;
                string subPath = e.FullPath.Substring(watcher.Path.Length + 1, length);

#if DEBUG
                Interface.Oxide.LogInfo("[FSWatcher] SubPath resolved: {0}", subPath);
#endif

                QueuedChange change;
                lock (changeQueueLock)
                {
                    if (!changeQueue.TryGetValue(subPath, out change))
                    {
                        change = new QueuedChange();
                        changeQueue[subPath] = change;
#if DEBUG
                        Interface.Oxide.LogInfo("[FSWatcher] New change queued for: {0}", subPath);
#endif
                    }

                    change.timer?.Destroy();
                    change.timer = null;

                    switch (e.ChangeType)
                    {
                        case WatcherChangeTypes.Changed:
                            if (change.type != WatcherChangeTypes.Created)
                            {
                                change.type = WatcherChangeTypes.Changed;
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] ChangeType set to Changed for: {0}", subPath);
#endif
                            }
                            break;

                        case WatcherChangeTypes.Created:
                            change.type = change.type == WatcherChangeTypes.Deleted
                                ? WatcherChangeTypes.Changed
                                : WatcherChangeTypes.Created;
#if DEBUG
                            Interface.Oxide.LogInfo("[FSWatcher] ChangeType set to Created for: {0}", subPath);
#endif
                            break;

                        case WatcherChangeTypes.Deleted:
                            if (change.type == WatcherChangeTypes.Created)
                            {
                                changeQueue.Remove(subPath);
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] Deleted before processing, removed from queue: {0}", subPath);
#endif
                                return;
                            }

                            change.type = WatcherChangeTypes.Deleted;
#if DEBUG
                            Interface.Oxide.LogInfo("[FSWatcher] ChangeType set to Deleted for: {0}", subPath);
#endif
                            break;
                    }
                }

                Interface.Oxide.NextTick(() =>
                {
#if DEBUG
                    Interface.Oxide.LogInfo("[FSWatcher] NextTick started for: {0}", subPath);
#endif

                    if (Environment.OSVersion.Platform == PlatformID.Unix)
                    {
                        switch (e.ChangeType)
                        {
                            case WatcherChangeTypes.Created:
                                if (IsFileSymlink(e.FullPath))
                                {
#if DEBUG
                                    Interface.Oxide.LogInfo("[FSWatcher] Loading symlink: {0}", e.FullPath);
#endif
                                    LoadWatcherSymlink(e.FullPath);
                                }
                                break;

                            case WatcherChangeTypes.Deleted:
                                if (m_symlinkWatchers.ContainsKey(e.FullPath))
                                {
                                    m_symlinkWatchers.TryGetValue(e.FullPath, out FileSystemWatcher symlinkWatcher);
                                    symlinkWatcher?.Dispose();
                                    m_symlinkWatchers.Remove(e.FullPath);
#if DEBUG
                                    Interface.Oxide.LogInfo("[FSWatcher] Symlink watcher disposed: {0}", e.FullPath);
#endif
                                }
                                break;
                        }
                    }

                    change.timer?.Destroy();
                    change.timer = timers.Once(0.6f, () =>
                    {
#if DEBUG
                        Interface.Oxide.LogInfo("[FSWatcher] Timer fired for: {0}", subPath);
#endif

                        lock (changeQueueLock)
                        {
                            change.timer = null;
                            changeQueue.Remove(subPath);
#if DEBUG
                            Interface.Oxide.LogInfo("[FSWatcher] Removed from changeQueue: {0}", subPath);
#endif
                        }

                        if (Regex.Match(subPath, @"include\\", RegexOptions.IgnoreCase).Success)
                        {
#if DEBUG
                            Interface.Oxide.LogInfo("[FSWatcher] Include path detected: {0}", subPath);
#endif
                            if (change.type == WatcherChangeTypes.Created || change.type == WatcherChangeTypes.Changed)
                            {
                                FirePluginSourceChanged(subPath);
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] FirePluginSourceChanged called for include: {0}", subPath);
#endif
                            }
                            return;
                        }

                        switch (change.type)
                        {
                            case WatcherChangeTypes.Changed:
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] Handling Changed for: {0}", subPath);
#endif
                                if (watchedPlugins.Contains(subPath))
                                {
                                    FirePluginSourceChanged(subPath);
#if DEBUG
                                    Interface.Oxide.LogInfo("[FSWatcher] FirePluginSourceChanged called for: {0}", subPath);
#endif
                                }
                                else
                                {
                                    FirePluginAdded(subPath);
#if DEBUG
                                    Interface.Oxide.LogInfo("[FSWatcher] FirePluginAdded called for: {0}", subPath);
#endif
                                }
                                break;

                            case WatcherChangeTypes.Created:
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] Handling Created for: {0}", subPath);
#endif
                                FirePluginAdded(subPath);
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] FirePluginAdded called for: {0}", subPath);
#endif
                                break;

                            case WatcherChangeTypes.Deleted:
#if DEBUG
                                Interface.Oxide.LogInfo("[FSWatcher] Handling Deleted for: {0}", subPath);
#endif
                                if (watchedPlugins.Contains(subPath))
                                {
                                    FirePluginRemoved(subPath);
#if DEBUG
                                    Interface.Oxide.LogInfo("[FSWatcher] FirePluginRemoved called for: {0}", subPath);
#endif
                                }
                                break;
                        }
                    });
                });
            }
            catch (Exception ex)
            {
#if DEBUG
                Interface.Oxide.LogError("[FSWatcher] Exception in watcher_Changed: {0}", ex);
#endif
            }
        }

        private void watcher_Error(object sender, ErrorEventArgs e)
        {
            Interface.Oxide.NextTick(() =>
            {
                Interface.Oxide.LogError("FSWatcher error: {0}", e.GetException());
                RemoteLogger.Exception("FSWatcher error", e.GetException());
            });
        }
    }
}
