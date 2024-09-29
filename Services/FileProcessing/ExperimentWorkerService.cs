
using API_backend.Models;

namespace API_backend.Services.FileProcessing
{
    public class ExperimentWorkerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IExperimentQueue _experimentQueue;
        private readonly ILogger<ExperimentWorkerService> _logger;

        public ExperimentWorkerService(
            IServiceProvider serviceProvider,
            IExperimentQueue experimentQueue,
            ILogger<ExperimentWorkerService> logger)
        {
            _serviceProvider = serviceProvider;
            _experimentQueue = experimentQueue;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ExperimentWorkerService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var experimentRequest = await _experimentQueue.DequeueAsync(stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var experimentService = scope.ServiceProvider.GetRequiredService<ExperimentService>();

                    // Enrich logs with UserID
                    using (_logger.BeginScope(new Dictionary<string, object> { ["UserID"] = experimentRequest.UserID }))
                    {
                        _logger.LogInformation("Processing experiment {ExperimentID}", experimentRequest.ExperimentID);
                        await experimentService.RunExperimentAsync(experimentRequest);
                        _logger.LogInformation("Experiment {ExperimentID} processed successfully", experimentRequest.ExperimentID);
                    }
                }
                catch (OperationCanceledException)
                {
                    // Graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing experiments.");
                }
            }

            _logger.LogInformation("ExperimentWorkerService stopping.");
        }
    }
}