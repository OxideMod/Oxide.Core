using Oxide.Core.Plugins;

namespace Oxide.Core.Pooling
{
    /// <summary>
    /// A interface to provide a loosely coupled pooling system
    /// </summary>
    public interface IPoolProvider
    {
        /// <summary>
        /// Retrieves a item from the pool
        /// </summary>
        /// <returns></returns>
        object Get();

        /// <summary>
        /// Returns a specific item back to the pool and resetting the object back to its default state
        /// </summary>
        /// <param name="item"></param>
        void Free(object item);

        /// <summary>
        /// Called when a plugin is unloaded so related types can be purged from the pools
        /// </summary>
        /// <param name="plugin"></param>
        void OnPluginUnloaded(Plugin plugin);
    }
}
