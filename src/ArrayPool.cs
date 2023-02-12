using Oxide.Pooling;
using System;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        [Obsolete("Get is outdated, use Oxide.Pooling.Pool.CalimArray")]
        public static object[] Get(int length) => Pool.ClaimArray<object>(length);

        [Obsolete("Free is outdated, use Oxide.Pooling.Pool.Unclaim")]
        public static void Free(object[] array) => Pool.Unclaim(ref array);
    }
}
