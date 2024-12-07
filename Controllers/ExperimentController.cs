using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_backend.Services.FileProcessing;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/experiment")]
    public class ExperimentController(ExperimentService experimentService, ILogger<ExperimentController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Submits a new experiment.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitExperiment([FromBody] ExperimentSubmissionRequest request)
        {
            // Get user ID from authenticated user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            logger.LogInformation("User {UserID} is submitting an experiment with AlgorithmID {AlgorithmID}", userId, request.AlgorithmId);

            try
            {
                Debug.Assert(userId != null, nameof(userId) + " != null");
                Guid experimentId = await experimentService.SubmitExperimentAsync(request, userId);

                logger.LogInformation("Experiment {ExperimentID} submitted successfully by user {UserID}", experimentId, userId);

                return Ok(new { message = "Experiment submitted successfully.", experimentId });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while submitting experiment for user {UserID}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while submitting the experiment." });
            }
        }



        /// <summary>
        /// Gets the status of an experiment.
        /// </summary>
        [HttpGet("status/{experimentId}")]
        public async Task<IActionResult> GetExperimentStatus(Guid experimentId)
        {
            logger.LogInformation("Fetching status for ExperimentID {ExperimentID}", experimentId);

            var status = await experimentService.GetExperimentStatusAsync(experimentId);
            if (status == null)
            {
                logger.LogWarning("Experiment {ExperimentID} not found.", experimentId);
                return NotFound(new { message = "Experiment not found." });
            }

            logger.LogInformation("Experiment {ExperimentID} has status {Status}", experimentId, status);

            return Ok(new { experimentId, status });
        }

        /// <summary>
        /// Gets all experiments related to the currently authenticated user.
        /// </summary>
        [HttpGet("user/getUserExperiments")]
        public async Task<IActionResult> GetExperimentsByUser()
        {

            // Get user ID from authenticated user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            logger.LogInformation("Fetching experiments for UserID {UserID}", userId);

            try
            {
                var experiments = await experimentService.GetExperimentsByUserAsync(userId);

                if (experiments == null || !experiments.Any())
                {
                    logger.LogWarning("No experiments found for user {UserID}", userId);
                    return NotFound(new { message = "No experiments found for this user." });
                }

                logger.LogInformation("Found {Count} experiments for user {UserID}", experiments.Count, userId);

                return Ok(experiments);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred while fetching experiments for user {UserID}", userId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while fetching experiments." });
            }
        }

        [HttpGet("getlogs")]
        public async Task<IActionResult> GetExperimentLog(Guid experimentId)
        {
            throw new NotImplementedException();
        }


    }

    /// <summary>
    /// Request model for submitting an experiment.
    /// </summary>
    public class ExperimentSubmissionRequest
    {
        public ExperimentSubmissionRequest(int algorithmId, string datasetName, int nodeCount, string driverMemory, int driverCores, int executorNumber, int executorCores, string executorMemory, int memoryOverhead /*, List<ParameterValueDto> parameterValues*/)
        {
            AlgorithmId = algorithmId;
            DatasetName = datasetName;
            NodeCount = nodeCount;
            DriverMemory = driverMemory;
            DriverCores = driverCores;
            ExecutorNumber = executorNumber;
            ExecutorCores = executorCores;
            ExecutorMemory = executorMemory;
            MemoryOverhead = memoryOverhead;
        }

        public int AlgorithmId { get; set; }

        // Docker Swarm Parameters
        public string DatasetName { get; set; }
        public int NodeCount { get; set; }
        public string DriverMemory { get; set; }
        public int DriverCores { get; set; }
        public int ExecutorNumber { get; set; }
        public int ExecutorCores { get; set; }
        public string ExecutorMemory { get; set; }
        public int MemoryOverhead { get; set; }

        // Algorithm Parameter Values
        public ICollection<ParameterValueDto> ParameterValues { get; set; }
    }

    /// <summary>
    /// DTO for parameter values.
    /// </summary>
    public class ParameterValueDto(string value)
    {
        public int ParameterId { get; set; }
        public string Value { get; set; } = value;
    }
}