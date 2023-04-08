using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class ArrayPool<T> : IArrayPoolSource<T>
    {
        public const int MaxPooledArrays = 256;
        public const int MaxArrayLength = 50;

        private readonly Type _poolType;
        private readonly T[] _empty;
        private readonly List<List<T[]>> _pool;

        public ArrayPool()
        {
            _poolType = typeof(T[]);
            _empty = new T[0];
            _pool = new List<List<T[]>>();

            for (int i = 0; i < MaxArrayLength; i++)
            {
                List<T[]> p = new List<T[]>();
                _pool.Add(p);
            }
        }

        public T[] Get() => _empty;

        public T[] Get(int length)
        {
            if (length <= 0)
            {
                return Get();
            }

            List<T[]> values = _pool[length - 1];

            lock (values)
            {
                if (values.Count == 0)
                {
                    return new T[length];
                }

                T[] item = values[0];
                values.RemoveAt(0);
                return item;
            }
        }

        public void Free(ref object item)
        {
            if (item == null || item.GetType() != _poolType)
            {
                return;
            }

            T[] items = (T[])item;
            item = null;
            if (items.Length == 0)
            {
                return;
            }

            for (int i = 0; i < items.Length; i++)
            {
                T itm = items[i];
                items[i] = default;

                if (itm is IPoolObject obj && obj.ShouldPool)
                {
                    PoolFactory.Free(ref obj);
                }
            }

            List<T[]> pool = _pool[items.Length - 1];

            lock (pool)
            {
                if (pool.Count >= MaxPooledArrays)
                {
                    return;
                }

                pool.Add(items);
            }
        }
    }
}
