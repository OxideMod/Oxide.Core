using System;
using Xunit;
using Oxide.Core.Libraries;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Global library.
    /// </summary>
    public class GlobalTests
    {
        private readonly Global globalLib = new Global();

        /// <summary>
        /// Tests that IsGlobal property returns true.
        /// </summary>
        [Fact]
        public void IsGlobal_ReturnsTrue()
        {
            Assert.True(globalLib.IsGlobal);
        }

        /// <summary>
        /// Tests that MakeVersion returns the correct VersionNumber.
        /// </summary>
        [Fact]
        public void MakeVersion_ReturnsCorrectVersionNumber()
        {
            VersionNumber version = globalLib.MakeVersion(1, 2, 3);
            Assert.Equal(new VersionNumber(1, 2, 3), version);
        }

        /// <summary>
        /// Tests that MakeVersion with different values returns a different VersionNumber.
        /// </summary>
        [Fact]
        public void MakeVersion_DifferentValues_ReturnsDifferentVersionNumber()
        {
            VersionNumber version1 = globalLib.MakeVersion(1, 2, 3);
            VersionNumber version2 = globalLib.MakeVersion(3, 2, 1);
            Assert.NotEqual(version1, version2);
        }

        /// <summary>
        /// Tests that New creates a new instance of the specified type with parameters.
        /// </summary>
        [Fact]
        public void New_WithParameters_CreatesInstance()
        {
            object instance = globalLib.New(typeof(DateTime), new object[] { 2021, 1, 1 });
            Assert.IsType<DateTime>(instance);
            DateTime dt = (DateTime)instance;
            Assert.Equal(2021, dt.Year);
            Assert.Equal(1, dt.Month);
            Assert.Equal(1, dt.Day);
        }

        /// <summary>
        /// Tests that New creates a new instance of the specified type without parameters.
        /// </summary>
        [Fact]
        public void New_WithoutParameters_CreatesInstance()
        {
            object instance = globalLib.New(typeof(DateTime), null);
            Assert.IsType<DateTime>(instance);
        }

        /// <summary>
        /// Tests that New creates a new object of a simple type.
        /// </summary>
        [Fact]
        public void New_SimpleType_CreatesInstance()
        {
            object instance = globalLib.New(typeof(TestClass), null);
            Assert.IsType<TestClass>(instance);
        }

        /// <summary>
        /// Helper class for testing object creation.
        /// </summary>
        private class TestClass
        {
            public string Name { get; set; }
        }
    }
}
