using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal abstract class BasePoolProvider<T> : IPoolProvider<T>
    {
        private int MaxPoolSize { get; }
        private readonly Stack<T> pooledData;

        protected BasePoolProvider()
        {
            MaxPoolSize = 50; // TODO: Set based on configuration
            pooledData = new Stack<T>(MaxPoolSize);
        }

        public T Take()
        {
            T item;
            lock (pooledData)
            {
                if (!pooledData.TryPop(out item))
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
                    pooledData.Push(typed);
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
