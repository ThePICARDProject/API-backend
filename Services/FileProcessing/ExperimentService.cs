using System.Diagnostics;
using API_Backend.Data;
using API_Backend.Models;
using Microsoft.EntityFrameworkCore;
using API_Backend.Controllers;
using API_Backend.Models;
using API_Backend.Services.Docker_Swarm;
using API_backend.Models;
using K4os.Compression.LZ4.Engine;

namespace API_Backend.Services.FileProcessing
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
        public ExperimentService(ApplicationDbContext dbContext, DockerSwarm dockerSwarm, ILogger<ExperimentService> logger, IExperimentQueue experimentQueue, IWebHostEnvironment environment)
        {
            _dbContext = dbContext;
            _logger = logger;
            _experimentQueue = experimentQueue;

            _dockerSwarm = dockerSwarm;
        }

        #region Public Methods

        /// <summary>
        /// Submits a new experiment request.
        /// </summary>
        /// <param name="request">The experiment submission request.</param>
        /// <param name="userId">The ID of the user submitting the experiment.</param>
        /// <returns>The ID of the newly created experiment.</returns>
        public async Task<Guid> SubmitExperimentAsync(ExperimentSubmissionRequest request, string userId)
        {
            Guid experimentId = Guid.NewGuid();

            _logger.LogInformation("User {UserID} is submitting a new experiment with ExperimentID {ExperimentID}", userId, experimentId);

            try
            {
                // Create a new ExperimentRequest entity
                var experimentRequest = new ExperimentRequest
                {
                    ExperimentName = request.ExperimentName,
                    ExperimentID = experimentId,
                    UserID = userId,
                    AlgorithmID = request.AlgorithmId,
                    Algorithm = _dbContext.Algorithms.FirstOrDefault(x => x.AlgorithmID == request.AlgorithmId),
                    CreatedAt = DateTime.UtcNow,
                    Status = ExperimentStatus.WaitingInQueue,
                    DatasetName = request.DatasetName
                };
                _dbContext.ExperimentRequests.Add(experimentRequest);

                // Create DockerSwarmParameters entity
                var dockerParams = new ClusterParameters
                {
                    ExperimentID = experimentId,
                    NodeCount = request.NodeCount,
                    DriverMemory = request.DriverMemory,
                    DriverCores = request.DriverCores,
                    ExecutorNumber = request.ExecutorNumber,
                    ExecutorCores = request.ExecutorCores,
                    ExecutorMemory = request.ExecutorMemory,
                    MemoryOverhead = request.MemoryOverhead
                };
                _dbContext.ClusterParameters.Add(dockerParams);

                // Create ExperimentAlgorithmParameterValue entities
                var parameterValues = request.ParameterValues.Select(pv => new ExperimentAlgorithmParameterValue
                {
                    ExperimentID = experimentId,
                    ParameterID = pv.ParameterId,
                    AlgorithmParameter = _dbContext.AlgorithmParameters.FirstOrDefault(x => x.ParameterID == pv.ParameterId),
                    Value = pv.Value
                }).ToList();

                // Add entities to the context
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
        private async Task UpdateExperimentStatusAsync(Guid experimentId, ExperimentStatus status, string? errorMessage = null)
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

                    if (!string.IsNullOrEmpty(errorMessage) && errorMessage.Length <= 255)
                        experiment.ErrorMessage = errorMessage;
                    else if (!string.IsNullOrEmpty(errorMessage) && errorMessage.Length > 255)
                        experiment.ErrorMessage = "Error is too long to store in the database.";

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

                    StoredDataSet dataset = await _dbContext.StoredDataSets.FirstOrDefaultAsync(x => x.User.UserID == experiment.UserID && x.Name == experiment.DatasetName);
                    ExperimentResponse response = await _dockerSwarm.SubmitExperiment(experiment, dataset);

                    _logger.LogInformation("Experiment process exited with code {ExitCode} for ExperimentID {ExperimentID}", response.ErrorCode, experiment.ExperimentID);
                    
                     if (response.ErrorCode != 0)
                    {
                        _logger.LogError("ExperimentID {ExperimentID} failed with error: {Error}", experiment.ExperimentID, response.ErrorMessage);
                        throw new Exception($"Experiment failed: {response.ErrorMessage}");
                    }
                    else
                    {
                        await this.ProcessExperimentResultsAsync(experiment, response);
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
        public async Task<ExperimentStatus?> GetExperimentStatusAsync(Guid experimentId)
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
        public async Task<ExperimentRequest?> GetExperimentByIdAsync(Guid experimentId)
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

        #endregion

         #region Private Methods

        /// <summary>
        /// Processes the experiment results after execution.
        /// </summary>
        /// <param name="experiment">The experiment request whose results are to be processed.</param>
        private async Task ProcessExperimentResultsAsync(ExperimentRequest experiment, ExperimentResponse resultsPath)
        {
            _logger.LogInformation("Processing results for ExperimentID {ExperimentID}", experiment.ExperimentID);

            // Update status to BeingProcessed
            await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.BeingProcessed);

            try
            {
                    // Save experiment result (placeholder implementation)
                    var experimentResult = new ExperimentResult
                    {
                        ExperimentID = experiment.ExperimentID,
                        ResultFilePath = resultsPath.OutputPath, // Replace with actual path
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