using System.Collections.Generic;

namespace Umod
{
    public static class ArrayPool
    {
        private static readonly List<Queue<object[]>> _pooledArrays = new List<Queue<object[]>>();

        private const int MaxArrayLength = 50;
        private const int InitialPoolAmount = 64;
        private const int MaxPoolAmount = 256;

        static ArrayPool()
        {
            for (int i = 0; i < MaxArrayLength; i++)
            {
                _pooledArrays.Add(new Queue<object[]>());
                SetupArrays(i + 1);
            }
        }

        public static object[] Get(int length)
        {
            if (length == 0 || length > MaxArrayLength)
            {
                return new object[length];
            }

            Queue<object[]> arrays = _pooledArrays[length - 1];
            lock (arrays)
            {
                if (arrays.Count == 0)
                {
                    SetupArrays(length);
                }
                return arrays.Dequeue();
            }
        }

        public static void Free(object[] array)
        {
            if (array != null && array.Length != 0 && array.Length <= MaxArrayLength)
            {
                // Cleanup array
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = null;
                }

                Queue<object[]> arrays = _pooledArrays[array.Length - 1];
                lock (arrays)
                {
                    if (arrays.Count > MaxPoolAmount)
                    {
                        for (int i = 0; i < InitialPoolAmount; i++)
                        {
                            arrays.Dequeue();
                        }

                        return;
                    }

                    arrays.Enqueue(array);
                }
            }
        }

        private static void SetupArrays(int length)
        {
            Queue<object[]> arrays = _pooledArrays[length - 1];
            for (int i = 0; i < InitialPoolAmount; i++)
            {
                arrays.Enqueue(new object[length]);
            }
        }
    }
}
