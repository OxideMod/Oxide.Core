using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using uMod.Logging;

namespace uMod.Plugins.Watchers
{
    public abstract class AbstractWatcher : ChangeWatcher
    {
        protected class QueuedChange
        {
            internal WatcherChangeTypes type;
            internal Libraries.Timer.TimerInstance timer;
        }

        // The file system watcher
        protected FileSystemWatcher watcher;

        // Changes are buffered briefly to avoid duplicate events
        protected Dictionary<string, QueuedChange> changeQueue;

        protected Libraries.Timer timers;

        /// <summary>
        /// Initializes a new instance of the SourceWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        protected AbstractWatcher(string directory, string filter)
        {
            watchedFiles = new HashSet<string>();
            changeQueue = new Dictionary<string, QueuedChange>();
            timers = Interface.uMod.GetLibrary<Libraries.Timer>();

            LoadWatcher(directory, filter);
        }

        /// <summary>
        /// Loads the file system watcher
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected void LoadWatcher(string directory, string filter)
        {
            // Create the watcher
            watcher = new FileSystemWatcher(directory, filter);
            watcher.Changed += (sender, e) => watcher_Changed(sender, e);
            watcher.Created += (sender, e) => watcher_Changed(sender, e);
            watcher.Deleted += (sender, e) => watcher_Changed(sender, e);
            watcher.Error += (sender, e) => watcher_Error(sender, e);
            watcher.NotifyFilter = GetNotifyFilters();
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;
            GC.KeepAlive(watcher);
        }

        /// <summary>
        /// Get the filesystem watcher notify filters
        /// </summary>
        /// <returns></returns>
        protected virtual NotifyFilters GetNotifyFilters()
        {
            return NotifyFilters.LastWrite;
        }

        /// <summary>
        /// Called when the watcher has registered a file system change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected virtual void watcher_Changed(object sender, FileSystemEventArgs args)
        {
            FileSystemWatcher watcher = (FileSystemWatcher)sender;
            int length = args.FullPath.Length - watcher.Path.Length - Path.GetExtension(args.Name).Length - 1;
            string subPath = args.FullPath.Substring(watcher.Path.Length + 1, length);

            if (!changeQueue.TryGetValue(subPath, out QueuedChange change))
            {
                change = new QueuedChange();
                changeQueue[subPath] = change;
            }
            change.timer?.Destroy();
            change.timer = null;

            switch (args.ChangeType)
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
                            FireChanged(subPath);
                        }

                        return;
                    }

                    switch (change.type)
                    {
                        case WatcherChangeTypes.Changed:
                            if (watchedFiles.Contains(subPath))
                            {
                                FireChanged(subPath);
                            }
                            else
                            {
                                FireAdded(subPath);
                            }

                            break;

                        case WatcherChangeTypes.Created:
                            FireAdded(subPath);
                            break;

                        case WatcherChangeTypes.Deleted:
                            if (watchedFiles.Contains(subPath))
                            {
                                FireRemoved(subPath);
                            }

                            break;
                    }
                });
            });
        }

        protected virtual void watcher_Error(object sender, ErrorEventArgs e)
        {
            Interface.uMod.NextTick(() =>
            {
                Interface.uMod.LogError("Watcher error: {0}", e.GetException());
                RemoteLogger.Exception("Watcher error", e.GetException());
            });
        }
    }
}
