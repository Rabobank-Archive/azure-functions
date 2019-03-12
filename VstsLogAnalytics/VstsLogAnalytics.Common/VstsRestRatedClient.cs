using SecurePipelineScan.VstsService;
using System;
using System.Collections.Concurrent;
using System.Threading;

namespace VstsLogAnalytics.Common
{
    public class VstsRestRatedClient : IVstsRestClient
    {
        private readonly IVstsRestClient vstsRestClient;
        private TimeSpan Window { get; set; } = TimeSpan.FromSeconds(60);
        private readonly int threshold = 500;

        private ConcurrentQueue<DateTime> queue = new ConcurrentQueue<DateTime>();

        public int QueueCount => queue.Count;

        public VstsRestRatedClient(IVstsRestClient vstsRestClient)
        {
            this.vstsRestClient = vstsRestClient;
        }

        public VstsRestRatedClient(IVstsRestClient vstsRestClient, TimeSpan window, int threshold)
        {
            this.vstsRestClient = vstsRestClient;
            this.threshold = threshold;
            Window = window;
        }

        public TResponse Get<TResponse>(IVstsRestRequest<TResponse> request) where TResponse : new()
        {
            CheckForQueueStatus();

            var item = DateTime.UtcNow;
            queue.Enqueue(item);

            return vstsRestClient.Get(request);
        }

        private void CheckForQueueStatus()
        {
            while (queue.Count > threshold)
            {
                DequeueNotInWindow();
                Thread.Sleep(TimeSpan.FromSeconds(1));
            }
        }

        /// <summary>
        /// Dequeues all items not in the sliding window
        /// </summary>
        private void DequeueNotInWindow()
        {
            while (queue.Count > 0 && queue.TryPeek(out var result) &&
                result < DateTime.UtcNow.Subtract(Window))
            {
                queue.TryDequeue(out var dequeuedItem);
            }
        }

        public TResponse Post<TResponse>(IVstsPostRequest<TResponse> request) where TResponse : new()
        {
            CheckForQueueStatus();

            var item = DateTime.UtcNow;
            queue.Enqueue(item);

            return vstsRestClient.Post(request);
        }

        public TResponse Put<TResponse>(IVstsRestRequest<TResponse> request, TResponse body) where TResponse : new()
        {
            return vstsRestClient.Put(request, body);
        }

        public void Delete(IVstsRestRequest request)
        {
            CheckForQueueStatus();

            var item = DateTime.UtcNow;
            queue.Enqueue(item);

            vstsRestClient.Delete(request);
        }
    }
}