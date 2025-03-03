using System.IO;
using Xunit;
using Oxide.Core.Configuration;
using Oxide.Core;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for DynamicConfigFile functionality, ensuring that default configurations are created and loaded correctly.
    /// </summary>
    public class ConfigTests
    {
        /// <summary>
        /// Initializes a new instance of the ConfigTests class and ensures the Oxide framework is initialized.
        /// </summary>
        public ConfigTests()
        {
            // Ensure Oxide is initialized so that InstanceDirectory is set.
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        /// <summary>
        /// Tests that a DynamicConfigFile creates a default config when the file is missing.
        /// </summary>
        [Fact]
        public void DynamicConfigFile_CreatesDefault_WhenFileMissing()
        {
            // Use the allowed directory from the Oxide instance. If it's null, fallback to a temporary directory.
            string instanceDir = Interface.Oxide.InstanceDirectory;
            if (string.IsNullOrEmpty(instanceDir))
            {
                instanceDir = Path.GetTempPath();
            }
            string configPath = Path.Combine(instanceDir, "DynamicConfigTest.json");

            // Ensure a clean state.
            if (File.Exists(configPath))
                File.Delete(configPath);

            var config = new DynamicConfigFile(configPath);

            // Act Read an object; this should create a new default config and save it.
            var obj = config.ReadObject<TestConfig>();

            // Assert Verify that the file now exists and default values are set.
            Assert.True(File.Exists(configPath));
            Assert.Equal("Default", obj.Value);

            // Clean up Remove the test config file.
            File.Delete(configPath);
        }

        private class TestConfig
        {
            public string Value { get; set; } = "Default";
        }
    }
}
