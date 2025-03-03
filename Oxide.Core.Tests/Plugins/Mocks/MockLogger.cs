using System.Collections.Generic;
using Oxide.Core.Logging;

namespace Oxide.Core.Tests.Plugins.Mocks
{
    /// <summary>
    /// Mock logger for testing.
    /// </summary>
    public class MockLogger : Logger
    {
        // Store log messages for testing
        public List<string> LogMessages { get; } = new List<string>();

        public MockLogger() : base(true) // Pass 'true' (or false) as needed
        {
        }

        #region Methods
        // Write log messages to the list
        public override void Write(LogType type, string format, params object[] args)
        {
            string message = string.Format(format, args);
            LogMessages.Add($"[{type}] {message}");
        }
        // Write exceptions to the list
        public override void WriteException(string message, System.Exception ex)
        {
            LogMessages.Add($"[Exception] {message} - {ex.Message}");
        }

        // Process log messages
        protected override void ProcessMessage(LogMessage message)
        {
            // For testing, simply record the console message.
            LogMessages.Add(message.ConsoleMessage);
        }
        #endregion
    }
}
