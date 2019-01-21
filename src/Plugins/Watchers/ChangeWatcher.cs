using System.Collections.Generic;

namespace uMod.Plugins.Watchers
{
    public delegate void AddEvent(string name);

    public delegate void ChangeEvent(string name);

    public delegate void RemoveEvent(string name);

    /// <summary>
    /// A class that watches for changes in the source of an arbitrary set of plugins
    /// </summary>
    public abstract class ChangeWatcher
    {
        // The plugin list
        protected ICollection<string> watchedFiles;

        /// <summary>
        /// Called when new plugin has been added
        /// </summary>
        public event AddEvent OnAdded;

        /// <summary>
        /// Called when the source of the plugin has changed
        /// </summary>
        public event ChangeEvent OnChanged;

        /// <summary>
        /// Called when new plugin has been removed
        /// </summary>
        public event RemoveEvent OnRemoved;

        /// <summary>
        /// Fires the OnAdded event
        /// </summary>
        /// <param name="name"></param>
        protected void FireAdded(string name) => OnAdded?.Invoke(name);

        /// <summary>
        /// Fires the OnChanged event
        /// </summary>
        /// <param name="name"></param>
        protected void FireChanged(string name) => OnChanged?.Invoke(name);

        /// <summary>
        /// Fires the OnRemoved event
        /// </summary>
        /// <param name="name"></param>
        protected void FireRemoved(string name) => OnRemoved?.Invoke(name);

        /// <summary>
        /// Adds a filename-plugin mapping to this watcher
        /// </summary>
        /// <param name="name"></param>
        public void AddMapping(string name) => watchedFiles.Add(name);

        /// <summary>
        /// Removes the specified mapping from this watcher
        /// </summary>
        /// <param name="name"></param>
        public void RemoveMapping(string name) => watchedFiles.Remove(name);
    }
}
