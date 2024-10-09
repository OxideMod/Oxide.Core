using System;
using Oxide.Pooling;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        private static IArrayPool<object> Pool { get; }

        static ArrayPool()
        {
            Pool = ArrayPool<object>.Shared;
        }

        [Obsolete("Use ArrayPool<T>.Shared")]
        public static object[] Get(int length) => Pool.Take(length);

        [Obsolete("Use ArrayPool<T>.Shared")]
        public static void Free(object[] array) => Pool.Return(array);
    }
}
