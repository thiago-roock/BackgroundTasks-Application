using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Worker.BackgroundTasks.Tasks
{
    public class WorkerTask : BackgroundService
    {
        private readonly ILogger _logger;
        public WorkerTask(ILogger<WorkerTask> logger)
        {
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
            }
        }
    }   
}