using System;

namespace uMod.Utilities
{
    /// <summary>
    /// Utility methods to help with time management
    /// </summary>
    public class Time
    {
        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Returns DateTime.Now
        /// </summary>
        /// <returns></returns>
        public DateTime Now => DateTime.Now;

        /// <summary>
        /// Returns DateTime.UtcNow
        /// </summary>
        /// <returns></returns>
        public DateTime UtcNow => DateTime.UtcNow;

        /// <summary>
        /// Returns a DateTime from a Unix timestamp
        /// </summary>
        /// <param name="timestamp">Unix timestamp</param>
        /// <returns></returns>
        public DateTime ToDateTime(uint timestamp) => Epoch.AddSeconds(timestamp);

        /// <summary>
        /// Returns a Unix timestamp for the current time
        /// </summary>
        /// <returns></returns>
        public uint Timestamp => (uint)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        /// <summary>
        /// Returns a Unix timestamp from a DateTime
        /// </summary>
        /// <param name="time">DateTime</param>
        /// <returns></returns>
        public uint ToTimestamp(DateTime time) => (uint)time.Subtract(Epoch).TotalSeconds;
    }
}
