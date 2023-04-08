using Oxide.Pooling;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        private static readonly IArrayPoolSource<object> _pool;

        static ArrayPool()
        {
            _pool = (IArrayPoolSource<object>)PoolFactory.GetSource<object[]>();
        }

        public static object[] Get(int length) => _pool.Get(length);

        public static void Free(object[] array) => _pool.Free(array);
    }
}
