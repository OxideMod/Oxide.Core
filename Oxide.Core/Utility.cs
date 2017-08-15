using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Oxide.Core
{
    /// <summary>
    /// A partially thread-safe HashSet (iterating is not thread-safe)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentHashSet<T> : ICollection<T>
    {
        readonly HashSet<T> collection;
        readonly object syncRoot = new object();

        public ConcurrentHashSet()
        {
            collection = new HashSet<T>();
        }

        public ConcurrentHashSet(ICollection<T> values)
        {
            collection = new HashSet<T>(values);
        }

        public bool IsReadOnly => false;
        public int Count { get { lock (syncRoot) return collection.Count; } }
        public bool Contains(T value) { lock (syncRoot) return collection.Contains(value); }
        public bool Add(T value) { lock (syncRoot) return collection.Add(value); }
        public bool Remove(T value) { lock (syncRoot) return collection.Remove(value); }
        public void Clear() { lock (syncRoot) collection.Clear(); }
        public void CopyTo(T[] array, int index) { lock (syncRoot) collection.CopyTo(array, index); }
        public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
        public bool Any(Func<T, bool> callback) { lock (syncRoot) return collection.Any(callback); }
        public T[] ToArray() { lock (syncRoot) return collection.ToArray(); }

        public bool TryDequeue(out T value)
        {
            lock (syncRoot)
            {
                value = collection.ElementAtOrDefault(0);
                if (value != null) collection.Remove(value);
                return value != null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<T>.Add(T value) => Add(value);
    }

    public class Utility
    {
        public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        {
            var dfs = Interface.Oxide.DataFileSystem;
            if (!dfs.ExistsDatafile(name)) return;
            if (ProtoStorage.Exists(name))
            {
                Interface.Oxide.LogWarning("Failed to import JSON file: {0} already exists.", name);
                return;
            }
            try
            {
                var data = dfs.ReadObject<T>(name);
                ProtoStorage.Save(data, name);
                if (deleteAfter) File.Delete(dfs.GetFile(name).Filename);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Failed to convert datafile to proto storage: " + name, ex);
            }
        }

        public static void PrintCallStack() => Interface.Oxide.LogDebug("CallStack:{0}{1}", Environment.NewLine, new StackTrace(1, true));

        public static string FormatBytes(double bytes)
        {
            string type;
            if (bytes > 1024 * 1024)
            {
                type = "mb";
                bytes /= (1024 * 1024);
            }
            else if (bytes > 1024)
            {
                type = "kb";
                bytes /= 1024;
            }
            else
                type = "b";
            return $"{bytes:0}{type}";
        }

        /// <summary>
        /// Gets the path only
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetDirectoryName(string name)
        {
            try
            {
                name = name.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                return name.Substring(0, name.LastIndexOf(Path.DirectorySeparatorChar));
            }
            catch
            {
                return null;
            }
        }

        public static string GetFileNameWithoutExtension(string value)
        {
            var lastIndex = value.Length - 1;
            for (var i = lastIndex; i >= 1; i--)
            {
                if (value[i] != '.') continue;
                lastIndex = i - 1;
                break;
            }
            var firstIndex = 0;
            for (var i = lastIndex - 1; i >= 0; i--)
            {
                switch (value[i])
                {
                    case '/':
                    case '\\':
                        {
                            firstIndex = i + 1;
                            goto End;
                        }
                }
            }
            End:
            return value.Substring(firstIndex, (lastIndex - firstIndex + 1));
        }

        public static string CleanPath(string path)
        {
            return path?.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }
    }

    /// <summary>
    /// A dictionary which returns null for non-existant keys and removes keys when setting an index to null.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Hash<TKey, TValue> : IDictionary<TKey, TValue>
    {
        readonly IDictionary<TKey, TValue> dictionary;

        public Hash()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (TryGetValue(key, out value))
                    return value;
                if (typeof(TValue).IsValueType)
                    return (TValue)Activator.CreateInstance(typeof(TValue));
                return default(TValue);
            }

            set
            {
                if (value == null)
                    dictionary.Remove(key);
                else
                    dictionary[key] = value;
            }
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values =>  dictionary.Values;
        public int Count => dictionary.Count;
        public bool IsReadOnly => dictionary.IsReadOnly;

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => dictionary.GetEnumerator();
        public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
        public bool Contains(KeyValuePair<TKey, TValue> item) => dictionary.Contains(item);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int index) => dictionary.CopyTo(array, index);
        public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value);
        public void Add(TKey key, TValue value) => dictionary.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => dictionary.Add(item);
        public bool Remove(TKey key) => dictionary.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => dictionary.Remove(item);
        public void Clear() => dictionary.Clear();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
