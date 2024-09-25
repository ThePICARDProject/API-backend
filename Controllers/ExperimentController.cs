using Microsoft.AspNetCore.Mvc;
using API_Backend.Services;
using API_Backend.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Collections.Generic;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/experiment")]
    public class ExperimentController : ControllerBase
    {
        private readonly ExperimentService _experimentService;

        public ExperimentController(ExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        /// <summary>
        /// Submits a new experiment.
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitExperiment([FromBody] ExperimentSubmissionRequest request)
        {
            // Get user ID from authenticated user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            string experimentId = await _experimentService.SubmitExperimentAsync(request, userId);

            return Ok(new { message = "Experiment submitted successfully.", experimentId });
        }

        /// <summary>
        /// Gets the status of an experiment.
        /// </summary>
        [HttpGet("status/{experimentId}")]
        public async Task<IActionResult> GetExperimentStatus(string experimentId)
        {
            var status = await _experimentService.GetExperimentStatusAsync(experimentId);
            if (status == null)
            {
                return NotFound(new { message = "Experiment not found." });
            }
            return Ok(new { experimentId, status });
        }
    }

    /// <summary>
    /// Request model for submitting an experiment.
    /// </summary>
    public class ExperimentSubmissionRequest
    {
        public int AlgorithmID { get; set; }
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
    public class ParameterValueDto
    {
        public int ParameterID { get; set; }
        public string Value { get; set; }
    }
}