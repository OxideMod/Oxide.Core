using System.IO;

namespace uMod.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class ConfigWatcher : AbstractWatcher
    {
        /// <summary>
        /// Initializes a new instance of the SourceWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public ConfigWatcher(string directory, string filter) : base(directory, filter)
        {
        }

        /// <summary>
        /// Called when the watcher has registered a file system change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        protected override void watcher_Changed(object sender, FileSystemEventArgs args)
        {
            if (Interface.uMod.ConfigChanges.Contains(args.FullPath))
            {
                Interface.uMod.ConfigChanges.Remove(args.FullPath);
            }
            else
            {
                base.watcher_Changed(sender, args);
            }
        }
    }
}
