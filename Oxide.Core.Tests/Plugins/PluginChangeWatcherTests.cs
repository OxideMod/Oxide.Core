using Xunit;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests that PluginChangeWatcher fires its events correctly.
    /// </summary>
    public class PluginChangeWatcherTests
    {
        [Fact]
        public void PluginChangeWatcher_FiresEvents()
        {
            // Arrange: Create a test PluginChangeWatcher instance.
            var watcher = new TestPluginChangeWatcher();
            string sourceChanged = null, pluginAdded = null, pluginRemoved = null;

            watcher.OnPluginSourceChanged += name => sourceChanged = name;
            watcher.OnPluginAdded += name => pluginAdded = name;
            watcher.OnPluginRemoved += name => pluginRemoved = name;

            // Act: Fire each event.
            watcher.FirePluginSourceChanged("sourceFile");
            watcher.FirePluginAdded("newPlugin");
            watcher.FirePluginRemoved("oldPlugin");

            // Assert: Verify that events are fired with the expected values.
            Assert.Equal("sourceFile", sourceChanged);
            Assert.Equal("newPlugin", pluginAdded);
            Assert.Equal("oldPlugin", pluginRemoved);
        }

        private class TestPluginChangeWatcher : PluginChangeWatcher
        {
            public new void FirePluginSourceChanged(string name) => base.FirePluginSourceChanged(name);
            public new void FirePluginAdded(string name) => base.FirePluginAdded(name);
            public new void FirePluginRemoved(string name) => base.FirePluginRemoved(name);
        }
    }
}
