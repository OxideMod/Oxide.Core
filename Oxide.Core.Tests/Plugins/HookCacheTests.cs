using System.Collections.Generic;
using Xunit;
using Oxide.Core.Plugins;

namespace Oxide.Core.Tests.Plugins
{
    /// <summary>
    /// Tests the caching behavior of the HookCache class.
    /// </summary>
    public class HookCacheTests
    {
        /// <summary>
        /// Tests that GetHookMethod returns consistent results for the same arguments
        /// </summary>
        [Fact]
        public void GetHookMethod_ReturnsConsistentResults_ForSameArgs()
        {
            // Arrange
            var cache = new HookCache();
            // Set up the cache with a dummy hook method.
            var dummyMethod = new HookMethod(GetType().GetMethod(nameof(DummyHookMethod), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            cache.SetupMethods(new List<HookMethod> { dummyMethod });

            // Act
            HookCache outCache1;
            var methods1 = cache.GetHookMethod(new object[0], 0, out outCache1);
            HookCache outCache2;
            var methods2 = cache.GetHookMethod(new object[0], 0, out outCache2);

            // Assert: The same instance should be returned for the same arguments.
            Assert.Same(methods1, methods2);
            Assert.Same(outCache1, outCache2);
        }
        
        /// <summary>
        /// Tests that GetHookMethod returns expected results for different input parameters
        /// </summary>
        [Fact]
        public void GetHookMethod_ReturnsExpectedResults_ForDifferentParameters()
        {
            // Arrange
            var cache = new HookCache();
            var dummyMethod = new HookMethod(GetType().GetMethod(nameof(DummyHookMethodWithParam), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            cache.SetupMethods(new List<HookMethod> { dummyMethod });

            // Act - Call with matching parameter
            HookCache outCache;
            var methods = cache.GetHookMethod(new object[] { "test" }, 1, out outCache);

            // Assert - Should match
            Assert.NotNull(methods);
            Assert.Equal(dummyMethod.Method, methods[0].Method);
        }
        
        /// <summary>
        /// Tests that SetupMethods properly initializes the cache
        /// </summary>
        [Fact]
        public void SetupMethods_InitializesCache()
        {
            // Arrange
            var cache = new HookCache();
            var dummyMethod1 = new HookMethod(GetType().GetMethod(nameof(DummyHookMethod), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            var dummyMethod2 = new HookMethod(GetType().GetMethod(nameof(DummyHookMethodWithParam), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            
            // Act
            cache.SetupMethods(new List<HookMethod> { dummyMethod1, dummyMethod2 });
            
            // Assert - We can verify setup worked by checking property values
            HookCache outCache1;
            var methods1 = cache.GetHookMethod(new object[0], 0, out outCache1);
            Assert.NotNull(methods1);
            
            HookCache outCache2;
            var methods2 = cache.GetHookMethod(new object[] { "test" }, 1, out outCache2);
            Assert.NotNull(methods2);
        }
        
        /// <summary>
        /// Tests that GetHookMethod returns methods with expected parameter count
        /// </summary>
        [Fact]
        public void GetHookMethod_ReturnsMethods_WithExpectedParameterCount()
        {
            // Arrange
            var cache = new HookCache();
            var noParamMethod = new HookMethod(GetType().GetMethod(nameof(DummyHookMethod), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            var withParamMethod = new HookMethod(GetType().GetMethod(nameof(DummyHookMethodWithParam), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance));
            
            // Setup methods
            cache.SetupMethods(new List<HookMethod> { noParamMethod, withParamMethod });
            
            // Act - Get method with zero parameters
            HookCache outCache;
            var methods = cache.GetHookMethod(new object[0], 0, out outCache);
            
            // Assert - Should get at least one method
            Assert.NotNull(methods);
            Assert.Contains(methods, m => m.Parameters.Length == 0);
        }

        private void DummyHookMethod() { }
        
        private void DummyHookMethodWithParam(string param) { }
    }
}
