extern alias References;

using References::Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;

namespace Oxide.Core.Configuration
{
    /// <summary>
    /// Represents a config file
    /// </summary>
    public abstract class ConfigFile
    {
        private static JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            DefaultValueHandling = DefaultValueHandling.Populate, Culture = CultureInfo.InvariantCulture,
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        [JsonIgnore]
        public string Filename { get; private set; }

        protected ConfigFile(string filename)
        {
            Filename = filename;
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
            string source = File.ReadAllText(filename ?? Filename);
            JsonConvert.PopulateObject(source, this, SerializerSettings);
        }

        /// <summary>
        /// Saves this config to the specified file
        /// </summary>
        /// <param name="filename"></param>
        public virtual void Save(string filename = null)
        {
            string source = JsonConvert.SerializeObject(this, SerializerSettings);
            File.WriteAllText(filename ?? Filename, source);
        }
    }
}
