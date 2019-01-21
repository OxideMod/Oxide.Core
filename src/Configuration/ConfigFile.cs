extern alias References;

using References::Newtonsoft.Json;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace uMod.Configuration
{
    /// <summary>
    /// Represents a config file
    /// </summary>
    public abstract class ConfigFile
    {
        [JsonIgnore]
        public string Filename { get; private set; }

        private readonly string _chroot;

        protected ConfigFile(string filename)
        {
            Filename = filename;
            _chroot = Interface.uMod.InstanceDirectory;
        }

        /// <summary>
        /// Loads a config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public static T Load<T>(string filename) where T : ConfigFile
        {
            T config = (T)Activator.CreateInstance(typeof(T), filename);
            config.Load();
            return config;
        }

        /// <summary>
        /// Loads this config from the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Load(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            string source = File.ReadAllText(filename);
            JsonConvert.PopulateObject(source, this);
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(string filename = null)
        {
            filename = CheckPath(filename ?? Filename);
            if (Interface.uMod.Config.Options.ConfigWatchers)
            {
                Interface.uMod.ConfigChanges.Add(filename);
            }
            string source = JsonConvert.SerializeObject(this, Formatting.Indented);
            File.WriteAllText(filename, source);
        }

        /// <summary>
        /// Check if file path is in chroot directory
        /// </summary>
        /// <param name="filename"></param>
        internal string CheckPath(string filename)
        {
            filename = SanitizeName(filename);
            string path = Path.GetFullPath(filename);
            if (!path.StartsWith(_chroot, StringComparison.Ordinal))
            {
                throw new Exception($"Only access to 'umod' directory!\nPath: {path}");
            }

            return path;
        }

        /// <summary>
        /// Makes the specified name safe for use in a filename
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string SanitizeName(string name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                name = name.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);
                name = Regex.Replace(name, "[" + Regex.Escape(new string(Path.GetInvalidPathChars())) + "]", "_");
                name = Regex.Replace(name, @"\.+", ".");
                return name.TrimStart('.');
            }

            return string.Empty;
        }
    }
}
