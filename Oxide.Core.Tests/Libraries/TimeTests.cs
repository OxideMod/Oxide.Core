using System;
using Xunit;
using Oxide.Core.Libraries;

namespace Oxide.Core.Tests.Libraries
{
    /// <summary>
    /// Tests for the Time library.
    /// </summary>
    public class TimeTests
    {
        private readonly Time timeLib = new Time();

        /// <summary>
        /// Tests that GetCurrentTime returns a value close to DateTime.UtcNow.
        /// </summary>
        [Fact]
        public void GetCurrentTime_ReturnsUtcNow()
        {
            DateTime before = DateTime.UtcNow;
            DateTime current = timeLib.GetCurrentTime();
            DateTime after = DateTime.UtcNow;
            Assert.True(current >= before && current <= after);
        }

        /// <summary>
        /// Tests conversion from Unix timestamp to DateTime.
        /// </summary>
        [Fact]
        public void GetDateTimeFromUnix_ReturnsCorrectDateTime()
        {
            uint timestamp = 1609459200; // 2021-01-01 00:00:00 UTC
            DateTime dt = timeLib.GetDateTimeFromUnix(timestamp);
            Assert.Equal(new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc), dt);
        }

        /// <summary>
        /// Tests that GetUnixTimestamp returns a value within a reasonable range.
        /// </summary>
        [Fact]
        public void GetUnixTimestamp_ReturnsValue()
        {
            uint ts = timeLib.GetUnixTimestamp();
            Assert.True(ts > 0);
        }

        /// <summary>
        /// Tests conversion from DateTime to Unix timestamp.
        /// </summary>
        [Fact]
        public void GetUnixFromDateTime_ReturnsCorrectTimestamp()
        {
            DateTime dt = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint ts = timeLib.GetUnixFromDateTime(dt);
            Assert.Equal("1609459200", ts.ToString());
        }
    }
}
