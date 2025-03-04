using Xunit;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for CSPlugin functionality, including hook discovery.
    /// </summary>
    public class CSPluginTests
    {
        [Fact]
        public void CSPlugin_DiscoverHooks_RegistersHookMethods()
        {
            // Arrange Create an instance of our test CSPlugin subclass.
            var plugin = new TestCSPlugin();

            // Act Call the hook "MyHook" with a value.
            var result = plugin.CallHook("MyHook", 42);

            // Assert The hook method should be invoked and return the expected string.
            Assert.Equal("Test hook called with value: 42", result);
        }

        // Test subclass of CSPlugin. Do not override OnCallHook because it is sealed in CSPlugin.
        private class TestCSPlugin : CSPlugin
        {
            public TestCSPlugin()
            {
                Name = "TestCSPlugin";
                Title = "Test CSPlugin";
                Author = "Test";
                Version = new VersionNumber(1, 0, 0);
            }

            [HookMethod("MyHook")]
            private object MyHookMethod(int value)
            {
                return $"Test hook called with value: {value}";
            }

            public override void Load()
            {
                // Minimal load logic.
            }
        }
    }
}
