using Xunit;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Tests.Plugins.Mocks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Globalization;

namespace Oxide.Core.Tests.Plugins
{
    public class PluginTests : System.IDisposable
    {


        public PluginTests()
        {
            Interface.Initialize();
            Interface.Oxide.Load();
        }

        [Fact]
        public void CallHook_WithArguments_ReturnsExpectedString()
        {
            // Arrange
            var fake = new FakePlugin();

            // Act
            var result = fake.CallHook("TestHook", 1, "arg2", true);

            // Assert
            Assert.Equal("Hook: TestHook, Args: 3", result);
        }

        [Fact]
        public void CallHook_WithNoArguments_ReturnsExpectedString()
        {
            // Arrange
            var fake = new FakePlugin();

            // Act
            var result = fake.CallHook("NoArgs");

            // Assert
            Assert.Equal("Hook: NoArgs, Args: 0", result);
        }

        [Fact]
        public void PluginProperties_AreInitializedCorrectly()
        {
            // Arrange
            var fake = new FakePlugin();

            // Assert
            Assert.Equal("FakePlugin", fake.Name);
            Assert.Equal("Fake Plugin", fake.Title);
            Assert.Equal("Test", fake.Author);
            Assert.Equal(new VersionNumber(1, 0, 0), fake.Version);
        }

        public void Dispose()
        {
            // Dispose of any resources if necessary
        }
    }
}
