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

        private void DummyHookMethod() { }
    }
}
