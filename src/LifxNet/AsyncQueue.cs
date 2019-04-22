using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
    /// <summary>
    /// Based on https://stackoverflow.com/a/22938345 . Ideally we'd use BufferBlock from the TPL Dataflow
    /// library, but since this is already a library we probably don't want the extra dependency.
    /// </summary>
    internal class AsyncQueue<T>
    {
        private ConcurrentQueue<T> _bufferQueue;
        private ConcurrentQueue<TaskCompletionSource<T>> _consumerQueue;
        private object _syncRoot = new object();

        public AsyncQueue()
        {
            _bufferQueue = new ConcurrentQueue<T>();
            _consumerQueue = new ConcurrentQueue<TaskCompletionSource<T>>();
        }

        /// <summary>
        /// Add an item to the queue.
        /// </summary>
        /// <remarks>
        /// If there are pending consumers waiting in the consumer queue, then satisfy one of those.
        /// Otherwise, store it in the buffer.
        /// </remarks>
        public void Enqueue(T item)
        {
            TaskCompletionSource<T> promise;
            do
            {
                if (_consumerQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
                    return;
                }
            }
            while (promise != null);

            lock (_syncRoot)
            {
                if (_consumerQueue.TryDequeue(out promise) &&
                    !promise.Task.IsCanceled &&
                    promise.TrySetResult(item))
                {
                    return;
                }

                _bufferQueue.Enqueue(item);
            }
        }

        /// <summary>
        /// Take an item from the queue.
        /// </summary>
        /// <remarks>
        /// If there are pending items in the buffer queue, then return one.
        /// Otherwise, place a request in the consumer queue.
        /// </remarks>
        public Task<T> DequeueAsync(CancellationToken cancellationToken)
        {
            T item;

            if (!_bufferQueue.TryDequeue(out item))
            {
                lock (_syncRoot)
                {
                    if (!_bufferQueue.TryDequeue(out item))
                    {
                        var promise = new TaskCompletionSource<T>();
                        cancellationToken.Register(() => promise.TrySetCanceled());

                        _consumerQueue.Enqueue(promise);

                        return promise.Task;
                    }
                }
            }

            return Task.FromResult(item);
        }
    }
}