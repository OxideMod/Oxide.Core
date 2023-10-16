using System.Collections.Generic;
using System.Linq;

namespace Oxide.Pooling
{
    public abstract class BasePoolProvider<T> : IPoolProvider<T>
    {
        private int MaxPoolSize { get; }
        private readonly ICollection<T> pooledData;

        protected BasePoolProvider()
        {
            MaxPoolSize = 50; // TODO: Set based on configuration

            if (MaxPoolSize > 6)
            {
                pooledData = new HashSet<T>();
            }
            else
            {
                pooledData = new List<T>(MaxPoolSize);
            }
        }

        public T Take()
        {
            T item;
            lock (pooledData)
            {
                if (pooledData.Count > 0)
                {
                    item = pooledData.ElementAt(0);
                    pooledData.Remove(item);

                }
                else
                {
                    item = InstantiateItem();
                }
            }

            OnTake(item);
            return item;
        }

        public void Return(object item)
        {
            if (!(item is T typed) || !OnReturn(typed)) return;

            lock (pooledData)
            {
                if (pooledData.Count < MaxPoolSize)
                {
                    pooledData.Add(typed);
                }
            }
        }

        protected abstract T InstantiateItem();

        protected virtual void OnTake(T item)
        {
        }

        protected virtual bool OnReturn(T item)
        {
            return true;
        }
    }
}
