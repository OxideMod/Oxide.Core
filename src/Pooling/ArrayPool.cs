using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class ArrayPool<T> : IArrayPoolSource<T> where T : class
    {
        public const int MaxPooledArrays = 256;
        public const int MaxArrayLength = 50;

        private readonly bool _arePooledItems;
        private readonly Type _poolType;
        private readonly T[] _empty;
        private readonly List<List<T[]>> _pool;

        public ArrayPool()
        {
            _poolType = typeof(T[]);
            _empty = new T[0];
            _pool = new List<List<T[]>>();
            _arePooledItems = typeof(IPoolObject).IsAssignableFrom(_poolType.GetElementType());

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

            if (length > MaxArrayLength)
            {
                return new T[length];
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

        public void Free(object item)
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

            IPoolSource<T> elementSource = _arePooledItems ? PoolFactory.GetSource<T>() : null;

            for (int i = 0; i < items.Length; i++)
            {
                object itm = items[i];
                items[i] = null;

                if (_arePooledItems)
                {
                    IPoolObject obj = itm as IPoolObject;

                    if (obj.Source != null)
                    {
                        obj.Source.Free(obj);
                    }
                    else if (elementSource != null)
                    {
                        elementSource.Free(obj);
                    }
                }
            }

            if (items.Length > MaxArrayLength)
            {
                return;
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
