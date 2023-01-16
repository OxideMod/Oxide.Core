using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Core.Pooling.Defaults
{
    internal class DefaultArrayPool : IArrayPoolProvider
    {
        private static int MaxPoolSize = 256;
        private static int ArrayMaxLength = 50;
        private static int StartingObjects = 0;

        private readonly Dictionary<Type, PoolInstance> _elementPool = new Dictionary<Type, PoolInstance>();

        private class PoolInstance
        {
            public Type ElementType { get; }
            public Array Empty { get; }
            public List<Queue<Array>> Pool { get; }

            public PoolInstance(Type arrayType)
            {
                ElementType = arrayType;
                Pool = new List<Queue<Array>>();
                Empty = Array.CreateInstance(arrayType, 0);

                for (int i = 0; i < ArrayMaxLength; i++)
                {
                    Queue<Array> a = new Queue<Array>();
                    Pool.Add(a);

                    for (int s = 0; s < StartingObjects; s++)
                    {
                        a.Enqueue(Array.CreateInstance(arrayType, i + 1));
                    }
                }
            }

            public Array Get(int length)
            {
                if (length == 0)
                {
                    return Empty;
                }

                Queue<Array> queue = Pool[length - 1];

                lock (queue)
                {
                    if (queue.Count == 0)
                    {
                        return Array.CreateInstance(ElementType, length);
                    }

                    return queue.Dequeue();
                }
            }

            public void Free(Array array)
            {
                Queue<Array> queue = Pool[array.Length - 1];
                lock (queue)
                {
                    if (queue.Count < MaxPoolSize)
                    {
                        queue.Enqueue(array);
                    }
                }
            }
        }

        public void Free(object item)
        {
            if (item == null || !(item is Array array))
            {
                return;
            }

            Array.Clear(array, 0, array.Length);

            if (array.Length != 0 && array.Length <= ArrayMaxLength)
            {
                Type elementType = array.GetType().GetElementType();
                lock (_elementPool)
                {
                    if (_elementPool.TryGetValue(elementType, out PoolInstance instance))
                    {
                        instance.Free(array);
                    }
                }
            }
        }

        public Array Get(Type arrayType, int length)
        {
            if (arrayType == null)
            {
                return null;
            }

            if (length > ArrayMaxLength)
            {
                return Array.CreateInstance(arrayType, length);
            }

            PoolInstance instance;
            lock (_elementPool)
            {
                if (!_elementPool.TryGetValue(arrayType, out instance))
                {
                    instance = new PoolInstance(arrayType);
                    _elementPool[arrayType] = instance;
                }
            }
            var i = instance.Get(length);
            return i;
        }

        public object Get() => Get(typeof(object), 0);

        public void OnPluginUnloaded(Plugin plugin)
        {
            List<Type> toRemove = Pool.List<Type>();
            lock (_elementPool)
            {
                foreach (var itemPair in _elementPool)
                {
                    if (itemPair.Key.IsRelatedTo(plugin))
                    {
                        toRemove.Add(itemPair.Key);
                    }
                }

                foreach (Type item in toRemove)
                {
                    _elementPool.Remove(item);
                }
            }

            Pool.Free(ref toRemove);
        }
    }
}
