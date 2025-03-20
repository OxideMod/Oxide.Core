using Xunit;
using Oxide.Core.Libraries.Covalence;
using System.Collections.Generic;
using System.Linq;

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
        
        /// <summary>
        /// Tests that Parse method correctly parses bold tags.
        /// </summary>
        [Fact]
        public void Parse_WithBoldTags_ReturnsBoldElements()
        {
            // Arrange
            string input = "This is [b]bold[/b] text";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(3, elements.Count);
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("This is ", elements[0].Val);
            
            Assert.Equal(ElementType.Bold, elements[1].Type);
            Assert.Equal(1, elements[1].Body.Count);
            Assert.Equal(ElementType.String, elements[1].Body[0].Type);
            Assert.Equal("bold", elements[1].Body[0].Val);
            
            Assert.Equal(ElementType.String, elements[2].Type);
            Assert.Equal(" text", elements[2].Val);
        }
        
        /// <summary>
        /// Tests that Parse method correctly parses italic tags.
        /// </summary>
        [Fact]
        public void Parse_WithItalicTags_ReturnsItalicElements()
        {
            // Arrange
            string input = "This is [i]italic[/i] text";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(3, elements.Count);
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("This is ", elements[0].Val);
            
            Assert.Equal(ElementType.Italic, elements[1].Type);
            Assert.Equal(1, elements[1].Body.Count);
            Assert.Equal(ElementType.String, elements[1].Body[0].Type);
            Assert.Equal("italic", elements[1].Body[0].Val);
            
            Assert.Equal(ElementType.String, elements[2].Type);
            Assert.Equal(" text", elements[2].Val);
        }
        
        /// <summary>
        /// Tests that Parse method correctly parses color tags with hex value.
        /// </summary>
        [Fact]
        public void Parse_WithColorHexTags_ReturnsColorElements()
        {
            // Arrange
            string input = "[#ff0000]Red[/#]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Single(elements);
            Assert.Equal(ElementType.Color, elements[0].Type);
            Assert.Equal("ff0000ff", elements[0].Val);
            Assert.Single(elements[0].Body);
            Assert.Equal(ElementType.String, elements[0].Body[0].Type);
            Assert.Equal("Red", elements[0].Body[0].Val);
        }
        
        /// <summary>
        /// Tests that Parse method correctly parses color tags with named color.
        /// </summary>
        [Fact]
        public void Parse_WithColorNameTags_ReturnsColorElements()
        {
            // Arrange
            string input = "[#red]Red[/#]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Single(elements);
            Assert.Equal(ElementType.Color, elements[0].Type);
            Assert.Equal("ff0000ff", elements[0].Val);
            Assert.Single(elements[0].Body);
            Assert.Equal(ElementType.String, elements[0].Body[0].Type);
            Assert.Equal("Red", elements[0].Body[0].Val);
        }
        
        /// <summary>
        /// Tests that Parse method correctly parses size tags.
        /// </summary>
        [Fact]
        public void Parse_WithSizeTags_ReturnsSizeElements()
        {
            // Arrange
            string input = "[+20]Larger[/+]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Single(elements);
            Assert.Equal(ElementType.Size, elements[0].Type);
            Assert.Equal(20, elements[0].Val);
            Assert.Single(elements[0].Body);
            Assert.Equal(ElementType.String, elements[0].Body[0].Type);
            Assert.Equal("Larger", elements[0].Body[0].Val);
        }
        
        /// <summary>
        /// Tests that Parse method handles nested tags correctly.
        /// </summary>
        [Fact]
        public void Parse_WithNestedTags_ReturnsNestedElements()
        {
            // Arrange
            string input = "[b]Bold [i]and Italic[/i][/b]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Single(elements);
            Assert.Equal(ElementType.Bold, elements[0].Type);
            Assert.Equal(2, elements[0].Body.Count);
            
            Assert.Equal(ElementType.String, elements[0].Body[0].Type);
            Assert.Equal("Bold ", elements[0].Body[0].Val);
            
            Assert.Equal(ElementType.Italic, elements[0].Body[1].Type);
            Assert.Single(elements[0].Body[1].Body);
            Assert.Equal(ElementType.String, elements[0].Body[1].Body[0].Type);
            Assert.Equal("and Italic", elements[0].Body[1].Body[0].Val);
        }
        
        /// <summary>
        /// Tests that Parse method handles unclosed tags correctly.
        /// </summary>
        [Fact]
        public void Parse_WithUnclosedTags_HandlesGracefully()
        {
            // Arrange
            string input = "[b]Bold text without closing tag";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(2, elements.Count);
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("[b]", elements[0].Val);
            
            Assert.Equal(ElementType.String, elements[1].Type);
            Assert.Equal("Bold text without closing tag", elements[1].Val);
        }
        
        /// <summary>
        /// Tests ToRustLegacy converter.
        /// </summary>
        [Fact]
        public void ToRustLegacy_ConvertsMarkup_ToRustLegacyFormat()
        {
            // Arrange
            string input = "[#ff0000]Red Text[/#]";
            
            // Act
            string result = Formatter.ToRustLegacy(input);
            
            // Assert
            Assert.Contains("[color #ff0000]", result);
            Assert.Contains("[color #ffffff]", result);
        }
        
        /// <summary>
        /// Tests handling of invalid color codes.
        /// </summary>
        [Fact]
        public void Parse_WithInvalidColorCode_HandlesGracefully()
        {
            // Arrange
            string input = "[#xyz]Invalid Color[/#]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(2, elements.Count);
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("[#xyz]Invalid Color", elements[0].Val);
            
            Assert.Equal(ElementType.String, elements[1].Type);
            Assert.Equal("[/#]", elements[1].Val);
        }
        
        /// <summary>
        /// Tests handling of invalid size values.
        /// </summary>
        [Fact]
        public void Parse_WithInvalidSizeValue_HandlesGracefully()
        {
            // Arrange
            string input = "[+abc]Invalid Size[/+]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(2, elements.Count);
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("[+abc]Invalid Size", elements[0].Val);
            
            Assert.Equal(ElementType.String, elements[1].Type);
            Assert.Equal("[/+]", elements[1].Val);
        }
        
        /// <summary>
        /// Tests handling of mixed and complex markup.
        /// </summary>
        [Fact]
        public void Parse_WithMixedComplexMarkup_HandlesCorrectly()
        {
            // Arrange
            string input = "Normal [b]Bold[/b] [i]Italic[/i] [#red]Red[/#] [+20]Large[/+] [b][i]Bold Italic[/i][/b]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Equal(10, elements.Count);
            
            // Normal
            Assert.Equal(ElementType.String, elements[0].Type);
            Assert.Equal("Normal ", elements[0].Val);
            
            // Bold
            Assert.Equal(ElementType.Bold, elements[1].Type);
            Assert.Single(elements[1].Body);
            Assert.Equal("Bold", elements[1].Body[0].Val);
            
            // Space
            Assert.Equal(ElementType.String, elements[2].Type);
            Assert.Equal(" ", elements[2].Val);
            
            // Italic
            Assert.Equal(ElementType.Italic, elements[3].Type);
            Assert.Single(elements[3].Body);
            Assert.Equal("Italic", elements[3].Body[0].Val);
            
            // Space
            Assert.Equal(ElementType.String, elements[4].Type);
            Assert.Equal(" ", elements[4].Val);
            
            // Red
            Assert.Equal(ElementType.Color, elements[5].Type);
            Assert.Equal("ff0000ff", elements[5].Val);
            Assert.Single(elements[5].Body);
            Assert.Equal("Red", elements[5].Body[0].Val);
            
            // Space
            Assert.Equal(ElementType.String, elements[6].Type);
            Assert.Equal(" ", elements[6].Val);
            
            // Large
            Assert.Equal(ElementType.Size, elements[7].Type);
            Assert.Equal(20, elements[7].Val);
            Assert.Single(elements[7].Body);
            Assert.Equal("Large", elements[7].Body[0].Val);
            
            // Space
            Assert.Equal(ElementType.String, elements[8].Type);
            Assert.Equal(" ", elements[8].Val);
            
            // Bold Italic
            Assert.Equal(ElementType.Bold, elements[9].Type);
            Assert.Single(elements[9].Body);
            Assert.Equal(ElementType.Italic, elements[9].Body[0].Type);
            Assert.Single(elements[9].Body[0].Body);
            Assert.Equal("Bold Italic", elements[9].Body[0].Body[0].Val);
        }

        /// <summary>
        /// Tests that ToPlaintext handles empty input correctly.
        /// </summary>
        [Fact]
        public void ToPlaintext_WithEmptyInput_ReturnsEmptyString()
        {
            // Arrange
            string input = "";
            
            // Act
            string result = Formatter.ToPlaintext(input);
            
            // Assert
            Assert.Equal("", result);
        }
        
        /// <summary>
        /// Tests that ToPlaintext handles whitespace-only input correctly.
        /// </summary>
        [Fact]
        public void ToPlaintext_WithWhitespaceInput_ReturnsWhitespace()
        {
            // Arrange
            string input = "   \t\n";
            
            // Act
            string result = Formatter.ToPlaintext(input);
            
            // Assert
            Assert.Equal("   \t\n", result);
        }
        
        /// <summary>
        /// Tests that ToPlaintext handles unclosed tags correctly.
        /// </summary>
        [Fact]
        public void ToPlaintext_WithUnclosedTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[b]Bold text without closing tag";
            
            // Act
            string result = Formatter.ToPlaintext(input);
            
            // Assert
            Assert.Equal("[b]Bold text without closing tag", result);
        }
        
        /// <summary>
        /// Tests that ToPlaintext handles escaped brackets correctly.
        /// </summary>
        [Fact]
        public void ToPlaintext_WithEscapedBrackets_HandlesCorrectly()
        {
            // Arrange
            string input = "This \\[b] is not a tag";
            
            // Act
            string result = Formatter.ToPlaintext(input);
            
            // Assert
            Assert.Equal("This \\[b] is not a tag", result);
        }
        
        /// <summary>
        /// Tests that ToRustLegacy handles empty tags correctly.
        /// </summary>
        [Fact]
        public void ToRustLegacy_WithEmptyTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[b][/b][i][/i][#ff0000][/#]";
            
            // Act
            string result = Formatter.ToRustLegacy(input);
            
            // Assert
            Assert.Contains("[color #ff0000]", result);
            Assert.Contains("[color #ffffff]", result);
        }
        
        /// <summary>
        /// Tests that ToRustLegacy handles named color tags correctly.
        /// </summary>
        [Fact]
        public void ToRustLegacy_WithNamedColorTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[#red]Red[/#] and [#green]Green[/#]";
            
            // Act
            string result = Formatter.ToRustLegacy(input);
            
            // Assert
            Assert.Contains("[color #ff0000]Red", result);
            Assert.Contains("[color #008000]Green", result);
        }
        
        /// <summary>
        /// Tests that ToRustLegacy handles bold tags correctly.
        /// </summary>
        [Fact]
        public void ToRustLegacy_WithBoldTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[b]Bold[/b]";
            
            // Act
            string result = Formatter.ToRustLegacy(input);
            
            // Assert
            Assert.Equal("Bold", result);
        }
        
        /// <summary>
        /// Tests that ToRustLegacy handles italic tags correctly.
        /// </summary>
        [Fact]
        public void ToRustLegacy_WithItalicTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[i]Italic[/i]";
            
            // Act
            string result = Formatter.ToRustLegacy(input);
            
            // Assert
            Assert.Equal("Italic", result);
        }
        
        /// <summary>
        /// Tests that ToUnity handles empty tags correctly.
        /// </summary>
        [Fact]
        public void ToUnity_WithEmptyTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[b][/b][i][/i][#ff0000][/#]";
            
            // Act
            string result = Formatter.ToUnity(input);
            
            // Assert
            Assert.Equal("<b></b><i></i><color=#ff0000ff></color>", result);
        }
        
        /// <summary>
        /// Tests that ToUnity handles size tags correctly.
        /// </summary>
        [Fact]
        public void ToUnity_WithSizeTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[+10]Small[/+] and [+30]Large[/+]";
            
            // Act
            string result = Formatter.ToUnity(input);
            
            // Assert
            Assert.Contains("<size=10>Small</size>", result);
            Assert.Contains("<size=30>Large</size>", result);
        }
        
        /// <summary>
        /// Tests that ToUnity handles nested tags with size and color correctly.
        /// </summary>
        [Fact]
        public void ToUnity_WithNestedSizeAndColorTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[+20][#ff0000]Large Red Text[/#][/+]";
            
            // Act
            string result = Formatter.ToUnity(input);
            
            // Assert
            Assert.Contains("<size=20>", result);
            Assert.Contains("</size>", result);
            Assert.Contains("<color=#ff0000ff>", result);
            Assert.Contains("</color>", result);
            Assert.Contains("Large Red Text", result);
        }
        
        /// <summary>
        /// Tests that Parse handles complex nested elements correctly.
        /// </summary>
        [Fact]
        public void Parse_WithComplexNestedElements_ParsesCorrectly()
        {
            // Arrange
            string input = "[b][i][#ff0000][+20]Complex[/+][/#][/i][/b]";
            
            // Act
            List<Element> elements = Formatter.Parse(input);
            
            // Assert
            Assert.Single(elements);
            Assert.Equal(ElementType.Bold, elements[0].Type);
            
            // Check nesting: Bold > Italic > Color > Size
            Assert.Single(elements[0].Body);
            Assert.Equal(ElementType.Italic, elements[0].Body[0].Type);
            
            Assert.Single(elements[0].Body[0].Body);
            Assert.Equal(ElementType.Color, elements[0].Body[0].Body[0].Type);
            Assert.Equal("ff0000ff", elements[0].Body[0].Body[0].Val);
            
            Assert.Single(elements[0].Body[0].Body[0].Body);
            Assert.Equal(ElementType.Size, elements[0].Body[0].Body[0].Body[0].Type);
            Assert.Equal(20, elements[0].Body[0].Body[0].Body[0].Val);
            
            Assert.Single(elements[0].Body[0].Body[0].Body[0].Body);
            Assert.Equal(ElementType.String, elements[0].Body[0].Body[0].Body[0].Body[0].Type);
            Assert.Equal("Complex", elements[0].Body[0].Body[0].Body[0].Body[0].Val);
        }
        
        /// <summary>
        /// Tests that ToUnity handles complex nested tags correctly.
        /// </summary>
        [Fact]
        public void ToUnity_WithComplexNestedTags_HandlesCorrectly()
        {
            // Arrange
            string input = "[b]Bold [#ff0000]Red [i]and Italic[/i][/#][/b]";
            
            // Act
            string result = Formatter.ToUnity(input);
            
            // Assert
            Assert.Contains("<b>", result);
            Assert.Contains("</b>", result);
            Assert.Contains("<color=#ff0000", result);
            Assert.Contains("</color>", result);
            Assert.Contains("<i>", result);
            Assert.Contains("</i>", result);
            Assert.Contains("Bold ", result);
            Assert.Contains("Red ", result);
            Assert.Contains("and Italic", result);
        }
    }
}
