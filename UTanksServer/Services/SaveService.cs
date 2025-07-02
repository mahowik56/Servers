using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

using Serilog;

namespace UTanksServer.Services
{
    public interface ISaveService
    {
        void Enqueue(object diff);
        Task StartAsync(CancellationToken token);
    }

    public class SaveService : ISaveService
    {
        private static readonly ILogger Logger = Log.Logger.ForContext<SaveService>();
        private readonly ConcurrentQueue<object> _queue = new();

        public void Enqueue(object diff)
        {
            _queue.Enqueue(diff);
        }

        public async Task StartAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), token);

                while (_queue.TryDequeue(out var diff))
                {
                    // TODO: write diff to DB
                    Logger.Debug("Saved diff {Diff}", diff);
                }
            }
        }
    }
}
