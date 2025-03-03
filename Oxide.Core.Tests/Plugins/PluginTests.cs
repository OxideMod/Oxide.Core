using Xunit;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Contains tests for the base Plugin functionality. 
    /// Verifies hook calling behavior, error event invocation, and other plugin-related features.
    /// </summary>
    public class PluginTests : System.IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the PluginTests class.
        /// Ensures that the Oxide framework is initialized before any plugin is constructed.
        /// </summary>
        public PluginTests()
        {
            // Initialize Oxide core so that libraries are registered.
            Interface.Initialize();
            // Fully load OxideMod so that GetLibrary<T>() calls succeed.
            Interface.Oxide.Load();
        }

        /// <summary>
        /// Tests that calling a hook with arguments returns the expected string.
        /// </summary>
        [Fact]
        public void CallHook_WithArguments_ReturnsExpectedString()
        {
            // Arrange: Create a new fake plugin.
            var fake = new FakePlugin();

            // Act: Call the hook "TestHook" with 3 arguments.
            var result = fake.CallHook("TestHook", 1, "arg2", true);

            // Assert: The result should indicate that "TestHook" was called with 3 arguments.
            Assert.Equal("Hook: TestHook, Args: 3", result);
        }

        /// <summary>
        /// Tests that calling a hook with no arguments returns the expected string.
        /// </summary>
        [Fact]
        public void CallHook_WithNoArguments_ReturnsExpectedString()
        {
            // Arrange: Create a new fake plugin.
            var fake = new FakePlugin();

            // Act: Call the hook "NoArgs" with no arguments.
            var result = fake.CallHook("NoArgs");

            // Assert: The result should indicate that "NoArgs" was called with 0 arguments.
            Assert.Equal("Hook: NoArgs, Args: 0", result);
        }

        /// <summary>
        /// Tests that calling RaiseError on a plugin correctly invokes the OnError event.
        /// </summary>
        [Fact]
        public void RaiseError_InvokesOnErrorEvent()
        {
            // Arrange: Create a fake plugin and subscribe to its OnError event.
            var fake = new FakePlugin();
            bool errorRaised = false;
            fake.OnError += (plugin, message) => errorRaised = true;

            // Act: Trigger an error.
            fake.RaiseError("Test Error");

            // Assert: Verify that the error event was raised.
            Assert.True(errorRaised);
        }

        /// <summary>
        /// Performs cleanup after tests (if necessary).
        /// </summary>
        public void Dispose()
        {
            // Dispose of any resources if necessary.
        }
    }
}
