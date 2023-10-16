using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Pooling
{
    public class BaseArrayPoolProvider<T> : IArrayPoolProvider<T>
    {
        private readonly int maxSetCapacity;
        private readonly int maxArrayLength;

        private readonly T[] empty;
        private readonly ICollection<ICollection<T[]>> pooledArrays;

        public BaseArrayPoolProvider()
        {
            maxSetCapacity = 100;
            maxArrayLength = 50;
            // ReSharper disable once VirtualMemberCallInConstructor
            empty = InstantiateArray(0);

            if (maxArrayLength > 7)
            {
                pooledArrays = new HashSet<ICollection<T[]>>();
            }
            else
            {
                pooledArrays = new List<ICollection<T[]>>(maxArrayLength - 1);
            }

            for (int i = 0; i < maxArrayLength; i++)
            {
                ICollection<T[]> pool;

                if (maxSetCapacity > 6)
                {
                    pool = new HashSet<T[]>();
                }
                else
                {
                    pool = new List<T[]>(maxSetCapacity);
                }

                pooledArrays.Add(pool);
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

            ICollection<T[]> pooled = pooledArrays.ElementAt(length - 1);

            lock (pooled)
            {
                if (pooled.Count != 0)
                {
                    item = pooled.ElementAt(0);
                    pooled.Remove(item);
                }
                else
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

            ICollection<T[]> pooled = pooledArrays.ElementAt(array.Length - 1);

            lock (pooled)
            {
                if (pooled.Count < maxSetCapacity)
                {
                    pooled.Add(array);
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
