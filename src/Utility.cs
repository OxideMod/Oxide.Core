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

namespace Oxide.Core
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

    public class Utility
    {
        public static void DatafileToProto<T>(string name, bool deleteAfter = true)
        {
            DataFileSystem dfs = Interface.Oxide.DataFileSystem;
            if (!dfs.ExistsDatafile(name))
            {
                return;
            }

            if (ProtoStorage.Exists(name))
            {
                Interface.Oxide.LogWarning("Failed to import JSON file: {0} already exists.", name);
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
                if (value[i] != '.')
                {
                    continue;
                }

                lastIndex = i - 1;
                break;
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
            return JsonConvert.SerializeObject(obj, (indented) ? Formatting.Indented : Formatting.None);
        }

        public static IPAddress GetLocalIP()
        {
            UnicastIPAddressInformation mostSuitableIp = null;
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface network in networkInterfaces)
            {
                if (network.OperationalStatus == OperationalStatus.Up)
                {
                    IPInterfaceProperties properties = network.GetIPProperties();

                    if (properties.GatewayAddresses.Count == 0)
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation address in properties.UnicastAddresses)
                    {
                        if (address.Address.AddressFamily != AddressFamily.InterNetwork || IPAddress.IsLoopback(address.Address))
                        {
                            continue;
                        }

                        if (!address.IsDnsEligible)
                        {
                            if (mostSuitableIp == null)
                            {
                                mostSuitableIp = address;
                            }

                            continue;
                        }

                        if (address.PrefixOrigin != PrefixOrigin.Dhcp)
                        {
                            if (mostSuitableIp == null || !mostSuitableIp.IsDnsEligible)
                            {
                                mostSuitableIp = address;
                            }

                            continue;
                        }

                        return address.Address;
                    }
                }
            }

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
    }
}
