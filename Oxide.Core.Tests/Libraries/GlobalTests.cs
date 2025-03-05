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
        /// Tests that MakeVersion returns the correct VersionNumber.
        /// </summary>
        [Fact]
        public void MakeVersion_ReturnsCorrectVersionNumber()
        {
            VersionNumber version = globalLib.MakeVersion(1, 2, 3);
            Assert.Equal(new VersionNumber(1, 2, 3), version);
        }

        /// <summary>
        /// Tests that New creates a new instance of the specified type.
        /// </summary>
        [Fact]
        public void New_CreatesInstance()
        {
            object instance = globalLib.New(typeof(DateTime), new object[] { 2021, 1, 1 });
            Assert.IsType<DateTime>(instance);
            DateTime dt = (DateTime)instance;
            Assert.Equal(2021, dt.Year);
        }
    }
}
