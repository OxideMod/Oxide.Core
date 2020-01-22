﻿extern alias References;

using Oxide.Core.Configuration;
using References::Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Oxide.Core
{
    /// <summary>
    /// Manages all data files
    /// </summary>
    public class DataFileSystem
    {
        /// <summary>
        /// Gets the directory that this system works in
        /// </summary>
        public string Directory { get; private set; }

        // All currently loaded datafiles
        private readonly Dictionary<string, DynamicConfigFile> _datafiles;

        /// <summary>
        /// Initializes a new instance of the DataFileSystem class
        /// </summary>
        /// <param name="directory"></param>
        public DataFileSystem(string directory)
        {
            Directory = directory;
            _datafiles = new Dictionary<string, DynamicConfigFile>();
            KeyValuesConverter converter = new KeyValuesConverter();
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.Converters.Add(converter);
        }

        /// <summary>
        /// Read file as string from Data directory
        /// </summary>
        /// <param name="fileName">Name of file (eg. MyData.json)</param>
        /// <returns>Content string or null if file wasn't found</returns>
        public string GetFileString(string fileName)
        {
            fileName = DynamicConfigFile.SanitizeName(fileName);
            var filePath = Path.Combine(Directory, fileName);

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File '{filePath}' not found");

            var result = File.ReadAllText(filePath);
            return result;
        }

        public DynamicConfigFile GetFile(string name)
        {
            name = DynamicConfigFile.SanitizeName(name);
            DynamicConfigFile datafile;
            if (_datafiles.TryGetValue(name, out datafile))
            {
                return datafile;
            }

            datafile = new DynamicConfigFile(Path.Combine(Directory, $"{name}.json"));
            _datafiles.Add(name, datafile);
            return datafile;
        }

        /// <summary>
        /// Check if datafile exists
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool ExistsDatafile(string name) => GetFile(name).Exists();

        /// <summary>
        /// Gets a datafile
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public DynamicConfigFile GetDatafile(string name)
        {
            DynamicConfigFile datafile = GetFile(name);

            // Does it exist?
            if (datafile.Exists())
            {
                // Load it
                datafile.Load();
            }
            else
            {
                // Just make a new one
                datafile.Save();
            }

            return datafile;
        }

        /// <summary>
        /// Gets data files from path, with optional search pattern
        /// </summary>
        /// <param name="path"></param>
        /// <param name="searchPattern"></param>
        /// <returns></returns>
        public string[] GetFiles(string path = "", string searchPattern = "*")
        {
            return System.IO.Directory.GetFiles(Path.Combine(Directory, path), searchPattern);
        }

        /// <summary>
        /// Saves the specified datafile
        /// </summary>
        /// <param name="name"></param>
        public void SaveDatafile(string name) => GetFile(name).Save();

        public T ReadObject<T>(string name)
        {
            if (!ExistsDatafile(name))
            {
                T instance = Activator.CreateInstance<T>();
                WriteObject(name, instance);
                return instance;
            }

            return GetFile(name).ReadObject<T>();
        }

        public void WriteObject<T>(string name, T Object, bool sync = false) => GetFile(name).WriteObject(Object, sync);

        /// <summary>
        /// Read data files in a batch and send callback
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="callback"></param>
        public void ForEachObject<T>(string name, Action<T> callback)
        {
            string folder = DynamicConfigFile.SanitizeName(name);
            IEnumerable<DynamicConfigFile> files = _datafiles.Where(d => d.Key.StartsWith(folder)).Select(a => a.Value);
            foreach (DynamicConfigFile file in files)
            {
                callback?.Invoke(file.ReadObject<T>());
            }
        }
    }
}
