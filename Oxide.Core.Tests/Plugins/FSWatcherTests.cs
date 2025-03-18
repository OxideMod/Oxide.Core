using System;
using System.IO;
using System.Threading;
using Xunit;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the FSWatcher functionality by directly testing the FirePluginAdded method.
    /// </summary>
    public class FSWatcherTests
    {
        /// <summary>
        /// Tests that the PluginChangeWatcher fires events correctly.
        /// </summary>
        [Fact]
        public void PluginChangeWatcher_FiresPluginAddedEvent()
        {
            // Arrange
            string eventFiredPlugin = null;
            var mockWatcher = new MockPluginChangeWatcher();
            mockWatcher.OnPluginAdded += name => eventFiredPlugin = name;
            
            // Act
            mockWatcher.TriggerPluginAdded("test-plugin");
            
            // Assert
            Assert.Equal("test-plugin", eventFiredPlugin);
        }
        
        /// <summary>
        /// Simple mock of PluginChangeWatcher for testing event firing
        /// </summary>
        private class MockPluginChangeWatcher : PluginChangeWatcher
        {
            public void TriggerPluginAdded(string name)
            {
                FirePluginAdded(name);
            }
        }
    }
}
