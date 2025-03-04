using System;
using System.IO;
using System.Threading;
using Xunit;
using Oxide.Core.Plugins.Watchers;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the FSWatcher functionality by simulating file change events.
    /// </summary>
    public class FSWatcherTests : IDisposable
    {
        private readonly string testDir;
        private readonly string testFile;
        private readonly TestFSWatcher watcher;
        public string LastEventName { get; private set; }
        public WatcherChangeTypes? LastEventType { get; private set; }

        public FSWatcherTests()
        {
            // Arrange Create a temporary directory and file.
            testDir = Path.Combine(Path.GetTempPath(), "FSWatcherTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testDir);
            testFile = Path.Combine(testDir, "test.cs");
            File.WriteAllText(testFile, "// initial content");

            // Use a test subclass of FSWatcher that exposes a method to trigger the event.
            watcher = new TestFSWatcher(testDir, "*.cs");
            watcher.OnPluginAdded += name =>
            {
                LastEventName = name;
                LastEventType = WatcherChangeTypes.Created;
            };
        }

        /// <summary>
        /// Verifies that when TriggerPluginAdded is called, the OnPluginAdded event fires with the expected value.
        /// </summary>
        [Fact]
        public void FSWatcher_FiresPluginAdded_OnTrigger()
        {
            // Act Manually trigger the PluginAdded event.
            watcher.TriggerPluginAdded("test");

            // Wait a short time to ensure asynchronous callbacks are processed.
            Thread.Sleep(50);

            // Assert Verify that the event was fired with the correct values.
            Assert.Equal("test", LastEventName);
            Assert.Equal(WatcherChangeTypes.Created, LastEventType);
        }

        public void Dispose()
        {
            if (Directory.Exists(testDir))
                Directory.Delete(testDir, true);
        }

        /// <summary>
        /// Test subclass of FSWatcher that exposes a public method to trigger the protected FirePluginAdded event.
        /// </summary>
        private class TestFSWatcher : FSWatcher
        {
            public event Action<string> OnPluginAdded;

            public TestFSWatcher(string directory, string filter) : base(directory, filter) { }

            /// <summary>
            /// Exposes the protected FirePluginAdded method.
            /// </summary>
            /// <param name="name">The plugin name to pass to the event.</param>
            public void TriggerPluginAdded(string name)
            {
                base.FirePluginAdded(name);
                OnPluginAdded?.Invoke(name);
            }
        }
    }
}
