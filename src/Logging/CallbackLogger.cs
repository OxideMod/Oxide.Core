namespace Oxide.Core.Logging
{
    public class CallbackLogger : Logger
    {
        private NativeDebugCallback Callback { get; }

        /// <summary>
        /// Initialises a new instance of the CallbackLogger class
        /// </summary>
        /// <param name="callback"></param>
        public CallbackLogger(NativeDebugCallback callback) : base(true)
        {
            Callback = callback;
        }

        /// <summary>
        /// Processes the specified message
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessMessage(LogMessage message) => Callback.Invoke(message.LogfileMessage);
    }
}
