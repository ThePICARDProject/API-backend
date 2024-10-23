
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
                string experimentId = await experimentService.SubmitExperimentAsync(request, userId);

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
        public async Task<IActionResult> GetExperimentStatus(string experimentId)
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
    }

    /// <summary>
    /// Request model for submitting an experiment.
    /// </summary>
    public class ExperimentSubmissionRequest
    {
        public ExperimentSubmissionRequest(int algorithmId, string parameters, string driverMemory, string executorMemory, int? cores, int? nodes, string memoryOverhead, List<ParameterValueDto> parameterValues)
        {
            AlgorithmId = algorithmId;
            Parameters = parameters;
            DriverMemory = driverMemory;
            ExecutorMemory = executorMemory;
            Cores = cores;
            Nodes = nodes;
            MemoryOverhead = memoryOverhead;
            ParameterValues = parameterValues;
        }

        public int AlgorithmId { get; set; }
        public string Parameters { get; set; } // JSON serialized parameters

        // Docker Swarm Parameters
        public string DriverMemory { get; set; }
        public string ExecutorMemory { get; set; }
        public int? Cores { get; set; }
        public int? Nodes { get; set; }
        public string MemoryOverhead { get; set; }

        // Algorithm Parameter Values
        public List<ParameterValueDto> ParameterValues { get; set; }
    }

    /// <summary>
    /// DTO for parameter values.
    /// </summary>
    public abstract class ParameterValueDto(string value)
    {
        public int ParameterId { get; set; }
        public string Value { get; set; } = value;
    }
}