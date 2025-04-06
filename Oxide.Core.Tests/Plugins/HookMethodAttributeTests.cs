using Xunit;
using Oxide.Core.Plugins;
using System;
using System.Reflection;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests for the HookMethodAttribute class
    /// </summary>
    public class HookMethodAttributeTests
    {
        /// <summary>
        /// Tests that the attribute properly stores the hook name
        /// </summary>
        [Fact]
        public void HookMethodAttribute_Constructor_SetsNameProperty()
        {
            // Arrange & Act
            const string hookName = "TestHook";
            var attribute = new HookMethodAttribute(hookName);
            
            // Assert
            Assert.Equal(hookName, attribute.Name);
            
            // Also test the attribute applied to a real method
            var method = typeof(TestClass).GetMethod("TestMethod", BindingFlags.Public | BindingFlags.Instance);
            var appliedAttr = method.GetCustomAttribute<HookMethodAttribute>();
            Assert.NotNull(appliedAttr);
            Assert.Equal("Hook_Test", appliedAttr.Name);
        }
        
        // Helper class with a method decorated with the attribute
        private class TestClass
        {
            [HookMethodAttribute("Hook_Test")]
            public void TestMethod() { }
        }
    }
} 