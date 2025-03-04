using Xunit;
using Oxide.Core;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the command line parsing functionality.
    /// </summary>
    public class CommandLineTests
    {
        /// <summary>
        /// Verifies that the CommandLine class correctly parses key/value arguments.
        /// </summary>
        [Fact]
        public void CommandLine_ParsesKeyValue_ArgumentsCorrectly()
        {
            // Arrange
            string[] args = new[] { "+server.port", "28015", "-nolog" };
            var commandLine = new CommandLine(args);

            // Act
            bool hasPort = commandLine.HasVariable("server.port");
            string portValue = commandLine.GetVariable("server.port");

            // Assert
            Assert.True(hasPort);
            Assert.Equal("28015", portValue);
        }
    }
}
