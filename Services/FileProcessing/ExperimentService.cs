using API_Backend.Models;
using API_Backend.Data;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Diagnostics;
using System.IO;
using API_Backend.Controllers;

namespace API_Backend.Services
{
    public class ExperimentService
    {
        private readonly ApplicationDbContext _dbContext;

        public ExperimentService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// Submits a new experiment request.
        /// </summary>
        public async Task<string> SubmitExperimentAsync(ExperimentSubmissionRequest request, string userId)
        {
            string experimentId = Guid.NewGuid().ToString();

            var experimentRequest = new ExperimentRequest
            {
                ExperimentID = experimentId,
                UserID = userId,
                AlgorithmID = request.AlgorithmID,
                CreatedAt = DateTime.UtcNow,
                Status = ExperimentStatus.WaitingInQueue,
                Parameters = request.Parameters
            };

            // Add Docker Swarm Parameters
            var dockerParams = new DockerSwarmParameters
            {
                ExperimentID = experimentId,
                DriverMemory = request.DriverMemory,
                ExecutorMemory = request.ExecutorMemory,
                Cores = request.Cores,
                Nodes = request.Nodes,
                MemoryOverhead = request.MemoryOverhead
            };

            // Add parameter values
            var parameterValues = request.ParameterValues?.Select(pv => new ExperimentAlgorithmParameterValue
            {
                ExperimentID = experimentId,
                ParameterID = pv.ParameterID,
                Value = pv.Value
            }).ToList();

            _dbContext.ExperimentRequests.Add(experimentRequest);
            _dbContext.DockerSwarmParameters.Add(dockerParams);
            if (parameterValues != null && parameterValues.Any())
            {
                _dbContext.ExperimentAlgorithmParameterValues.AddRange(parameterValues);
            }

            await _dbContext.SaveChangesAsync();

            return experimentId;
        }

        /// <summary>
        /// Retrieves the next queued experiment.
        /// </summary>
        public async Task<ExperimentRequest?> GetNextQueuedExperimentAsync()
        {
            return await _dbContext.ExperimentRequests
                .FirstOrDefaultAsync(e => e.Status == ExperimentStatus.WaitingInQueue);
        }

        /// <summary>
        /// Updates the status of an experiment.
        /// </summary>
        public async Task UpdateExperimentStatusAsync(string experimentId, ExperimentStatus status, string errorMessage = null)
        {
            var experiment = await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
            if (experiment != null)
            {
                experiment.Status = status;
                if (status == ExperimentStatus.BeingExecuted)
                    experiment.StartTime = DateTime.UtcNow;
                if (status == ExperimentStatus.Finished || status == ExperimentStatus.Failed)
                    experiment.EndTime = DateTime.UtcNow;
                if (!string.IsNullOrEmpty(errorMessage))
                    experiment.ErrorMessage = errorMessage;

                await _dbContext.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Runs the experiment.
        /// </summary>
        public async Task RunExperimentAsync(ExperimentRequest experiment)
        {
            try
            {
                // Update status to BeingExecuted
                await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.BeingExecuted);

                // Create unique working directory
                string experimentsRootDir = "/path/to/experiments"; // Replace with actual path
                string experimentDir = Path.Combine(experimentsRootDir, experiment.ExperimentID);
                Directory.CreateDirectory(experimentDir);

                // Copy submit.sh to experiment directory if needed
                // File.Copy("/path/to/submit.sh", Path.Combine(experimentDir, "submit.sh"));

                // Prepare to start the experiment process
                var startInfo = new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = "submit.sh", // Or provide full path
                    WorkingDirectory = experimentDir,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };

                using (Process process = new Process { StartInfo = startInfo })
                {
                    process.Start();

                    // Optionally read output/error
                    string output = await process.StandardOutput.ReadToEndAsync();
                    string error = await process.StandardError.ReadToEndAsync();

                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        throw new Exception($"Experiment failed: {error}");
                    }
                }

                // Process results
                await ProcessExperimentResultsAsync(experiment);
            }
            catch (Exception ex)
            {
                await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.Failed, ex.Message);
            }
        }

        /// <summary>
        /// Processes the experiment results.
        /// </summary>
        private async Task ProcessExperimentResultsAsync(ExperimentRequest experiment)
        {
            // Update status to BeingProcessed
            await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.BeingProcessed);

            // Implement result processing logic here
            // For example, retrieve outputs from HDFS

            // Simulate result processing delay
            await Task.Delay(2000);

            // Save experiment result (placeholder)
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

            // Update status to Finished
            await UpdateExperimentStatusAsync(experiment.ExperimentID, ExperimentStatus.Finished);
        }

        /// <summary>
        /// Gets the status of an experiment.
        /// </summary>
        public async Task<ExperimentStatus?> GetExperimentStatusAsync(string experimentId)
        {
            var experiment = await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
            return experiment?.Status;
        }

        /// <summary>
        /// Retrieves an experiment by ID.
        /// </summary>
        public async Task<ExperimentRequest> GetExperimentByIdAsync(string experimentId)
        {
            return await _dbContext.ExperimentRequests.FirstOrDefaultAsync(e => e.ExperimentID == experimentId);
        }

        /// <summary>
        /// Retrieves experiment result by experiment ID.
        /// </summary>
        public async Task<ExperimentResult> GetExperimentResultAsync(string experimentId)
        {
            return await _dbContext.ExperimentResults.FirstOrDefaultAsync(r => r.ExperimentID == experimentId);
        }
    }
}