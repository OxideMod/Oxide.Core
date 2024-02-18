using System;
using System.IO;

namespace Oxide.IO.Windows
{
    internal class WindowsFileSystemWatcher : CachedFileSystemWatcher
    {
        private readonly FileSystemWatcher watcher;
        private int currentSub = -1;

        public WindowsFileSystemWatcher(IFileSystem fs, string directory, bool subDirs, NotifyMask filter, StringComparison stringComparison = StringComparison.OrdinalIgnoreCase) : base(directory, subDirs, filter, fs, stringComparison)
        {
            watcher = new FileSystemWatcher(Directory, "*")
            {
                EnableRaisingEvents = false
            };

            watcher.IncludeSubdirectories = IncludeSubDirectories;
            watcher.NotifyFilter = NotifyFilters.LastAccess
                                   | NotifyFilters.LastWrite
                                   | NotifyFilters.FileName
                                   | NotifyFilters.DirectoryName;

            watcher.Deleted += OnDeleted;
            watcher.Changed += OnChanged;
            watcher.Created += OnCreated;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;
        }

        public override void EndInit()
        {
            watcher.EnableRaisingEvents = true;
            base.EndInit();
        }

        protected override int SubscribeTo(string directory)
        {
            return currentSub++;
        }

        protected override bool UnsubscribeFrom(int id)
        {
            return true;
        }

        protected override void OnDispose()
        {
            watcher.Dispose();
        }

        private void OnRenamed(object sender, RenamedEventArgs args)
        {
            bool isDirectory = System.IO.Directory.Exists(args.FullPath);
            CleanName(args.OldFullPath, out string directory, out string name);
            OnFileSystemEvent(directory, name, isDirectory ? NotifyMask.OnMovedFrom | NotifyMask.DirectoryOnly : NotifyMask.OnMovedFrom);
            CleanName(args.FullPath, out directory, out name);
            OnFileSystemEvent(directory, name, isDirectory ? NotifyMask.OnMovedTo | NotifyMask.DirectoryOnly : NotifyMask.OnMovedTo);
        }

        private void OnDeleted(object sender, FileSystemEventArgs args)
        {
            bool isDirectory = args.FullPath.EndsWith(Path.DirectorySeparatorChar.ToString()) || !Path.HasExtension(args.FullPath);
            CleanName(args.FullPath, out string directory, out string name);
            OnFileSystemEvent(directory, name, isDirectory ? NotifyMask.OnDeleted | NotifyMask.DirectoryOnly : NotifyMask.OnDeleted);
        }

        private void OnError(object sender, ErrorEventArgs args) => OnFileSystemError(args.GetException());

        private void OnCreated(object sender, FileSystemEventArgs args)
        {
            bool isDirectory = System.IO.Directory.Exists(args.FullPath);
            CleanName(args.FullPath, out string directory, out string name);
            OnFileSystemEvent(directory, name, isDirectory ? NotifyMask.OnCreated | NotifyMask.DirectoryOnly : NotifyMask.OnCreated);
        }

        private void OnChanged(object sender,  FileSystemEventArgs args)
        {
            bool isDirectory = System.IO.Directory.Exists(args.FullPath);
            CleanName(args.FullPath, out string directory, out string name);
            OnFileSystemEvent(directory, name, isDirectory ? NotifyMask.OnModified | NotifyMask.DirectoryOnly : NotifyMask.OnModified);
        }

        protected override void OnFileSystemEvent(string directory, string name, NotifyMask mask)
        {
            directory = FileSystem.ResolvePath(directory);
            string fullPath = Path.Combine(directory, name);
            CleanName(fullPath, out directory, out name);
            base.OnFileSystemEvent(directory, name, mask);
        }
    }
}
