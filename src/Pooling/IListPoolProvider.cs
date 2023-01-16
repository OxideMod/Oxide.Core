using System;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Core.Pooling
{
    /// <summary>
    /// A interface to provide a loosely coupled pooling system for <see cref="List{T}"/> Types
    /// </summary>
    public interface IListPoolProvider : IPoolProvider
    {
        /// <summary>
        /// Retrieves a <see cref="List{T}"/> from the pool
        /// </summary>
        /// <param name="listType">The List Type</param>
        /// <returns></returns>
        IList Get(Type listType);
    }
}
