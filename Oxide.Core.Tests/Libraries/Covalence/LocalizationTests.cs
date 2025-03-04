using System.Collections.Generic;
using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the Localization functionality.
    /// </summary>
    public class LocalizationTests
    {
        /// <summary>
        /// Verifies that the English localization dictionary returns the expected string for a known key.
        /// </summary>
        [Fact]
        public void Localization_ReturnsExpectedString_ForKnownKey()
        {
            // Arrange: Use the English language dictionary.
            Dictionary<string, string> english = Localization.languages["en"];

            // Act
            string value = english["NoPermissionPlayers"];

            // Assert
            Assert.Equal("No players with this permission", value);
        }
    }
}
