extern alias References;
using System;
using System.Collections.Generic;
using Oxide.Core.RemoteConsole;
using References::Cysharp.Text;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// Types for logger
    /// </summary>
    public enum LogType
    {
        Chat,
        Error,
        Info,
        Warning,
        Debug
    }

    /// <summary>
    /// Represents a logger
    /// </summary>
    public abstract class Logger
    {
        private const string ConsoleFormat = "[Oxide] {0} [{1}] {2}";
        private const string FileFormat = "{0} [{1}] {2}";

        /// <summary>
        /// Represents a single log message
        /// </summary>
        public struct LogMessage
        {
            public LogType Type;
            public string ConsoleMessage;
            public string LogfileMessage;
        }

        // The message queue
        protected Queue<LogMessage> MessageQueue;

        // Should messages be processed immediately and on the same thread?
        private bool processImmediately;

        /// <summary>
        /// Initializes a new instance of the Logger class
        /// </summary>
        /// <param name="processImmediately"></param>
        protected Logger(bool processImmediately)
        {
            // Initialize
            this.processImmediately = processImmediately;
            if (!processImmediately)
            {
                MessageQueue = new Queue<LogMessage>();
            }
        }

        /// <summary>
        /// Creates a log message from the specified arguments
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected LogMessage CreateLogMessage(LogType type, string format, object[] args)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            if (args.Length != 0)
            {
                msg.ConsoleMessage = string.Format(msg.ConsoleMessage, args);
                msg.LogfileMessage = string.Format(msg.LogfileMessage, args);
            }
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T>(LogType type, string format, T arg)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2>(LogType type, string format, T1 arg1, T2 arg2)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
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
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3, T4>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3, arg4);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3, arg4);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
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
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3, T4, T5>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3, arg4, arg5);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3, arg4, arg5);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
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
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3, T4, T5, T6>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3, arg4, arg5, arg6);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3, arg4, arg5, arg6);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
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
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3, T4, T5, T6, T7>(LogType type, string format, T1 arg1, T2 arg2,
            T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3, arg4, arg5, arg6, arg7);
            return msg;
        }

        /// <summary>
        /// Creates a log message from the specified arguments
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
        /// <returns>LogMessage</returns>
        protected LogMessage CreateLogMessage<T1, T2, T3, T4, T5, T6, T7, T8>(LogType type, string format, T1 arg1,
            T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            LogMessage msg = new LogMessage
            {
                Type = type,
                ConsoleMessage = ZString.Format(ConsoleFormat, DateTime.Now.ToShortTimeString(), type, format),
                LogfileMessage = ZString.Format(FileFormat, DateTime.Now.ToShortTimeString(), type, format)
            };

            if (Interface.Oxide.Config.Console.MinimalistMode)
            {
                msg.ConsoleMessage = format;
            }

            msg.ConsoleMessage = ZString.Format(msg.ConsoleMessage, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            msg.LogfileMessage = ZString.Format(msg.LogfileMessage, arg1, arg2, arg3, arg4, arg5, arg6, arg7, arg8);
            return msg;
        }

        /// <summary>
        /// Handles the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <param name="stackTrace"></param>
        /// <param name="logType"></param>
        public virtual void HandleMessage(string message, string stackTrace, LogType logType)
        {
            ConsoleColor consoleColor;
            string remoteType;

            if (message.ToLower().Contains("[chat]"))
            {
                logType = LogType.Chat;
            }

            switch (logType)
            {
                case LogType.Chat:
                    consoleColor = ConsoleColor.Green;
                    remoteType = "Chat";
                    break;

                case LogType.Error:
                    consoleColor = ConsoleColor.Red;
                    remoteType = "Error";
                    break;

                case LogType.Warning:
                    consoleColor = ConsoleColor.Yellow;
                    remoteType = "Warning";
                    break;

                default:
                    consoleColor = ConsoleColor.Gray;
                    remoteType = "Generic";
                    break;
            }

            Interface.Oxide.ServerConsole.AddMessage(message, consoleColor);
            Interface.Oxide.RemoteConsole.SendMessage(new RemoteMessage
            {
                Message = message,
                Identifier = -1,
                Type = remoteType,
                Stacktrace = stackTrace
            });
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void Write(LogType type, string format, params object[] args)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, args);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg"></param>
        /// <typeparam name="T"></typeparam>
        public virtual void Write<T>(LogType type, string format, T arg)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        public virtual void Write<T1, T2>(LogType type, string format, T1 arg1, T2 arg2)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <typeparam name="T1"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="T3"></typeparam>
        public virtual void Write<T1, T2, T3>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
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
        public virtual void Write<T1, T2, T3, T4>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3, arg4);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
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
        public virtual void Write<T1, T2, T3, T4, T5>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3, arg4, arg5);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
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
        public virtual void Write<T1, T2, T3, T4, T5, T6>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3, arg4, arg5, arg6);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
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
        public virtual void Write<T1, T2, T3, T4, T5, T6, T7>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3, arg4, arg5, arg6, arg7);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
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
        public virtual void Write<T1, T2, T3, T4, T5, T6, T7, T8>(LogType type, string format, T1 arg1, T2 arg2, T3 arg3, T4 arg4, T5 arg5, T6 arg6, T7 arg7, T8 arg8)
        {
            // Create the structure
            LogMessage message = CreateLogMessage(type, format, arg2, arg3, arg4, arg5, arg6, arg7, arg8);

            // Pass to overload
            Write(message);
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="message"></param>
        internal virtual void Write(LogMessage message)
        {
            // If we're set to process immediately, do so, otherwise enqueue
            if (processImmediately)
            {
                ProcessMessage(message);
                return;
            }

            MessageQueue.Enqueue(message);
        }

        /// <summary>
        /// Processes the specified message
        /// </summary>
        /// <param name="message"></param>
        protected virtual void ProcessMessage(LogMessage message)
        {
        }

        /// <summary>
        /// Writes an exception to this logger
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public virtual void WriteException(string message, Exception ex)
        {
            string formatted = ExceptionHandler.FormatException(ex);
            if (formatted != null)
            {
                Write(LogType.Error, $"{message}{Environment.NewLine}{formatted}");
                return;
            }

            Exception outerEx = ex;
            while (ex.InnerException != null)
            {
                ex = ex.InnerException;
            }

            if (outerEx.GetType() != ex.GetType())
            {
                Write(LogType.Error, "ExType: {0}", outerEx.GetType().Name);
            }

            Write(LogType.Error, $"{message} ({ex.GetType().Name}: {ex.Message})\n{ex.StackTrace}");
        }

        /// <summary>
        /// Called when logger is removed
        /// </summary>
        public virtual void OnRemoved()
        {
        }
    }
}
