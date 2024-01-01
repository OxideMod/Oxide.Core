using System;
using Oxide.Core;
using Oxide.Data.StorageDrivers;

namespace Oxide.Configuration
{
    internal class ConfigurationManager : IConfigurationManager
    {
        public IStorageDriver Driver { get; }

        public ConfigurationManager(IStorageDriver driver)
        {
            Driver = driver ?? FileDriver.CreateDefault(Interface.Oxide.ConfigDirectory);
        }

        public T ReadConfig<T>(string name = null, IStorageDriver driver = null)
        {
            Parse<T>(out Type context, ref name, ref driver);

            try
            {
                return (T)driver.Read(name, context);
            }
            catch (NullReferenceException e)
            {
                Interface.Oxide.LogDebug($"Unable to locate configuration file with key: {name} | {e.Message}");
            }

            return default;
        }

        public void WriteConfig<T>(T config, string name = null, IStorageDriver driver = null)
        {
            Parse<T>(out Type context, ref name, ref driver);
            driver.Write(name, config);
        }

        private void Parse<T>(out Type context, ref string name, ref IStorageDriver driver)
        {
            context = typeof(T);

            if (string.IsNullOrEmpty(name))
            {
                name = context.Name;
            }

            if (driver == null)
            {
                driver = Driver;
            }
        }
    }
}
