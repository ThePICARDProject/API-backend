using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using API_Backend.Models;

namespace API_Backend.Services.FileProcessing
{
    /// <summary>
    /// Background service that processes experiments from the queue.
    /// </summary>
    public class ExperimentWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1); // Limit to 1 concurrent experiment

        public ExperimentWorkerService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await _semaphore.WaitAsync(stoppingToken);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var experimentService = scope.ServiceProvider.GetRequiredService<ExperimentService>();
                        var nextExperiment = await experimentService.GetNextQueuedExperimentAsync();

                        if (nextExperiment != null)
                        {
                            await experimentService.RunExperimentAsync(nextExperiment);
                        }
                    }
                }
                finally
                {
                    _semaphore.Release();
                }

                // Wait before checking the queue again
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }
}