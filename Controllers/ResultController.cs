using API_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using API_backend.Services.FileProcessing;

namespace API_Backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/result")]
    public class ResultController(ExperimentService experimentService, ILogger<ResultController> logger)
        : ControllerBase
    {
        /// <summary>
        /// Gets the processed results of an experiment.
        /// </summary>
        [HttpGet("data/{experimentId}")]
        public async Task<IActionResult> GetProcessedResults(string experimentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            logger.LogInformation("User {UserID} is requesting results for ExperimentID {ExperimentID}", userId, experimentId);

            var experiment = await experimentService.GetExperimentByIdAsync(experimentId);

            if (experiment is not { Status: ExperimentStatus.Finished })
            {
                logger.LogWarning("Results not available for ExperimentID {ExperimentID}", experimentId);
                return NotFound(new { message = "Results not available or experiment not completed." });
            }

            // Ensure the requesting user is the owner of the experiment
            if (experiment.UserID != userId)
            {
                logger.LogWarning("User {UserID} is not authorized to access results for ExperimentID {ExperimentID}", userId, experimentId);
                return Forbid();
            }

            // Retrieve result data
            var result = await experimentService.GetExperimentResultAsync(experimentId);

            if (result == null)
            {
                logger.LogWarning("Experiment results not found for ExperimentID {ExperimentID}", experimentId);
                return NotFound(new { message = "Experiment results not found." });
            }

            logger.LogInformation("Returning results for ExperimentID {ExperimentID}", experimentId);

            // Return the result data
            return Ok(new { experimentId, result });
        }
    }
}