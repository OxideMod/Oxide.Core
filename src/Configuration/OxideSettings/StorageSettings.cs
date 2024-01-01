using System.IO;
using System.Runtime.Serialization;

namespace Oxide.Configuration.Settings
{
    public sealed class StorageSettings
    {
        /// <summary>
        /// Path containing server files
        /// </summary>
        [IgnoreDataMember]
        public string RootDirectory { get; }

        /// <summary>
        /// Path where extensions will be loaded from
        /// </summary>
        [IgnoreDataMember]
        public string ExtensionDirectory { get; }

        /// <summary>
        /// Path that hold all oxides sub folders
        /// </summary>
        [IgnoreDataMember]
        public string OxideDirectory { get; }

        /// <summary>
        /// Path to all configuration files
        /// </summary>
        [IgnoreDataMember]
        public string ConfigurationDirectory { get; }

        /// <summary>
        /// Path that holds all data files
        /// </summary>
        [IgnoreDataMember]
        public string StorageDirectory { get; }

        /// <summary>
        /// Path that log files are created at
        /// </summary>
        [IgnoreDataMember]
        public string LoggingDirectory { get; }

        /// <summary>
        /// Path containing translation files
        /// </summary>
        [IgnoreDataMember]
        public string LocalizationDirectory { get; }

        /// <summary>
        /// Path that contains plugins
        /// </summary>
        [IgnoreDataMember]
        public string PluginDirectory { get; }

        public StorageSettings(string rootDirectory, string extensionDirectory)
        {
            RootDirectory = rootDirectory;
            ExtensionDirectory = extensionDirectory;
            OxideDirectory = Path.Combine(rootDirectory, "oxide");
            ConfigurationDirectory = Path.Combine(OxideDirectory, "config");
            StorageDirectory = Path.Combine(OxideDirectory, "data");
            LoggingDirectory = Path.Combine(OxideDirectory, "logs");
            LocalizationDirectory = Path.Combine(OxideDirectory, "lang");
            PluginDirectory = Path.Combine(OxideDirectory, "plugins");
        }

        public void Initialize()
        {
            EnsureCreated(OxideDirectory);
            EnsureCreated(ConfigurationDirectory);
            EnsureCreated(StorageDirectory);
            EnsureCreated(LoggingDirectory);
            EnsureCreated(LocalizationDirectory);
            EnsureCreated(PluginDirectory);
        }

        private void EnsureCreated(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}
