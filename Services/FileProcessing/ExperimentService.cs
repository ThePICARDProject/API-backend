using System.Diagnostics;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.EntityFrameworkCore;
using API_Backend.Controllers;
using API_backend.Models;
using API_backend.Services.Docker_Swarm;

namespace API_backend.Services.FileProcessing
{
    /// <summary>
    /// Service for handling experiment-related operations.
    /// </summary>
    public class ExperimentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly DockerSwarm _dockerSwarm;
        private readonly ILogger<ExperimentService> _logger;
        private readonly IExperimentQueue _experimentQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExperimentService"/> class.
        /// </summary>
        /// <param name="dbContext">The application database context.</param>
        /// <param name="logger">The logger instance.</param>
        /// <param name="experimentQueue">The experiment task queue.</param>
        public ExperimentService(ApplicationDbContext dbContext, ILogger<ExperimentService> logger, IExperimentQueue experimentQueue)
        {
            _dbContext = dbContext;
            _dockerSwarm = new DockerSwarm();
            _logger = logger;
            _experimentQueue = experimentQueue;
        }

        #region Public Methods

        /// <summary>
        /// Submits a new experiment request.
        /// </summary>
        /// <param name="request">The experiment submission request.</param>
        /// <param name="userId">The ID of the user submitting the experiment.</param>
        /// <returns>The ID of the newly created experiment.</returns>
        public async Task<string> SubmitExperimentAsync(ExperimentSubmissionRequest request, string userId)
        {
            string experimentId = Guid.NewGuid().ToString();

            _logger.LogInformation("User {UserID} is submitting a new experiment with ExperimentID {ExperimentID}", userId, experimentId);

            try
            {
                // Create a new ExperimentRequest entity
                var experimentRequest = new ExperimentRequest
                {
                    ExperimentID = experimentId,
                    UserID = userId,
                    AlgorithmID = request.AlgorithmId,
                    CreatedAt = DateTime.UtcNow,
                    Status = ExperimentStatus.WaitingInQueue,
                    Parameters = request.Parameters
                };

                // Create DockerSwarmParameters entity
                var dockerParams = new ClusterParameters
                {
                    ExperimentID = experimentId,
                    NodeCount = request.NodeCount,
                    DriverMemory = request.DriverMemory,
                    DriverCores = request.DriverCores,
                    ExecutorNumber = request.ExecutorNumber,
                    ExecutorMemory = request.ExecutorMemory,
                    MemoryOverhead = request.MemoryOverhead
                };

                // Create ExperimentAlgorithmParameterValue entities
                var parameterValues = request.ParameterValues.Select(pv => new ExperimentAlgorithmParameterValue
                {
                    ExperimentID = experimentId,
                    ParameterID = pv.ParameterId,
                    Value = pv.Value
                }).ToList();

                // Add entities to the context
                _dbContext.ExperimentRequests.Add(experimentRequest);
                _dbContext.DockerSwarmParameters.Add(dockerParams);
                if (parameterValues.Any())
                {
                    _dbContext.ExperimentAlgorithmParameterValues.AddRange(parameterValues);
                }

                // Save changes to the database
                await _dbContext.SaveChangesAsync();

                // Enqueue the experiment after saving
                _experimentQueue.QueueExperiment(experimentRequest);

                _logger.LogInformation("Experiment {ExperimentID} enqueued for processing and submitted successfully by user {UserID}", experimentId, userId);

                return experimentId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while submitting experiment for user {UserID}", userId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves the next experiment in the queue.
        /// </summary>
        /// <returns>The next queued experiment request, or null if none are queued.</returns>
        public async Task<ExperimentRequest?> GetNextQueuedExperimentAsync()
        {
            _logger.LogInformation("Retrieving the next queued experiment.");

            try
            {
                var experiment = await _dbContext.ExperimentRequests
                    .Where(e => e.Status == ExperimentStatus.WaitingInQueue)
                    .OrderBy(e => e.CreatedAt)
                    .FirstOrDefaultAsync();

                if (experiment != null)
                {
                    _logger.LogInformation("Next queued experiment found: ExperimentID {ExperimentID}", experiment.ExperimentID);
                }
                else
                {
                    _logger.LogInformation("No experiments are waiting in the queue.");
                }

                return experiment;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving the next queued experiment.");
                throw;
            }
        }

        /// <summary>
        /// Updates the status of an experiment.
        /// </summary>
        /// <param name="experimentId">The ID of the experiment to update.</param>
        /// <param name="status">The new status of the experiment.</param>
        /// <param name="errorMessage">Optional error message if the experiment failed.</param>
        private async Task UpdateExperimentStatusAsync(string experimentId, ExperimentStatus status, string? errorMessage = null)
        {
            _logger.LogInformation("Updating status of ExperimentID {ExperimentID} to {Status}", experimentId, status);

            try
            {
                var experiment = await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
                if (experiment != null)
                {
                    experiment.Status = status;

                    if (status == ExperimentStatus.BeingExecuted)
                    {
                        experiment.StartTime = DateTime.UtcNow;
                    }
                    else if (status == ExperimentStatus.Finished || status == ExperimentStatus.Failed)
                    {
                        experiment.EndTime = DateTime.UtcNow;
                    }

                    if (!string.IsNullOrEmpty(errorMessage))
                    {
                        experiment.ErrorMessage = errorMessage;
                    }

                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation("ExperimentID {ExperimentID} status updated to {Status}", experimentId, status);
                }
                else
                {
                    _logger.LogWarning("ExperimentID {ExperimentID} not found while attempting to update status.", experimentId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating status for ExperimentID {ExperimentID}", experimentId);
                throw;
            }
        }

        /// <summary>
        /// Runs the experiment by executing the necessary processes.
        /// </summary>
        /// <param name="experiment">The experiment request to run.</param>
        public async Task RunExperimentAsync(ExperimentRequest experiment)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["UserID"] = experiment.UserID }))
            {
                _logger.LogInformation("Starting execution of ExperimentID {ExperimentID}", experiment.ExperimentID);
                try
                {
                    // Update status to BeingExecuted
                    await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.BeingExecuted);

                    _logger.LogInformation("Starting experiment process for ExperimentID {ExperimentID}", experiment.ExperimentID);

                    // Read output and error streams asynchronously
                    ExperimentResponse error = await _dockerSwarm.SubmitExperiment(experiment);

                    _logger.LogInformation("Experiment process exited with code {ExitCode} for ExperimentID {ExperimentID}", error.ErrorCode, experiment.ExperimentID);

                    if (error.ErrorCode != 0)
                    {
                        _logger.LogError("ExperimentID {ExperimentID} failed with error: {Error}", experiment.ExperimentID, error.ErrorMessage);
                        throw new Exception($"Experiment failed: {error.ErrorMessage}");
                    }
                    else
                    {
                        _logger.LogInformation("ExperimentID {ExperimentID} completed successfully.", experiment.ExperimentID);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while running ExperimentID {ExperimentID}", experiment.ExperimentID);
                    await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.Failed, ex.Message);
                }
            }
        }

        /// <summary>
        /// Gets the status of an experiment.
        /// </summary>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <returns>The status of the experiment, or null if not found.</returns>
        public async Task<ExperimentStatus?> GetExperimentStatusAsync(string experimentId)
        {
            _logger.LogInformation("Retrieving status for ExperimentID {ExperimentID}", experimentId);

            try
            {
                var experiment = await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
                if (experiment != null)
                {
                    _logger.LogInformation("Status for ExperimentID {ExperimentID} is {Status}", experimentId, experiment.Status);
                    return experiment.Status;
                }
                else
                {
                    _logger.LogWarning("ExperimentID {ExperimentID} not found while retrieving status.", experimentId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving status for ExperimentID {ExperimentID}", experimentId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves an experiment by its ID.
        /// </summary>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <returns>The experiment request, or null if not found.</returns>
        public async Task<ExperimentRequest?> GetExperimentByIdAsync(string experimentId)
        {
            _logger.LogInformation("Retrieving ExperimentID {ExperimentID}", experimentId);

            try
            {
                var experiment = await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
                if (experiment != null)
                {
                    _logger.LogInformation("ExperimentID {ExperimentID} retrieved successfully", experimentId);
                    return experiment;
                }
                else
                {
                    _logger.LogWarning("ExperimentID {ExperimentID} not found.", experimentId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving ExperimentID {ExperimentID}", experimentId);
                throw;
            }
        }

        /// <summary>
        /// Retrieves experiment result by experiment ID.
        /// </summary>
        /// <param name="experimentId">The ID of the experiment.</param>
        /// <returns>The experiment result, or null if not found.</returns>
        public async Task<ExperimentResult?> GetExperimentResultAsync(string experimentId)
        {
            _logger.LogInformation("Retrieving results for ExperimentID {ExperimentID}", experimentId);

            try
            {
                var result = await _dbContext.ExperimentResults.FirstOrDefaultAsync(r => r.ExperimentID == experimentId);
                if (result != null)
                {
                    _logger.LogInformation("Results retrieved for ExperimentID {ExperimentID}", experimentId);
                    return result;
                }
                else
                {
                    _logger.LogWarning("Results not found for ExperimentID {ExperimentID}", experimentId);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while retrieving results for ExperimentID {ExperimentID}", experimentId);
                throw;
            }
        }

        #endregion

         #region Private Methods

        /// <summary>
        /// Processes the experiment results after execution.
        /// </summary>
        /// <param name="experiment">The experiment request whose results are to be processed.</param>
        private async Task ProcessExperimentResultsAsync(ExperimentRequest experiment)
        {
            // THIS NEEDS TO BE A SEPERATE REQUEST, WHERE WE WILL PROCESS BASED ON 
            // SELECTED PARAMETERS/CONDITIONS

            _logger.LogInformation("Processing results for ExperimentID {ExperimentID}", experiment.ExperimentID);

            // Update status to BeingProcessed
            await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.BeingProcessed);

            try
            {
                // Implement your result processing logic here
                // For example, retrieve outputs from HDFS or process data files

                // Simulate result processing delay
                await Task.Delay(2000);

                // Save experiment result (placeholder implementation)
                var experimentResult = new ExperimentResult
                {
                    ExperimentID = experiment.ExperimentID,
                    CSVFilePath = "/path/to/csv", // Replace with actual path
                    CSVFileName = "result.csv",
                    MetaDataFilePath = "/path/to/metadata", // Replace with actual path
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.ExperimentResults.Add(experimentResult);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation("Experiment results saved for ExperimentID {ExperimentID}", experiment.ExperimentID);

                // Update status to Finished
                await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.Finished);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing results for ExperimentID {ExperimentID}", experiment.ExperimentID);
                await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.Failed, ex.Message);
            }
        }

        #endregion
    }
}