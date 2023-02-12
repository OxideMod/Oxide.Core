using System;
using System.Collections.Generic;
using System.Reflection;

namespace Oxide.Pooling
{
    internal class DynamicPool<T> : IClaimable<T> where T : class, new()
    {
        public const int MAX_POOL_SIZE = 256;
        private readonly object[] _methodInvoke;
        private readonly Type _type;
        private readonly MethodInfo _resetMethod;
        private readonly Queue<T> _pool = new Queue<T>();

        public DynamicPool()
        {
            _type = typeof(T);
            Type reset = typeof(IResetable);

            if (reset.IsAssignableFrom(_type))
            {
                _resetMethod = reset.GetMethod("Reset", BindingFlags.Public | BindingFlags.Instance);
            }
            else
            {
                _resetMethod = _type.GetMethod("Clear", BindingFlags.Public | BindingFlags.Instance);
            }

            if (_resetMethod != null)
            {
                _methodInvoke = Pool.ClaimArray<object>(0);
            }
        }

        public T Claim()
        {
            lock (_pool)
            {
                if (_pool.Count == 0)
                {
                    return new T();
                }

                T item = _pool.Dequeue();
                return item;
            }
        }

        public void Unclaim(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (_resetMethod != null)
            {
                _resetMethod.Invoke(instance, _methodInvoke);
            }

            lock (_pool)
            {
                if (_pool.Count >= MAX_POOL_SIZE)
                {
                    return;
                }

                _pool.Enqueue(instance);
            }
        }
    }
}
