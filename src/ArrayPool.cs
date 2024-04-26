using System;
using Oxide.Pooling;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        private static IArrayPoolProvider<object> Pool { get; }

        static ArrayPool()
        {
            Pool = Interface.Services.GetArrayPoolProvider<object>();
        }

        [Obsolete("Use Interface.Oxide.PoolFactory")]
        public static object[] Get(int length) => Pool.Take(length);

        [Obsolete("Use Interface.Oxide.PoolFactory")]
        public static void Free(object[] array) => Pool.Return(array);
    }
}
