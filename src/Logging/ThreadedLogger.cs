using System.Threading;

namespace uMod.Logging
{
    /// <summary>
    /// Represents a logger that processes messages on a worker thread
    /// </summary>
    public abstract class ThreadedLogger : Logger
    {
        // Sync mechanisms
        private readonly AutoResetEvent waitEvent;
        private readonly object syncRoot;
        private bool exit;

        // The worker thread
        private readonly Thread workerThread;

        /// <summary>
        /// Initializes a new instance of the ThreadedLogger class
        /// </summary>
        protected ThreadedLogger() : base(false)
        {
            // Initialize
            waitEvent = new AutoResetEvent(false);
            exit = false;
            syncRoot = new object();

            // Create the thread
            workerThread = new Thread(Worker) { IsBackground = true };
            workerThread.Start();
        }

        ~ThreadedLogger()
        {
            OnRemoved();
        }

        public override void OnRemoved()
        {
            if (!exit)
            {
                exit = true;
                waitEvent.Set();
                workerThread.Join();
            }
        }

        /// <summary>
        /// Writes a message to the current logfile
        /// </summary>
        /// <param name="msg"></param>
        internal override void Write(LogMessage msg)
        {
            lock (syncRoot)
            {
                base.Write(msg);
            }

            waitEvent.Set();
        }

        /// <summary>
        /// Begins a batch process operation
        /// </summary>
        protected abstract void BeginBatchProcess();

        /// <summary>
        /// Finishes a batch process operation
        /// </summary>
        protected abstract void FinishBatchProcess();

        /// <summary>
        /// The worker thread
        /// </summary>
        private void Worker()
        {
            // Loop until it's time to exit
            while (!exit)
            {
                // Wait for signal
                waitEvent.WaitOne();

                // Iterate each item in the queue
                lock (syncRoot)
                {
                    if (MessageQueue.Count > 0)
                    {
                        BeginBatchProcess();
                        try
                        {
                            while (MessageQueue.Count > 0)
                            {
                                // Dequeue
                                LogMessage message = MessageQueue.Dequeue();

                                // Process
                                ProcessMessage(message);
                            }
                        }
                        finally
                        {
                            FinishBatchProcess();
                        }
                    }
                }
            }
        }
    }
}
