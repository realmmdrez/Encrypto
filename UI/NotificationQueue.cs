using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Encrypto.UI
{
    public static class NotificationQueue
    {
        private static readonly Queue<Func<Task>> _queue = new();
        private static bool _isRunning;

        public static void Enqueue(Func<Task> notification)
        {
            _queue.Enqueue(notification);

            if (!_isRunning)
                _ = RunAsync();
        }

        private static async Task RunAsync()
        {
            _isRunning = true;

            while (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                await next();
            }

            _isRunning = false;
        }
    }
}