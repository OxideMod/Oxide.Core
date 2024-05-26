﻿using System;
using Oxide.Pooling;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        private static readonly IArrayPoolProvider<object> pool;

        static ArrayPool()
        {
            pool = Interface.Oxide.PoolFactory.GetArrayProvider<object>();
        }

        [Obsolete("Use Interface.Oxide.PoolFactory")]
        public static object[] Get(int length) => pool.Take(length);

        [Obsolete("Use Interface.Oxide.PoolFactory")]
        public static void Free(object[] array) => pool.Return(array);
    }
}
