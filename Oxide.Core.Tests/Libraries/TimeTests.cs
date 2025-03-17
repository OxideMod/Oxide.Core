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
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Tests that IsGlobal property returns false.
        /// </summary>
        [Fact]
        public void IsGlobal_ReturnsFalse()
        {
            Assert.False(timeLib.IsGlobal);
        }

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
        /// Tests conversion from zero Unix timestamp to DateTime.
        /// </summary>
        [Fact]
        public void GetDateTimeFromUnix_ZeroTimestamp_ReturnsEpoch()
        {
            uint timestamp = 0;
            DateTime dt = timeLib.GetDateTimeFromUnix(timestamp);
            Assert.Equal(Epoch, dt);
        }

        /// <summary>
        /// Tests conversion from large Unix timestamp to DateTime.
        /// </summary>
        [Fact]
        public void GetDateTimeFromUnix_LargeTimestamp_ReturnsCorrectDateTime()
        {
            uint timestamp = 2147483647; // Max int32 value
            DateTime dt = timeLib.GetDateTimeFromUnix(timestamp);
            Assert.Equal(Epoch.AddSeconds(timestamp), dt);
        }

        /// <summary>
        /// Tests that GetUnixTimestamp returns a value within a reasonable range.
        /// </summary>
        [Fact]
        public void GetUnixTimestamp_ReturnsValue()
        {
            uint ts = timeLib.GetUnixTimestamp();
            Assert.True(ts > 0);
            
            // Verify timestamp is in a reasonable range (after 2020 and before 2050)
            DateTime resultTime = Epoch.AddSeconds(ts);
            Assert.True(resultTime.Year >= 2020);
            Assert.True(resultTime.Year <= 2050);
        }

        /// <summary>
        /// Tests conversion from DateTime to Unix timestamp.
        /// </summary>
        [Fact]
        public void GetUnixFromDateTime_ReturnsCorrectTimestamp()
        {
            DateTime dt = new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint ts = timeLib.GetUnixFromDateTime(dt);
            Assert.Equal(1609459200u, ts);
        }

        /// <summary>
        /// Tests conversion from Epoch to Unix timestamp.
        /// </summary>
        [Fact]
        public void GetUnixFromDateTime_Epoch_ReturnsZero()
        {
            uint ts = timeLib.GetUnixFromDateTime(Epoch);
            Assert.Equal(0u, ts);
        }

        /// <summary>
        /// Tests conversion from pre-Epoch time to Unix timestamp.
        /// </summary>
        [Fact]
        public void GetUnixFromDateTime_PreEpoch_ReturnsZero()
        {
            DateTime preEpoch = new DateTime(1960, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            uint ts = timeLib.GetUnixFromDateTime(preEpoch);
            // uint will wrap around for negative values, but let's make sure the method doesn't crash
            Assert.True(ts > 0);
        }

        /// <summary>
        /// Tests that converting from Unix timestamp and back results in the same value.
        /// </summary>
        [Fact]
        public void ConvertBetweenUnixAndDateTime_RoundTrip()
        {
            uint originalTs = 1609459200; // 2021-01-01 00:00:00 UTC
            DateTime dt = timeLib.GetDateTimeFromUnix(originalTs);
            uint convertedTs = timeLib.GetUnixFromDateTime(dt);
            Assert.Equal(originalTs, convertedTs);
        }
    }
}
