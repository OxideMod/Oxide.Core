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
using System.Security.Cryptography;
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
                if (TryGetValue(key, out TValue value))
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
        public static Utilities.Plugins Plugins = new Utilities.Plugins();
        public static Utilities.Random Random = new Utilities.Random();
        public static Utilities.Time Time = new Utilities.Time();

        /// <summary>
        /// Converts a data file to Protobuf format
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="deleteAfter"></param>
        public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        {
            DataFileSystem dfs = Interface.uMod.DataFileSystem;
            if (dfs.ExistsDatafile(name))
            {
                if (ProtoStorage.Exists(name))
                {
                    Interface.uMod.LogWarning($"Failed to import JSON file: {name} already exists");
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
                    Interface.uMod.LogException($"Failed to convert datafile to proto storage: {name}", ex);
                }
            }
        }

        /// <summary>
        /// Print the call stack to the log file
        /// </summary>
        public static void PrintCallStack() => Interface.uMod.LogDebug("CallStack: {0}{1}", Environment.NewLine, new StackTrace(1, true));

        /// <summary>
        /// Returns the formatted bytes from a double
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
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
        /// Gets the path only for a directory
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

        /// <summary>
        /// Gets the filename of a file without the extension
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
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
            return value.Substring(firstIndex, lastIndex - firstIndex + 1);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string CleanPath(string path)
        {
            return path?.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Converts a string of JSON to a JSON object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="jsonstr"></param>
        /// <returns></returns>
        public static T ConvertFromJson<T>(string jsonstr) => JsonConvert.DeserializeObject<T>(jsonstr);

        /// <summary>
        /// Converts a JSON object to a string of JSON
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="indented"></param>
        /// <returns></returns>
        public static string ConvertToJson(object obj, bool indented = false)
        {
            return JsonConvert.SerializeObject(obj, indented ? Formatting.Indented : Formatting.None);
        }

        /// <summary>
        /// Gets the local network IP of the machine
        /// </summary>
        /// <returns></returns>
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

        /// <summary>
        /// Returns if the provided IP address is a local network IP
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool IsLocalIP(string ipAddress)
        {
            string[] split = ipAddress.Split(new[] { "." }, StringSplitOptions.RemoveEmptyEntries);
            int[] ip = { int.Parse(split[0]), int.Parse(split[1]), int.Parse(split[2]), int.Parse(split[3]) };
            return ip[0] == 0 || ip[0] == 10 || ip[0] == 127 || ip[0] == 192 && ip[1] == 168 || ip[0] == 172 && ip[1] >= 16 && ip[1] <= 31;
        }

        /// <summary>
        /// Returns if the provided IP address is a valid IPv4 address
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <returns></returns>
        public static bool ValidateIPv4(string ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress.Trim()))
            {
                string[] splitValues = ipAddress.Replace("\"", string.Empty).Trim().Split('.');
                return splitValues.Length == 4 && splitValues.All(r => byte.TryParse(r, out _));
            }

            return false;
        }

        /// <summary>
        /// Gets only the numbers from a string
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static int GetNumbers(string input)
        {
            int.TryParse(Regex.Replace(input, "[^.0-9]", ""), out int numbers);
            return numbers;
        }

        /// <summary>
        /// Gets file checksum
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        internal static string GetHash(string filePath, HashAlgorithm algorithm)
        {
            using (BufferedStream stream = new BufferedStream(File.OpenRead(filePath), 100000))
            {
                byte[] hash = algorithm.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();
            }
        }

        /// <summary>
        /// Attempt to upgrade a file
        /// </summary>
        /// <param name="originalPath"></param>
        /// <param name="newPath"></param>
        /// <returns></returns>
        internal static bool TryUpgrade(string originalPath, string newPath)
        {
            if (!File.Exists(originalPath) || File.Exists(newPath)) // file upgraded or can't be upgraded
            {
                return true;
            }

            try
            {
                File.Move(originalPath, newPath);
                return true;
            }
            catch (Exception)
            {
                // Ignore
            }

            return false;
        }
    }
}
