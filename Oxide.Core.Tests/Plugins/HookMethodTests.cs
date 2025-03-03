using System;
using System.ComponentModel;
using System.Reflection;
using Xunit;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests the behavior of the HookMethod class,
    /// ensuring that signature matching works as expected.
    /// </summary>
    public class HookMethodTests
    {
        // A dummy hook method for testing signature matching.
        private void DummyHookMethod(int a, string b) { }

        /// <summary>
        /// Verifies that HookMethod.HasMatchingSignature returns true for matching arguments.
        /// </summary>
        [Fact]
        public void HasMatchingSignature_ReturnsTrue_ForMatchingArgs()
        {
            // Arrange
            MethodInfo methodInfo = GetType().GetMethod(nameof(DummyHookMethod), BindingFlags.NonPublic | BindingFlags.Instance);
            var hookMethod = new HookMethod(methodInfo);

            // Act
            bool exact;
            bool result = hookMethod.HasMatchingSignature(new object[] { 42, "test" }, out exact);

            // Assert
            Assert.True(result);
            Assert.True(exact);
        }

        /// <summary>
        /// Verifies that HookMethod.HasMatchingSignature returns false for non-matching arguments.
        /// </summary>
        [Fact]
        public void HasMatchingSignature_ReturnsFalse_ForNonMatchingArgs()
        {
            // Arrange
            MethodInfo methodInfo = GetType().GetMethod(nameof(DummyHookMethod), BindingFlags.NonPublic | BindingFlags.Instance);
            var hookMethod = new HookMethod(methodInfo);

            // Act
            bool exact;
            bool result = hookMethod.HasMatchingSignature(new object[] { "wrong", 123 }, out exact);

            // Assert
            Assert.False(result);
        }
    }
}
