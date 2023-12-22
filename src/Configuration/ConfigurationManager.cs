using System;
using System.IO;
using Oxide.Data;

namespace Oxide.Configuration
{
    internal class ConfigurationManager : IConfigurationManager
    {
        protected string ConfigDirectory { get; }
        protected IDataReaderWriter ReaderWriter { get; }

        public ConfigurationManager(string configDirectory, IDataReaderWriter rw)
        {
            ConfigDirectory = configDirectory ?? throw new ArgumentNullException(nameof(configDirectory));
            ReaderWriter = rw ?? throw new ArgumentNullException(nameof(rw));
        }

        public T ReadConfig<T>(string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DataHelpers.GetFileNameFromType<T>();
            }
            else
            {
                name = name.Replace("..", string.Empty);
            }

            return ReaderWriter.Read<T>(Path.Combine(ConfigDirectory, name));
        }

        public void WriteConfig<T>(T config, string name = null)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = DataHelpers.GetFileNameFromType<T>();
            }
            else
            {
                name = name.Replace("..", string.Empty);
            }

            ReaderWriter.Write(config, Path.Combine(ConfigDirectory, name));
        }
    }
}
