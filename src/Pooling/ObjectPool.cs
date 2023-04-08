using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class ObjectPool<T> : IPoolSource<T> where T : class
    {
        public const int MaxPoolSize = 256;

        private readonly Type _poolType;
        private readonly List<T> _pool;

        public ObjectPool()
        {
            _poolType = typeof(T);
            _pool = new List<T>();
        }

        public T Get()
        {
            lock (_pool)
            {
                if (_pool.Count == 0)
                {
                    return (T)Activator.CreateInstance(typeof(T));
                }

                T item = _pool[0];
                _pool.RemoveAt(0);
                return item;
            }
        }

        public void Free(ref object item)
        {
            if (item == null || item.GetType() != _poolType)
            {
                return;
            }

            if (item is IPoolObject poolObj)
            {
                if (poolObj.ShouldPool)
                {
                    poolObj.Cleanup();
                }
                else
                {
                    return;
                }
            }

            T obj = (T)item;
            item = null;

            lock (_pool)
            {
                if (_pool.Count >= MaxPoolSize)
                {
                    return;
                }

                _pool.Add(obj);
            }
        }
    }
}
