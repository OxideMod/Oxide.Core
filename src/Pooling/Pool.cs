using Oxide.Core.Plugins;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Oxide.Core.Pooling
{
    public class Pool
    {
        private static readonly Type _listType = typeof(List<>);
        private static readonly Type _arrayType = typeof(Array);

        private static IArrayPoolProvider _arrayPool;
        private static IListPoolProvider _listPool;
        private static readonly Dictionary<Type, IPoolProvider> _itemPool = new Dictionary<Type, IPoolProvider>();

        #region Registration

        /// <summary>
        /// Register a new provider with the pool
        /// </summary>
        /// <param name="poolType">The type this provider will pool for</param>
        /// <param name="provider">The provider instance</param>
        /// <returns>False if a provider already exists</returns>
        public static bool RegisterProvider(Type poolType, IPoolProvider provider)
        {
            if (poolType == null || provider == null)
            {
                throw new ArgumentNullException();
            }

            if (poolType.IsArray)
            {
                if (provider is IArrayPoolProvider array)
                {
                    lock (_arrayType)
                    {
                        if (_arrayPool == null)
                        {
                            _arrayPool = array;
                            return true;
                        }
                    }
                }
                return false;
            }

            if (poolType.IsGenericType && poolType.GetGenericTypeDefinition() == _listType && provider is IListPoolProvider lists)
            {
                lock (_listType)
                {
                    if (_listPool == null)
                    {
                        _listPool = lists;
                        return true;
                    }
                }
                return false;
            }

            lock (_itemPool)
            {
                if (_itemPool.ContainsKey(poolType))
                {
                    return false;
                }

                _itemPool.Add(poolType, provider);
            }
            return true;
        }

        /// <summary>
        /// Unregister a provider with the pool
        /// </summary>
        /// <param name="poolType"></param>
        /// <returns>False if a provider was not found</returns>
        public static bool UnregisterProvider(Type poolType)
        {
            if (poolType == null)
            {
                throw new ArgumentNullException();
            }

            lock (_itemPool)
            {
                return _itemPool.Remove(poolType);
            }
        }

        internal static void RegisterDefaultPools()
        {
            lock (_arrayType)
            {
                if (_arrayPool == null)
                {
                    _arrayPool = new Defaults.DefaultArrayPool();
                }
            }

            lock (_listType)
            {
                if (_listPool == null)
                {
                    _listPool = new Defaults.DefaultListPool();
                }
            }
        }

        internal static void OnPluginUnload(Plugin plugin)
        {
            lock (_listType)
            {
                _listPool?.OnPluginUnloaded(plugin);
            }

            lock (_arrayType)
            {
                _arrayPool?.OnPluginUnloaded(plugin);
            }

            List<Type> toRemove = List<Type>();
            lock (_itemPool)
            {
                foreach (var itemPair in _itemPool)
                {
                    try
                    {
                        itemPair.Value.OnPluginUnloaded(plugin);
                    }
                    catch
                    {
                    }

                    if (itemPair.Key.IsRelatedTo(plugin) || itemPair.Value.IsRelatedTo(plugin))
                    {
                        toRemove.Add(itemPair.Key);
                    }
                }

                foreach (Type key in toRemove)
                {
                    _itemPool.Remove(key);
                }
            }

            Free(ref toRemove);
        }

        #endregion

        #region Pooling

        /// <summary>
        /// Retrieves a item from the pool
        /// </summary>
        /// <param name="objType"></param>
        /// <returns></returns>
        public static object Get(Type objType)
        {
            if (objType == null)
            {
                return null;
            }

            if (objType.IsArray)
            {
                return Array(objType.GetElementType(), 0);
            }

            if (objType.IsGenericType && objType.GetGenericTypeDefinition() == _listType)
            {
                return List(objType.GetGenericTypeDefinition());
            }

            lock (_itemPool)
            {
                if (!_itemPool.TryGetValue(objType, out IPoolProvider provider))
                {
                    return null;
                }

                return provider.Get();
            }
        }

        /// <summary>
        /// Retrieves an Array from the pool
        /// </summary>
        /// <param name="arrayType"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static Array Array(Type arrayType, int length = 0)
        {
            if (arrayType == null)
            {
                return null;
            }

            IArrayPoolProvider provider;

            lock (_arrayType)
            {
                provider = _arrayPool;
            }

            if (provider == null)
            {
                return System.Array.CreateInstance(arrayType, length);
            }

            return provider.Get(arrayType, length);
        }

        /// <summary>
        /// Retrieves an <typeparamref name="T"/> from the pool
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] Array<T>(int length) => (T[])Array(typeof(T), length);

        /// <summary>
        /// Retrieves a List from the pool
        /// </summary>
        /// <param name="listType"></param>
        /// <returns></returns>
        public static IList List(Type listType)
        {
            if (listType == null)
            {
                return null;
            }

            IListPoolProvider provider;

            lock (_listType)
            {
                provider = _listPool;
            }

            if (provider == null)
            {
                return (IList)Activator.CreateInstance(_listType.MakeGenericType(listType));
            }

            return provider.Get(listType);
        }

        /// <summary>
        /// Retrieves a List from the pool
        /// </summary>
        /// <returns></returns>
        public static List<T> List<T>() => (List<T>)List(typeof(T));

        /// <summary>
        /// Returns an object back to its provider and resets it back to a default state
        /// </summary>
        /// <param name="obj"></param>
        public static void Free<T>(ref T obj)
        {
            if (obj == null)
            {
                return;
            }
            
            Type type = typeof(T);

            if (type.IsArray)
            {
                lock (_arrayType)
                {
                    _arrayPool?.Free(obj);
                }
            }
            else if (type.IsGenericType && type.GetGenericTypeDefinition() == _listType)
            {
                lock (_listType)
                {
                    _listPool?.Free(obj);
                }
            }
            else
            {
                lock (_itemPool)
                {
                    if (_itemPool.TryGetValue(type, out IPoolProvider provider))
                    {
                        provider.Free(obj);
                    }
                }
            }

            obj = default;
        }

        #endregion
    }
}
