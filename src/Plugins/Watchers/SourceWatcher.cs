namespace uMod.Plugins.Watchers
{
    /// <summary>
    /// Represents a file system watcher
    /// </summary>
    public sealed class SourceWatcher : AbstractWatcher
    {
        /// <summary>
        /// Initializes a new instance of the SourceWatcher class
        /// </summary>
        /// <param name="directory"></param>
        /// <param name="filter"></param>
        public SourceWatcher(string directory, string filter) : base(directory, filter)
        {
        }
    }
}
