using Oxide.Core.RemoteConsole;
using System;
using System.Collections.Generic;

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
        Warning
    }

    /// <summary>
    /// Represents a logger
    /// </summary>
    public abstract class Logger
    {
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
        private bool processImediately;

        /// <summary>
        /// Initializes a new instance of the Logger class
        /// </summary>
        /// <param name="processImediately"></param>
        protected Logger(bool processImediately)
        {
            // Initialize
            this.processImediately = processImediately;
            if (!processImediately)
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
                ConsoleMessage = $"[Oxide] {DateTime.Now.ToShortTimeString()} [{type}] {format}",
                LogfileMessage = $"{DateTime.Now.ToShortTimeString()} [{type}] {format}"
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
                    remoteType = "chat";
                    break;

                case LogType.Error:
                    consoleColor = ConsoleColor.Red;
                    remoteType = "error";
                    break;

                case LogType.Warning:
                    consoleColor = ConsoleColor.Yellow;
                    remoteType = "warning";
                    break;

                default:
                    consoleColor = ConsoleColor.Gray;
                    remoteType = "generic";
                    break;
            }

            Interface.Oxide.ServerConsole.AddMessage(message, consoleColor);
            Interface.Oxide.RemoteConsole.SendMessage(new RemoteMessage
            {
                Text = message,
                Identifier = 0,
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
        /// <param name="message"></param>
        internal virtual void Write(LogMessage message)
        {
            // If we're set to process immediately, do so, otherwise enqueue
            if (processImediately)
            {
                ProcessMessage(message);
            }
            else
            {
                MessageQueue.Enqueue(message);
            }
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
