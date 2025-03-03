using System.Linq;
using Xunit;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Contains tests for the PluginManager class, verifying that plugins are registered,
    /// unregistered, and that hook subscriptions and conflict logging behave as expected.
    /// </summary>
    [Collection("Oxide Sequential Tests")]
    public class PluginManagerTests
    {
        /// <summary>
        /// Initializes a new instance of the PluginManagerTests class.
        /// This constructor ensures that the Oxide framework is initialized before any tests run.
        /// </summary>
        public PluginManagerTests()
        {
            // Ensure the Oxide framework is initialized.
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        /// <summary>
        /// Verifies that adding a plugin registers it in the PluginManager.
        /// </summary>
        [Fact]
        public void AddPlugin_RegistersPlugin()
        {
            // Arrange: Create a PluginManager and a fake plugin.
            var manager = new PluginManager(null);
            var plugin = new FakePlugin();

            // Act: Add the plugin.
            bool added = manager.AddPlugin(plugin);

            // Assert: Plugin should be registered and retrievable by its name.
            Assert.True(added);
            Assert.Equal(plugin, manager.GetPlugin(plugin.Name));
        }

        /// <summary>
        /// Verifies that removing a plugin unsubscribes it from hooks and removes it from the manager.
        /// </summary>
        [Fact]
        public void RemovePlugin_UnsubscribesAndRemovesPlugin()
        {
            // Arrange: Create a PluginManager and add a fake plugin.
            var manager = new PluginManager(null);
            var plugin = new FakePlugin();
            manager.AddPlugin(plugin);

            // Act: Remove the plugin.
            bool removed = manager.RemovePlugin(plugin);

            // Assert: The plugin should be removed (GetPlugin returns null).
            Assert.True(removed);
            Assert.Null(manager.GetPlugin(plugin.Name));
        }

        /// <summary>
        /// Verifies that only subscribed plugins receive hook calls.
        /// </summary>
        [Fact]
        public void CallHook_OnlySubscribedPluginReceivesHook()
        {
            // Arrange: Create a PluginManager and two fake plugins.
            var manager = new PluginManager(null);
            var subPlugin = new FakePlugin();
            var unsubPlugin = new FakePlugin();

            // Add both plugins to the manager but subscribe only subPlugin to the hook "TestHook".
            manager.AddPlugin(subPlugin);
            manager.AddPlugin(unsubPlugin);
            manager.SubscribeToHook("TestHook", subPlugin);

            // Act: Call the hook with one argument.
            var result = manager.CallHook("TestHook", "arg1");

            // Assert: Only the subscribed plugin should respond.
            Assert.Equal("Hook: TestHook, Args: 1", result);
        }

        /// <summary>
        /// Verifies that when multiple plugins return different values for the same hook,
        /// a conflict warning is logged and the last non-null value is returned.
        /// </summary>
        [Fact]
        public void MultiplePluginsReturningDifferentValues_LogsConflictWarning()
        {
            // Arrange: Create a PluginManager with a mock logger.
            var mockLogger = new MockLogger();
            var manager = new PluginManager(mockLogger);

            // Create two fake plugins returning different string values.
            var pluginA = new FakePluginReturning("A");
            var pluginB = new FakePluginReturning("B");

            // Add the plugins to the manager.
            manager.AddPlugin(pluginA);
            manager.AddPlugin(pluginB);

            // Subscribe both plugins to the "TestHook".
            manager.SubscribeToHook("TestHook", pluginA);
            manager.SubscribeToHook("TestHook", pluginB);

            // Act: Call the hook.
            var result = manager.CallHook("TestHook");

            // Assert: The last non-null result ("B") should be returned,
            // and at least one log message should mention a conflict.
            Assert.Equal("B", result);
            Assert.True(mockLogger.LogMessages.Any(m => m.ToLower().Contains("conflict")),
                "Expected a conflict warning to be logged, but none was found.");
        }
    }
}
