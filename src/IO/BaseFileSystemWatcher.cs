using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Oxide.Core;

namespace Oxide.IO
{
    internal abstract class BaseFileSystemWatcher : IFileSystemWatcher
    {
        private sealed class GuidDisposable : IDisposable
        {
            public Guid Id { get; }
            private BaseFileSystemWatcher Watcher { get; }

            public GuidDisposable(Guid id, BaseFileSystemWatcher watcher)
            {
                Id = id;
                Watcher = watcher;
            }

            ~GuidDisposable() => Dispose();

            public void Dispose() => Watcher.Unsubscribe(Id);
        }

        public string Directory { get; }
        public bool IncludeSubDirectories { get; }
        public NotifyMask Filter { get; }
        private List<Regex> filters { get; }
        private readonly Dictionary<Guid, IObserver<FileSystemEvent>> registeredWatchers;
        private readonly Dictionary<int, string> subscribedDirectories;

        protected IFileSystem FileSystem { get; }
        protected StringComparison CompareMethod { get; }

        #region Initialization

        protected BaseFileSystemWatcher(string directory, bool subDirs, NotifyMask filter, IFileSystem fs, StringComparison comparer)
        {
            CompareMethod = comparer;
            registeredWatchers = new Dictionary<Guid, IObserver<FileSystemEvent>>();
            subscribedDirectories = new Dictionary<int, string>();
            FileSystem = fs;
            Directory = fs.ResolvePath(directory);
            IncludeSubDirectories = subDirs;
            Filter = filter;
            filters = new List<Regex>();
        }

        public virtual void BeginInit()
        {
            LogDebug("Initializing FileSystem Watcher. . .");
        }

        public virtual void EndInit()
        {
            WatchDirectory(Directory, IncludeSubDirectories);
            Interface.Oxide.LogInfo($"Initialized FileSystem {GetType().Name} Watcher!");
        }

        #endregion

        public IDisposable Subscribe(IObserver<FileSystemEvent> observer)
        {
            if (observer == null)
            {
                throw new ArgumentNullException(nameof(observer));
            }

            lock (registeredWatchers)
            {
                if (registeredWatchers.Values.Contains(observer))
                {
                    throw new ArgumentException("Observer already is subscribed", nameof(observer));
                }

                GuidDisposable @guid = new GuidDisposable(Guid.NewGuid(), this);
                registeredWatchers.Add(@guid.Id, observer);
                return @guid;
            }
        }

        private void Unsubscribe(Guid guid)
        {
            lock (registeredWatchers)
            {
                registeredWatchers.Remove(guid);
            }
        }

        protected void WatchDirectory(string directory, bool recurse = false)
        {
            directory = FileSystem.ResolvePath(directory);

            if (!directory.StartsWith(Directory, CompareMethod))
            {
                LogDebug("Failed to watch directory, path does not inherit parent.");
                return;
            }

            lock (filters)
            {
                foreach (Regex filter in filters)
                {
                    if (filter.IsMatch(directory))
                    {
                        LogDebug("Failed to watch directory, Ignore pattern found");
                        return;
                    }
                }
            }

            lock (subscribedDirectories)
            {
                if (InternalIsMonitoredDirectory(directory))
                {
                    LogDebug("Failed to watch directory, directory is already watched.");
                    return;
                }

                int id = SubscribeTo(directory);
                subscribedDirectories[id] = directory;
            }

            if (!recurse)
            {
                return;
            }

            string[] dirs = System.IO.Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);

            for (int i = 0; i < dirs.Length; i++)
            {
                LogDebug($"Recursively adding subdirectory: {dirs[i]}");
                WatchDirectory(dirs[i], recurse);
            }
        }

        public IFileSystemWatcher ClearFilters()
        {
            lock (filters)
            {
                filters.Clear();
            }
            return this;
        }

        public IFileSystemWatcher Ignore(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return this;
            }

            pattern = Regex.Escape(pattern)
                           .Replace("\\*", ".+")
                           .Replace("\\?", ".");

            pattern = "^" + pattern + "$";

            lock (filters)
            {
                foreach (Regex reg in filters)
                {
                    if (reg.ToString() == pattern)
                    {
                        return this;
                    }
                }

                filters.Add(new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase));
            }

            return this;
        }

        protected abstract int SubscribeTo(string directory);

        protected abstract bool UnsubscribeFrom(int id);

        public bool IsMonitoredDirectory(int id)
        {
            lock (subscribedDirectories)
            {
                return subscribedDirectories.ContainsKey(id);
            }
        }

        public bool IsMonitoredDirectory(string directory)
        {
            directory = FileSystem.ResolvePath(directory);
            lock (subscribedDirectories)
            {
                return InternalIsMonitoredDirectory(directory);
            }
        }

        private bool InternalIsMonitoredDirectory(string directory)
        {
            return subscribedDirectories.ContainsValue(directory);
        }

        private List<int> removeBuffer = new List<int>();
        protected bool UnwatchDirectory(string directory)
        {
            bool removed = false;
            lock (subscribedDirectories)
            {
                foreach (KeyValuePair<int,string> pair in subscribedDirectories)
                {
                    if (pair.Value.StartsWith(directory, CompareMethod))
                    {
                        UnsubscribeFrom(pair.Key);
                        removeBuffer.Add(pair.Key);
                        removed = true;
                    }
                }

                foreach (int i in removeBuffer)
                {
                    subscribedDirectories.Remove(i);
                }

                removeBuffer.Clear();
            }

            return removed;
        }

        protected string GetDirectoryById(int id)
        {
            lock (subscribedDirectories)
            {
                return subscribedDirectories.TryGetValue(id, out string dir) ? dir : null;
            }
        }

        public void Dispose()
        {
            lock (registeredWatchers)
            {
                foreach (IObserver<FileSystemEvent> observer in registeredWatchers.Values)
                {
                    try
                    {
                        observer.OnCompleted();
                    }
                    catch
                    {
                        // Ignored
                    }
                }

                registeredWatchers.Clear();
            }

            lock (subscribedDirectories)
            {
                foreach (KeyValuePair<int,string> directory in subscribedDirectories)
                {
                    UnsubscribeFrom(directory.Key);
                }

                subscribedDirectories.Clear();
            }

            OnDispose();
        }

        protected virtual void OnDispose()
        {
        }

        protected virtual bool ShouldIgnore(string directory, string name, NotifyMask mask)
        {
            string path = Path.Combine(directory, name);
            lock (filters)
            {
                foreach (Regex regex in filters)
                {
                    if (regex.IsMatch(path))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        protected virtual void OnFileSystemEvent(string directory, string name, NotifyMask mask)
        {
            switch (mask)
            {
                case NotifyMask.OnMovedFrom | NotifyMask.DirectoryOnly:
                case NotifyMask.OnDeleted | NotifyMask.DirectoryOnly:
                    UnwatchDirectory(Path.Combine(directory, name));
                    break;

                case NotifyMask.OnMovedTo | NotifyMask.DirectoryOnly:
                    WatchDirectory(Path.Combine(directory, name), IncludeSubDirectories);
                    break;

                case NotifyMask.OnCreated | NotifyMask.DirectoryOnly:
                    WatchDirectory(Path.Combine(directory, name), IncludeSubDirectories);
                    break;
            }

            if (ShouldIgnore(directory, name, mask))
            {
                return;
            }

            FileSystemEvent evt = new FileSystemEvent(directory, name, mask);

            lock (registeredWatchers)
            {
                foreach (IObserver<FileSystemEvent> observer in registeredWatchers.Values)
                {
                    try
                    {
                        observer.OnNext(evt);
                    }
                    catch
                    {
                        // Ignored
                    }
                }
            }
        }

        protected virtual void OnFileSystemError(Exception exception)
        {
            lock (registeredWatchers)
            {
                foreach (IObserver<FileSystemEvent> observer in registeredWatchers.Values)
                {
                    try
                    {
                        observer.OnError(exception);
                    }
                    catch
                    {
                        // Ignored
                    }
                }
            }
        }

        protected void LogDebug(string message)
        {
#if DEBUG
            Interface.Oxide.LogDebug($"[{GetType().Name}] {message}");
#endif
        }

        protected static void CleanName(string fullPath, out string directory, out string name)
        {
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar);
            directory = Path.GetDirectoryName(fullPath);
            name = Path.GetFileName(fullPath);
        }
    }
}
