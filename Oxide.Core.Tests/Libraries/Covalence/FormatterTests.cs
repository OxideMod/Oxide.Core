using Xunit;
using Oxide.Core.Libraries.Covalence;

namespace Oxide.Core.Tests.Libraries.Covalence
{
    /// <summary>
    /// Tests for the Formatter utility.
    /// </summary>
    public class FormatterTests
    {
        /// <summary>
        /// Verifies that ToPlaintext removes markup tags correctly.
        /// </summary>
        [Fact]
        public void ToPlaintext_StripsMarkup_Correctly()
        {
            // Arrange: Sample text with bold and italic markup.
            string input = "[b]Bold[/b] and [i]Italic[/i]";

            // Act: Convert to plain text.
            string plain = Formatter.ToPlaintext(input);

            // Assert: The result should be plain text without any markup.
            Assert.Equal("Bold and Italic", plain);
        }

        /// <summary>
        /// Verifies that ToUnity converts color markup into Unity-style color tags.
        /// </summary>
        [Fact]
        public void ToUnity_ConvertsMarkup_ToUnityFormat()
        {
            // Arrange: Sample text with color markup.
            string input = "[#ff0000]Red Text[/#]";

            // Act: Convert to Unity format.
            string unityFormatted = Formatter.ToUnity(input);

            // Assert: The output should contain Unity color tags.
            Assert.Contains("<color=#ff0000", unityFormatted);
            Assert.Contains("</color>", unityFormatted);
        }
    }
}
