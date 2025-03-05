using System;
using System.Linq;
using System.Reflection;
using Xunit;
using Oxide.Core.Libraries;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the base Library class.
    /// </summary>
    public class LibraryTests
    {
        private class TestLibrary : Library
        {
            [LibraryFunction("TestFunction")]
            public int TestFunction(int a, int b) => a + b;

            [LibraryProperty("TestProperty")]
            public string TestProperty { get; } = "TestValue";
        }

        private readonly TestLibrary lib = new TestLibrary();

        /// <summary>
        /// Tests that a registered function can be retrieved and invoked.
        /// </summary>
        [Fact]
        public void GetFunction_ReturnsRegisteredFunction()
        {
            MethodInfo mi = lib.GetFunction("TestFunction");
            Assert.NotNull(mi);
            object result = mi.Invoke(lib, new object[] { 3, 4 });
            Assert.Equal(7, result);
        }

        /// <summary>
        /// Tests that a registered property can be retrieved and its value is correct.
        /// </summary>
        [Fact]
        public void GetProperty_ReturnsRegisteredProperty()
        {
            PropertyInfo pi = lib.GetProperty("TestProperty");
            Assert.NotNull(pi);
            string value = (string)pi.GetValue(lib);
            Assert.Equal("TestValue", value);
        }

        /// <summary>
        /// Tests that attempting to retrieve a non-existent function returns null.
        /// </summary>
        [Fact]
        public void GetFunction_ReturnsNull_ForNonExistentFunction()
        {
            MethodInfo mi = lib.GetFunction("NonExistent");
            Assert.Null(mi);
        }
    }
}
