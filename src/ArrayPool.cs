using Oxide.Core.Pooling;
using System;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        [Obsolete("This method is depricated; Please use method Oxide.Core.Pooling.Pool.Array")]
        public static object[] Get(int length) => Pool.Array<object>(length);

        [Obsolete("This method is depricated; Please use method Oxide.Core.Pooling.Pool.Free")]
        public static void Free(object[] array) => Pool.Free(ref array);
    }
}
