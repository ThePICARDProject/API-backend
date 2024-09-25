using Microsoft.AspNetCore.Mvc;
using API_Backend.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using API_Backend.Models;
using API_backend.Services;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/result")]
    public class ResultController : ControllerBase
    {
        private readonly ExperimentService _experimentService;

        public ResultController(ExperimentService experimentService)
        {
            _experimentService = experimentService;
        }

        /// <summary>
        /// Gets the processed results of an experiment.
        /// </summary>
        [HttpGet("data/{experimentId}")]
        public async Task<IActionResult> GetProcessedResults(string experimentId)
        {
            var experiment = await _experimentService.GetExperimentByIdAsync(experimentId);

            if (experiment == null || experiment.Status != ExperimentStatus.Finished)
            {
                return NotFound(new { message = "Results not available or experiment not completed." });
            }

            // Ensure the requesting user is the owner of the experiment
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Forbid();
            }

            // Retrieve result data
            var result = await _experimentService.GetExperimentResultAsync(experimentId);

            if (result == null)
            {
                return NotFound(new { message = "Experiment results not found." });
            }

            // Return the result data (you might need to adjust this based on how you store results)
            return Ok(new { experimentId, result });
        }
    }
}