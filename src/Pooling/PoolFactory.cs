using System;
using System.Collections.Generic;

namespace Oxide.Pooling
{
    public static class PoolFactory
    {
        private static Dictionary<Type, IPoolSource> sources { get; } = new Dictionary<Type, IPoolSource>();

        public static IPoolSource<T> GetSource<T>(bool create = true) where T : class
        {
            Type generic = typeof(T);

            lock (sources)
            {
                if (!sources.TryGetValue(generic, out IPoolSource source) && create)
                {
                    source = CreateSource<T>();
                    sources[generic] = source;
                }

                return source as IPoolSource<T>;
            }
        }

        public static IPoolSource<T> CreateSource<T>() where T : class
        {
            Type type = typeof(T);

            if (type.IsArray)
            {
                Type poolType = typeof(ArrayPool<>).MakeGenericType(type.GetElementType());
                return (IPoolSource<T>)Activator.CreateInstance(poolType);
            }

            return new ObjectPool<T>();
        }

        /// <summary>
        /// Retreives a pooled item from the <see cref="IPoolSource{T}"/>
        /// </summary>
        /// <typeparam name="T">The object type</typeparam>
        /// <remarks>
        /// If the <see cref="IPoolSource"/> doesn't exist it this method will create a new instance of the requested object until a <see cref="IPoolSource"/> is created
        /// </remarks>
        /// <returns>The pooled object</returns>
        public static T Get<T>() where T : class
        {
            IPoolSource<T> source = GetSource<T>(false);

            if (source != null)
            {
                return source.Get();
            }

            return (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// Retreives a pooled array from the <see cref="IArrayPoolSource{T}"/>
        /// </summary>
        /// <typeparam name="T">The array element type</typeparam>
        /// <param name="index">The length of the array needed</param>
        /// <returns>The pooled array</returns>
        public static T[] GetArray<T>(int index)
        {
            IArrayPoolSource<T> source = (IArrayPoolSource<T>)GetSource<T[]>();
            return source.Get(index);
        }

        /// <summary>
        /// Retreives a pooled List from the <see cref="IPoolSource{T}"/>
        /// </summary>
        /// <typeparam name="T">The list element type</typeparam>
        /// <returns>The pooled list</returns>
        public static List<T> GetList<T>()
        {
            IPoolSource<List<T>> source = GetSource<List<T>>();
            return source.Get();
        }

        /// <summary>
        /// Returns a item back to a pool source
        /// </summary>
        /// <typeparam name="T">The object Type being returned</typeparam>
        /// <param name="item">The item instance</param>
        /// <remarks>
        /// If a <see cref="IPoolSource"/> is not found the item is ignored
        /// </remarks>
        public static void Free<T>(T item)
        {
            if (item == null)
            {
                return;
            }

            if (item is IPoolObject obj && obj.Source != null)
            {
                obj.Source.Free(item);
                return;
            }

            Type type = item.GetType();

            lock (sources)
            {
                if (sources.TryGetValue(type, out IPoolSource source))
                {
                    source.Free(item);
                }
            }
        }
    }
}
