using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class ObjectPool<T> : IPoolSource<T> where T : class
    {
        public const int MaxPoolSize = 256;

        private readonly Type _poolType;
        private readonly List<T> _pool;
        private readonly bool _isPooledInterface;

        public ObjectPool()
        {
            _poolType = typeof(T);
            _pool = new List<T>();
            _isPooledInterface = typeof(IPoolObject).IsAssignableFrom(typeof(T));
        }

        public T Get()
        {
            T item;
            lock (_pool)
            {
                if (_pool.Count == 0)
                {
                    item = (T)Activator.CreateInstance(typeof(T));
                }
                else
                {
                    item = _pool[0];
                    _pool.RemoveAt(0);
                }
            }

            if (_isPooledInterface && item is IPoolObject pooled)
            {
                pooled.Source = this;
            }

            return item;
        }

        public void Free(object item)
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
