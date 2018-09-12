extern alias References;

using References::Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

#if DEBUG
using System.Text;
#endif

namespace uMod
{
    /// <summary>
    /// A partially thread-safe HashSet (iterating is not thread-safe)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ConcurrentHashSet<T> : ICollection<T>
    {
        private readonly HashSet<T> collection;
        private readonly object syncRoot = new object();

        public ConcurrentHashSet()
        {
            collection = new HashSet<T>();
        }

        public ConcurrentHashSet(ICollection<T> values)
        {
            collection = new HashSet<T>(values);
        }

        public bool IsReadOnly => false;
        public int Count { get { lock (syncRoot) { return collection.Count; } } }
        public bool Contains(T value)
        {
            lock (syncRoot)
            {
                return collection.Contains(value);
            }
        }
        public bool Add(T value)
        {
            lock (syncRoot)
            {
                return collection.Add(value);
            }
        }
        public bool Remove(T value)
        {
            lock (syncRoot)
            {
                return collection.Remove(value);
            }
        }
        public void Clear()
        {
            lock (syncRoot)
            {
                collection.Clear();
            }
        }
        public void CopyTo(T[] array, int index)
        {
            lock (syncRoot)
            {
                collection.CopyTo(array, index);
            }
        }
        public IEnumerator<T> GetEnumerator() => collection.GetEnumerator();
        public bool Any(Func<T, bool> callback)
        {
            lock (syncRoot)
            {
                return collection.Any(callback);
            }
        }
        public T[] ToArray()
        {
            lock (syncRoot)
            {
                return collection.ToArray();
            }
        }

        public bool TryDequeue(out T value)
        {
            lock (syncRoot)
            {
                value = collection.ElementAtOrDefault(0);
                if (value != null)
                {
                    collection.Remove(value);
                }

                return value != null;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void ICollection<T>.Add(T value) => Add(value);
    }

    /// <summary>
    /// A dictionary which returns null for non-existent keys and removes keys when setting an index to null.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class Hash<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private readonly IDictionary<TKey, TValue> dictionary;

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
                {
                    return value;
                }

                if (typeof(TValue).IsValueType)
                {
                    return (TValue)Activator.CreateInstance(typeof(TValue));
                }

                return default(TValue);
            }

            set
            {
                if (value == null)
                {
                    dictionary.Remove(key);
                }
                else
                {
                    dictionary[key] = value;
                }
            }
        }

        public ICollection<TKey> Keys => dictionary.Keys;
        public ICollection<TValue> Values => dictionary.Values;
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

    public class Utility
    {
        public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        {
            DataFileSystem dfs = Interface.uMod.DataFileSystem;
            if (dfs.ExistsDatafile(name))
            {
                if (ProtoStorage.Exists(name))
                {
                    Interface.uMod.LogWarning("Failed to import JSON file: {0} already exists.", name);
                    return;
                }

                try
                {
                    T data = dfs.ReadObject<T>(name);
                    ProtoStorage.Save(data, name);
                    if (deleteAfter)
                    {
                        File.Delete(dfs.GetFile(name).Filename);
                    }
                }
                catch (Exception ex)
                {
                    Interface.uMod.LogException("Failed to convert datafile to proto storage: " + name, ex);
                }
            }
        }

        public static void PrintCallStack() => Interface.uMod.LogDebug("CallStack:{0}{1}", Environment.NewLine, new StackTrace(1, true));

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
            {
                type = "b";
            }

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
            int lastIndex = value.Length - 1;
            for (int i = lastIndex; i >= 1; i--)
            {
                if (value[i] == '.')
                {
                    lastIndex = i - 1;
                    break;
                }
            }
            int firstIndex = 0;
            for (int i = lastIndex - 1; i >= 0; i--)
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

        public static T ConvertFromJson<T>(string jsonstr) => JsonConvert.DeserializeObject<T>(jsonstr);

        public static string ConvertToJson(object obj, bool indented = false)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        }

        public static IPAddress GetLocalIP()
        {
            UnicastIPAddressInformation mostSuitableIp = null;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
#if DEBUG
                StringBuilder debugOutput = new StringBuilder();
                debugOutput.AppendLine(string.Empty);
#endif

                if (network.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = network.GetIPProperties();

                    if (properties.GatewayAddresses.Count == 0 || properties.GatewayAddresses[0].Address.Equals(IPAddress.Parse("0.0.0.0")))
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation ip in properties.UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(ip.Address))
                        {
                            continue;
                        }

#if DEBUG
                        debugOutput.AppendLine($"IP address: {ip.Address}");
                        debugOutput.AppendLine($"Is DNS eligible: {ip.IsDnsEligible}");
                        debugOutput.AppendLine($"Is lookback: {IPAddress.IsLoopback(ip.Address)}");
                        debugOutput.AppendLine($"Is using DHCP: {ip.PrefixOrigin == PrefixOrigin.Dhcp}");
                        debugOutput.AppendLine($"Address family: {ip.Address.AddressFamily}");
                        debugOutput.AppendLine($"Gateway address: {properties.GatewayAddresses[0].Address}");
#endif

                        if (!ip.IsDnsEligible)
                        {
                            if (mostSuitableIp == null)
                            {
                                mostSuitableIp = ip;
                            }

                            continue;
                        }

                        if (ip.PrefixOrigin != PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            {
                                mostSuitableIp = ip;
                            }

                            continue;
                        }

#if DEBUG
                        debugOutput.AppendLine($"Resulting IP address: {ip.Address}");
                        Interface.uMod.LogDebug(debugOutput.ToString());
#endif

                        return ip.Address;
                    }
                }
            }

#if DEBUG
            Interface.uMod.LogDebug($"Most suitable IP: {mostSuitableIp?.Address}");
#endif

            return mostSuitableIp?.Address;
        }

        public static bool IsLocalIP(string ipAddress)
        {
            string[] split = ipAddress.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            int[] ip = { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) };
            return ip[0] == 0 || ip[0] == 10 || ip[0] == 127 || ip[0] == 192 && ip[1] == 168 || ip[0] == 172 && ip[1] >= 16 && ip[1] <= 31;
        }

        public static bool ValidateIPv4(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress.Trim()))
            {
                string[] splitValues = ipAddress.Replace("\"", string.Empty).Trim().Split('.');
                return splitValues.Length == 4 && splitValues.All(r => byte.TryParse(r, out _));
            }

            return false;
        }

        public static int GetNumbers(string input)
        {
            int numbers;
            int.TryParse(Regex.Replace(input, "[^.0-9]", ""), out numbers);
            return numbers;
        }
    }
}
