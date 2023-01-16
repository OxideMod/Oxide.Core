using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Core.Pooling.Defaults
{
    public class DefaultListPool : IListPoolProvider
    {
        private static int MaxPoolSize = 156;
        private readonly Type _listType = typeof(List<>);
        private readonly Dictionary<Type, Queue<IList>> _listPool = new Dictionary<Type, Queue<IList>>();

        public void Free(object item)
        {
            if (item == null || !(item is IList list))
            {
                return;
            }

            list.Clear();
            Type type = list.GetType();

            if (!type.IsGenericType || type.GetGenericTypeDefinition() != _listType)
            {
                return;
            }

            type = type.GetGenericArguments()[0];
            Queue<IList> queue;
            lock (_listPool)
            {
                if (!_listPool.TryGetValue(type, out queue))
                {
                    return;
                }
            }

            lock (queue)
            {
                if (queue.Count >= MaxPoolSize)
                {
                    return;
                }

                queue.Enqueue(list);
            }
        }

        public IList Get(Type listType)
        {
            if (listType == null)
            {
                return null;
            }

            Queue<IList> queue;
            lock (_listPool)
            {
                if (!_listPool.TryGetValue(listType, out queue))
                {
                    queue = new Queue<IList>();
                    _listPool[listType] = queue;
                }
            }

            lock (queue)
            {
                if (queue.Count == 0)
                {
                    return (IList)Activator.CreateInstance(_listType.MakeGenericType(listType));
                }

                return queue.Dequeue();
            }
        }

        public object Get() => Get(typeof(object));

        public void OnPluginUnloaded(Plugin plugin)
        {
            List<Type> toRemove = (List<Type>)Get(typeof(Type));

            lock (_listPool)
            {
                foreach (var itemPair in _listPool)
                {
                    if (itemPair.Key.IsRelatedTo(plugin))
                    {
                        toRemove.Add(itemPair.Key);
                    }
                }

                foreach (Type item in toRemove)
                {
                    _listPool.Remove(item);
                }
            }

            Free(toRemove);
        }
    }
}
