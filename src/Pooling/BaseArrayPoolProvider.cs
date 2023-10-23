using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class BaseArrayPoolProvider<T> : IArrayPoolProvider<T>
    {
        private readonly int maxSetCapacity;
        private readonly int maxArrayLength;

        private readonly T[] empty;
        private readonly Stack<T[]>[] pooledArrays;

        public BaseArrayPoolProvider()
        {
            maxSetCapacity = 100;
            maxArrayLength = 50;
            // ReSharper disable once VirtualMemberCallInConstructor
            empty = InstantiateArray(0);

            pooledArrays = new Stack<T[]>[maxArrayLength];

            for (int i = 0; i < pooledArrays.Length; i++)
            {
                pooledArrays[i] = new Stack<T[]>(maxSetCapacity);
            }
        }

        public T[] Take() => empty;

        public T[] Take(int length)
        {
            if (length == 0)
            {
                return empty;
            }

            if (length > maxArrayLength)
            {
                return InstantiateArray(length);
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, "must be at least zero");
            }

            T[] item;
            Stack<T[]> pooled = pooledArrays[length - 1];

            lock (pooled)
            {
                if (!pooled.TryPop(out item))
                {
                    item = InstantiateArray(length);
                }
            }

            OnTake(item);
            return item;
        }

        public void Return(object item)
        {
            if (!(item is T[] array))
            {
                return;
            }

            if (array.Length == 0 || array.Length > maxArrayLength)
            {
                return;
            }

            if (!OnReturn(array))
            {
                return;
            }

            Stack<T[]> pooled = pooledArrays[array.Length - 1];

            lock (pooled)
            {
                if (pooled.Count < maxSetCapacity)
                {
                    pooled.Push(array);
                }
            }
        }

        protected virtual void OnTake(T[] array)
        {
        }

        protected virtual bool OnReturn(T[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = default;
            }

            return true;
        }

        protected virtual T[] InstantiateArray(int length) => new T[length];
    }
}
