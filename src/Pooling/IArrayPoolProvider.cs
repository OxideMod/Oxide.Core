using System;

namespace Oxide.Core.Pooling
{
    /// <summary>
    /// A interface to provide a loosely coupled pooling system for <see cref="Array"/> Types
    /// </summary>
    public interface IArrayPoolProvider : IPoolProvider
    {
        /// <summary>
        /// Retrieves an <see cref="Array"/> from the pool
        /// </summary>
        /// <param name="arrayType">The Array Type</param>
        /// <param name="length">The desired length of the array</param>
        /// <returns></returns>
        Array Get(Type arrayType, int length);
    }
}
