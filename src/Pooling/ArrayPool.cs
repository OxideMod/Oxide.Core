using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    internal class ArrayPool<T> : IArrayPool<T>
    {
        public const int MaxArrayLength = 50;
        public const int MaxPoolSize = 256;

        public T[] Empty { get; }

        private List<Queue<T[]>> Pool { get; }

        public ArrayPool()
        {
            Empty = (T[])Array.CreateInstance(typeof(T), 0);
            Pool = new List<Queue<T[]>>(49);
            for (int i = 0; i < MaxArrayLength - 1; i++)
            {
                Pool.Add(new Queue<T[]>());
            }
        }

        public T[] Claim(int parameter)
        {
            if (parameter < 0)
            {
                throw new IndexOutOfRangeException("Index is less than zero");
            }

            if (parameter == 0)
            {
                return Claim();
            }

            if (parameter > MaxArrayLength)
            {
                return (T[])Array.CreateInstance(typeof(T), parameter);
            }

            Queue<T[]> p = Pool[parameter - 1];
            lock (p)
            {
                if (p.Count == 0)
                {
                    return (T[])Array.CreateInstance(typeof(T), parameter);
                }

                return p.Dequeue();
            }
        }

        public T[] Claim() => Empty;

        public void Unclaim(T[] instance)
        {
            if (instance == null || instance.Length == 0)
            {
                return;
            }

            Array.Clear(instance, 0, instance.Length);

            if (instance.Length > MaxArrayLength)
            {
                return;
            }

            Queue<T[]> p = Pool[instance.Length - 1];

            lock (p)
            {
                if (p.Count >= MaxPoolSize)
                {
                    return;
                }

                p.Enqueue(instance);
            }
        }
    }
}
