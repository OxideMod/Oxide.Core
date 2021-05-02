﻿using System.Collections.Generic;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// Represents a set of loggers that fall under a single logger
    /// </summary>
    public sealed class CompoundLogger : Logger
    {
        // Loggers under this compound logger
        private readonly HashSet<Logger> subloggers;

        // Any cached messages for new loggers
        private readonly List<LogMessage> messagecache;
        private bool usecache;

        private readonly object Lock = new object();

        /// <summary>
        /// Initializes a new instance of the CompoundLogger class
        /// </summary>
        public CompoundLogger() : base(true)
        {
            // Initialize
            subloggers = new HashSet<Logger>();
            messagecache = new List<LogMessage>();
            usecache = true;
        }

        /// <summary>
        /// Adds a sublogger to this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void AddLogger(Logger logger)
        {
            // Register it
            subloggers.Add(logger);

            lock (Lock)
            {
                // Write the message cache to it
                foreach (LogMessage t in messagecache)
                {
                    logger.Write(t);
                }
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
            subloggers.Remove(logger);
        }

        /// <summary>
        /// Removes and cleans up all loggers
        /// </summary>
        public void Shutdown()
        {
            foreach (Logger sublogger in subloggers)
            {
                sublogger.OnRemoved();
            }

            subloggers.Clear();
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void Write(LogType type, string format, params object[] args)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, args);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, args));
                }
            }
        }

        /// <summary>
        /// Disables logger message cache
        /// </summary>
        public void DisableCache()
        {
            usecache = false;
            lock (Lock)
            {
                messagecache.Clear();
            }
        }
    }
}
