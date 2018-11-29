using System.Collections.Generic;
using System.Linq;

namespace uMod.Logging
{
    /// <summary>
    /// Represents a set of loggers that fall under a single logger
    /// </summary>
    public sealed class CompoundLogger : Logger
    {
        // Loggers under this compound logger
        private readonly HashSet<Logger> subLoggers;

        // Any cached messages for new loggers
        private readonly List<LogMessage> messageCache;
        private bool useCache;

        /// <summary>
        /// Initializes a new instance of the CompoundLogger class
        /// </summary>
        public CompoundLogger() : base(true)
        {
            // Initialize
            subLoggers = new HashSet<Logger>();
            messageCache = new List<LogMessage>();
            useCache = true;
        }

        /// <summary>
        /// Adds a sublogger to this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void AddLogger(Logger logger)
        {
            // Register it
            subLoggers.Add(logger);

            // Write the message cache to it
            foreach (LogMessage message in messageCache.ToList())
            {
                logger.Write(message);
            }
        }

        /// <summary>
        /// Removes a sublogger from this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void RemoveLogger(Logger logger)
        {
            // Unregister it
            logger.OnRemoved();
            subLoggers.Remove(logger);
        }

        /// <summary>
        /// Removes and cleans up all loggers
        /// </summary>
        public void Shutdown()
        {
            foreach (Logger subLogger in subLoggers)
            {
                subLogger.OnRemoved();
            }
            subLoggers.Clear();
        }

        /// <summary>
        /// Writes a message to all sub-loggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void Write(LogType type, string format, params object[] args)
        {
            // Write to all current sub-loggers
            foreach (Logger logger in subLoggers)
            {
                logger.Write(type, format, args);
            }

            // Cache it for any loggers added late
            if (useCache)
            {
                messageCache.Add(CreateLogMessage(type, format, args));
            }
        }

        /// <summary>
        /// Disables logger message cache
        /// </summary>
        public void DisableCache()
        {
            useCache = false;
            messageCache.Clear();
        }
    }
}
