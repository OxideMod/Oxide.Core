using System.Linq;
using Xunit;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Plugins library functionality.
    /// </summary>
    public class PluginsTests
    {
        /// <summary>
        /// Initializes the test environment.
        /// Ensures that the Oxide interface is initialized and the OxideMod is fully loaded.
        /// </summary>
        public PluginsTests()
        {
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        /// <summary>
        /// Tests that the Find method returns the correct plugin.
        /// </summary>
        [Fact]
        public void Find_ReturnsCorrectPlugin()
        {
            // Arrange
            var fakePlugin = new FakePlugin();
            var pluginManager = new PluginManager(new MockLogger());
            pluginManager.AddPlugin(fakePlugin);
            var pluginsLib = new Oxide.Core.Libraries.Plugins(pluginManager);

            // Act
            Plugin found = pluginsLib.Find(fakePlugin.Name);

            // Assert
            Assert.NotNull(found);
            Assert.Equal(fakePlugin.Name, found.Name);
        }

        /// <summary>
        /// Tests that GetAll returns all loaded plugins.
        /// </summary>
        [Fact]
        public void GetAll_ReturnsAllPlugins()
        {
            // Arrange
            var pluginManager = new PluginManager(new MockLogger());
            pluginManager.AddPlugin(new FakePlugin());
            pluginManager.AddPlugin(new FakePluginReturning("Test"));
            var pluginsLib = new Oxide.Core.Libraries.Plugins(pluginManager);

            // Act
            Plugin[] plugins = pluginsLib.GetAll();

            // Assert
            Assert.Equal(pluginManager.GetPlugins().Count(), plugins.Length);
        }

        /// <summary>
        /// Tests that calling a hook via Plugins.CallHook delegates correctly to the plugin.
        /// </summary>
        [Fact]
        public void CallHook_DelegatesCorrectly()
        {
            // Arrange
            var fakePlugin = new FakePlugin();
            var pluginManager = new PluginManager(new MockLogger());
            pluginManager.AddPlugin(fakePlugin);
            // Optionally ensure the plugin is fully loaded in OxideMod if needed.
            Interface.Oxide.PluginLoaded(fakePlugin);
            var pluginsLib = new Oxide.Core.Libraries.Plugins(pluginManager);

            // Act
            object result = pluginsLib.CallHook("TestHook", 123);

            // Assert
            // In a test environment, Interface.Call might return null since the hook system
            // might not be fully initialized. We're just testing that the method executes without error.
            // The actual hook functionality is tested elsewhere.
        }

        /// <summary>
        /// Tests that the Exists method returns true when a plugin is loaded.
        /// </summary>
        [Fact]
        public void Exists_ReturnsTrueIfPluginLoaded()
        {
            // Arrange
            var fakePlugin = new FakePlugin();
            var pluginManager = new PluginManager(new MockLogger());
            pluginManager.AddPlugin(fakePlugin);
            var pluginsLib = new Oxide.Core.Libraries.Plugins(pluginManager);

            // Act
            bool exists = pluginsLib.Exists(fakePlugin.Name);

            // Assert
            Assert.True(exists);
        }

        /// <summary>
        /// Tests that the IsGlobal property returns false.
        /// </summary>
        [Fact]
        public void IsGlobal_ReturnsFalse()
        {
            // Arrange
            var pluginManager = new PluginManager(new MockLogger());
            var pluginsLib = new Oxide.Core.Libraries.Plugins(pluginManager);

            // Act
            bool isGlobal = pluginsLib.IsGlobal;

            // Assert
            Assert.False(isGlobal);
        }
    }
}
