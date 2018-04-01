using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Core
{
    public static class ArrayPool
    {
        private const int MaxArrayLength = 10;
        private const int InitialPoolAmount = 64;
        private const int MaxPoolAmount = 256;

        private static List<Queue<object[]>> _pooledArrays = new List<Queue<object[]>>();

        static ArrayPool()
        {
            for(int i = 0; i < MaxArrayLength; i++)
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
            var arrays = _pooledArrays[length - 1];
            if (arrays.Count == 0)
            {
                SetupArrays(length);
            }
            return arrays.Dequeue();
        }

        public static void Free(object[] array)
        {
            if (array.Length == 0 || array.Length > MaxArrayLength)
            {
                return;
            }
            //Cleanup array
            for(int i = 0; i < array.Length; i++)
            {
                array[i] = null;
            }
            if (_pooledArrays[array.Length].Count > MaxPoolAmount)
            {
                return;
            }
            _pooledArrays[array.Length].Enqueue(array);
        }

        private static void SetupArrays(int length)
        {
            var arrays = _pooledArrays[length - 1];
            for (int i = 0; i < InitialPoolAmount; i++)
            {
                arrays.Enqueue(new object[length]);
            }
        }
    }
}
