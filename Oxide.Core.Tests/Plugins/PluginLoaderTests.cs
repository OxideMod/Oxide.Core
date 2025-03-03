using System.IO;
using System.Linq;
using Xunit;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Contains tests for the plugin loader functionality, verifying that plugins can be scanned from directories
    /// and loaded correctly.
    /// </summary>
    [Collection("Oxide Sequential Tests")]
    public class PluginLoaderTests
    {
        /// <summary>
        /// Initializes a new instance of the PluginLoaderTests class.
        /// This constructor ensures that the Oxide framework is initialized before tests run.
        /// </summary>
        public PluginLoaderTests()
        {
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        /// <summary>
        /// Tests that the ScanDirectory method of the plugin loader returns the correct plugin names 
        /// from a directory containing dummy plugin files.
        /// </summary>
        [Fact]
        public void ScanDirectory_ReturnsPluginNames()
        {
            // Arrange: Create an instance of the test plugin loader and a temporary test directory.
            var loader = new MyTestPluginLoader();
            var testDir = Path.Combine(Path.GetTempPath(), "TestPlugins");
            Directory.CreateDirectory(testDir);

            // Create two dummy plugin files.
            File.WriteAllText(Path.Combine(testDir, "TestA.cs"), "// plugin A");
            File.WriteAllText(Path.Combine(testDir, "TestB.cs"), "// plugin B");

            // Act: Scan the directory for plugin files.
            var names = loader.ScanDirectory(testDir).ToList();

            // Assert: Verify that both plugin file names are returned.
            Assert.Contains("TestA", names);
            Assert.Contains("TestB", names);
        }

        /// <summary>
        /// Tests that the Load method of the plugin loader successfully loads a plugin and sets its name correctly.
        /// </summary>
        [Fact]
        public void LoadPlugin_ReturnsNonNullPlugin()
        {
            // Arrange: Create an instance of the test plugin loader and specify a temporary directory and a plugin name.
            var loader = new MyTestPluginLoader();
            var testDir = Path.Combine(Path.GetTempPath());
            var pluginName = "MyPlugin";

            // Act: Load the plugin using the test loader.
            var plugin = loader.Load(testDir, pluginName);

            // Assert: Verify that the loaded plugin is not null and its name matches the expected plugin name.
            Assert.NotNull(plugin);
            Assert.Equal(pluginName, plugin.Name);
        }
    }
}
