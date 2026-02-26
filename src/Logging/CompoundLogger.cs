extern alias References;
using System.Collections.Generic;

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
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <typeparam name="T"></typeparam>
        public override void Write<T>(LogType type, string format, T arg)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        public override void Write<T1, T2>(LogType type, string format, T1 arg1, T2 arg2)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        public override void Write<T1, T2, T3>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        public override void Write<T1, T2, T3, T4>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3, arg4);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3, arg4));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        public override void Write<T1, T2, T3, T4, T5>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3, arg4, arg5);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3, arg4, arg5));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        public override void Write<T1, T2, T3, T4, T5, T6>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3, arg4, arg5, arg6);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3, arg4, arg5, arg6));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        /// <param name="arg7"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        public override void Write<T1, T2, T3, T4, T5, T6, T7>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3, arg4, arg5, arg6, arg7));
                }
            }
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="arg5"></param>
        /// <param name="arg6"></param>
        /// <param name="arg7"></param>
        /// <param name="arg8"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <typeparam name="T4"></typeparam>
        /// <typeparam name="T5"></typeparam>
        /// <typeparam name="T6"></typeparam>
        /// <typeparam name="T7"></typeparam>
        /// <typeparam name="T8"></typeparam>
        public override void Write<T1, T2, T3, T4, T5, T6, T7, T8>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            // Write to all current subloggers
            foreach (Logger logger in subloggers)
            {
                logger.Write(type, format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            }

            // Cache it for any loggers added late
            if (usecache)
            {
                lock (Lock)
                {
                    messagecache.Add(CreateLogMessage(type, format, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8));
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
