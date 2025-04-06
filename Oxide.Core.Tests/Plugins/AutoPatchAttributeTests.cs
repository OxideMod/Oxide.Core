using Xunit;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the AutoPatchAttribute class
    /// </summary>
    public class AutoPatchAttributeTests
    {
        /// <summary>
        /// Tests that the attribute can be instantiated
        /// </summary>
        [Fact]
        public void AutoPatchAttribute_Constructor_CreatesInstance()
        {
            // Arrange & Act
            var attribute = new AutoPatchAttribute();
            
            // Assert
            Assert.NotNull(attribute);
            Assert.IsType<AutoPatchAttribute>(attribute);
        }
    }
} 