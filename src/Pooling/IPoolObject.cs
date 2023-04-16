using System;

namespace Oxide.Pooling
{
    public interface IPoolObject : IDisposable
    {
        /// <summary>
        /// The source pool this item came from
        /// </summary>
        IPoolSource Source { get; set; }

        /// <summary>
        /// Instruct the <see cref="IPoolSource"/> to ignore this object
        /// </summary>
        bool ShouldPool { get; set; }

        /// <summary>
        /// Reset this item back to the default state
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method should be called automatcially when this object gets returned to the <see cref="IPoolSource"/>
        /// </para>
        /// <para>
        /// This will not be called if <see cref="ShouldPool"/> is false
        /// </para>
        /// </remarks>
        void Cleanup();
    }
}
