using System;
using System.Threading;
using System.Threading.Tasks;


namespace NetMQ
{
    internal sealed class NetMQSynchronizationContext : SynchronizationContext
    {
        private readonly NetMQPoller m_poller;

        public NetMQSynchronizationContext(NetMQPoller poller)
        {
            m_poller = poller;
        }

        /// <summary>Dispatches an asynchronous message to a synchronization context.</summary>
        /// <remarks>
        /// If the poller has been disposed the callback is executed on the thread pool instead.
        /// Awaiting code captures this synchronization context while running on the poller thread,
        /// and its continuations may be posted after the poller is disposed. Throwing here would
        /// crash the process (the await machinery rethrows exceptions from
        /// <see cref="SynchronizationContext.Post"/> on the thread pool), while dropping the
        /// callback would hang the awaiting code forever.
        /// </remarks>
        public override void Post(SendOrPostCallback d, object? state)
        {
            if (m_poller.IsDisposed)
            {
                ThreadPool.QueueUserWorkItem(s => d(s), state);
                return;
            }

            var task = new Task(() => d(state));
            try
            {
                task.Start(m_poller);
            }
            catch (Exception ex) when (ex is TaskSchedulerException or ObjectDisposedException)
            {
                // The poller was disposed concurrently with this call. Touch task.Exception so the
                // faulted task does not later surface as an UnobservedTaskException.
                _ = task.Exception;
                ThreadPool.QueueUserWorkItem(s => d(s), state);
            }
        }

        /// <summary>Dispatches a synchronous message to a synchronization context.</summary>
        /// <remarks>
        /// If the poller has been disposed the callback is executed synchronously on the calling
        /// thread, for the same reasons described on <see cref="Post"/>.
        /// </remarks>
        public override void Send(SendOrPostCallback d, object? state)
        {
            if (m_poller.IsDisposed)
            {
                d(state);
                return;
            }

            var task = new Task(() => d(state));
            try
            {
                task.Start(m_poller);
            }
            catch (Exception ex) when (ex is TaskSchedulerException or ObjectDisposedException)
            {
                _ = task.Exception;
                d(state);
                return;
            }
            task.Wait();
        }
    }
}
